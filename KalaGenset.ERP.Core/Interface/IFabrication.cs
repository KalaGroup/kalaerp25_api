using KalaGenset.ERP.Core.Request.Canopy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IFabrication
    {
        Task<List<Dictionary<string, object>>> GetCpyPrcddlFabAsync(
             string pcCode, string machineCode, string kva, string model, string suppCode);

        Task<List<Dictionary<string, object>>> GetCpyKitFabAsync(
    string pcCode, string machineCode, string planCode,
    string partCode, string cpyKit, string suppCode);

        Task<List<Dictionary<string, object>>> CpyKitDtsAsync(
    string pcCode, int batchQty, string cpyKitCode, string bomCode, string pfbCode);

        Task<string> SubmitFabricationAsync(CpyPrcFabRequest cpyPrcFabReq, CancellationToken cancellationToken = default);

        Task<string> SubmitFabricationCheckerAsync(CpyPrcFabCheckerRequest req, CancellationToken ct = default);
    }
}
