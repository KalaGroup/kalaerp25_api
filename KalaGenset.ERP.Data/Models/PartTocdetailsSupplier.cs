using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PartTocdetailsSupplier
{
    public int Id { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public string SuppCode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public double Rate { get; set; }

    public double Rlt { get; set; }

    public double Cons6M { get; set; }

    public double ConsLog { get; set; }

    public double Fos { get; set; }

    public double Moq { get; set; }

    public double Poper { get; set; }

    public double StockPer { get; set; }

    public string Postatus { get; set; } = null!;

    public string TmatType { get; set; } = null!;

    public string Potype { get; set; } = null!;

    public string PcostCode { get; set; } = null!;

    public string PcostUomcode { get; set; } = null!;

    public string ForPccode { get; set; } = null!;

    public string Product { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public bool Active { get; set; }

    public bool Auth { get; set; }
}
