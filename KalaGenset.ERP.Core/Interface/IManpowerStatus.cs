using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IManpowerStatus
    {
        /// <summary>Companies the login may view charts for (33 -> 01/03/28, else self).</summary>
        Task<List<CompanyOptionDTO>> GetViewCompaniesAsync(string companyCode);

        /// <summary>Departments (ProfitCenters) that have W1/W2/W3 sanctioned stations, for the company.</summary>
        Task<List<ManpowerDeptDTO>> GetDepartmentsAsync(string companyCode);

        /// <summary>Stations + sanctioned (by skill) for a profit center, from the GDD master.</summary>
        Task<List<ManpowerStationDTO>> GetStationsByDepartmentAsync(int pcId, string companyCode);

        /// <summary>Saved manning records for the View grid (from 6MManpowerStatus + Details).</summary>
        Task<List<ManpowerStatusRecordDTO>> GetManpowerRecordsAsync(string companyCode, DateTime? date, string? shift, int? pcId);

        /// <summary>Records across a date range for the shortage trend charts.</summary>
        Task<List<ManpowerShortageTrendDTO>> GetShortageTrendAsync(string companyCode, DateTime fromDate, DateTime toDate);

        /// <summary>Insert/update the whole batch in one transaction (header + details; Sanctioned frozen on update).</summary>
        Task<bool> SaveManpowerBatchAsync(ManpowerStatusBatchRequest request);

        /// <summary>Soft-delete one station line (sets Discard) identified by its MCode + SrNo.</summary>
        Task<bool> DeleteManpowerRecordAsync(string mcode, int srNo, string? modifiedBy);
    }
}