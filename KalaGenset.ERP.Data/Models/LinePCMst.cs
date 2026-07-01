using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class LinePcmst
{
    public string LineWisePc { get; set; } = null!;

    public string LineDesc { get; set; } = null!;

    public string ParentDgPc { get; set; } = null!;

    public int? DivisionId { get; set; }

    public string Active { get; set; } = null!;

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }
}
