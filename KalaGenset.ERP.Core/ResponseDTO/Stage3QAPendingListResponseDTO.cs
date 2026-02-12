namespace KalaGenset.ERP.Core.ResponseDTO
{
    public  class Stage3QAPendingListResponseDTO
    {
        public string PFBCode { get; set; } = "";
        public string ProfitCenterCode { get; set; } = "";
        public string QPCStatus { get; set; } = "";
        public double KVA { get; set; }
        public string Phase { get; set; } = "";
        public string Model { get; set; } = "";
        public string PartDesc { get; set; } = "";
        public string Partcode { get; set; } = "";
        public string Engine { get; set; } = "";
        public string Alternator { get; set; } = "";
        public string Canopy { get; set; } = "";
        public string ControlPanel1 { get; set; } = "";
        public string ControlPanel2 { get; set; } = "";
        public string Battery1 { get; set; } = "";
        public string Battery2 { get; set; } = "";
        public string Battery3 { get; set; } = "";
        public string Battery4 { get; set; } = "";
        public string Battery5 { get; set; } = "";
        public string Battery6 { get; set; } = "";
        public string KRM { get; set; } = "";
    }
}
