using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class SupplierPriceListChanged
{
    public string EmployeeCode { get; set; } = null!;

    public DateTime ChangeDateTime { get; set; }

    public string SupplierCode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public double Rate { get; set; }

    public int CostingCode { get; set; }

    public bool KalaToBio { get; set; }
}
