using KalaGenset.ERP.Core.Request.Canopy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IReverse
    {
        Task<List<Dictionary<string, object>>> GetRevPCCodeAsync(string strTransType, string catId);
        Task<List<Dictionary<string, object>>> LoadRevPrcDtsAsync(string strPCCode, string catId);
        Task<string> SubmitRevCpyTransAsync(CpyRevRequest cpyRevReq , CancellationToken ct = default);
    }
}
