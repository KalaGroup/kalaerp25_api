using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PurchaseCosting
{
    /// <summary>
    /// Purchase Costing
    /// </summary>
    public int Pccode { get; set; }

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string SupplierCode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public string Uomcode { get; set; } = null!;

    public double Pom { get; set; }

    public double Pol { get; set; }

    public double MatCost { get; set; }

    public double LabCost { get; set; }

    public double TotalCost { get; set; }

    public double AmtA { get; set; }

    public double AmtB { get; set; }

    public double AmtC { get; set; }

    public double AmtD { get; set; }

    public double Edperc { get; set; }

    public double Cessperc { get; set; }

    public double HcessPerc { get; set; }

    public double Vatperc { get; set; }

    public double TransCharges { get; set; }

    public string RateSelected { get; set; } = null!;

    public string OrderValidSelected { get; set; } = null!;

    public double ValidQty { get; set; }

    public DateTime ValidDate { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public double CurrencyRate { get; set; }

    public double ExCurrencyRate { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string AuthRemark { get; set; } = null!;

    public bool Discard { get; set; }

    public bool Active { get; set; }

    public bool Auth { get; set; }

    public bool KalaToBio { get; set; }
}
