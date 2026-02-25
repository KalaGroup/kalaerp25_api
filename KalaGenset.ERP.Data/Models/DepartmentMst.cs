using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class DepartmentMst
{
    public int Dcode { get; set; }

    public DateTime Dt { get; set; }

    public string Dname { get; set; } = null!;

    public string AliaseDname { get; set; } = null!;

    public int DivisionId { get; set; }

    public string Remark { get; set; } = null!;

    public bool ParentKit { get; set; }

    public bool Active { get; set; }

    public bool Discard { get; set; }

    public bool Auth { get; set; }
}
