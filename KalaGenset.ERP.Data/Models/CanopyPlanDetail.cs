using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class CanopyPlanDetail
{
    public string Cpcode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public int SrNo { get; set; }

    public string Partcode { get; set; } = null!;

    public string BomCode { get; set; } = null!;

    public string PartCodeWop { get; set; } = null!;

    public double Qty { get; set; }

    public string? PlanCode { get; set; }

    public DateTime? PlanDate { get; set; }

    public double? DayPlanQty { get; set; }

    public double CpyWopQty { get; set; }

    public string CpyWopStatus { get; set; } = null!;

    public double CpyWipQty { get; set; }

    public string CpyWipStatus { get; set; } = null!;

    public string NestingLockStatus { get; set; } = null!;

    public string? ShiftType { get; set; }

    public bool Checker1 { get; set; }
}
