using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ProductionPlanMaster
{
    public int Id { get; set; }

    public DateTime Dt { get; set; }

    public string BracketId { get; set; } = null!;

    public string CatId { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string ProcessType { get; set; } = null!;

    public bool ProcessStatus { get; set; }

    public string Remark { get; set; } = null!;

    /// <summary>
    /// 1 - Active 0- UnActive
    /// </summary>
    public bool Active { get; set; }
}
