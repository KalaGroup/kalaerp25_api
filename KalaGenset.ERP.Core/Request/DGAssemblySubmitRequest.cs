using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.RequestDTO
{
    public class DGAssemblySubmitRequest
    {
        public string? JBCode { get; set; }
        public int StageNo { get; set; }
        public string? ProductCode { get; set; }
        public string? EngPartCode { get; set; }
        public string? EngSrNo { get; set; }
        public string? AltPartcode { get; set; }
        public string? AltSrno { get; set; }
        public string? CpyPartcode { get; set; }
        public string? CpySrno { get; set; }
        public string? BatPartcode { get; set; }
        public string? BatSrno { get; set; }
        public string? Bat2Partcode { get; set; }
        public string? Bat2Srno { get; set; }
        public string? Bat3Srno { get; set; }
        public string? Bat3Partcode { get; set; }
        public string? Bat4Srno { get; set; }
        public string? Bat4Partcode { get; set; }
        public int? QA6M { get; set; }
        public string? PrcStatus { get; set; }
        //public string? PrcChkDts { get; set; }
        public string? EngPlay { get; set; }
        public string? PCCode { get; set; }
        public IFormFile? RecordedAudioFile { get; set; }
        public IFormFile? RecordedVideoFile { get; set; }
        public List<ProcessCheckpointDTO>? PrcChkDts { get; set; }
        public string? CPType { get; set; }
        public string? CPPartcode { get; set; }
        public string? CPSrno { get; set; }
        public string? CP2Partcode { get; set; }
        public string? CP2Srno { get; set; }
        public string? KRMPartcode { get; set; }
        public string? KRMSrno { get; set; }
        public string? PfbCode { get; set; }
      //  public string? PrcDts { get; set; }
        public List<DgKitDTO>? DGKitDetails { get; set; }
        public string? Remark { get; set; }
        public string? JobCardCode { get; set; }
    }

    public class ProcessCheckpointDTO
    {
        public int PrcId { get; set; }  // Process ID
        public string Remark { get; set; }  // Remark text
    }

    public class DgKitDTO
    { 
      public string? PartCode { get; set; }
      public string? StockQty { get; set; }
      public string? TotalQty { get; set; }
      public string? PFBRate { get; set; }
      public string? KITQty { get; set; }  
    }
}
