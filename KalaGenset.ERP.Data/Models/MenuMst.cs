using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class MenuMst
{
    public int MenuId { get; set; }

    public string MenuName { get; set; } = null!;

    public string? Routerlink { get; set; }
}
