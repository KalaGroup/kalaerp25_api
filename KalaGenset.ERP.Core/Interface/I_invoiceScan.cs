using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface I_invoiceScan
    {
        public Task<List<Dictionary<string, object>>> GetScanDtsInvAsync(string strSrNo);

        public Task<string> SubmitAsync(string invoiceId);

        public Task<string> SendEmailAsync(string invId);
    }
}
