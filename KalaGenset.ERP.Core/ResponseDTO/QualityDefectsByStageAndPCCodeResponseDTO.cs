namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class QualityDefectsByStageAndPCCodeResponseDTO
    {
        public string PCCode { get; set; }  

        public string QDCCode { get; set; }

        public string QDCName { get; set; }

        public double Rate { get; set; }

        public string Stage { get; set; }

        public string CID { get; set; }

        public string CompanyCode { get; set; }

    }
}
