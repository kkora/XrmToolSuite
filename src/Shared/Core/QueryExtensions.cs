using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.Core
{
    public static class QueryExtensions
    {
        /// <summary>
        /// Retrieves ALL records for a QueryExpression, transparently handling paging.
        /// Honors BackgroundWorker cancellation if provided.
        /// </summary>
        public static List<Entity> RetrieveAll(
            this IOrganizationService service,
            QueryExpression query,
            Action<int> onPageRetrieved = null,
            BackgroundWorker worker = null)
        {
            var all = new List<Entity>();

            query.PageInfo = new PagingInfo
            {
                PageNumber = 1,
                Count = query.PageInfo?.Count > 0 ? query.PageInfo.Count : 5000
            };

            while (true)
            {
                if (worker?.CancellationPending == true) break;

                var page = service.RetrieveMultiple(query);
                all.AddRange(page.Entities);
                onPageRetrieved?.Invoke(all.Count);

                if (!page.MoreRecords) break;

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }

            return all;
        }

        /// <summary>Shorthand for a paged retrieve of selected columns.</summary>
        public static List<Entity> RetrieveAll(
            this IOrganizationService service,
            string entityLogicalName,
            params string[] columns)
        {
            var query = new QueryExpression(entityLogicalName)
            {
                ColumnSet = columns == null || columns.Length == 0
                    ? new ColumnSet(true)
                    : new ColumnSet(columns)
            };
            return service.RetrieveAll(query);
        }
    }
}
