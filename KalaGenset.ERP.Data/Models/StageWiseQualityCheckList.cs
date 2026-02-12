using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class StageWiseQualityCheckList
{
    public int StageWiseQcid { get; set; }

    public string Pccode { get; set; } = null!;

    public string StageName { get; set; } = null!;

    public decimal FromKva { get; set; }

    public decimal ToKva { get; set; }

    public bool IsActive { get; set; }

    public bool IsAuth { get; set; }

    public bool IsDiscard { get; set; }

    public string? CheckerAuthRemark { get; set; }

    public string? MakerRemark { get; set; }

    public virtual ICollection<StageWiseQualityCheckListDetail> StageWiseQualityCheckListDetails { get; set; } = new List<StageWiseQualityCheckListDetail>();
}
