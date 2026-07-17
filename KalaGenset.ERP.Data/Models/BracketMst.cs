using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class BracketMst
{
    public int Bid { get; set; }

    public DateTime Dt { get; set; }

    public int BracketSrNo { get; set; }

    public string BracketName { get; set; } = null!;

    public double BracketAmount { get; set; }

    public double FromKva { get; set; }

    public string ProductId { get; set; } = null!;

    public double ToKva { get; set; }

    public double BracketPointage { get; set; }

    public double BracketPointageAopSales { get; set; }

    public string Remark { get; set; } = null!;

    public bool Active { get; set; }
}
