using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.Request.ControlPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IControlPanel
    {
        public Task<List<Dictionary<string, object>>> GetControlPanelAsync(string strJobCardType, string lineWisePC);
        Task<string> SubmitCPAsync(JobCard_CPRequest job_CPreq);
        Task<List<Dictionary<string, object>>> GetCheckerCPLoad();
        Task<List<Dictionary<string, object>>> GetJobCardCpyCheckerAsync(string strJobCardType, string strcompID, string planCode);
        Task<string> CPCheckerSubmitAsync(CP_JobCardCheckerRequest job_CPCheckerreq);
    }
}
