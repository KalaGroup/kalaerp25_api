using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PrimaryCompAssignDetail
{
    public string Pcacode { get; set; } = null!;

    public int SrNo { get; set; }

    public string SupEmpCode { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool NotifyStatus { get; set; }

    public int RecSavedAppStatus { get; set; }
}
