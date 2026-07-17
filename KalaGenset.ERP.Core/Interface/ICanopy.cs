using KalaGenset.ERP.Core.Request.Canopy;

namespace KalaGenset.ERP.Core.Interface
{
    public interface ICanopy
    {
        //Fetch Canopy Plan Details
        public Task<List<Dictionary<string, object>>> GetCanopyPlanAsync(string strJobCardType, string lineWisePC);
        Task<string> SubmitAsync(JobCard_CpyRequest job_Cpyreq);
        //Fetch Canopy Plan Details for Checker
        Task<List<Dictionary<string, object>>> GetCheckerCPPlanLoadAsync();
        Task<List<Dictionary<string, object>>> GetJobCardCpyCheckerAsync(string strJobCardType, string strcompID, string planCode);
        Task<List<Dictionary<string, object>>> GetJobCardCpyCheckerDoneAsync(string strJobCardType, string strcompID, string planCode);
        public Task<List<Dictionary<string, object>>> Get6MTypesAsync();
        public Task<List<Dictionary<string, object>>> JobcardCorReqEmpNameAsync();
        Task<string> CheckerSubmitAsync(Canopy_JobCardCheckerRequest job_CpyCheckerreq);
        Task<List<Dictionary<string, object>>> GetStageSheetDataAsync(string cpCode, string partCode, string stage,string pcCode);
        Task<List<Dictionary<string, object>>> GetLineByProcessAsync(string ProcessName,string compCode);

        /// Sheet metal job card hold process 
        Task<List<Dictionary<string, object>>> GetConopyHoldAsync(string compCode);
        Task<string> JobCardConopyReqInActiveHoldAsync(Canopy_JobCardHoldRequest job_CpyHoldreq);

    }
}
