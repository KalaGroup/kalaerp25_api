using KalaGenset.ERP.Core.Request.Canopy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IPowderCoating
    {
        Task<List<Dictionary<string, object>>> GetCpyKitPCAsync(string pcCode, string machineCode, string planCode,string partCode, string cpyKit, string kva);
        Task<string> SubmitPowderCoatingAsync(CpyPrcPCRequest cpyPrcPCReq, CancellationToken cancellationToken = default);
        Task<string> SubmitPowderCoatingCheckerAsync(CpyPrcPCCheckerRequest req, CancellationToken ct = default);
        
    }

}
