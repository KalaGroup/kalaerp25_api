using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class RoleProfitCenterMapping
{
    public int RoleId { get; set; }

    public int Pcid { get; set; }

    public int Cid { get; set; }

    public bool IsActive { get; set; }
}
