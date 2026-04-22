using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request.Jobcard;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IJobcard
    {
        public Task<List<Dictionary<string, object>>> GetDGAsync(string strJobCardType, string strCompID, string strAssemblyLine);

        public Task<string> SubmitJobCardAsync(JobCardSubmitRequest request);

        public Task<List<Dictionary<string, object>>> GetDGJobcard1CheckerDetails(string jobCode);

        public Task<List<string>> GetPendingAuthJobCodes();

        public Task<List<Dictionary<string, object>>> GetPlanDetails(string jobCode);

        public Task<string> SubmitJobcard1Checker(Jobcard1CheckerSubmitRequest jobcard1CheckerSubmitReq);

        public Task<List<Dictionary<string, object?>>> GetJobCardDG2DtsAsync(string strJobCardType, string strcompID, string strAssemblyLine);

        public Task<List<Dictionary<string, object?>>> GetJobCard2CPAsync();

        public Task<string> GetCPStkAsync(string strKVA, string ph, string panelType, string compId, string assemblyLine);
    }
}
