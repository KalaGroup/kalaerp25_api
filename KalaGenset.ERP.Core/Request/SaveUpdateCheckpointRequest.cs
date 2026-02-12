using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request
{
    public class SaveUpdateCheckpointRequest
    {
        public int StageWiseQcid { get; set; }
        public List<CheckpointItemDto> CheckpointItems { get; set; }
        public string CheckerRemark { get; set; }
    }

    public class CheckpointItemDto
    {
        public int SrNo { get; set; }
        public string SubAssemblyPart { get; set; }
        public string Checkpoint { get; set; }
        public string Specification { get; set; }
        public string Observation { get; set; }
        public string Ok { get; set; }
    }
}
