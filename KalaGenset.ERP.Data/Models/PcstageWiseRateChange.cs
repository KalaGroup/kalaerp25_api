using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PcstageWiseRateChange
{
    public int Id { get; set; }

    public DateTime Dt { get; set; }

    public string Pccode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public string StageName { get; set; } = null!;

    public string JobCode { get; set; } = null!;

    public double Rate { get; set; }

    public string? PccodeAct { get; set; }
}
