using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.RequestDTO;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IEngineDGAssembly
    {
        public Task<BaseGetStageScanDts> GetStageScanDetailsByQrSrNo(EngineDetailsRequest qrSrNo);
        public Task<TestReportScanDts?> GetTestReportScanDetails(TestReportDetailsRequest testReportDetailsRequestDTO);
        public Task<List<Dictionary<string, object?>>> GetPackingSlipScanDtsAsync(PackingSlipDetailsRequest req);
        public Task<List<Dictionary<string, object?>>> GetJobCard1RptAsync(JobCardRequest req);
        public Task<List<Dictionary<string, object?>>> GetJobCard2RptAsync(JobCardRequest req);
        public Task<string> SubmitJobCard2Details(Jobcard2SubmitDetails jobcard2SubmitDetailsReq);
        public Task<List<Dictionary<string, object?>>> GetJobCardDGDtsAsync(string strJobCardType, string strcompID);
        public Task<List<GetDGKitDetails>> GetDGKitDetailsFromDB(string strPrdPartCode, string strPCCode);
        public Task<List<GetTRKitDetails>> GetTRKitDetailsFromDB(string strPartcode, string strDGSrNo, string strPfbCode);
        // public Task<List<dynamic>> GetMOFAdditionalPartDtsFromDB(string strMOFCode);
        public Task<List<MOFAddPartDetailsResponseDTO>> GetMOFAdditionalPartDtsFromDB(string strMOFCode);
        public Task<bool> CheckStageStatus(string JBCode, string EngSrNo, int StageNo);
        public Task SubmitDGAssemblyDetails(DGAssemblySubmitRequest dgStageScanReq);
        public Task<string> SubmitDGAssemblyStage4Details(DGAssemblySubmitRequest dgStageScanReq);
        public Task<string> SubmitTestReportDetails(TestReportSubmitDetails testReportSubmitDetailsDTO);
        public Task<string> SubmitPackingSlipDetails(PackingSlipSubmitDetails packingSlipSubmitDetailsReq);
        public IQueryable<Select6MResponseDTO> Select6MFromDB();
        public Task<List<GetTRPrcChkDts>> FetchProcessCheckpointsFromDB(string stageName, string statusName);
        public Task<string> UploadVideoAndPDFAsync(UploadVideopdfDGAssemblyRequest uploadVideopdfDGAssemblyReq);
    }
}
