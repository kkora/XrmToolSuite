using System;
using System.Linq;

namespace XrmToolSuite.AuditComplianceChecker.Analysis
{
    /// <summary>
    /// Name/type heuristics that flag tables and columns likely to hold sensitive/regulated data
    /// (PII, financial, credentials). Used to prioritise audit-coverage gaps. Pure, deterministic,
    /// SDK-free, and unit-tested. Heuristics are intentionally conservative and name-based; they are
    /// hints, not a classification — a real DLP/sensitivity review still governs.
    /// </summary>
    public static class SensitivityHeuristics
    {
        /// <summary>
        /// Substrings that mark a logical name as sensitive. Case-insensitive, matched against the
        /// lower-cased logical name. Documented as a single constant so the rule set is auditable.
        /// Covers: government IDs, DOB, financials/compensation, banking/payment, credentials/secrets,
        /// and direct contact identifiers.
        /// </summary>
        // NOTE: these are matched as case-insensitive SUBSTRINGS of the lower-cased logical name, so every
        // entry must be distinctive enough not to appear inside ordinary schema words. Short, ambiguous
        // tokens are deliberately excluded and covered by longer synonyms instead:
        //   "tin" -> covered by taxid/nationalid/ssn (bare "tin" falsely matches set-"tin"-g, mee-"tin"-g, rou-"tin"-g)
        //   "pin" -> covered by password/secret/credential (bare "pin" falsely matches ship-"pin"-g, shop-"pin"-g)
        //   "routing" -> narrowed to "routingnumber" (bare "routing" falsely matches routingrule / msdyn_routing*)
        public static readonly string[] SensitiveNamePatterns =
        {
            "ssn", "socialsecurity", "nationalid", "passport", "taxid", "vatnumber",
            "dob", "dateofbirth", "birth", "birthdate",
            "salary", "compensation", "wage", "income", "networth",
            "bank", "iban", "swift", "routingnumber", "accountnumber", "creditcard", "cardnumber",
            "creditscore", "payment",
            "password", "secret", "apikey", "token", "credential",
            "email", "emailaddress", "phone", "mobile", "telephone",
            "healthid", "medical", "diagnosis", "biometric", "driverslicense", "licensenumber",
        };

        /// <summary>
        /// Attribute type names that are inherently sensitive regardless of column name (financial
        /// amounts). Compared case-insensitively against the attribute type string.
        /// </summary>
        public static readonly string[] SensitiveTypeNames = { "money" };

        /// <summary>True when the table's logical name contains a sensitive pattern.</summary>
        public static bool IsSensitiveTable(string logical)
        {
            if (string.IsNullOrWhiteSpace(logical)) return false;
            var name = logical.ToLowerInvariant();
            return SensitiveNamePatterns.Any(p => name.IndexOf(p, StringComparison.Ordinal) >= 0);
        }

        /// <summary>
        /// True when a column's name contains a sensitive pattern, or its type is inherently sensitive
        /// (e.g. Money). <paramref name="type"/> may be null.
        /// </summary>
        public static bool IsSensitiveColumn(string logical, string type)
        {
            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.ToLowerInvariant();
                if (SensitiveTypeNames.Any(st => t.IndexOf(st, StringComparison.Ordinal) >= 0))
                    return true;
            }

            if (string.IsNullOrWhiteSpace(logical)) return false;
            var name = logical.ToLowerInvariant();
            return SensitiveNamePatterns.Any(p => name.IndexOf(p, StringComparison.Ordinal) >= 0);
        }
    }
}
