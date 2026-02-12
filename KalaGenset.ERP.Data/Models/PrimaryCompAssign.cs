using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PrimaryCompAssign
{
    public string Pcacode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string MaxSrNo { get; set; } = null!;

    public string Yr { get; set; } = null!;

    public string CompNo { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string WarrantyStatus { get; set; } = null!;

    public string EmpCode { get; set; } = null!;

    public string KoelempCode { get; set; } = null!;

    public string ServiceNo { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public string CompType { get; set; } = null!;

    public string AssignByEmpCode { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string TechnicianCode { get; set; } = null!;

    public bool Active { get; set; }

    public bool Auth { get; set; }

    public bool Status { get; set; }

    public string Astatus { get; set; } = null!;

    public bool MailStatus { get; set; }

    public bool WsStatusKalaToPms { get; set; }
}
