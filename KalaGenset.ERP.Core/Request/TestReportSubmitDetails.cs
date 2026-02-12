using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KalaGenset.ERP.Core.RequestDTO
{
    public class TestReportSubmitDetails
    {
        public string? PFBCode { get; set; }
        public string? DGSrNo { get; set; }
        public string? TRCode { get; set; }
        public string? TRTime { get; set; }
        public string? QA6M { get; set; }
        public string? QAStatus { get; set; }
        public string? DieselQty { get; set; }
        public string? DieselRate { get; set; }
        public string? Remark { get; set; }
        public string? PrcStatus { get; set; }
        public List<TRProcessCheckpointDTO>? TRPrcChkDts { get; set; }
        public List<TRDgKitDTO>? TRDGKitDetails { get; set; }
        public IFormFile? RecordedAudioFile { get; set; }
        public IFormFile? RecordedVideoFile { get; set; }
    }

    public class TRProcessCheckpointDTO
    {
        public int PrcId { get; set; }  // Process ID
        public string Remark { get; set; }  // Remark text
    }

    public class TRDgKitDTO
    {
        public string? PartCode { get; set; }
        public string? SerialNo { get; set; }
    }
}
