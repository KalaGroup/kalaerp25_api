using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ManpowerStatus
{
    public int ManpowerStatusId { get; set; }

    public DateOnly Dt { get; set; }

    public string? Yr { get; set; }

    public string Shift { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public int PcId { get; set; }

    public string? Pccode { get; set; }

    public string Wkcode { get; set; } = null!;

    public int SancSkilled { get; set; }

    public int SancSemi { get; set; }

    public int SancUnskilled { get; set; }

    public int AvailSkilled { get; set; }

    public int AvailSemi { get; set; }

    public int AvailUnskilled { get; set; }

    public string? Remark { get; set; }

    public bool Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int Absent { get; set; }
}
