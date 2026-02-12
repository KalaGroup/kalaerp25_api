using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Data.Models
{
    public class BaseGetStageScanDts
    {
        public string JobCode { get; set; }
        public string JobDt { get; set; }
        public string DgProductCode { get; set; }
        public string DgProductDesc { get; set; }
        public string EngPartCode { get; set; }
        public string EngPartDesc { get; set; }
        public int? JPriority { get; set; }
        public string AltDts { get; set; }
        public string? Stage1Status { get; set; }
        public int BatCnt { get; set; }
        public double KVA { get; set; }
        public string Cpydts { get; set; }
        public string BatDts { get; set; }
        public string Bat2Dts { get; set; }
    }

    // 🔹 Class for "Stage First Start Data"
    public class GetStageFirstStartDts : BaseGetStageScanDts
    {
        [NotMapped]
        public int BatCnt { get; set; }
        [NotMapped]
        public double KVA { get; set; }
        [NotMapped]
        public string Cpydts { get; set; }
        [NotMapped]
        public string BatDts { get; set; }
        [NotMapped]
        public string Bat2Dts { get; set; }
        public double? EngStk { get; set; }
    }

    // 🔹 Class for "Stage First End Data"
    public class GetStageFirstEndDts : BaseGetStageScanDts
    {
        [NotMapped]
        public int BatCnt { get; set; }
        [NotMapped]
        public double KVA { get; set; }
        [NotMapped]
        public string Cpydts { get; set; }
        [NotMapped]
        public string BatDts { get; set; }
        [NotMapped]
        public string Bat2Dts { get; set; }
        public double? DGS1Stk { get; set; }
    }

    //class for GEt Second stage Scan data
    public class GetSecondStageDts : BaseGetStageScanDts
    {
        [NotMapped]
        public new int? JPriority { get; set; }
        [NotMapped]
        public string? Stage1Status { get; set; }
        public double? DGS3Stk { get; set; }        
        public string BatDts { get; set; }
        public string Bat2Dts { get; set; }
        public string Stage3Status { get; set; }
    }

    //Get DG Assembly Stage 3 Start Details
    public class GetStageThirdStartDts : BaseGetStageScanDts
    {
        [NotMapped]
        public new int? JPriority { get; set; }
        [NotMapped]
        public string? Stage1Status { get; set; }
        public double? DGS4Stk { get; set; }
        public string PanelType { get; set; }
        public string KRM { get; set; }
        public string CPdts { get; set; }
        public string CP2dts { get; set; }
        public string KRMdts { get; set; }
        public string Stage4Status { get; set; }
        public string? PFBCode { get; set; }
    }

    //Get DG Assembly Stage 3 End Details
    public class GetStageThirdEndDts : BaseGetStageScanDts
    {
        [NotMapped]
        public new int? JPriority { get; set; }
        [NotMapped]
        public string? Stage1Status { get; set; }
        public int DGS4Stk { get; set; }
        public string PanelType { get; set; }
        public string KRM { get; set; }
        public string CPdts { get; set; }
        public string CP2dts { get; set; }
        public string KRMdts { get; set; }
        public string Stage4Status { get; set; }
        public string? PFBCode { get; set; }
    }
}
