using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class CanopyPlan
{
    public string Cpcode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string MaxSrNo { get; set; } = null!;

    public string Yr { get; set; } = null!;

    public DateTime FromDt { get; set; }

    public DateTime ToDt { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string PlanPccode { get; set; } = null!;

    public string CpyStatus { get; set; } = null!;

    public string HoldStatus { get; set; } = null!;

    public string PlanType { get; set; } = null!;

    public string PlanStatus { get; set; } = null!;

    public bool Checker1 { get; set; }

    public bool Active { get; set; }

    public bool Discard { get; set; }
}
