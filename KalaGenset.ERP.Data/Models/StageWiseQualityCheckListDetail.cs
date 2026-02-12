using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class StageWiseQualityCheckListDetail
{
    public int StageWiseQcid { get; set; }

    public int SrNo { get; set; }

    public string? SubAssemblyPart { get; set; }

    public string? QualityProcessCheckpoint { get; set; }

    public string? Specification { get; set; }

    public string? Observation { get; set; }

    public string? OkNok { get; set; }

    public virtual StageWiseQualityCheckList StageWiseQc { get; set; } = null!;
}
