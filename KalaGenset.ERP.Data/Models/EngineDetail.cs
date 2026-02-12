using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class EngineDetail
{
    public int Id { get; set; }

    public string QrsrNo { get; set; } = null!;

    public string? EngDesc { get; set; }

    public string? EngCode { get; set; }

    public string? Stock { get; set; }
}
