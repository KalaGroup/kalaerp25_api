using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class WorkStation
{
    public string Wkcode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string WorkStationName { get; set; } = null!;

    public string DeptCode { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public bool Active { get; set; }
}
