namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class StageAndKvaWiseCheckpointListResponse
    {
        public int StageWiseQcid { get; set; }

        public int SrNo { get; set; }

        public string? SubAssemblyPart { get; set; }

        public string? QualityProcessCheckpoint { get; set; }

        public string? Specification { get; set; }

       // public string? Observation { get; set; }

       // public string? OkNok { get; set; }
    }
}
