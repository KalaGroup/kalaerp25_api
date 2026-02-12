using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class LoginMst
{
    public int Id { get; set; }

    public DateTime Dt { get; set; }

    public DateTime ChangeDt { get; set; }

    public DateTime? LastLoginDt { get; set; }

    public string SupplierCode { get; set; } = null!;

    public string CustomerCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string PassWord { get; set; } = null!;

    public string MPin { get; set; } = null!;

    public string Imeino { get; set; } = null!;

    public bool AllowMapp { get; set; }

    public string DregId { get; set; } = null!;

    public string LoginType { get; set; } = null!;

    public string RightsType { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public int LoginStatus { get; set; }

    public string OnMachine { get; set; } = null!;

    public string AngRights { get; set; } = null!;

    public string OnAccountOf { get; set; } = null!;

    public bool Active { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public bool KalaToBio { get; set; }
}
