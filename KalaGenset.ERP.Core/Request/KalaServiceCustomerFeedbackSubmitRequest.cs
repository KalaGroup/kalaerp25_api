using Microsoft.AspNetCore.Http;

namespace KalaGenset.ERP.Core.Request
{
    public class KalaServiceCustomerFeedbackSubmitRequest
    {
        public string products { get; set; }
        public string promptness { get; set; }
        public string technical { get; set; }
        public string delivery { get; set; }
        public string communication { get; set; }

        // For uploaded file (image, pdf, etc.)
        public IFormFile file { get; set; }

        public string ecode { get; set; }
        public string companyId { get; set; }
        public string actno { get; set; }
        public string engno { get; set; }
    }
}
