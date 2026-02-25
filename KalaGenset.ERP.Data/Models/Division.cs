using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Division
{
    public int DivisionId { get; set; }

    public string DivisionCode { get; set; } = null!;

    public string Division1 { get; set; } = null!;

    public string DivisionShortName { get; set; } = null!;

    public int? DivisionParentId { get; set; }

    public string? DivisionMailId { get; set; }

    public string Remark { get; set; } = null!;

    public bool Active { get; set; }

    public bool Discard { get; set; }
}
