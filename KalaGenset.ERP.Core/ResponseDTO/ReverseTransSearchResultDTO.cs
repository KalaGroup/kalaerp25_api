using System;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class ReverseTransSearchResultDTO
    {
        public string? Stage4Code { get; set; }
        public string? TRCode { get; set; }
        public int SelectR { get; set; }
        public string? KVA { get; set; }
        public string? Phase { get; set; }
        public string? Model { get; set; }
        public string? Panel { get; set; }
        public string? EngSrNo { get; set; }
        public string? AltSrno { get; set; }
        public string? CpySrno { get; set; }
        public string? BatSrNo { get; set; }
        public string? Bat2SrNo { get; set; }
        public string? Bat3SrNo { get; set; }
        public string? Bat4SrNo { get; set; }
        public string? CPSrNo { get; set; }
        public string? CP2SrNo { get; set; }
        public string? KRMSrNo { get; set; }
        public string? Partcode { get; set; }
        public string? JobCode { get; set; }
        public string? J2Priority { get; set; }
        public DateTime? Dt { get; set; }
        public string? JobCard1 { get; set; }
        public string? PanelType { get; set; }
    }
}
