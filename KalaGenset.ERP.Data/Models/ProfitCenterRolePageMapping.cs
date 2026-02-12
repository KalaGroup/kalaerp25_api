using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ProfitCenterRolePageMapping
{
    public int PrpageMappingId { get; set; }

    public int Pcid { get; set; }

    public int RoleId { get; set; }

    public int PageId { get; set; }

    public bool PermissionStatus { get; set; }

    public virtual PageMst Page { get; set; } = null!;

    public virtual ProfitCenter Pc { get; set; } = null!;

    public virtual RoleMst Role { get; set; } = null!;
}
