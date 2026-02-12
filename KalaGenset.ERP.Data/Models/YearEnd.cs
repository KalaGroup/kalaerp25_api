using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class YearEnd
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string SysVersion { get; set; } = null!;

    public string MarqueeText { get; set; } = null!;
}
