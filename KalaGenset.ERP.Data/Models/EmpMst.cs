using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class EmpMst
{
    public int EmpId { get; set; }

    public string? EmpCode { get; set; }

    public string Ename { get; set; } = null!;

    public bool Active { get; set; }

    public string Password { get; set; } = null!;

    public int? GradeId { get; set; }

    public long? DesignationId { get; set; }

    public string? Ccode { get; set; }

    public string? Pccode { get; set; }

    public DateOnly? JoinDate { get; set; }

    public int? EmployeeType { get; set; }

    public int? Contractor { get; set; }

    public int? WorkDesignation { get; set; }

    public bool IsOvertime { get; set; }

    public bool ByOfferLetter { get; set; }

    public bool TempEmp { get; set; }

    public byte[]? PhotoCopy { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
