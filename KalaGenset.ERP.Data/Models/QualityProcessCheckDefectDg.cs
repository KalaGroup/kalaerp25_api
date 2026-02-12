using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class QualityProcessCheckDefectDg
{
    public int QpcdefectDgid { get; set; }

    public int QualityProcessCheckerDgid { get; set; }

    public string? Pccode { get; set; }

    public string? Qdccode { get; set; }

    public double? ActualValue { get; set; }

    public double? Tolerance { get; set; }

    public string? Instrument { get; set; }

    public double? Rate { get; set; }

    public double? FromRange { get; set; }

    public double? ToRange { get; set; }

    public virtual QualityProcessCheckerDg QualityProcessCheckerDg { get; set; } = null!;
}
