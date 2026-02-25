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
    }
}
