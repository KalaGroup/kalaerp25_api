using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Video
{
    /// <summary>
    /// 1
    /// </summary>
    public int VideoId { get; set; }

    public DateTime Sysdt { get; set; }

    public string TrVideoType { get; set; } = null!;

    public string PdirVideoType { get; set; } = null!;

    public string EngSrNo { get; set; } = null!;

    public string? TrVideoName { get; set; }

    public string? PdirVideoName { get; set; }

    public string VideoPath { get; set; } = null!;

    public string EmpCode { get; set; } = null!;

    public bool Active { get; set; }
}
