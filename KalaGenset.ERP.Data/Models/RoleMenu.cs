using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class RoleMenu
{
    public int RoleMenuId { get; set; }

    public int RoleId { get; set; }

    public int MenuId { get; set; }

    public bool IsActive { get; set; }

    public string? EmpCode { get; set; }
}
