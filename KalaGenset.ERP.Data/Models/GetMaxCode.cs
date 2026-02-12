using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class GetMaxCode
{
    public string CompCode { get; set; } = null!;

    public string Yr { get; set; } = null!;

    public string Prefix { get; set; } = null!;

    public int MaxValue { get; set; }

    public string FormName { get; set; } = null!;

    public string TblName { get; set; } = null!;

    public string Remark { get; set; } = null!;
}
