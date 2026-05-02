using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.Request.Logistic;
using KalaGenset.ERP.Core.ResponseDTO;

namespace KalaGenset.ERP.Core.Interface
{
    public interface ILogistic
    {
        public Task<List<PCNameForMTFScanDTO>> GetPCodeAllAsync(string PCCode, string ReqType);

        public Task<List<MTFCodeAndNoDTO>> GetMTFCodeAndMTFNoAsync(string FPCCode, string TPCCode);

        //public Task<List<PartDescOfMTFScanDTO>> GetReqProductDtlAsync(string MTFCode);
        public Task<List<Dictionary<string, object>>> GetReqProductDtlAsync(string MTFCode);

        public Task<List<GetMTFSrNoDtsDTO>> GetMTFSrNoDtlAsync(string MTFCode);

        public Task<string> SubmitMTFScanDetailsAsync(MTFScanSubmitRequest mtfScanSubmitRequest);

        Task<List<ReqCodeForMTFScanDTO>> GetReqCodeForMTFAsync(string FPCCode, string TPCCode);

        Task<List<ReqDetailsForMTFDTO>> GetReqDetailsAsync(string PCCode, string strBomCode, string StrReqCode,double StrReqQty, double StrMTFQty);

        public Task<string> SubmitMTFWIPInternalAsync(MTFWIPInternalRequest req);
    }
}
