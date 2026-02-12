using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Stockwip
{
    public string FromProfitCenterCode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public string IssueCode { get; set; } = null!;

    public DateTime? IssueDate { get; set; }

    public double IssueQty { get; set; }

    public string ToProfitCenterCode { get; set; } = null!;

    public string ReceivedCode { get; set; } = null!;

    public DateTime? ReceivedDate { get; set; }

    public double ReceivedQty { get; set; }

    public int StockType { get; set; }

    public int PanelTypeId { get; set; }

    public string? StageName { get; set; }

    public string? LotNo { get; set; }

    public string? Grade { get; set; }
}
