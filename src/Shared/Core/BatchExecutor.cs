using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace XrmToolSuite.Core
{
    public class BatchFault
    {
        public int RequestIndex { get; set; }
        public string Message { get; set; }
        public OrganizationServiceFault Fault { get; set; }
    }

    public class BatchResult
    {
        public int Succeeded { get; set; }
        public List<BatchFault> Faults { get; } = new List<BatchFault>();
        public bool Cancelled { get; set; }
        public bool HasErrors => Faults.Count > 0;
    }

    /// <summary>
    /// Executes large sets of OrganizationRequests via ExecuteMultiple,
    /// with chunking, progress reporting, and cancellation support.
    /// </summary>
    public static class BatchExecutor
    {
        public static BatchResult Execute(
            IOrganizationService service,
            IReadOnlyList<OrganizationRequest> requests,
            int batchSize = 200,
            bool continueOnError = true,
            Action<int, int> onProgress = null, // (processed, total)
            BackgroundWorker worker = null)
        {
            if (batchSize < 1 || batchSize > 1000)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be 1-1000.");

            var result = new BatchResult();
            var total = requests.Count;

            for (var offset = 0; offset < total; offset += batchSize)
            {
                if (worker?.CancellationPending == true)
                {
                    result.Cancelled = true;
                    return result;
                }

                var chunk = requests.Skip(offset).Take(batchSize).ToList();

                var execute = new ExecuteMultipleRequest
                {
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = continueOnError,
                        ReturnResponses = false
                    },
                    Requests = new OrganizationRequestCollection()
                };
                execute.Requests.AddRange(chunk);

                var response = (ExecuteMultipleResponse)service.Execute(execute);

                var chunkFaults = 0;
                if (response.IsFaulted)
                {
                    foreach (var item in response.Responses.Where(r => r.Fault != null))
                    {
                        chunkFaults++;
                        result.Faults.Add(new BatchFault
                        {
                            RequestIndex = offset + item.RequestIndex,
                            Message = item.Fault.Message,
                            Fault = item.Fault
                        });
                    }
                }

                result.Succeeded += chunk.Count - chunkFaults;
                onProgress?.Invoke(Math.Min(offset + chunk.Count, total), total);
            }

            return result;
        }
    }
}
