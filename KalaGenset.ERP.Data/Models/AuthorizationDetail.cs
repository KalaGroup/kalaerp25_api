using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class AuthorizationDetail
{
    public int Aid { get; set; }

    public DateTime AuthDateTime { get; set; }

    public string EmpId { get; set; } = null!;

    public string AuthForm { get; set; } = null!;

    public string TransactionNo { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public bool Status { get; set; }
}
