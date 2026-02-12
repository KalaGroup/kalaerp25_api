using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class DispatchInstruction
{
    public string Dino { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public bool ChakanWarehouse { get; set; }

    public string Wfdid { get; set; } = null!;

    public string Ppwcode { get; set; } = null!;

    public string Mofcode { get; set; } = null!;

    public int QtytoDispatch { get; set; }

    public double DispatchAmount { get; set; }

    public string ConsigneeCode { get; set; } = null!;

    public string IndentorCode { get; set; } = null!;

    public double Octri { get; set; }

    public string OctriBy { get; set; } = null!;

    public double Transport { get; set; }

    public string TransportBy { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string BranchCode { get; set; } = null!;

    public string BranchCodeOld { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string ReverseRemark { get; set; } = null!;

    public string AuthRemark1 { get; set; } = null!;

    public string AuthRemark2 { get; set; } = null!;

    public string AuthRemarkChkWh { get; set; } = null!;

    public string CrLimitAuthRemark { get; set; } = null!;

    public double CrLimitAmt { get; set; }

    public double ClosingAmt { get; set; }

    public double UnClearClosingAmt { get; set; }

    public double DidoneInvPenAmt { get; set; }

    public double OutsDiff { get; set; }

    public string Invstatus { get; set; } = null!;

    public string Lcno { get; set; } = null!;

    public DateTime? Lcdt { get; set; }

    public string? Lctype { get; set; }

    public string LctypeDesc { get; set; } = null!;

    public double OpeningBal { get; set; }

    public DateTime? LcexpiryDt { get; set; }

    public string Lcdesc { get; set; } = null!;

    public string OnAcParty { get; set; } = null!;

    public string DescriptionManual { get; set; } = null!;

    public double NetWeight { get; set; }

    public double GrossWeight { get; set; }

    public string PortOfLoading { get; set; } = null!;

    public string PortOfDischarge { get; set; } = null!;

    public string VesselFlightNo { get; set; } = null!;

    public string DeliveryTerms { get; set; } = null!;

    public string TransportRoute { get; set; } = null!;

    public string NotificationNo { get; set; } = null!;

    public DateTime? NotificationDate { get; set; }

    public string NotificationAvailed { get; set; } = null!;

    public string ExportRemark { get; set; } = null!;

    public string TaxationBy { get; set; } = null!;

    public string Pdirstatus { get; set; } = null!;

    public string BinvStatus { get; set; } = null!;

    public string Pslstatus { get; set; } = null!;

    public bool Auth1 { get; set; }

    public bool Auth2 { get; set; }

    public bool AuthChkWh { get; set; }

    public bool CrLimitAuth { get; set; }

    public string TrallocationStatus { get; set; } = null!;

    public string TrallocationRemark { get; set; } = null!;

    public int MofschId { get; set; }

    public bool Active { get; set; }

    public bool Rdiscard { get; set; }

    public bool Status { get; set; }
}
