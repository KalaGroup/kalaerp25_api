using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class CalibrationMst
{
    public int InstrumentId { get; set; }

    public int CompanyId { get; set; }

    public string PartCode { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? IdNo { get; set; }

    public string? SrNo { get; set; }

    public string? Make { get; set; }

    public string? Range { get; set; }

    public string? Unit { get; set; }

    public string? Lc { get; set; }

    public string? Location { get; set; }

    public DateTime? CalDate { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsActive { get; set; }

    public bool IsDiscard { get; set; }

    public bool Auth { get; set; }

    public string? MakerRemark { get; set; }

    public string? CheckerRemark { get; set; }
}
