using System;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace XrmToolSuite.CollectorTests
{
    /// <summary>
    /// Builds <see cref="EntityMetadata"/> / <see cref="AttributeMetadata"/> fixtures for the tests that
    /// exercise metadata-driven analyzer branches (custom-table detection, wide tables, publisher-prefix
    /// and secured-column scans). The SDK metadata types expose get-only sealed properties, so the values
    /// are set through their non-public setters via reflection — the standard technique for seeding
    /// metadata without a live org. Isolated here so the row-driven tests stay reflection-free.
    /// </summary>
    internal static class MetaBuilder
    {
        public static EntityMetadata Entity(string logicalName, bool isCustom = true, bool isIntersect = false,
            string schemaName = null, string displayName = null, string description = null,
            AttributeMetadata[] attributes = null, Guid? metadataId = null)
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
            return e;
        }

        /// <summary>A Label whose UserLocalizedLabel is populated (the Label(string,int) ctor leaves it null).</summary>
        private static Label MakeLabel(string text)
        {
            var localized = new LocalizedLabel(text, 1033);
            return new Label(localized, new[] { localized });
        }

        public static AttributeMetadata Attribute(string schemaName, bool isCustom = true, bool isSecured = false)
        {
            var a = new StringAttributeMetadata();
            Set(a, "LogicalName", schemaName.ToLowerInvariant());
            Set(a, "SchemaName", schemaName);
            Set(a, "IsCustomAttribute", (bool?)isCustom);
            Set(a, "IsSecured", (bool?)isSecured);
            return a;
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
