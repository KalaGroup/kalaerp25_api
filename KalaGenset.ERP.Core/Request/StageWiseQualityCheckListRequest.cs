namespace KalaGenset.ERP.Core.Request
{
    public class StageWiseQualityCheckListRequest
    {
        public string pcCode { get; set; }
        public string stageName { get; set; }
        public decimal fromKVA { get; set; }
        public decimal toKVA { get; set; }
        public string makerRemark { get; set; }
        public List<StageWiseQualityCheckListDetailsRequest> checkpointItems { get; set; }

    }

    public class StageWiseQualityCheckListDetailsRequest
    { 
        public int srNo { get; set; }
        public string subAssemblyPart { get; set; }
        public string qualityProcessCheckpoint { get; set; }
        public string specification { get; set; }
        public string observation { get; set; }
        public string ok_nok { get; set; }
    }
}
