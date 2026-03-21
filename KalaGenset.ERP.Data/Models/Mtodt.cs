using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Mtodt
{
    public int Id { get; set; }

    public DateTime SysDt { get; set; }

    public int? Dgid { get; set; }

    public DateTime Dt { get; set; }

    public string PrdType { get; set; } = null!;

    public string Kva { get; set; } = null!;

    public string Phase { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string Cp { get; set; } = null!;

    public string Partcode { get; set; } = null!;

    public DateTime DtValidity { get; set; }

    public int Qty { get; set; }

    public bool Auth { get; set; }

    public DateTime? AuthDt { get; set; }

    public bool Active { get; set; }

    public bool Discard { get; set; }
}
