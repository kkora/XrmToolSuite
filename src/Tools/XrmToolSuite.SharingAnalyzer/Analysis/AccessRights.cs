using System;
using System.Collections.Generic;

namespace XrmToolSuite.SharingAnalyzer.Analysis
{
    /// <summary>
    /// The Dataverse record-level access-rights bitmask, as stored in
    /// <c>principalobjectaccess.accessrightsmask</c>. Values mirror the platform
    /// <c>Microsoft.Crm.Sdk.Messages.AccessRights</c> flag enum exactly (kept here as SDK-free
    /// constants so the sharing logic stays unit-testable without Microsoft.Xrm.Sdk). Note the gaps:
    /// only these bits are defined by the platform.
    /// </summary>
    [Flags]
    public enum AccessRight
    {
        None = 0,
        Read = 1,          // 0x1     ReadAccess
        Write = 2,         // 0x2     WriteAccess
        Append = 4,        // 0x4     AppendAccess
        AppendTo = 16,     // 0x10    AppendToAccess
        Create = 32,       // 0x20    CreateAccess
        Delete = 65536,    // 0x10000 DeleteAccess
        Share = 262144,    // 0x40000 ShareAccess
        Assign = 524288,   // 0x80000 AssignAccess
    }

    /// <summary>
    /// Decodes an <c>accessrightsmask</c> into the named rights it grants and a compact summary string.
    /// Pure, deterministic, SDK-free.
    /// </summary>
    public static class AccessRights
    {
        // Evaluated in this order so decoded lists / summaries read consistently.
        private static readonly (AccessRight Flag, string Name, string Code)[] Ordered =
        {
            (AccessRight.Read, "Read", "R"),
            (AccessRight.Write, "Write", "W"),
            (AccessRight.Append, "Append", "A"),
            (AccessRight.AppendTo, "AppendTo", "AT"),
            (AccessRight.Create, "Create", "C"),
            (AccessRight.Delete, "Delete", "D"),
            (AccessRight.Share, "Share", "S"),
            (AccessRight.Assign, "Assign", "AS"),
        };

        /// <summary>Named rights present in the mask, in a stable order. Empty for <c>None</c>.</summary>
        public static List<string> Decode(int mask)
        {
            var rights = new List<string>();
            foreach (var r in Ordered)
                if ((mask & (int)r.Flag) == (int)r.Flag && r.Flag != AccessRight.None)
                    rights.Add(r.Name);
            return rights;
        }

        /// <summary>
        /// Compact one-line summary of the granted rights using short codes joined by '/'
        /// (e.g. <c>R/W/D</c>). Returns <c>None</c> for an empty mask and <c>Unknown(bits)</c> when the
        /// mask carries bits outside the defined set.
        /// </summary>
        public static string Summary(int mask)
        {
            if (mask == 0) return "None";

            var codes = new List<string>();
            int consumed = 0;
            foreach (var r in Ordered)
            {
                if ((mask & (int)r.Flag) == (int)r.Flag)
                {
                    codes.Add(r.Code);
                    consumed |= (int)r.Flag;
                }
            }

            int leftover = mask & ~consumed;
            if (codes.Count == 0)
                return $"Unknown(0x{mask:X})";

            var summary = string.Join("/", codes);
            if (leftover != 0)
                summary += $"+0x{leftover:X}";
            return summary;
        }

        /// <summary>True when the mask carries any write-class right (Write/Delete/Assign/Share).</summary>
        public static bool IsElevated(int mask) =>
            (mask & ((int)AccessRight.Write | (int)AccessRight.Delete |
                     (int)AccessRight.Assign | (int)AccessRight.Share)) != 0;
    }
}
