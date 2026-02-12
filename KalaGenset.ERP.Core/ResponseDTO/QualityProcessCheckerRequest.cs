using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class QualityProcessCheckerRequest
    {
        /// <summary>
        /// Main request class for Quality Process Checker
        /// </summary>
        public QProcessCheckerData QProcessCheckerData { get; set; }
        public List<CheckpointDetail> CheckpointsDetails { get; set; }
        public List<DefectDetail>? DefectDetails { get; set; }
    }

    /// <summary>
    /// Header/Master data for Quality Process Check
    /// </summary>
    public class QProcessCheckerData
    {
        public string pccode { get; set; }
        public string ecode { get; set; }
        public string cid { get; set; }
        public string? JobCode { get; set; }      // For Stage 1 & 2
        public string? PFBCode { get; set; }      // ADD THIS - For Stage 3
        public int Kva { get; set; }
        public string partCode { get; set; }
        public int priority { get; set; }
        public string qualityStatus { get; set; }
        public string stageName { get; set; }
        public string model { get; set; }

        // Stage 1 & 2 fields
        public string? EngSrNo { get; set; }
        public string? AltSrNo { get; set; }

        // Stage 2 additional fields
        public string? CpySrNo { get; set; }
        public string? BatSrNo { get; set; }
        public string? Bat2SrNo { get; set; }
        public string? Bat3SrNo { get; set; }
        public string? Bat4SrNo { get; set; }
        public string? Bat5SrNo { get; set; }
        public string? Bat6SrNo { get; set; }

        // Stage 3 fields
        public string? Engine { get; set; }
        public string? Alternator { get; set; }
        public string? Canopy { get; set; }
        public string? ControlPanel1 { get; set; }
        public string? ControlPanel2 { get; set; }
        public string? Battery1 { get; set; }
        public string? Battery2 { get; set; }
        public string? Battery3 { get; set; }
        public string? Battery4 { get; set; }
        public string? Battery5 { get; set; }
        public string? Battery6 { get; set; }
        public string? Krm { get; set; }
    }

    /// <summary>
    /// Checkpoint detail item
    /// </summary>
    public class CheckpointDetail
    {
        public int SrNo { get; set; }
        public int StageWiseQcId { get; set; }
        public string Remark { get; set; }
        public string Ok { get; set; }
        public string sixM { get; set; }
        public string RaiseEsp { get; set; }
        public string subAssemblyPart { get; set; }
    }

    /// <summary>
    /// Defect detail item - Used for Rework/Reject status
    /// </summary>
    public class DefectDetail
    {
        public string QdcCode { get; set; }
        public double ActualValue { get; set; }
        public double Tolerance { get; set; }
        public string Instrument { get; set; }
        public double Rate { get; set; }
        public double FromRange { get; set; }
        public double ToRange { get; set; }
    }
}
