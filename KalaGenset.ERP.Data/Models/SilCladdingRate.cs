using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class SilCladdingRate
{
    public int Id { get; set; }

    public string Kva { get; set; } = null!;

    public string Model { get; set; } = null!;

    public double Rate { get; set; }

    public string CompanyCode { get; set; } = null!;

    public bool Active { get; set; }

    public bool Discard { get; set; }

    public bool Auth { get; set; }
}
