using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class MachineDownTime
{
    public int MachineDownTimeId { get; set; }

    public DateOnly Dt { get; set; }

    public string? Yr { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string DeptCode { get; set; } = null!;

    public string MachineCode { get; set; } = null!;

    public int Shift1Min { get; set; }

    public int Shift2Min { get; set; }

    public int TotalMin { get; set; }

    public string? Remark { get; set; }

    public bool Active { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int LineShift1Min { get; set; }

    public int LineShift2Min { get; set; }

    public int LineTotalMin { get; set; }

    public string? Status { get; set; }
}
