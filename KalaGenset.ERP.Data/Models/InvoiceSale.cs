using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class InvoiceSale
{
    public string Invid { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public DateTime SubDt { get; set; }

    public int Invcode { get; set; }

    public string Mecode { get; set; } = null!;

    public string Sofcode { get; set; } = null!;

    public string Dicode { get; set; } = null!;

    public string CustomerCode { get; set; } = null!;

    public string IndentorCode { get; set; } = null!;

    public int Qty { get; set; }

    public string Pono { get; set; } = null!;

    public DateTime? PoDate { get; set; }

    public string ChapterNo { get; set; } = null!;

    public string Tmcode { get; set; } = null!;

    public string ActOff { get; set; } = null!;

    public string TransportName { get; set; } = null!;

    public string TmobileNo { get; set; } = null!;

    /// <summary>
    /// RR - Vehicle Registerd NR - Not registered
    /// </summary>
    public string VehicleType { get; set; } = null!;

    public string VehicleNo { get; set; } = null!;

    public string NatureOfRemoval { get; set; } = null!;

    public DateTime IssueDate { get; set; }

    public DateTime IssueTime { get; set; }

    public DateTime RemovalDate { get; set; }

    public DateTime RemovalTime { get; set; }

    public double Edper { get; set; }

    public double EcessPer { get; set; }

    public double HecessPer { get; set; }

    public double VatPer { get; set; }

    public double Cstper { get; set; }

    public double ServiceTaxPer { get; set; }

    public double Tcsper { get; set; }

    public double Other { get; set; }

    public double SurCharge { get; set; }

    public double Cgstper { get; set; }

    public double Sgstper { get; set; }

    public double Igstper { get; set; }

    public string CgstinWords { get; set; } = null!;

    public string SgstinWords { get; set; } = null!;

    public string IgstinWords { get; set; } = null!;

    public string ExciseDutyInWords { get; set; } = null!;

    public string Cessinwords { get; set; } = null!;

    public string Hedcessinwords { get; set; } = null!;

    public string AmountInWords { get; set; } = null!;

    public double Frieght { get; set; }

    public int PrintCount { get; set; }

    public string Invstatus { get; set; } = null!;

    public string GoutStatus { get; set; } = null!;

    public string CreditStatus { get; set; } = null!;

    public string DebitStatus { get; set; } = null!;

    public string IrecStatus { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string Invtype { get; set; } = null!;

    public string BasicInWords { get; set; } = null!;

    public string OnAcParty { get; set; } = null!;

    public string DescriptionManual { get; set; } = null!;

    public double NetWeight { get; set; }

    public double GrossWeight { get; set; }

    public string PortOfLoading { get; set; } = null!;

    public string PortOfDischarge { get; set; } = null!;

    public string VesselFlightNo { get; set; } = null!;

    public string DeliveryTerms { get; set; } = null!;

    public string TransportRoute { get; set; } = null!;

    public string Viostatus { get; set; } = null!;

    public bool StockTransferStatus { get; set; }

    public string Remark { get; set; } = null!;

    public string TallyNarration { get; set; } = null!;

    public string FormNo { get; set; } = null!;

    public DateTime? FormDt { get; set; }

    public double FormAmt { get; set; }

    public string FilePath { get; set; } = null!;

    public string FormRemark { get; set; } = null!;

    public string Jvstatus { get; set; } = null!;

    public string RejStatus { get; set; } = null!;

    public DateTime? DelDt { get; set; }

    public bool Active { get; set; }

    public bool Auth { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public bool KalaToBio { get; set; }

    public bool? MailStatusB { get; set; }

    public bool? MailStatusD { get; set; }

    public bool? MailStatus { get; set; }

    public bool MailStatusTransit { get; set; }

    public string VmarchStatus { get; set; } = null!;

    public DateTime? PhysicalDispatchDt { get; set; }

    public DateTime? UnloadingDt { get; set; }

    public DateTime? DeliveryAckCopySubDt { get; set; }

    public string DeliveryAckCopySubRemark { get; set; } = null!;

    public DateTime? BillSubmissionDt { get; set; }

    public string BillAckNo { get; set; } = null!;

    public DateTime? BillAckDt { get; set; }

    public string? BillAttachment { get; set; }

    public string? InvDigitalSignAttachment { get; set; }

    public string BillSubmissionRemark { get; set; } = null!;

    public string DgcbillNo { get; set; } = null!;

    public DateTime? DgcbillNoDt { get; set; }

    public double DgcbillAmount { get; set; }

    public string DgcbillRemark { get; set; } = null!;

    public DateTime? ExpectedPaymentDt { get; set; }

    public string ExpectedPaymentRemark { get; set; } = null!;

    public string Rtnno { get; set; } = null!;

    public DateTime? RtnnoDt { get; set; }

    public double RtnnoAmount { get; set; }

    public DateTime? RtnbillSubmissionDt { get; set; }

    public string RtnbillSubmissionRemark { get; set; } = null!;

    public DateTime? RtnnoPaymentDt { get; set; }

    public string RtnnoPaymentRemark { get; set; } = null!;

    public string CformPaymentRemark { get; set; } = null!;

    public DateTime? CustAckDt { get; set; }

    public string CustAckStatus { get; set; } = null!;

    public string CustAckName { get; set; } = null!;

    public string CustAckSign { get; set; } = null!;

    public double CustLatitude { get; set; }

    public double CustLongitude { get; set; }

    public string GateOut { get; set; } = null!;

    public DateTime? GateOutTime { get; set; }

    public DateTime? PhysicalDispDt { get; set; }

    public DateTime? ReceiptAtSiteDt { get; set; }

    public DateTime? ReceieptOfPoddt { get; set; }

    public string InvUom { get; set; } = null!;

    public string InvDesc { get; set; } = null!;

    public double MatAmt { get; set; }

    public double LabAmt { get; set; }

    public string EInvIrnno { get; set; } = null!;

    public string EInvAckNo { get; set; } = null!;

    public DateTime? EInvAckDt { get; set; }

    public string EWayBillNo { get; set; } = null!;

    public string PrcinvType { get; set; } = null!;

    public double InvBasicAmt { get; set; }

    public DateTime? HardCopyConfirmDt { get; set; }

    public DateTime? HardCopySubmitDt { get; set; }

    public DateTime? PhysicalCopySubmitDt { get; set; }

    public DateTime? BillSubBankDt1 { get; set; }

    public DateTime? BillSubErpdt { get; set; }

    public DateTime? PayCollectionDt1 { get; set; }

    public DateTime? InstallationDt { get; set; }

    public DateTime? CommisioningDt { get; set; }

    public DateTime? HandOverDate { get; set; }

    public DateTime? InstCommReportDt { get; set; }

    public DateTime? BoqsubDt { get; set; }

    public DateTime? ReceiptInstPodt { get; set; }

    public DateTime? BillSubCustDt2 { get; set; }

    public DateTime? BillAckCustDt2 { get; set; }

    public DateTime? PayCollectionDt2 { get; set; }

    public DateTime? DocPwdreadyDt { get; set; }

    public DateTime? CustComm4ElectBillDt { get; set; }

    public DateTime? ElectBillDt { get; set; }

    public DateTime? DocSubtoElInspecDt { get; set; }

    public DateTime? FinalApprovalDt { get; set; }

    public DateTime? BillSubCustDt3 { get; set; }

    public DateTime? BillAckCustDt3 { get; set; }

    public DateTime? PayCollectionDt3 { get; set; }

    public string? Pwdremark { get; set; }

    public string? Itcremark { get; set; }
}
