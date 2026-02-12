using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class AlternatorDetail
{
    public int Id { get; set; }

    public string QrsrNo { get; set; } = null!;

    public string? Trstatus { get; set; }

    public string? AltDesc { get; set; }

    public string? AltPart { get; set; }

    public string? Stock { get; set; }
}
