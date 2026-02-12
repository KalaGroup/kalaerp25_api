using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class RolePermission
{
    public int RolePermissionId { get; set; }

    public int Pcid { get; set; }

    public int? PageId { get; set; }

    public bool? Add { get; set; }

    public bool? Edit { get; set; }

    public bool? Delete { get; set; }

    public bool? Auth { get; set; }

    public bool? Export { get; set; }
}
