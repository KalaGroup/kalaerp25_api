using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Uom
{
    public string Uid { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Uname { get; set; } = null!;

    public string AliseName { get; set; } = null!;

    public string UnitTypeId { get; set; } = null!;

    public string Remark { get; set; } = null!;

    /// <summary>
    /// 1 - Active
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// 1 - A 0- UA
    /// </summary>
    public bool Auth { get; set; }

    public bool Status { get; set; }

    public bool KalaToBio { get; set; }
}
