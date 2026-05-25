using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.Models;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IQuality
    {
        public IEnumerable<CompanyDTO> FetchCompanyDetails();

        public IEnumerable<PCNameForMTFScanDTO> FetchPCNames();

        public IEnumerable<PartcodeForCalibrationDTO> FetchPartcodesForCalibration();

        public Task<bool> SaveCalibrationMasterAsync(CalibrationMasterRequest request);

        public Task<List<CalibrationMst>> GetUnauthorizedCalibrationDataAsync(int companyId);

        public Task<List<DivisionCodeAnd_NameDTO>> GetDivisonCOdeAndNameFromDB();

        public Task<List<DepartmentCodeAndNameDTO>> GetDepartmentsByDivisionCodeAsync(int divisionId);

        public Task<List<WorkstationCodeAndNameDTO>> GetWorkstationsByDepartmentCodeAsync();

        public Task<string> CreateKaizenSheet(CreateKaizenSheetRequest request);
        Task<List<KaizenSheetListResponse>> GetAllKaizenSheets();
        Task<List<KaizenSheetFullResponse>> GetAllKaizenSheetsFull();
        Task<bool> DeleteKaizenSheet(int id);
        Task<string> UpdateKaizenSheet(int id, CreateKaizenSheetRequest request);
        public Task<bool> AuthorizeKaizenSheet(int id);

        // ── DG Quality Master form endpoints (moved from IDgStageChecker) ─────
        Task<List<PartKvaDto>> GetActivePartKvaListAsync();

        Task SaveStageWiseQualityCheckListAsync(StageWiseQualityCheckListRequest request);

        Task<bool> CheckDuplicateQualityCheckListAsync(string pcCode, string stageName, string fromKva, string toKva, int? excludeId = null);

        Task<List<QualityCheckListReportDto>> GetAllQualityCheckListsAsync();

        Task UpdateStageWiseQualityCheckListAsync(UpdateStageWiseQualityCheckListRequest request);

        Task<bool> SoftDeleteStageWiseQualityCheckListAsync(int stageWiseQcid);

        // Checker action — sets IsAuth = true and stores the optional checker remark
        Task<bool> AuthorizeStageWiseQualityCheckListAsync(int stageWiseQcid, string? checkerRemark);

        // Checker action — rolls a previously authorized checklist back to Pending
        Task<bool> RevertAuthorizationStageWiseQualityCheckListAsync(int stageWiseQcid);
    }
}
