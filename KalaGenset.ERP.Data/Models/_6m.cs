using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class _6m
{
    public int Id { get; set; }

    public DateTime Dt { get; set; }

    public string Name { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public bool Active { get; set; }
}
