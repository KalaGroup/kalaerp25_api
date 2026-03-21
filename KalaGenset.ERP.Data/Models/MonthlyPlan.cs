using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class MonthlyPlan
{
    public string PlanCode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string Month { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public int VersionNo { get; set; }

    public string Product { get; set; } = null!;

    public bool Auth { get; set; }

    public bool? Active { get; set; }

    public bool? Discard { get; set; }
}
