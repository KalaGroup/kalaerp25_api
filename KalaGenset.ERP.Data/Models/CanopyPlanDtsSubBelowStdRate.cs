using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class CanopyPlanDtsSubBelowStdRate
{
    public string Cpcode { get; set; } = null!;

    public string CpyPartCode { get; set; } = null!;

    public int SrNo { get; set; }

    public string Partcode { get; set; } = null!;

    public int Cpqty { get; set; }

    public double Rate { get; set; }

    public int Strokes { get; set; }

    public string? CompCode { get; set; }

    public string? CatId { get; set; }
}
