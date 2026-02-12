using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Mof
{
    public string Mofcode { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string DivertMoffrom { get; set; } = null!;

    public string Qtnno { get; set; } = null!;

    public string Rtcno { get; set; } = null!;

    public string CategoryId { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public string ProductMob { get; set; } = null!;

    public int TallyHeadCode { get; set; }

    public string OnAccountOf { get; set; } = null!;

    public string CustomerCode { get; set; } = null!;

    public string IndentorCode { get; set; } = null!;

    public string OrderBy { get; set; } = null!;

    public string Accessories { get; set; } = null!;

    public bool ToolKit { get; set; }

    public int Qty { get; set; }

    public int PlanQty { get; set; }

    public int BalQty { get; set; }

    public int DivertedQty { get; set; }

    public string? IndentId { get; set; }

    public string Pono { get; set; } = null!;

    public DateTime? Podate { get; set; }

    public DateTime? TentativeDt { get; set; }

    public DateTime? SiteVisitDate { get; set; }

    public DateTime? SiteReadyDate { get; set; }

    public DateTime? WorkOrderDt { get; set; }

    public string? WorkOrderNo { get; set; }

    public double Poamount { get; set; }

    public string? PulseId { get; set; }

    public string? PulseSono { get; set; }

    public DateTime? PulseSodate { get; set; }

    public string Cpbomcode { get; set; } = null!;

    public double Cpbomamt { get; set; }

    public string Bomcode { get; set; } = null!;

    public double Mwt { get; set; }

    public double Msqft { get; set; }

    public double Mhrwt { get; set; }

    public double Mcrwt { get; set; }

    public double Mhramt { get; set; }

    public double Mcramt { get; set; }

    public double Bomprice { get; set; }

    public double TransferPrice { get; set; }

    public double Krmamt { get; set; }

    public double CpanelAmt { get; set; }

    /// <summary>
    /// 0
    /// </summary>
    public double MktPricelist { get; set; }

    public double MinPrice { get; set; }

    public double ProductPrice { get; set; }

    public double BasicPrice { get; set; }

    public double Nfakoel { get; set; }

    public double Nfakala { get; set; }

    public double Nfaother { get; set; }

    public double Cgst { get; set; }

    public double Sgst { get; set; }

    public double Igst { get; set; }

    public double ExciseDuty { get; set; }

    public double Cess { get; set; }

    public double HedCess { get; set; }

    public double Vat { get; set; }

    public double Cst { get; set; }

    public double ServiceTax { get; set; }

    public double EntryTax { get; set; }

    public double Other { get; set; }

    public double Tcs { get; set; }

    public double SurCharge { get; set; }

    public double Frieght { get; set; }

    public double LocalTransport { get; set; }

    public double Octri { get; set; }

    public string OctriBy { get; set; } = null!;

    public double SupTransportRate { get; set; }

    public double Transport { get; set; }

    public string TransportBy { get; set; } = null!;

    public double Unloading { get; set; }

    public string UnloadingBy { get; set; } = null!;

    public double Orc { get; set; }

    public string Orcby { get; set; } = null!;

    public int Orcagent { get; set; }

    public double Dginspection { get; set; }

    public string DginspectionBy { get; set; } = null!;

    public double InternalCommission { get; set; }

    public string PayModeCode { get; set; } = null!;

    public string PayDaysCode { get; set; } = null!;

    public string DispatchFromCode { get; set; } = null!;

    public string Destination { get; set; } = null!;

    public double DestinationAmt { get; set; }

    public double TransportSupRate { get; set; }

    public string FormType { get; set; } = null!;

    public double Insurance { get; set; }

    public string InsuranceBy { get; set; } = null!;

    public double Packing { get; set; }

    public string PackingBy { get; set; } = null!;

    public string CommissioningCode { get; set; } = null!;

    public double CommMinAmt { get; set; }

    public double CommAmt { get; set; }

    public string ServiceCode { get; set; } = null!;

    public double InstOfferAmt { get; set; }

    public string ServiceDealerCode { get; set; } = null!;

    public string Mofstatus { get; set; } = null!;

    /// <summary>
    /// For Dispatch Instructions If Qty Is complete Status Will Change To C
    /// </summary>
    public string Dspinst { get; set; } = null!;

    public string RecStatus { get; set; } = null!;

    public string DivertStatus { get; set; } = null!;

    public string Moftype { get; set; } = null!;

    public string ExportStatus { get; set; } = null!;

    public string Orcstatus { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string BranchCode { get; set; } = null!;

    public string BranchCodeOld { get; set; } = null!;

    public string CurrencyCode { get; set; } = null!;

    public double CurrencyRate { get; set; }

    public string Ppwstatus { get; set; } = null!;

    public string Ppmstatus { get; set; } = null!;

    public string MinvStatus { get; set; } = null!;

    public string Pfistatus { get; set; } = null!;

    public string MemoStatus { get; set; } = null!;

    public string TocallocateStatus { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string AddPartRemark { get; set; } = null!;

    public string AuthRemark { get; set; } = null!;

    public string AuthRemark1 { get; set; } = null!;

    public string AuthRemarkAc { get; set; } = null!;

    public string AuthRemark2 { get; set; } = null!;

    public string AuthRemarkService { get; set; } = null!;

    public string Qtype { get; set; } = null!;

    public string MofFirstlevel { get; set; } = null!;

    public bool Discard { get; set; }

    public bool TempStatus { get; set; }

    public string TaxationBy { get; set; } = null!;

    public bool SiteStatus { get; set; }

    public string? SiteStatusRemark { get; set; }

    public string? MktschRemark { get; set; }

    public string? MktgreSchStatus { get; set; }

    public string? PrdschRemark { get; set; }

    public string? AccPayRemark { get; set; }

    public bool MailStatus { get; set; }

    public bool MailStatusNfa { get; set; }

    public string Nfano { get; set; } = null!;

    public bool SaleTransit { get; set; }

    public string Dono { get; set; } = null!;

    public DateTime? Dodate { get; set; }

    public string DobankName { get; set; } = null!;

    public string DobankAddress { get; set; } = null!;

    public double DonetDisbursed { get; set; }

    public double Doadvance { get; set; }

    public string? ProjectName { get; set; }

    public DateTime? CommencementDt { get; set; }

    public string? AdvPayGuaRefNo { get; set; }

    public DateTime? CompletionDt { get; set; }

    public double? AmountRo { get; set; }

    public string? CompletionTime { get; set; }

    public string OmanBankCode { get; set; } = null!;

    public DateTime? RevComplDt { get; set; }

    public DateTime? ExpiryDtofGua { get; set; }

    public DateTime? ActCompletionDt { get; set; }

    public string? StartMaintPeriod { get; set; }

    public bool Active { get; set; }

    public bool Auth { get; set; }

    public bool Auth1 { get; set; }

    public bool AuthAc { get; set; }

    public bool AuthService { get; set; }

    public bool Auth2 { get; set; }

    public string Auth2HoldRemark { get; set; } = null!;

    public bool AuthNfa { get; set; }

    public string AuthNfaremark { get; set; } = null!;

    public string NfaholdStatus { get; set; } = null!;

    public string SectorCode { get; set; } = null!;

    public string InvoicePartDesc { get; set; } = null!;

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public Guid MsreplTranVersion { get; set; }
}
