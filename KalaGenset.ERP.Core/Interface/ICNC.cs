using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.ResponseDTO.CNC;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface ICNC
    {
        Task<List<Dictionary<string, object>>> LoadMachineAsync(string pcCode);
        Task<List<Dictionary<string, object>>> LoadOSSupplierAsync(string pcCode);
        Task<List<Dictionary<string, object>>> GetCpyPrcddlAsync(string pcCode, string machineCode, string kva, string model, string planCode, string catId);
        Task<List<Dictionary<string, object>>> LoadCatIDAsync(string pcCode, string planCode);
        Task<List<Dictionary<string, object>>> LoadProductAsync(string pcCode);
        Task<List<Dictionary<string, object>>> GetSheetPartDtsAsync(string pcCode, int sheetSrNo, string machineCode, string sheetPartcode,string planCode, string partcode, string catId);
        Task<List<Dictionary<string, object>>> GetTKitDtsAsync(string pcCode, string tKitId, int batchQty, string trnsType, string planCode, string prodCode);
        Task<string> SubmitCNCAsync(CpyPrcCNCRequest req, CancellationToken cancellationToken = default);
        Task<List<Dictionary<string, object>>> GetCheckerCPPlanLoadAsync(string pcCode);
        Task<List<Dictionary<string, object>>> GetCNC_chekerDetailsAsync(string compId, string planCode, string pcCode);
        Task<string> SubmitCncCheckerAsync(CpyPrcCNCCheckerRequest req, CancellationToken ct = default);
    }
}
