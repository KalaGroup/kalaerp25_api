using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class GetMaxSerialNo
{
    public string CompCode { get; set; } = null!;

    public string Yr { get; set; } = null!;

    public string Prefix { get; set; } = null!;

    public int MaxValue { get; set; }
}
