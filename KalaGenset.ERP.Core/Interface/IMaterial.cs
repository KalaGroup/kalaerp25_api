using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IMaterial
    {
        /// <summary>Companies the login may view for (33 -> 01/03/28, else self).</summary>
        Task<List<CompanyOptionDTO>> GetViewCompaniesAsync(string companyCode);

        /// <summary>Departments (all profit centers with lines) for the company.</summary>
        Task<List<MaterialDeptDTO>> GetDepartmentsAsync(string companyCode);

        /// <summary>Parts for a Plan (KVA) — the Raw part dropdown.</summary>
        Task<List<PartOptionDTO>> GetPartsByKvaAsync(string kva);

        /// <summary>Employees for the "person to communicate" dropdown.</summary>
        Task<List<EmployeeOptionDTO>> GetEmployeesAsync();

        /// <summary>ESP target employees (with PC codes) — proxied from the ERP20 API.</summary>
        Task<List<EspEmployeeDTO>> GetEspEmployeesAsync();

        /// <summary>Raise an ESP (Corporate Requisition) for a shortage line.</summary>
        Task<string> RaiseEspAsync(EspRaiseRequest req);

        /// <summary>Dated shortage rows for the charts.</summary>
        Task<List<MaterialTrendDTO>> GetTrendAsync(string companyCode, DateTime fromDate, DateTime toDate);

        /// <summary>Saved material records for the View grid (from 6MMaterial + Details).</summary>
        Task<List<MaterialRecordDTO>> GetMaterialRecordsAsync(string companyCode, DateTime? date, string? deptCode);

        /// <summary>Replace the day+department's lines with the submitted batch (one transaction).</summary>
        Task<bool> SaveMaterialBatchAsync(MaterialBatchRequest request);

        /// <summary>Soft-delete one material line (sets Active = 0) identified by its MCode + SrNo.</summary>
        Task<bool> DeleteMaterialRecordAsync(string mcode, int srNo, string? modifiedBy);
    }
}