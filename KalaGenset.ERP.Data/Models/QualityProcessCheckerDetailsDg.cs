using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class QualityProcessCheckerDetailsDg
{
    public int QpcdetailsDgid { get; set; }

    public int QualityProcessCheckerDgid { get; set; }

    public long? StageWiseQcid { get; set; }

    public long? SrNo { get; set; }

    public string? CheckerRemark { get; set; }

    public string? OkNok { get; set; }

    public virtual QualityProcessCheckerDg QualityProcessCheckerDg { get; set; } = null!;
}
