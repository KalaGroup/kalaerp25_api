using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class DispatchInstructionDetail
{
    public string Dino { get; set; } = null!;

    public int SrNo { get; set; }

    public string Rdgcode { get; set; } = null!;

    public DateTime? AllocDate { get; set; }

    public string Ppwcode { get; set; } = null!;

    public int Rdgqty { get; set; }

    public string InvStatus { get; set; } = null!;

    public string Pdirstatus { get; set; } = null!;

    public string Psldstatus { get; set; } = null!;

    public bool Status { get; set; }
}
