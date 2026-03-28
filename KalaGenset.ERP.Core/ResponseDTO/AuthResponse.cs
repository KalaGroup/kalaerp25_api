using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Response
{
    public class AuthResponse
    {
        public bool success { get; set; }
        public string token { get; set; }
        public string loginType { get; set; }
        public int userId { get; set; }
        public string companyId { get; set; }
        public string companyName { get; set; }
        public string profitCenterName { get; set; }
        public string empCode { get; set; }
        public string username { get; set; }
        public string pccode { get; set; }
        public string pccode_old { get; set; }
        public string message { get; set; }
    }
}
