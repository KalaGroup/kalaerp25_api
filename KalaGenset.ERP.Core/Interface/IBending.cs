using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.ResponseDTO.Bending;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IBending
    {
        Task<IEnumerable<BendingCpyKitDto>> GetCpyKitAsync(string pcCode,string machineCode,string planCode,string partCode,string cpyKit,CancellationToken cancellationToken = default);
        Task<IEnumerable<Dictionary<string, object?>>> GetCpyKitDtsAsync(string pcCode,int batchQty,string cpyKitCode,string bomCode,string pfbCode,CancellationToken cancellationToken = default);
        Task<string> SubmitBendingAsync(CpyPrcBendRequest request, CancellationToken cancellationToken = default);
        Task<string> SubmitBendingCheckerAsync(CpyPrcBendCheckerRequest req, CancellationToken ct = default);



    }
}
