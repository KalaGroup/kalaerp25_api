using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class InvoiceDealer
{
    public string Invid { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public DateTime SubDt { get; set; }

    public int Invcode { get; set; }

    public string MemoCode { get; set; } = null!;

    public string IndentorCode { get; set; } = null!;

    public string Tmcode { get; set; } = null!;

    public string Giircode { get; set; } = null!;

    /// <summary>
    /// For Bhilad
    /// </summary>
    public double Dqty { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime? IssTime { get; set; }

    public DateTime RemovalDate { get; set; }

    public DateTime? RemTime { get; set; }

    public double Cgstper { get; set; }

    public double Sgstper { get; set; }

    public double Igstper { get; set; }

    public double Vat { get; set; }

    public double Cst { get; set; }

    public double Tcs { get; set; }

    public string CgstinWords { get; set; } = null!;

    public string SgstinWords { get; set; } = null!;

    public string IgstinWords { get; set; } = null!;

    public string ExciseAmount { get; set; } = null!;

    public string InvoiceTotal { get; set; } = null!;

    public string ExciseAmountS { get; set; } = null!;

    public string IrecStatus { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string Invstatus { get; set; } = null!;

    public string GoutStatus { get; set; } = null!;

    public string CreditStatus { get; set; } = null!;

    public string DebitStatus { get; set; } = null!;

    public bool StockTransferStatus { get; set; }

    public string Remark { get; set; } = null!;

    public string TallyNarration { get; set; } = null!;

    public string TransportName { get; set; } = null!;

    public string TmobileNo { get; set; } = null!;

    public string VehicleNo { get; set; } = null!;

    public string Viostatus { get; set; } = null!;

    public string Jvstatus { get; set; } = null!;

    public string RejStatus { get; set; } = null!;

    public DateTime? DelDt { get; set; }

    public bool PrintCount { get; set; }

    public bool Active { get; set; }

    public bool Auth { get; set; }

    public bool Status { get; set; }

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

    public string InvDigitalSignAttachment { get; set; } = null!;

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

    public string FormNo { get; set; } = null!;

    public DateTime? FormDt { get; set; }

    public double FormAmt { get; set; }

    public DateTime? CustAckDt { get; set; }

    public string CustAckStatus { get; set; } = null!;

    public string CustAckName { get; set; } = null!;

    public string CustAckSign { get; set; } = null!;

    public string GateOut { get; set; } = null!;

    public DateTime? GateOutTime { get; set; }

    public DateTime? PhysicalDispDt { get; set; }

    public DateTime? ReceiptAtSiteDt { get; set; }

    public DateTime? ReceieptOfPoddt { get; set; }

    public string EInvIrnno { get; set; } = null!;

    public string EInvAckNo { get; set; } = null!;

    public DateTime? EInvAckDt { get; set; }

    public string EWayBillNo { get; set; } = null!;

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

    public DateTime? ElectBillDt { get; set; }

    public DateTime? DocSubtoElInspecDt { get; set; }

    public DateTime? FinalApprovalDt { get; set; }

    public DateTime? BillSubCustDt3 { get; set; }

    public DateTime? BillAckCustDt3 { get; set; }

    public DateTime? PayCollectionDt3 { get; set; }

    public string? Pwdremark { get; set; }

    public string? Itcremark { get; set; }
}
