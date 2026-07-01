using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class KaizenSheetHistory
{
    public int Id { get; set; }

    public int KaizenSheetMasterId { get; set; }

    public string? KaizenSheetNo { get; set; }

    public string Action { get; set; } = null!;

    public string? Remark { get; set; }

    public string? ActionBy { get; set; }

    public string? ActionByCode { get; set; }

    public DateTime ActionOn { get; set; }
}
