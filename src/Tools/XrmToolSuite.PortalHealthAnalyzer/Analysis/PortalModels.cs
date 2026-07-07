using System;
using System.Collections.Generic;

namespace XrmToolSuite.PortalHealthAnalyzer.Analysis
{
    /// <summary>
    /// The two Power Pages data schemas a website can live in. Classic portals use the <c>adx_</c>
    /// tables; enhanced/standard (Power Pages) portals use the <c>mspp_</c> tables. Exactly one schema
    /// is normally provisioned per environment; the collector detects which and everything downstream
    /// works off this single, schema-agnostic model.
    /// </summary>
    public enum PortalSchema
    {
        /// <summary>Classic Dynamics 365 Portals — <c>adx_*</c> tables.</summary>
        Adx,

        /// <summary>Enhanced / standard Power Pages — <c>mspp_*</c> tables.</summary>
        Mspp
    }

    /// <summary>
    /// A schema-normalized portal record (web page, template, page template, snippet, web role,
    /// list, web file, redirect …). Only the fields the health rules need are surfaced, so the same
    /// shape represents a record from either the <c>adx_</c> or <c>mspp_</c> schema. SDK-free.
    /// </summary>
    public sealed class PortalRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        /// <summary>False when the record's <c>statecode</c> is inactive.</summary>
        public bool Active { get; set; } = true;

        /// <summary>Parent web page (for pages), when set.</summary>
        public Guid? ParentId { get; set; }

        /// <summary>Page-template reference (for pages) or web-template reference (for page templates).</summary>
        public Guid? TemplateId { get; set; }
    }

    /// <summary>A single site-setting name/value pair for a website.</summary>
    public sealed class PortalSetting
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// A table/entity permission summarized for a health-level scan (not a deep security audit — that
    /// lives in the dedicated Portal Security Scanner). Carries just enough to surface anonymous access
    /// and over-broad scope.
    /// </summary>
    public sealed class PortalPermission
    {
        public string Name { get; set; }

        /// <summary>Access scope: Global / Contact / Account / Self / Parent (schema option-set text).</summary>
        public string Scope { get; set; }

        /// <summary>True when this permission grants anonymous (unauthenticated) read, write, or delete.</summary>
        public bool AnonymousReadWriteOrDelete { get; set; }

        /// <summary>Logical name of the Dataverse table the permission is bound to.</summary>
        public string EntityLogicalName { get; set; }
    }

    /// <summary>
    /// An entity-bound portal component (basic/entity form or entity list) with the Dataverse table it
    /// binds to and whether that table actually exists/is enabled in the environment.
    /// </summary>
    public sealed class PortalForm
    {
        public string Name { get; set; }
        public string EntityLogicalName { get; set; }

        /// <summary>True when the bound Dataverse table exists and is enabled; false flags a broken binding.</summary>
        public bool EntityExists { get; set; } = true;

        /// <summary>"Form" (basic/entity form) or "List" (entity list) — for report labelling.</summary>
        public string Kind { get; set; } = "Form";
    }

    /// <summary>
    /// A schema-normalized inventory of one Power Pages website's configuration, unifying the
    /// <c>adx_</c> and <c>mspp_</c> schemas into a single model the health rules evaluate. SDK-free
    /// (no <c>Microsoft.Xrm.Sdk</c>) so it is unit-testable with hand-built fixtures and liftable into
    /// a console/CI wrapper.
    /// </summary>
    public sealed class PortalInventory
    {
        public PortalSchema Schema { get; set; }
        public string WebsiteName { get; set; }
        public Guid WebsiteId { get; set; }

        public List<PortalRecord> Pages { get; set; } = new List<PortalRecord>();
        public List<PortalRecord> Templates { get; set; } = new List<PortalRecord>();      // web templates
        public List<PortalRecord> PageTemplates { get; set; } = new List<PortalRecord>();
        public List<PortalRecord> Snippets { get; set; } = new List<PortalRecord>();
        public List<PortalSetting> Settings { get; set; } = new List<PortalSetting>();
        public List<PortalRecord> WebRoles { get; set; } = new List<PortalRecord>();
        public List<PortalPermission> Permissions { get; set; } = new List<PortalPermission>();
        public List<PortalForm> Forms { get; set; } = new List<PortalForm>();               // entity/basic forms + entity lists
        public List<PortalRecord> Lists { get; set; } = new List<PortalRecord>();
        public List<PortalRecord> WebFiles { get; set; } = new List<PortalRecord>();
        public List<PortalRecord> Redirects { get; set; } = new List<PortalRecord>();

        /// <summary>
        /// Web-file ids referenced by pages/templates. A referenced id with no matching record in
        /// <see cref="WebFiles"/> is a broken asset reference (High). Populated best-effort by the
        /// collector; always safe to leave empty.
        /// </summary>
        public List<Guid> ReferencedWebFileIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Human-readable notes for tables that could not be retrieved (schema not provisioned, or the
        /// other model only). Surfaced as informational findings — retrieval never throws on a missing table.
        /// </summary>
        public List<string> UnavailableTables { get; set; } = new List<string>();

        /// <summary>Logical-name prefix for the active schema (<c>adx_</c> / <c>mspp_</c>).</summary>
        public string SchemaPrefix => Schema == PortalSchema.Adx ? "adx_" : "mspp_";

        /// <summary>Short badge for the active schema, used on cards and reports.</summary>
        public string SchemaLabel => Schema == PortalSchema.Adx ? "adx (classic)" : "mspp (enhanced)";
    }
}
