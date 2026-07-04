using System;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace XrmToolSuite.AnalyzerTests.Fakes
{
    /// <summary>
    /// Builds <see cref="EntityMetadata"/> / <see cref="AttributeMetadata"/> / relationship fixtures for the
    /// tests that exercise metadata-driven analyzer branches (custom-table detection, wide tables,
    /// publisher-prefix and secured-column scans, and cross-environment schema conflicts). The SDK metadata
    /// types expose get-only sealed properties, so the values are set through their non-public setters via
    /// reflection — the standard technique for seeding metadata without a live org.
    ///
    /// Shared by AnalyzerTests and CollectorTests (source-linked); kept in one place so both suites build
    /// metadata the same way.
    /// </summary>
    internal static class MetaBuilder
    {
        public static EntityMetadata Entity(string logicalName, bool isCustom = true, bool isIntersect = false,
            string schemaName = null, string displayName = null, string description = null,
            AttributeMetadata[] attributes = null, Guid? metadataId = null,
            OneToManyRelationshipMetadata[] oneToMany = null, ManyToManyRelationshipMetadata[] manyToMany = null)
        {
            var e = new EntityMetadata();
            Set(e, "LogicalName", logicalName);
            Set(e, "SchemaName", schemaName ?? logicalName);
            Set(e, "IsCustomEntity", (bool?)isCustom);
            Set(e, "IsIntersect", (bool?)isIntersect);
            if (metadataId.HasValue) Set(e, "MetadataId", (Guid?)metadataId.Value);
            if (displayName != null) Set(e, "DisplayName", MakeLabel(displayName));
            if (description != null) Set(e, "Description", MakeLabel(description));
            if (attributes != null) Set(e, "Attributes", attributes);
            if (oneToMany != null) Set(e, "OneToManyRelationships", oneToMany);
            if (manyToMany != null) Set(e, "ManyToManyRelationships", manyToMany);
            return e;
        }

        /// <summary>A simple string attribute (used by the custom-column / secured-column scans).</summary>
        public static AttributeMetadata Attribute(string schemaName, bool isCustom = true, bool isSecured = false,
            bool isManaged = false, string displayName = null)
        {
            var a = new StringAttributeMetadata();
            Set(a, "LogicalName", schemaName.ToLowerInvariant());
            Set(a, "SchemaName", schemaName);
            Set(a, "AttributeType", (AttributeTypeCode?)AttributeTypeCode.String);
            Set(a, "IsCustomAttribute", (bool?)isCustom);
            Set(a, "IsSecured", (bool?)isSecured);
            Set(a, "IsManaged", (bool?)isManaged);
            if (displayName != null) Set(a, "DisplayName", MakeLabel(displayName));
            return a;
        }

        /// <summary>A string attribute with an explicit max length (for the length-shrink conflict).</summary>
        public static StringAttributeMetadata StringAttr(string logicalName, int? maxLength = null)
        {
            var a = new StringAttributeMetadata();
            Set(a, "LogicalName", logicalName);
            Set(a, "SchemaName", logicalName);
            Set(a, "AttributeType", (AttributeTypeCode?)AttributeTypeCode.String);
            if (maxLength.HasValue) Set(a, "MaxLength", (int?)maxLength.Value);
            return a;
        }

        /// <summary>An integer attribute — used to force a type mismatch against a string of the same name.</summary>
        public static IntegerAttributeMetadata IntAttr(string logicalName)
        {
            var a = new IntegerAttributeMetadata();
            Set(a, "LogicalName", logicalName);
            Set(a, "SchemaName", logicalName);
            Set(a, "AttributeType", (AttributeTypeCode?)AttributeTypeCode.Integer);
            return a;
        }

        /// <summary>A picklist attribute with the given (value,label) options (for choice conflicts).</summary>
        public static PicklistAttributeMetadata PicklistAttr(string logicalName, params (int value, string label)[] options)
        {
            var a = new PicklistAttributeMetadata();
            Set(a, "LogicalName", logicalName);
            Set(a, "SchemaName", logicalName);
            Set(a, "AttributeType", (AttributeTypeCode?)AttributeTypeCode.Picklist);
            var os = new OptionSetMetadata();
            foreach (var (value, label) in options)
                os.Options.Add(new OptionMetadata(MakeLabel(label), value));
            Set(a, "OptionSet", os);
            return a;
        }

        /// <summary>A 1:N (or N:1) relationship with the given shape.</summary>
        public static OneToManyRelationshipMetadata OneToMany(string schemaName,
            string referencedEntity, string referencingEntity, string referencingAttribute)
        {
            var r = new OneToManyRelationshipMetadata();
            Set(r, "SchemaName", schemaName);
            Set(r, "ReferencedEntity", referencedEntity);
            Set(r, "ReferencingEntity", referencingEntity);
            Set(r, "ReferencingAttribute", referencingAttribute);
            return r;
        }

        /// <summary>An N:N relationship linking two entities.</summary>
        public static ManyToManyRelationshipMetadata ManyToMany(string schemaName, string entity1, string entity2)
        {
            var r = new ManyToManyRelationshipMetadata();
            Set(r, "SchemaName", schemaName);
            Set(r, "Entity1LogicalName", entity1);
            Set(r, "Entity2LogicalName", entity2);
            return r;
        }

        /// <summary>A Label whose UserLocalizedLabel is populated (the Label(string,int) ctor leaves it null).</summary>
        private static Label MakeLabel(string text)
        {
            var localized = new LocalizedLabel(text, 1033);
            return new Label(localized, new[] { localized });
        }

        /// <summary>Set a get-only/sealed metadata property via its non-public setter, falling back to the backing field.</summary>
        private static void Set(object target, string prop, object value)
        {
            var t = target.GetType();
            PropertyInfo pi = null;
            for (var cur = t; cur != null && pi == null; cur = cur.BaseType)
                pi = cur.GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var setter = pi?.GetSetMethod(true);
            if (setter != null) { setter.Invoke(target, new[] { value }); return; }

            var fieldName = "_" + char.ToLowerInvariant(prop[0]) + prop.Substring(1);
            for (var cur = t; cur != null; cur = cur.BaseType)
            {
                var f = cur.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null) { f.SetValue(target, value); return; }
            }
            throw new InvalidOperationException($"MetaBuilder cannot set '{prop}' on {t.Name}.");
        }
    }
}
