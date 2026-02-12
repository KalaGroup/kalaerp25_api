using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IKalaService
    {
        public Task<List<Dictionary<string, object>>> GetPendingSiteVisitAsync(string Ecode);

        public Task<string> SubmitCustomerFeedbackAsyc(KalaServiceCustomerFeedbackSubmitRequest submitCustomerFeedbackReq);

        public Task<string> SubmitSiteVisitDetailsAsync(SubmitSiteVisitDetailsRequest submitSiteVisitDetailsRequest);
    }
}
