using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class User
{
   // public int UserId { get; set; }

    public string UserId { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int? RoleId { get; set; }

    public virtual Role? Role { get; set; }
}
