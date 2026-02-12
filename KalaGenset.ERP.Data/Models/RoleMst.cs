using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class RoleMst
{
    public int RoleId { get; set; }

    public string? RoleName { get; set; }

    public bool IsActive { get; set; }
}
