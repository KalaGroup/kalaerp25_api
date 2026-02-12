using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IMarketing
    {
        public Task<List<Dictionary<string, object>>> GetPendingMOFNFAsync();

        public Task<string> SubmitMOFNFALevelAsync(MOFNFALevelAuthSubmitRequest req);
    }
}
