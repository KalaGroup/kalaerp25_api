using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class QualityProcessCheckerDg
{
    public int QualityProcessCheckerDgid { get; set; }

    public string? StageName { get; set; }

    public string? JobCode { get; set; }

    public string? PartCode { get; set; }

    public string? QualityStatus { get; set; }

    public int? Jpriority { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDiscard { get; set; }

    public virtual ICollection<QualityProcessCheckerDetailsDg> QualityProcessCheckerDetailsDgs { get; set; } = new List<QualityProcessCheckerDetailsDg>();
}
