using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class PrcChkDetail
{
    public int Id { get; set; }

    public DateTime Dt { get; set; }

    public string TransCode { get; set; } = null!;

    public string MainSerialNo { get; set; } = null!;

    public int Qa6m { get; set; }

    public string PrcName { get; set; } = null!;

    public string PrcStatus { get; set; } = null!;

    public int ChkPointId { get; set; }

    public string PrcChkPoints { get; set; } = null!;

    public DateTime DgstartTime { get; set; }

    public string Qastatus { get; set; } = null!;

    public DateTime? QastatusDt { get; set; }

    public bool Active { get; set; }
}
