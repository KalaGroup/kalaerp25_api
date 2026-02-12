using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.Models;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IDgStageChecker
    {      
        public Task<List<QualityDefectsByStageAndPCCodeResponseDTO>> GetQualityDefectAsync(string stagename, string pccode);

        public Task<object> GetStageQAPendingListAsync(string stageName, string PCCode);

        public Task<List<DGAssemblyProfitcenters>> GetDGAssemblyProfitcentersAsync();

        public Task<List<PartKvaDto>> GetActivePartKvaListAsync();

        public Task SaveStageWiseQualityCheckListAsync(StageWiseQualityCheckListRequest request);

        public Task<bool> CheckDuplicateQualityCheckListAsync(string pcCode, string stageName, string fromKva, string toKva);

        public Task<List<PendingAuthQualityListDto>> GetAllPendingAuthQualityListAsync();

        public Task<List<PendingAuthQAListDetailsResponse>> GetPendingAuthQAListDetailsAsync(int stageWiseQCId);

        public Task SaveOrUpdateQualityCheckpointAsync(SaveUpdateCheckpointRequest request);

        public Task<List<StageAndKvaWiseCheckpointListResponse>> GetStageAndKvaWiseCheckpointListResponsesAsync(string stageName, string pcCode, decimal kva);
        public Task SaveQAStatusStagewiseAsync(QualityProcessCheckerRequest request);
        public Task<List<EmployeeListForRaiseESPResponseDTO>> FetchEmployeeListAsync();
        public IQueryable<Select6MResponseDTO> Select6MFromDB();
    }
}
