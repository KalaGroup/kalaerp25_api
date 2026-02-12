using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class UserRole
{
    public int UserRoleId { get; set; }

    public int EmpId { get; set; }

    public int RoleId { get; set; }

    public int Cid { get; set; }

    public int Pcid { get; set; }

    public string? EmpCode { get; set; }
}
