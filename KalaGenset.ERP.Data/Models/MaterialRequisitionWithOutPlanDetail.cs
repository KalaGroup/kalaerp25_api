using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class MaterialRequisitionWithOutPlanDetail
{
    public string Reqcode { get; set; } = null!;

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public double Stk { get; set; }

    public double PndReqQty { get; set; }

    public double Fnorm { get; set; }

    public double ReqQty { get; set; }

    public double Moq { get; set; }

    public double Qty { get; set; }

    public string Reqstatus { get; set; } = null!;
}
