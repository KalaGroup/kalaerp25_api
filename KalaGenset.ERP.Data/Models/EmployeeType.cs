using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class EmployeeType
{
    public string Etid { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Etname { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public bool Active { get; set; }

    public bool Auth { get; set; }

    public bool Status { get; set; }

    public bool KalaToBio { get; set; }
}
