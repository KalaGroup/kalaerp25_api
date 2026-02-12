using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PageMst
{
    public int PageId { get; set; }

    public string PageName { get; set; } = null!;

    public bool IsActive { get; set; }
}
