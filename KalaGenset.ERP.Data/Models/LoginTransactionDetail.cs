using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class LoginTransactionDetail
{
    public int Ltid { get; set; }

    public DateTime TransactionDtTime { get; set; }

    public string EmpId { get; set; } = null!;

    /// <summary>
    /// LedgerID
    /// </summary>
    public string TransactionFrom { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public string TransactionNo { get; set; } = null!;

    public string? AttendanceDt { get; set; }

    public string? CompanyCode { get; set; }

    public bool Status { get; set; }
}
