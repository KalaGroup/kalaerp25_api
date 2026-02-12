using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PanelTypeKit
{
    public int PanelTypeId { get; set; }

    public DateTime Dt { get; set; }

    public double DgkVa { get; set; }

    public int Dgphase { get; set; }

    public string? Dgmodel { get; set; }

    public string Dgtype { get; set; } = null!;

    public string PanelTypeName { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public bool Active { get; set; }

    public bool Auth { get; set; }
}
