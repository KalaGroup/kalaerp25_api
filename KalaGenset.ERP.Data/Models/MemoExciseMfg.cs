using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class MemoExciseMfg
{
    public string Mecode { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string FromProfitCenter { get; set; } = null!;

    public string ToProfitCenter { get; set; } = null!;

    public int TallyHeadCode { get; set; }

    public string ConsigneeCode { get; set; } = null!;

    public string Tmcode { get; set; } = null!;

    public string Pono { get; set; } = null!;

    public DateTime? Podate { get; set; }

    public string Mfgdino { get; set; } = null!;

    public string VehicleNo { get; set; } = null!;

    public double WtPerUt { get; set; }

    public double SqftPerUt { get; set; }

    public string? CurrencyCode { get; set; }

    public double? CurrencyRate { get; set; }

    public bool Discard { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string AuthRemark1 { get; set; } = null!;

    public string AuthRemark2 { get; set; } = null!;

    public string AuthRemark3 { get; set; } = null!;

    public string Mestatus { get; set; } = null!;

    public string Opstatus { get; set; } = null!;

    /// <summary>
    /// For Rejection memo of mfg
    /// </summary>
    public string Metype { get; set; } = null!;

    /// <summary>
    /// 0-Normal,1-Raw,2-Process
    /// </summary>
    public int StockType { get; set; }

    public string OnAcParty { get; set; } = null!;

    public string PortOfLoading { get; set; } = null!;

    public string PortOfDischarge { get; set; } = null!;

    public string VesselFlightNo { get; set; } = null!;

    public string DeliveryTerms { get; set; } = null!;

    public string TransportRoute { get; set; } = null!;

    public string ExpStatus { get; set; } = null!;

    public string VehicleType { get; set; } = null!;

    public string Viostatus { get; set; } = null!;

    public string ExportStatus { get; set; } = null!;

    public bool Auth { get; set; }

    public bool Auth1 { get; set; }

    public bool Auth2 { get; set; }

    public bool Active { get; set; }

    public string Mmtfcode { get; set; } = null!;

    public string MtfscanStatus { get; set; } = null!;

    public string FromProfitCenterAct { get; set; } = null!;

    public string ToProfitCenterAct { get; set; } = null!;
}
