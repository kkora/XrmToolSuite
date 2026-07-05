using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// Emits the ERD model as structured JSON for downstream tooling. Hand-rolled (BCL-only, no
    /// Newtonsoft) so it stays in the SDK-free unit-test set and is fully deterministic.
    /// </summary>
    public static class ErdJson
    {
        public static string Emit(ErdModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var sb = new StringBuilder();
            var w = new JsonWriter(sb);
            w.BeginObject();

            w.Key("tableCount"); w.Number(model.Tables.Count); w.Comma();
            w.Key("relationshipCount"); w.Number(model.Relationships.Count); w.Comma();

            w.Key("tables"); w.BeginArray();
            for (int i = 0; i < model.Tables.Count; i++)
            {
                var t = model.Tables[i];
                w.BeginObject();
                w.Prop("logicalName", t.LogicalName); w.Comma();
                w.Prop("displayName", t.DisplayName); w.Comma();
                w.Prop("schemaName", t.SchemaName); w.Comma();
                w.Key("isCustom"); w.Bool(t.IsCustom); w.Comma();
                w.Key("isManaged"); w.Bool(t.IsManaged); w.Comma();
                w.Prop("primaryIdColumn", t.PrimaryIdColumn); w.Comma();
                w.Prop("primaryNameColumn", t.PrimaryNameColumn); w.Comma();

                w.Key("columns"); w.BeginArray();
                for (int c = 0; c < t.Columns.Count; c++)
                {
                    var col = t.Columns[c];
                    w.BeginObject();
                    w.Prop("logicalName", col.LogicalName); w.Comma();
                    w.Prop("type", col.Type); w.Comma();
                    w.Prop("requiredLevel", col.RequiredLevel); w.Comma();
                    w.Key("isPrimaryId"); w.Bool(col.IsPrimaryId); w.Comma();
                    w.Key("isPrimaryName"); w.Bool(col.IsPrimaryName); w.Comma();
                    w.Key("isLookup"); w.Bool(col.IsLookup); w.Comma();
                    w.Key("targets"); w.StringArray(col.Targets);
                    w.EndObject();
                    if (c < t.Columns.Count - 1) w.Comma();
                }
                w.EndArray(); w.Comma();

                w.Key("alternateKeys"); w.BeginArray();
                for (int k = 0; k < t.AlternateKeys.Count; k++)
                {
                    var key = t.AlternateKeys[k];
                    w.BeginObject();
                    w.Prop("name", key.Name); w.Comma();
                    w.Key("columns"); w.StringArray(key.Columns);
                    w.EndObject();
                    if (k < t.AlternateKeys.Count - 1) w.Comma();
                }
                w.EndArray();

                w.EndObject();
                if (i < model.Tables.Count - 1) w.Comma();
            }
            w.EndArray(); w.Comma();

            w.Key("relationships"); w.BeginArray();
            for (int i = 0; i < model.Relationships.Count; i++)
            {
                var r = model.Relationships[i];
                w.BeginObject();
                w.Prop("schemaName", r.SchemaName); w.Comma();
                w.Prop("relationType", r.RelationType); w.Comma();
                w.Prop("fromTable", r.FromTable); w.Comma();
                w.Prop("toTable", r.ToTable); w.Comma();
                w.Prop("lookupColumn", r.LookupColumn); w.Comma();
                w.Prop("cascadeSummary", r.CascadeSummary); w.Comma();
                w.Prop("requiredLevel", r.RequiredLevel);
                w.EndObject();
                if (i < model.Relationships.Count - 1) w.Comma();
            }
            w.EndArray();

            if (model.Notes != null && model.Notes.Count > 0)
            {
                w.Comma();
                w.Key("notes"); w.StringArray(model.Notes);
            }

            w.EndObject();
            return sb.ToString();
        }

        /// <summary>Minimal, indentation-free JSON writer. Deterministic and BCL-only.</summary>
        private sealed class JsonWriter
        {
            private readonly StringBuilder _sb;
            public JsonWriter(StringBuilder sb) { _sb = sb; }

            public void BeginObject() => _sb.Append('{');
            public void EndObject() => _sb.Append('}');
            public void BeginArray() => _sb.Append('[');
            public void EndArray() => _sb.Append(']');
            public void Comma() => _sb.Append(',');
            public void Key(string name) { Str(name); _sb.Append(':'); }
            public void Number(int n) => _sb.Append(n.ToString(CultureInfo.InvariantCulture));
            public void Bool(bool b) => _sb.Append(b ? "true" : "false");

            public void Prop(string name, string value) { Key(name); if (value == null) _sb.Append("null"); else Str(value); }

            public void StringArray(IEnumerable<string> items)
            {
                _sb.Append('[');
                bool first = true;
                if (items != null)
                    foreach (var s in items)
                    {
                        if (!first) _sb.Append(',');
                        Str(s ?? "");
                        first = false;
                    }
                _sb.Append(']');
            }

            private void Str(string s)
            {
                _sb.Append('"');
                foreach (var ch in s)
                {
                    switch (ch)
                    {
                        case '"': _sb.Append("\\\""); break;
                        case '\\': _sb.Append("\\\\"); break;
                        case '\b': _sb.Append("\\b"); break;
                        case '\f': _sb.Append("\\f"); break;
                        case '\n': _sb.Append("\\n"); break;
                        case '\r': _sb.Append("\\r"); break;
                        case '\t': _sb.Append("\\t"); break;
                        default:
                            if (ch < ' ') _sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                            else _sb.Append(ch);
                            break;
                    }
                }
                _sb.Append('"');
            }
        }
    }
}
