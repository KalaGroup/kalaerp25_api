using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Menu
{
    public int Id { get; set; }

    public string MenuName { get; set; } = null!;

    public string OrgName { get; set; } = null!;
}
