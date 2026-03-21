using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ProductWip
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = null!;

    public string FromPccode { get; set; } = null!;

    public string IssueCode { get; set; } = null!;

    public DateTime? IssueDate { get; set; }

    public double IssueQty { get; set; }

    public string ToPccode { get; set; } = null!;

    public string ReceivedCode { get; set; } = null!;

    public DateTime? ReceivedDate { get; set; }

    public double ReceivedQty { get; set; }

    public int StockType { get; set; }
}
