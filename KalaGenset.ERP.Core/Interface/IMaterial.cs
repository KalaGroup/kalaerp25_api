using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IMaterial
    {
        /// <summary>Departments (all profit centers with lines) for the company.</summary>
        Task<List<MaterialDeptDTO>> GetDepartmentsAsync(string companyCode);

        /// <summary>Saved material records for the View grid (from 6MMaterial + Details).</summary>
        Task<List<MaterialRecordDTO>> GetMaterialRecordsAsync(string companyCode, DateTime? date, string? deptCode);

        /// <summary>Replace the day+department's lines with the submitted batch (one transaction).</summary>
        Task<bool> SaveMaterialBatchAsync(MaterialBatchRequest request);

        /// <summary>Soft-delete one material line (sets Active = 0) identified by its MCode + SrNo.</summary>
        Task<bool> DeleteMaterialRecordAsync(string mcode, int srNo, string? modifiedBy);
    }
}