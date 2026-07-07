using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>Lightweight table descriptor for the picker (SDK-derived, but no SDK types leak out).</summary>
    public sealed class ErdTableInfo
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public bool IsCustom { get; set; }
        public bool IsManaged { get; set; }
        public Guid MetadataId { get; set; }
    }

    /// <summary>Publisher descriptor for publisher-scoped selection.</summary>
    public sealed class ErdPublisherInfo
    {
        public string Name { get; set; }
        public string Prefix { get; set; }
    }

    /// <summary>Solution descriptor for solution-scoped selection.</summary>
    public sealed class ErdSolutionInfo
    {
        public Guid Id { get; set; }
        public string UniqueName { get; set; }
        public string FriendlyName { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// Reads Dataverse metadata (Microsoft.Xrm.Sdk.Metadata) and projects it into the SDK-free
    /// <see cref="ErdModel"/>. Kept UI-free so the projection logic is testable; it degrades missing
    /// metadata to notes rather than throwing. Not part of the SDK-free unit-test set.
    /// </summary>
    public sealed class ErdCollector
    {
        /// <summary>All entities' logical names (via <see cref="RetrieveAllEntitiesRequest"/>, Entity filter).</summary>
        public List<string> ListTables(IOrganizationService svc, BackgroundWorker worker)
            => ListTableInfos(svc, worker).Select(t => t.LogicalName).ToList();

        /// <summary>All entities with display name + custom/managed status for the table picker.</summary>
        public List<ErdTableInfo> ListTableInfos(IOrganizationService svc, BackgroundWorker worker)
        {
            worker?.ReportProgress(0, "Retrieving entity list…");
            var resp = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = true
            });

            return resp.EntityMetadata
                .Where(e => e.LogicalName != null)
                .Select(e => new ErdTableInfo
                {
                    LogicalName = e.LogicalName,
                    DisplayName = Label(e.DisplayName) ?? e.LogicalName,
                    IsCustom = e.IsCustomEntity ?? false,
                    IsManaged = e.IsManaged ?? false,
                    MetadataId = e.MetadataId ?? Guid.Empty
                })
                .OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Unmanaged/visible solutions for solution-scoped selection.</summary>
        public List<ErdSolutionInfo> ListSolutions(IOrganizationService svc)
        {
            var qe = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "uniquename", "friendlyname", "version"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                        new ConditionExpression("uniquename", ConditionOperator.NotIn, "Default", "Active", "Basic")
                    }
                },
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
            };
            return svc.RetrieveMultiple(qe).Entities.Select(s => new ErdSolutionInfo
            {
                Id = s.Id,
                UniqueName = s.GetAttributeValue<string>("uniquename"),
                FriendlyName = s.GetAttributeValue<string>("friendlyname"),
                Version = s.GetAttributeValue<string>("version")
            }).ToList();
        }

        /// <summary>Publishers (with customization prefix) for publisher-scoped selection.</summary>
        public List<ErdPublisherInfo> ListPublishers(IOrganizationService svc)
        {
            var qe = new QueryExpression("publisher")
            {
                ColumnSet = new ColumnSet("friendlyname", "customizationprefix"),
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
            };
            return svc.RetrieveMultiple(qe).Entities
                .Select(p => new ErdPublisherInfo
                {
                    Name = p.GetAttributeValue<string>("friendlyname"),
                    Prefix = p.GetAttributeValue<string>("customizationprefix")
                })
                .Where(p => !string.IsNullOrEmpty(p.Prefix))
                .ToList();
        }

        /// <summary>Metadata ids of the entities that belong to a solution (solutioncomponent, componenttype 1).</summary>
        public HashSet<Guid> GetSolutionEntityIds(IOrganizationService svc, Guid solutionId)
        {
            var qe = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 1) // 1 = Entity
                    }
                }
            };
            return new HashSet<Guid>(svc.RetrieveMultiple(qe).Entities
                .Select(c => c.GetAttributeValue<Guid>("objectid")));
        }

        /// <summary>
        /// Builds the ERD for the given tables: one <see cref="RetrieveEntityRequest"/> per table
        /// (Entity | Attributes | Relationships), populating columns, keys and every relationship whose
        /// BOTH ends are in the selected set. Missing metadata degrades to a note. Honours cancellation.
        /// </summary>
        public ErdModel Build(IOrganizationService svc, IEnumerable<string> tableLogicalNames,
            BackgroundWorker worker, Action<string> progress)
        {
            var names = (tableLogicalNames ?? Enumerable.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var selected = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            var model = new ErdModel();
            var metaByName = new Dictionary<string, EntityMetadata>(StringComparer.OrdinalIgnoreCase);
            // required level per (entity, attribute) so relationships can report their lookup's requirement.
            var reqLevel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            int done = 0;
            foreach (var name in names)
            {
                if (worker != null && worker.CancellationPending) break;
                progress?.Invoke($"Reading metadata for {name} ({++done}/{names.Count})…");
                worker?.ReportProgress(names.Count == 0 ? 0 : done * 100 / names.Count, $"Reading {name}…");

                EntityMetadata em;
                try
                {
                    var resp = (RetrieveEntityResponse)svc.Execute(new RetrieveEntityRequest
                    {
                        LogicalName = name,
                        EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                        RetrieveAsIfPublished = true
                    });
                    em = resp.EntityMetadata;
                }
                catch (Exception ex)
                {
                    model.Notes.Add($"Could not read metadata for '{name}': {ex.Message}");
                    continue;
                }

                metaByName[name] = em;
                var table = BuildTable(em, reqLevel);
                model.Tables.Add(table);
            }

            // Relationships (dedup by schema name; both ends must be selected).
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var em in metaByName.Values)
            {
                if (worker != null && worker.CancellationPending) break;
                CollectRelationships(em, selected, reqLevel, seen, model);
            }

            model.Tables = model.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase).ToList();
            model.Relationships = model.Relationships
                .OrderBy(r => r.FromTable, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.ToTable, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return model;
        }

        private static ErdTable BuildTable(EntityMetadata em, Dictionary<string, string> reqLevel)
        {
            var table = new ErdTable
            {
                LogicalName = em.LogicalName,
                DisplayName = Label(em.DisplayName) ?? em.LogicalName,
                SchemaName = em.SchemaName,
                IsCustom = em.IsCustomEntity ?? false,
                IsManaged = em.IsManaged ?? false,
                PrimaryIdColumn = em.PrimaryIdAttribute,
                PrimaryNameColumn = em.PrimaryNameAttribute
            };

            foreach (var attr in (em.Attributes ?? new AttributeMetadata[0])
                .Where(a => a.LogicalName != null && a.AttributeOf == null)
                .OrderBy(a => a.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                var col = new ErdColumn
                {
                    LogicalName = attr.LogicalName,
                    Type = attr.AttributeType?.ToString() ?? "Unknown",
                    RequiredLevel = attr.RequiredLevel?.Value.ToString(),
                    IsPrimaryId = attr.IsPrimaryId ?? false,
                    IsPrimaryName = attr.IsPrimaryName ?? false
                };
                if (attr is LookupAttributeMetadata lookup)
                {
                    col.IsLookup = true;
                    col.Targets = (lookup.Targets ?? new string[0]).ToList();
                }
                table.Columns.Add(col);
                reqLevel[em.LogicalName + "|" + attr.LogicalName] = col.RequiredLevel;
            }

            foreach (var key in em.Keys ?? new EntityKeyMetadata[0])
            {
                table.AlternateKeys.Add(new ErdKey
                {
                    Name = Label(key.DisplayName) ?? key.LogicalName ?? key.SchemaName,
                    Columns = (key.KeyAttributes ?? new string[0]).ToList()
                });
            }

            return table;
        }

        private static void CollectRelationships(EntityMetadata em, HashSet<string> selected,
            Dictionary<string, string> reqLevel, HashSet<string> seen, ErdModel model)
        {
            // One-to-many: this entity is the "one" (referenced) side; the child holds the lookup.
            foreach (var r in em.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
            {
                if (r.SchemaName == null || !seen.Add(r.SchemaName)) continue;
                if (!selected.Contains(r.ReferencedEntity) || !selected.Contains(r.ReferencingEntity)) continue;
                model.Relationships.Add(new ErdRelationship
                {
                    SchemaName = r.SchemaName,
                    RelationType = "OneToMany",
                    FromTable = r.ReferencedEntity,
                    ToTable = r.ReferencingEntity,
                    LookupColumn = r.ReferencingAttribute,
                    CascadeSummary = Cascade(r.CascadeConfiguration),
                    RequiredLevel = Req(reqLevel, r.ReferencingEntity, r.ReferencingAttribute)
                });
            }

            // Many-to-one: this entity is the "many" (referencing) side. Only add if the parent-side
            // one-to-many wasn't already captured (i.e. the parent is outside the selected set won't add
            // it — but selected.Contains handles that; the seen-set dedup avoids the common duplicate).
            foreach (var r in em.ManyToOneRelationships ?? new OneToManyRelationshipMetadata[0])
            {
                if (r.SchemaName == null || seen.Contains(r.SchemaName)) continue;
                if (!selected.Contains(r.ReferencedEntity) || !selected.Contains(r.ReferencingEntity)) continue;
                seen.Add(r.SchemaName);
                model.Relationships.Add(new ErdRelationship
                {
                    SchemaName = r.SchemaName,
                    RelationType = "ManyToOne",
                    FromTable = r.ReferencingEntity,
                    ToTable = r.ReferencedEntity,
                    LookupColumn = r.ReferencingAttribute,
                    CascadeSummary = Cascade(r.CascadeConfiguration),
                    RequiredLevel = Req(reqLevel, r.ReferencingEntity, r.ReferencingAttribute)
                });
            }

            // Many-to-many.
            foreach (var r in em.ManyToManyRelationships ?? new ManyToManyRelationshipMetadata[0])
            {
                if (r.SchemaName == null || !seen.Add(r.SchemaName)) continue;
                if (!selected.Contains(r.Entity1LogicalName) || !selected.Contains(r.Entity2LogicalName)) continue;
                model.Relationships.Add(new ErdRelationship
                {
                    SchemaName = r.SchemaName,
                    RelationType = "ManyToMany",
                    FromTable = r.Entity1LogicalName,
                    ToTable = r.Entity2LogicalName,
                    LookupColumn = r.IntersectEntityName,
                    CascadeSummary = "n/a (intersect table)",
                    RequiredLevel = null
                });
            }
        }

        private static string Req(Dictionary<string, string> reqLevel, string entity, string attribute)
            => reqLevel.TryGetValue(entity + "|" + attribute, out var v) ? v : null;

        private static string Cascade(CascadeConfiguration c)
        {
            if (c == null) return null;
            var bits = new List<string>();
            void Add(string label, CascadeType? t)
            {
                if (t.HasValue && t.Value != CascadeType.NoCascade) bits.Add($"{label}={t.Value}");
            }
            Add("Assign", c.Assign);
            Add("Delete", c.Delete);
            Add("Merge", c.Merge);
            Add("Reparent", c.Reparent);
            Add("Share", c.Share);
            Add("Unshare", c.Unshare);
            return bits.Count == 0 ? "NoCascade" : string.Join(", ", bits);
        }

        private static string Label(Microsoft.Xrm.Sdk.Label label)
            => label?.UserLocalizedLabel?.Label;
    }
}
