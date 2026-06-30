using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IMachineDownTime
    {
        /// <summary>Departments for the company (AssignMachineToPC joined to ProfitCenter).</summary>
        Task<List<MachineDeptDTO>> GetDepartmentsAsync(string companyCode);

        /// <summary>Machines for a department (AssignMachineToPC).</summary>
        Task<List<MachineDTO>> GetMachinesByDepartmentAsync(string departmentCode);

        /// <summary>Saved down-time records for the View grid (from 6MMachineDownTime + Details).</summary>
        Task<List<MachineDownTimeRecordDTO>> GetDownTimeRecordsAsync(string companyCode, DateTime? date, string? departmentCode);

        /// <summary>Records across a date range for the Daily / Weekly / Monthly charts.</summary>
        Task<List<MachineDownTimeTrendDTO>> GetDownTimeTrendAsync(string companyCode, DateTime fromDate, DateTime toDate);

        /// <summary>Insert/update the whole batch in one transaction (header + details).</summary>
        Task<bool> SaveDownTimeBatchAsync(MachineDownTimeBatchRequest request);

        /// <summary>Soft-delete one machine line (sets Discard) identified by its MCode + SrNo.</summary>
        Task<bool> DeleteDownTimeRecordAsync(string mcode, int srNo, string? modifiedBy);
    }
}