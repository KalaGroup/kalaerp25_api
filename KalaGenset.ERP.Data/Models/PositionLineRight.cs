using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PositionLineRight
{
    public int Plrid { get; set; }

    public string Prmcode { get; set; } = null!;

    public string LineWisePc { get; set; } = null!;

    public string Active { get; set; } = null!;

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }
}
