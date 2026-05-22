using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request
{
    public class UpdateStageWiseQualityCheckListRequest
    {
        public int stageWiseQcid { get; set; }
        public string? makerRemark { get; set; }

        // Full checkpoint list as the user sees it after edits. Rows with
        // `stageWiseQcdetailId == null` are NEW (INSERT); rows with a value
        // are EXISTING (UPDATE in place).
        public List<UpdateStageWiseQualityCheckListDetailRequest> checkpointItems { get; set; }
            = new List<UpdateStageWiseQualityCheckListDetailRequest>();

        // Detail PKs the user removed during edit — these get hard-deleted.
        public List<int> deletedItemIds { get; set; } = new List<int>();
    }

    public class UpdateStageWiseQualityCheckListDetailRequest
    {
        public int? stageWiseQcdetailId { get; set; }
        public int srNo { get; set; }
        public string? subAssemblyPart { get; set; }
        public string? qualityProcessCheckpoint { get; set; }
        public string? specification { get; set; }
        public string? observation { get; set; }
        public string? ok_nok { get; set; }
    }
}
