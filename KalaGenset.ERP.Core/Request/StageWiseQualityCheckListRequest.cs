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

    // Update payload: carries the master id, full current item list (with
    // optional ids for existing rows), and the ids of rows the user explicitly
    // removed during the edit so the backend can hard-delete them.
    public class UpdateStageWiseQualityCheckListRequest
    {
        public int stageWiseQcid { get; set; }
        public string makerRemark { get; set; }
        public List<UpdateStageWiseQualityCheckListDetailsRequest> checkpointItems { get; set; }
        public List<int> deletedItemIds { get; set; } = new List<int>();
    }

    public class UpdateStageWiseQualityCheckListDetailsRequest
    {
        // 0 / null => new row to be inserted; > 0 => existing row to be updated.
        public int? stageWiseQcdetailId { get; set; }
        public int srNo { get; set; }
        public string subAssemblyPart { get; set; }
        public string qualityProcessCheckpoint { get; set; }
        public string specification { get; set; }
        public string observation { get; set; }
        public string ok_nok { get; set; }
    }
}
