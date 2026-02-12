using System.Data;
using Azure.Core;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KalaGenset.ERP.Core.Services
{
    /// <summary>
    /// Service class for handling DG (Diesel Generator) Quality Process Checking operations.
    /// Manages quality assurance workflows across three assembly stages.
    /// </summary>
    /// <remarks>
    /// This service handles:
    /// - Stage 1: Engine + Alternator Assembly QA
    /// - Stage 2: Battery + Canopy Component QA  
    /// - Stage 3: Final Assembly Process Feedback QA
    /// </remarks>
    public class DgStageCheckerService : IDgStageChecker
    {
        private readonly KalaDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DgStageCheckerService"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing ERP data.</param>

        public DgStageCheckerService(KalaDbContext context)
        {
            _context = context;
        }

        #region Quality Defect Methods

        /// <summary>
        /// Retrieves quality defect types for a specific stage and profit center.
        /// </summary>
        /// <param name="stagename">
        /// The stage name. Valid values:
        /// <list type="bullet">
        ///   <item><description>"Stage1" - Engine/Alternator assembly</description></item>
        ///   <item><description>"Stage2" - Battery/Canopy assembly</description></item>
        ///   <item><description>"Stage3" - Final assembly</description></item>
        /// </list>
        /// </param>
        /// <param name="pccode">The profit center code (e.g., "PC001").</param>
        /// <returns>
        /// A list of <see cref="QualityDefectsByStageAndPCCodeResponseDTO"/> containing:
        /// - QDCCode: Defect code
        /// - QDCName: Defect name/description
        /// - Rate: Defect rate value
        /// - Stage: Stage name
        /// </returns>
        /// <exception cref="Exception">Thrown when database operation fails.</exception>
        /// <example>
        /// <code>
        /// var defects = await service.GetQualityDefectAsync("Stage1", "PC001");
        /// </code>
        /// </example>

        public async Task<List<QualityDefectsByStageAndPCCodeResponseDTO>> GetQualityDefectAsync(string stagename, string pccode)
        {
            try
            {
                var stageNameParam = new SqlParameter("@StageName", stagename);
                var pcCodeParam = new SqlParameter("@PCCode", pccode);
                var result = await _context.Database
                    .SqlQueryRaw<QualityDefectsByStageAndPCCodeResponseDTO>("EXEC GetQualityDefectsByStageAndPCCode @StageName,@PCCode", stageNameParam, pcCodeParam)
                    .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Quality Defects By Stage And PCCode", ex);
            }
        }
        #endregion

        #region QA Pending List Methods

        /// <summary>
        /// Retrieves pending QA items for a specific stage and profit center.
        /// Returns different data structures based on stage.
        /// </summary>
        /// <param name="stageName">
        /// The stage name:
        /// <list type="bullet">
        ///   <item><description>"Stage1" or "Stage2" - Returns <see cref="Stage1And2QAPendingListResponseDTO"/></description></item>
        ///   <item><description>"Stage3" - Returns <see cref="Stage3QAPendingListResponseDTO"/></description></item>
        /// </list>
        /// </param>
        /// <param name="PCCode">The profit center code.</param>
        /// <returns>
        /// For Stage1/Stage2: List containing JobCode, KVA, Model, EngSrNo, AltSrNo, Battery serial numbers, etc.
        /// For Stage3: List containing PFBCode, Engine, Alternator, Canopy, Control Panels, Batteries, KRM.
        /// </returns>
        /// <exception cref="Exception">Thrown when database operation fails.</exception>
        /// <remarks>
        /// This method calls stored procedure: GetStageQAPendingList
        /// </remarks>

        public async Task<object> GetStageQAPendingListAsync(string stageName, string PCCode)
        {
            try
            {
                var stageNameParam = new SqlParameter("@StageName", stageName);
                var pcCodeParam = new SqlParameter("@PCCode", PCCode);

                if (stageName == "Stage3")
                {
                    var result = await _context.Database
                        .SqlQueryRaw<Stage3QAPendingListResponseDTO>(
                            "EXEC GetStageQAPendingList @StageName, @PCCode",
                            stageNameParam,
                            pcCodeParam
                        )
                        .ToListAsync();
                    return result;
                }
                else
                {
                    var result = await _context.Database
                        .SqlQueryRaw<Stage1And2QAPendingListResponseDTO>(
                            "EXEC GetStageQAPendingList @StageName, @PCCode",
                            stageNameParam,
                            pcCodeParam
                        )
                        .ToListAsync();
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching {stageName} QA Pending List", ex);
            }
        }
        #endregion

        #region Profit Center Methods

        /// <summary>
        /// Retrieves all active DG Assembly profit centers.
        /// </summary>
        /// <returns>
        /// A list of <see cref="DGAssemblyProfitcenters"/> containing:
        /// - ProfitCenter_Act: Profit center code
        /// - PCName: Profit center name
        /// </returns>
        /// <exception cref="Exception">Thrown when database operation fails.</exception>
        /// <remarks>
        /// Calls stored procedure: GetActiveDGAssemblyProfitCenters
        /// </remarks>

        public async Task<List<DGAssemblyProfitcenters>> GetDGAssemblyProfitcentersAsync()
        {
            try
            {
                var result = await _context.Database
                    .SqlQueryRaw<DGAssemblyProfitcenters>("EXEC GetActiveDGAssemblyProfitCenters")
                    .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching DG Assembly Profitcenters", ex);
            }
        }
        #endregion

        #region Part KVA Methods

        /// <summary>
        /// Retrieves all active part KVA values for dropdown population.
        /// </summary>
        /// <returns>
        /// A list of <see cref="PartKvaDto"/> containing KVA values.
        /// </returns>
        /// <exception cref="Exception">Thrown when database operation fails.</exception>
        /// <remarks>
        /// Calls stored procedure: GetActivePartKVAList
        /// </remarks>
        public async Task<List<PartKvaDto>> GetActivePartKvaListAsync()
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<PartKvaDto>("EXEC GetActivePartKVAList")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Part KVA list", ex);
            }
        }
        #endregion

        #region Quality Check List Methods

        /// <summary>
        /// Saves a new stage-wise quality check list with its checkpoint details.
        /// Creates master record and associated detail records in a transaction.
        /// </summary>
        /// <param name="request">
        /// The request object containing:
        /// <list type="bullet">
        ///   <item><description>pcCode: Profit center code</description></item>
        ///   <item><description>stageName: Stage name (Stage1/Stage2/Stage3)</description></item>
        ///   <item><description>fromKVA: Starting KVA range</description></item>
        ///   <item><description>toKVA: Ending KVA range</description></item>
        ///   <item><description>makerRemark: Remark from the maker</description></item>
        ///   <item><description>checkpointItems: List of checkpoint details</description></item>
        /// </list>
        /// </param>
        /// <exception cref="Exception">Thrown when save operation fails. Transaction is rolled back.</exception>
        /// <remarks>
        /// Tables affected:
        /// - StageWiseQualityCheckList (Master)
        /// - StageWiseQualityCheckListDetail (Details)
        /// </remarks>
        public async Task SaveStageWiseQualityCheckListAsync(StageWiseQualityCheckListRequest request)
        {
            // Start explicit transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔹 1. Save MASTER record
                var master = new StageWiseQualityCheckList
                {
                    Pccode = request.pcCode,
                    StageName = request.stageName,
                    FromKva = request.fromKVA,
                    ToKva = request.toKVA,
                    IsActive = true,
                    MakerRemark = request.makerRemark,
                };

                _context.StageWiseQualityCheckLists.Add(master);
                await _context.SaveChangesAsync();   // PK generated here

                // 🔹 2. Retrieve generated PK
                int stageWiseQCId = master.StageWiseQcid;

                // 🔹 3. Prepare DETAIL records
                var details = request.checkpointItems.Select(item =>
                    new StageWiseQualityCheckListDetail
                    {
                        StageWiseQcid = stageWiseQCId,   // SAME PK FOR ALL
                        SrNo = item.srNo,
                        SubAssemblyPart = item.subAssemblyPart,
                        QualityProcessCheckpoint = item.qualityProcessCheckpoint,
                        Specification = item.specification,
                        Observation = item.observation,
                        OkNok = item.ok_nok
                    }).ToList();

                // 🔹 4. Save DETAIL records
                _context.StageWiseQualityCheckListDetails.AddRange(details);
                await _context.SaveChangesAsync();

                // 🔹 5. Commit transaction
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // Rollback on any failure
                await transaction.RollbackAsync();
                throw new Exception("Error saving Stage Wise Quality Check List", ex);
            }
        }

        /// <summary>
        /// Checks if a duplicate quality check list exists for the given criteria.
        /// </summary>
        /// <param name="pcCode">Profit center code.</param>
        /// <param name="stageName">Stage name (Stage1/Stage2/Stage3).</param>
        /// <param name="fromKva">Starting KVA range (as string, will be converted to decimal).</param>
        /// <param name="toKva">Ending KVA range (as string, will be converted to decimal).</param>
        /// <returns>
        /// <c>true</c> if a duplicate exists; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="Exception">Thrown when KVA values are invalid or database operation fails.</exception>
        public async Task<bool> CheckDuplicateQualityCheckListAsync(string pcCode, string stageName, string fromKva, string toKva)
        {
            try
            {
                // 🔹 Convert string → decimal
                if (!decimal.TryParse(fromKva, out decimal fromKvaDecimal))
                    throw new Exception("Invalid FromKVA value");

                if (!decimal.TryParse(toKva, out decimal toKvaDecimal))
                    throw new Exception("Invalid ToKVA value");


                var exists = await _context.StageWiseQualityCheckLists
                    .AnyAsync(x => x.Pccode == pcCode
                               && x.StageName == stageName
                               && x.FromKva == fromKvaDecimal
                               && x.ToKva == toKvaDecimal);
                return exists;
            }
            catch (Exception ex)
            {
                throw new Exception("Error checking duplicate Quality Check List", ex);
            }
        }
        #endregion

        #region Pending Authorization Methods

        /// <summary>
        /// Retrieves all quality check lists pending authorization.
        /// </summary>
        /// <returns>
        /// A list of <see cref="PendingAuthQualityListDto"/> containing:
        /// - StageWiseQcid: Unique identifier
        /// - Pccode: Profit center code
        /// - PCName: Profit center name
        /// - StageName: Stage name
        /// - FromKva/ToKva: KVA range
        /// </returns>
        /// <exception cref="Exception">Thrown when database operation fails.</exception>
        /// <remarks>
        /// Filters: IsAuth = false, IsActive = true, IsDiscard = false
        /// </remarks>
        public async Task<List<PendingAuthQualityListDto>> GetAllPendingAuthQualityListAsync()
        {
            try
            {
                var result = await (
                    from q in _context.StageWiseQualityCheckLists
                    join pc in _context.ProfitCenters
                        on q.Pccode equals pc.Pccode
                    where q.IsAuth == false
                       && q.IsActive == true
                       && q.IsDiscard == false

                    select new PendingAuthQualityListDto
                    {
                        StageWiseQcid = q.StageWiseQcid,
                        Pccode = q.Pccode,
                        PCName = pc.Pcname,
                        StageName = q.StageName,
                        FromKva = q.FromKva,
                        ToKva = q.ToKva
                    }
                ).ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching pending auth quality list", ex);
            }
        }

        /// <summary>
        /// Retrieves checkpoint details for a specific quality check list pending authorization.
        /// </summary>
        /// <param name="stageWiseQCId">The unique identifier of the quality check list.</param>
        /// <returns>
        /// A list of <see cref="PendingAuthQAListDetailsResponse"/> containing:
        /// - SrNo: Serial number
        /// - SubAssemblyPart: Sub-assembly part name
        /// - QualityProcessCheckpoint: Checkpoint description
        /// - Specification: Specification details
        /// - Observation: Observation notes
        /// - OkNok: OK/NOK status
        /// </returns>
        /// <exception cref="Exception">Thrown when database operation fails.</exception>
        public async Task<List<PendingAuthQAListDetailsResponse>> GetPendingAuthQAListDetailsAsync(int stageWiseQCId)
        {
            try
            {
                var result = await _context.StageWiseQualityCheckListDetails
                    .Where(x => x.StageWiseQcid == stageWiseQCId)
                    .OrderBy(x => x.SrNo)
                    .Select(x => new PendingAuthQAListDetailsResponse
                    {
                        SrNo = x.SrNo,
                        SubAssemblyPart = x.SubAssemblyPart,
                        QualityProcessCheckpoint = x.QualityProcessCheckpoint,
                        Specification = x.Specification,
                        Observation = x.Observation,
                        OkNok = x.OkNok
                    })
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Pending Auth QA List Details", ex);
            }
        }

        /// <summary>
        /// Saves or updates quality checkpoint data and marks it as authorized.
        /// Replaces all existing checkpoint details with new ones.
        /// </summary>
        /// <param name="request">
        /// The request object containing:
        /// <list type="bullet">
        ///   <item><description>StageWiseQcid: Master record ID</description></item>
        ///   <item><description>CheckerRemark: Authorization remark</description></item>
        ///   <item><description>CheckpointItems: Updated list of checkpoints</description></item>
        /// </list>
        /// </param>
        /// <exception cref="Exception">Thrown when save operation fails.</exception>
        /// <remarks>
        /// Operations performed:
        /// 1. Updates master record (IsAuth = true, CheckerAuthRemark)
        /// 2. Deletes existing detail records
        /// 3. Inserts new detail records
        /// </remarks>
        public async Task SaveOrUpdateQualityCheckpointAsync(SaveUpdateCheckpointRequest request)
        {
            try
            {
                // Update MASTER record to set IsAuth = true and CheckerRemark
                var existingMasterRecord = await _context.StageWiseQualityCheckLists
                    .FirstOrDefaultAsync(q => q.StageWiseQcid == request.StageWiseQcid);
                if (existingMasterRecord != null)
                {
                    existingMasterRecord.IsAuth = true;
                    existingMasterRecord.CheckerAuthRemark = request.CheckerRemark;
                    await _context.SaveChangesAsync();
                }

                // Fetch all existing records based on StageWiseQcid
                var existingCheckpoints = await _context.StageWiseQualityCheckListDetails
                    .Where(q => q.StageWiseQcid == request.StageWiseQcid)
                    .ToListAsync();

                // Remove existing records if any
                if (existingCheckpoints != null && existingCheckpoints.Any())
                {
                    _context.StageWiseQualityCheckListDetails.RemoveRange(existingCheckpoints);
                }

                // Insert new CheckpointItems from UI
                if (request.CheckpointItems != null && request.CheckpointItems.Any())
                {
                    var newCheckpoints = request.CheckpointItems.Select(item => new StageWiseQualityCheckListDetail
                    {
                        StageWiseQcid = request.StageWiseQcid,
                        SrNo = item.SrNo,
                        SubAssemblyPart = item.SubAssemblyPart,
                        QualityProcessCheckpoint = item.Checkpoint,
                        Specification = item.Specification,
                        Observation = item.Observation,
                        OkNok = item.Ok
                    }).ToList();

                    await _context.StageWiseQualityCheckListDetails.AddRangeAsync(newCheckpoints);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving or updating Quality Checkpoint", ex);
            }
        }

        #endregion

        #region Checkpoint Retrieval Methods

        /// <summary>
        /// Retrieves quality checkpoints for a specific stage, profit center, and KVA value.
        /// Used to populate the quality checkpoint table in the UI.
        /// </summary>
        /// <param name="stageName">Stage name (Stage1/Stage2/Stage3).</param>
        /// <param name="pcCode">Profit center code.</param>
        /// <param name="kva">KVA value to match within the FromKva-ToKva range.</param>
        /// <returns>
        /// A list of <see cref="StageAndKvaWiseCheckpointListResponse"/> containing:
        /// - StageWiseQcid: Master record ID
        /// - SrNo: Serial number
        /// - SubAssemblyPart: Sub-assembly part name
        /// - QualityProcessCheckpoint: Checkpoint description
        /// - Specification: Specification details
        /// </returns>
        /// <exception cref="Exception">Thrown when database operation fails.</exception>
        /// <remarks>
        /// Filters: IsActive = true, IsDiscard = false, KVA within range
        /// </remarks>
        public async Task<List<StageAndKvaWiseCheckpointListResponse>> GetStageAndKvaWiseCheckpointListResponsesAsync(string stageName, string pcCode, decimal kva)
        {
            try
            {
                var result = await (
                    from checklist in _context.StageWiseQualityCheckLists
                    join details in _context.StageWiseQualityCheckListDetails
                        on checklist.StageWiseQcid equals details.StageWiseQcid
                    where checklist.StageName == stageName
                       && checklist.Pccode == pcCode
                       && kva >= checklist.FromKva
                       && kva <= checklist.ToKva
                       && checklist.IsActive == true
                       && checklist.IsDiscard == false
                    orderby details.SrNo
                    select new StageAndKvaWiseCheckpointListResponse
                    {
                        StageWiseQcid = checklist.StageWiseQcid,
                        SrNo = details.SrNo,
                        SubAssemblyPart = details.SubAssemblyPart,
                        QualityProcessCheckpoint = details.QualityProcessCheckpoint,
                        Specification = details.Specification,
                        // Observation = details.Observation,
                        // OkNok = details.OkNok
                    }
                ).ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Stage And KVA Wise Checkpoint List", ex);
            }
        }

        #endregion

        #region QA Status Save Method

        /// <summary>
        /// Saves quality assurance status for a job card/process feedback.
        /// This is the main method that handles Accept, Rework, and Reject actions.
        /// </summary>
        /// <param name="request">
        /// The comprehensive request object containing:
        /// <list type="table">
        ///   <listheader>
        ///     <term>Property</term>
        ///     <description>Description</description>
        ///   </listheader>
        ///   <item>
        ///     <term>QProcessCheckerData</term>
        ///     <description>Master data including:
        ///       - pccode: Profit center code
        ///       - ecode: Employee code
        ///       - cid: Company ID
        ///       - JobCode: Job card code (Stage1/2)
        ///       - PFBCode: Process feedback code (Stage3)
        ///       - Kva: KVA value
        ///       - partCode: Part code
        ///       - priority: Job priority
        ///       - qualityStatus: "Accept" | "Rework" | "Reject"
        ///       - stageName: "Stage1" | "Stage2" | "Stage3"
        ///       - model: Model name
        ///       - EngSrNo, AltSrNo: Engine/Alternator serial (Stage1/2)
        ///       - CpySrNo, BatSrNo-Bat6SrNo: Canopy/Battery serials (Stage2)
        ///       - Engine, Alternator, Canopy, ControlPanel1/2, Battery1-6, Krm: (Stage3)
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>CheckpointsDetails</term>
        ///     <description>List of checkpoint results:
        ///       - SrNo: Serial number
        ///       - StageWiseQcId: Checkpoint ID
        ///       - Remark: Checker remark
        ///       - Ok: "OK" or "NOK"
        ///       - sixM: 6M category (Man/Machine/Method/Material/Measurement/Mother Nature)
        ///       - RaiseEsp: Employee code to raise ESP
        ///       - subAssemblyPart: Sub-assembly part name
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>DefectDetails</term>
        ///     <description>List of defects (for Rework/Reject):
        ///       - QdcCode: Defect code
        ///       - ActualValue, Tolerance, Instrument
        ///       - Rate, FromRange, ToRange
        ///     </description>
        ///   </item>
        /// </list>
        /// </param>
        /// <exception cref="Exception">Thrown when save operation fails. Transaction is rolled back.</exception>
        /// <remarks>
        /// <para><b>Tables Affected:</b></para>
        /// <list type="bullet">
        ///   <item><description>QualityProcessCheckerDG - Master record</description></item>
        ///   <item><description>QualityProcessCheckerDetailsDG - Checkpoint details</description></item>
        ///   <item><description>QualityProcessCheckDefectDG - Defect details (Rework/Reject)</description></item>
        ///   <item><description>CorporateRequisition - ESP request (Rework only)</description></item>
        ///   <item><description>StockWIP - Stock entry (Accept only)</description></item>
        ///   <item><description>JobCardDetailsSub - Status update (Stage1/2)</description></item>
        ///   <item><description>ProcessFeedbackDetailsSub - Status update (Stage3)</description></item>
        /// </list>
        /// 
        /// <para><b>Status Values Set:</b></para>
        /// <list type="table">
        ///   <listheader>
        ///     <term>Action</term>
        ///     <description>Status Value</description>
        ///   </listheader>
        ///   <item><term>Accept</term><description>"D" (Done) for Stage1/2, "OK" for Stage3</description></item>
        ///   <item><term>Rework</term><description>"Rew"</description></item>
        ///   <item><term>Reject</term><description>"Rej"</description></item>
        /// </list>
        /// 
        /// <para><b>Business Rules:</b></para>
        /// <list type="number">
        ///   <item><description>ESP (CorporateRequisition) is created only for Rework, NOT for Reject</description></item>
        ///   <item><description>StockWIP entry is created only for Accept</description></item>
        ///   <item><description>Status update happens for all three actions</description></item>
        /// </list>
        /// </remarks>
        public async Task SaveQAStatusStagewiseAsync(QualityProcessCheckerRequest request)
        {
            string GetMaxValue = "0";
            string MaxSrNo = "0";
            string maxNo = "0";
            string reqMessg = "";


            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 🔹 1. Save MASTER record
                var master = new QualityProcessCheckerDg
                {
                    JobCode = request.QProcessCheckerData.JobCode,
                    PartCode = request.QProcessCheckerData.partCode,
                    Jpriority = request.QProcessCheckerData.priority,
                    QualityStatus = request.QProcessCheckerData.qualityStatus,
                    StageName = request.QProcessCheckerData.stageName,
                    IsActive = true,
                    IsDiscard = false
                };
                _context.QualityProcessCheckerDgs.Add(master);
                await _context.SaveChangesAsync();

                int qualityProcessCheckerDgId = master.QualityProcessCheckerDgid;

                // 🔹 2. Save DETAIL records
                foreach (var item in request.CheckpointsDetails)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        @"INSERT INTO QualityProcessCheckerDetailsDG(QualityProcessCheckerDGId, StageWiseQCId, SrNo, CheckerRemark, OK_NOK) 
                        VALUES ({0}, {1}, {2}, {3}, {4})",
                        qualityProcessCheckerDgId,
                        item.StageWiseQcId,
                        item.SrNo,
                        item.Remark ?? "",
                        item.Ok ?? ""
                    );
                }

                // 🔹 3. Update QA Status (only for Accept)
                if (request.QProcessCheckerData.qualityStatus == "Accept")
                {
                    var stageName = request.QProcessCheckerData.stageName;

                    // ============================================
                    // STAGE 1 & STAGE 2: Update JobCardDetailsSub
                    // ============================================
                    if (stageName == "Stage1" || stageName == "Stage2")
                    {
                        var jobCode = request.QProcessCheckerData.JobCode;
                        var pcCode = request.QProcessCheckerData.pccode;
                        var partCode = request.QProcessCheckerData.partCode;

                        // Build list of valid serial numbers
                        var serialNumbers = new List<string>();

                        // Common for Stage 1 & 2
                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.EngSrNo))
                            serialNumbers.Add(request.QProcessCheckerData.EngSrNo);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.AltSrNo))
                            serialNumbers.Add(request.QProcessCheckerData.AltSrNo);

                        if (stageName == "Stage1")
                        {
                            var sqlQuery = @"INSERT INTO StockWIP 
                               (FromProfitCenterCode, PartCode, ReceivedCode, ReceivedDate, ReceivedQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                              VALUES
                              (@PCCode, @ProductCode, @ReceivedCode, CAST(@ReceivedDate AS DATETIME), @ReceivedQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                            var parameters = new[]
                            {
                                new SqlParameter("@PCCode", request.QProcessCheckerData.pccode ?? (object)DBNull.Value),
                                new SqlParameter("@ProductCode", request.QProcessCheckerData.partCode?.Trim() ?? (object)DBNull.Value),
                                new SqlParameter("@ReceivedCode", $"{request.QProcessCheckerData.JobCode}-->{request.QProcessCheckerData.EngSrNo}"),
                                new SqlParameter("@ReceivedDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                                new SqlParameter("@ReceivedQty", 1) { SqlDbType = SqlDbType.Float },
                                new SqlParameter("@ToPCCode", request.QProcessCheckerData.pccode ?? (object)DBNull.Value),
                                new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                                new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                                new SqlParameter("@StageName", "StageIII")
                            };
                            await _context.Database.ExecuteSqlRawAsync(sqlQuery, parameters);
                        }

                        // Additional for Stage 2
                        if (stageName == "Stage2")
                        {
                            if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.CpySrNo))
                                serialNumbers.Add(request.QProcessCheckerData.CpySrNo);

                            if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.BatSrNo))
                                serialNumbers.Add(request.QProcessCheckerData.BatSrNo);

                            if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Bat2SrNo))
                                serialNumbers.Add(request.QProcessCheckerData.Bat2SrNo);

                            if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Bat3SrNo))
                                serialNumbers.Add(request.QProcessCheckerData.Bat3SrNo);

                            if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Bat4SrNo))
                                serialNumbers.Add(request.QProcessCheckerData.Bat4SrNo);

                            if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Bat5SrNo))
                                serialNumbers.Add(request.QProcessCheckerData.Bat5SrNo);

                            if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Bat6SrNo))
                                serialNumbers.Add(request.QProcessCheckerData.Bat6SrNo);

                            var sqlQuery1 = "";
                            sqlQuery1 = @"INSERT INTO StockWIP 
                               (FromProfitCenterCode, PartCode, ReceivedCode, ReceivedDate, ReceivedQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                              VALUES
                              (@PCCode, @ProductCode, @ReceivedCode, CAST(@ReceivedDate AS DATETIME), @ReceivedQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                            var parameters1 = new[]
                           {
                                new SqlParameter("@PCCode", request.QProcessCheckerData.pccode ?? (object)DBNull.Value),
                                new SqlParameter("@ProductCode", request.QProcessCheckerData.partCode?.Trim() ?? (object)DBNull.Value),
                                new SqlParameter("@ReceivedCode", $"{request.QProcessCheckerData.JobCode}-->{request.QProcessCheckerData.EngSrNo}"),
                                new SqlParameter("@ReceivedDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                                new SqlParameter("@ReceivedQty", 1) { SqlDbType = SqlDbType.Float },
                                new SqlParameter("@ToPCCode", request.QProcessCheckerData.pccode ?? (object)DBNull.Value),
                                new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                                new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                                new SqlParameter("@StageName", "StageIV")
                            };
                            await _context.Database.ExecuteSqlRawAsync(sqlQuery1, parameters1);
                        }

                        // Fetch and update records
                        if (serialNumbers.Any())
                        {
                            // Build comma-separated list for IN clause
                            var serialNumbersParam = string.Join("','", serialNumbers);

                            var records = await _context.JobCardDetailsSubs
                                .FromSqlRaw($@"SELECT jds.* FROM JobCardDetailsSub jds INNER JOIN JobCard j ON j.JobCode = jds.JobCode
                                              WHERE j.JobCode = {{0}}
                                              AND j.PCCode = {{1}}
                                              AND jds.PartCode = {{2}}
                                              AND jds.SerialNo IN ('{serialNumbersParam}')",
                                             jobCode, pcCode, partCode)
                                .ToListAsync();

                            foreach (var record in records)
                            {
                                if (stageName == "Stage1")
                                {
                                    record.Stage1Qastatus = "D";
                                }
                                else if (stageName == "Stage2")
                                {
                                    record.Stage3Qastatus = "D";
                                }
                            }

                            await _context.SaveChangesAsync();
                        }
                    }

                    // ============================================
                    // STAGE 3: Update ProcessFeedbackDetailsSub
                    // ============================================
                    else if (stageName == "Stage3")
                    {
                        var pfbCode = request.QProcessCheckerData.PFBCode;
                        var pcCode = request.QProcessCheckerData.pccode;
                        var partCode = request.QProcessCheckerData.partCode;

                        // Build list of valid serial numbers for Stage 3
                        var serialNumbers = new List<string>();

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Engine))
                            serialNumbers.Add(request.QProcessCheckerData.Engine);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Alternator))
                            serialNumbers.Add(request.QProcessCheckerData.Alternator);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Canopy))
                            serialNumbers.Add(request.QProcessCheckerData.Canopy);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.ControlPanel1))
                            serialNumbers.Add(request.QProcessCheckerData.ControlPanel1);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.ControlPanel2))
                            serialNumbers.Add(request.QProcessCheckerData.ControlPanel2);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Battery1))
                            serialNumbers.Add(request.QProcessCheckerData.Battery1);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Battery2))
                            serialNumbers.Add(request.QProcessCheckerData.Battery2);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Battery3))
                            serialNumbers.Add(request.QProcessCheckerData.Battery3);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Battery4))
                            serialNumbers.Add(request.QProcessCheckerData.Battery4);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Battery5))
                            serialNumbers.Add(request.QProcessCheckerData.Battery5);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Battery6))
                            serialNumbers.Add(request.QProcessCheckerData.Battery6);

                        if (!string.IsNullOrWhiteSpace(request.QProcessCheckerData.Krm))
                            serialNumbers.Add(request.QProcessCheckerData.Krm);

                        // Update ProcessFeedBack status
                        if (serialNumbers.Any())
                        {
                            // Build comma-separated list for IN clause
                            var serialNumbersParam = string.Join("','", serialNumbers);

                            var processFeedback = await _context.ProcessFeedbackDetailsSubs
                                                .FromSqlRaw($@"SELECT pfds.* FROM ProcessFeedbackDetailsSub pfds INNER JOIN ProcessFeedBack pf ON pf.Pfbcode = pfds.Pfbcode
                                                 WHERE pf.Pfbcode = {{0}}
                                                 AND pf.ProfitCenterCode = {{1}}
                                                 AND pf.PartCode = {{2}}
                                                 AND pfds.SerialNo IN ('{serialNumbersParam}')",
                                                 pfbCode, pcCode, partCode).ToListAsync();

                            if (processFeedback != null)
                            {
                                foreach (var record in processFeedback)
                                {
                                    record.Qpcstatus = "OK";
                                }
                                await _context.SaveChangesAsync();
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else if (request.QProcessCheckerData.qualityStatus == "Rework" || request.QProcessCheckerData.qualityStatus == "Reject")
                {
                    // Save Defect Records
                    if (request.DefectDetails?.Any() == true)
                    {
                        foreach (var defectItem in request.DefectDetails)
                        {
                            await _context.Database.ExecuteSqlRawAsync( @"INSERT INTO QualityProcessCheckDefectDG 
                             (QualityProcessCheckerDGId, QDCCode, ActualValue, Tolerance, Instrument, Rate, FromRange, ToRange) 
                              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})",
                                qualityProcessCheckerDgId,
                                defectItem.QdcCode ?? "",
                                defectItem.ActualValue,
                                defectItem.Tolerance,
                                defectItem.Instrument ?? "",
                                defectItem.Rate,
                                defectItem.FromRange,
                                defectItem.ToRange
                            );
                        }
                    }

                    string? YearEnd = await _context.YearEnds
                        .Select(y => $"{y.StartDate:yy}-{y.EndDate:yy}")
                        .FirstOrDefaultAsync();

                    if (!string.Equals(request.QProcessCheckerData?.qualityStatus, "Reject", StringComparison.OrdinalIgnoreCase))
                    {
                        // For Raise ESP request
                        foreach (var item in request.CheckpointsDetails.Where(i => i.sixM != null))
                        {
                            var recordGetMaxCodes = await _context.GetMaxCodes
                                .FirstOrDefaultAsync(x => x.Prefix == "COR" &&
                                                          x.TblName == "CorporateRequisition" &&
                                                          x.CompCode == request.QProcessCheckerData.cid &&
                                                          x.Yr == YearEnd);

                            int intmax = 0;

                            if (recordGetMaxCodes != null)
                            {
                                await _context.Database.ExecuteSqlRawAsync(
                                    @"UPDATE GetMaxCode SET MaxValue = MaxValue + 1 WHERE Prefix = @Prefix 
                                AND TblName = @TblName AND CompCode = @CompCode AND Yr = @Yr",
                                    new SqlParameter("@Prefix", "COR"),
                                    new SqlParameter("@TblName", "CorporateRequisition"),
                                    new SqlParameter("@CompCode", request.QProcessCheckerData.cid),
                                    new SqlParameter("@Yr", YearEnd)
                                );
                                intmax = recordGetMaxCodes.MaxValue + 1;
                            }

                            // ✅ SIMPLIFIED: Replace multiple if-else with PadLeft
                            GetMaxValue = (intmax == 0 ? 1 : intmax).ToString().PadLeft(6, '0');

                            maxNo = request.QProcessCheckerData.cid + GetMaxValue;
                            MaxSrNo = $"COR/{YearEnd?.Trim()}/{maxNo}";

                            var data = request.QProcessCheckerData;
                            string toPCCode = _context.Employees
                                .Where(e => e.Ecode == item.RaiseEsp)
                                .Select(e => e.ProfitCenterAct)
                                .FirstOrDefault() ?? "";

                            // ✅ SIMPLIFIED: Use Dictionary + LINQ instead of multiple if statements
                            var stageName = data.stageName;

                            if (stageName == "Stage1")
                            {
                                reqMessg = $"DG Quality Process Check, Jobcode: {data.JobCode}, KVA: {data.Kva}, Model: {data.model}, Eng Sr.No: {data.EngSrNo}, Alt Sr.No: {data.AltSrNo}, Sub-Assembly Part: {item.subAssemblyPart}, 6M Type: {item.sixM}, Remark: {item.Remark}";
                            }
                            else if (stageName == "Stage2")
                            {
                                // ✅ Use Dictionary for optional fields
                                var stage2Fields = new (string Label, string? Value)[]
                                {
                              ("Cpy Sr.No:", data.CpySrNo),
                              ("Bat Sr.No:", data.BatSrNo),
                              ("Bat2 Sr.No:", data.Bat2SrNo),
                              ("Bat3 Sr.No:", data.Bat3SrNo),
                              ("Bat4 Sr.No:", data.Bat4SrNo),
                              ("Bat5 Sr.No:", data.Bat5SrNo),
                              ("Bat6 Sr.No:", data.Bat6SrNo),
                              ("Sub-Assembly Part:", item.subAssemblyPart),
                              ("Remark:", item.Remark),
                              ("6M Type", item.sixM),
                                };

                                // Invalid values to exclude
                                var invalidValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                {
                                 "", "NA", "N/A", "NULL", "0", "-"
                            };


                                reqMessg = $"DG Quality Process Check, Jobcode: {data.JobCode}, KVA: {data.Kva}, Model: {data.model}, Eng Sr.No: {data.EngSrNo}, Alt Sr.No: {data.AltSrNo}";

                                reqMessg += string.Concat(stage2Fields
                                    .Where(f => !string.IsNullOrEmpty(f.Value) && !invalidValues.Contains(f.Value.Trim()))
                                    .Select(f => $", {f.Label}: {f.Value}"));
                            }
                            else // Stage3
                            {
                                // ✅ Use Dictionary for all Stage3 fields
                                var stage3Fields = new (string Label, string? Value)[]
                                {

                                ("PFBCode:",data.PFBCode),
                                ("Engine:", data.Engine),
                                ("Alternator:", data.Alternator),
                                ("Canopy:", data.Canopy),
                                ("Control Panel 1:", data.ControlPanel1),
                                ("Control Panel 2:", data.ControlPanel2),
                                ("Battery 1:", data.Battery1),
                                ("Battery 2:", data.Battery2),
                                ("Battery 3:", data.Battery3),
                                ("Battery 4:", data.Battery4),
                                ("Battery 5:", data.Battery5),
                                ("Battery 6:", data.Battery6),
                                ("KRM:", data.Krm),
                                ("Sub-Assembly Part:", item.subAssemblyPart),
                                ("Remark:", item.Remark),
                                ("6M Type", item.sixM),
                                };

                                // Invalid values to exclude
                                var invalidValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                 "", "NA", "N/A", "NULL", "0", "-"
                            };

                                reqMessg = "DG Quality Process Check, " + string.Join(", ", stage3Fields
                                          .Where(f => !string.IsNullOrEmpty(f.Value) && !invalidValues.Contains(f.Value.Trim()))
                                          .Select(f => $"{f.Label}: {f.Value}"));
                            }

                            await _context.Database.ExecuteSqlRawAsync(@"INSERT INTO CorporateRequisition (ReqCode, Dt, Yr, MaxSrNo, EmpCode, FromPCCode, ToEmpCode, ToPCCode,
                                                                   Priority, ReqMsg, CompanyCode, AssignStatus, Active, Discard)
                                                                   VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13})",
                                MaxSrNo.Trim(),
                                DateTime.Now,
                                MaxSrNo.Substring(4, 5),
                                MaxSrNo.Substring(10, 8),
                                data.ecode.Trim(),
                                data.pccode.Trim(),
                                item.RaiseEsp,
                                toPCCode,
                                "High Priority",
                                reqMessg.Trim(),
                                data.cid,
                                "P",
                                "1",
                                "1"
                            );
                        } 
                    }

                    // ✅ Update QA Status for both Reject and Rework
                    if (request.QProcessCheckerData.qualityStatus == "Reject" || request.QProcessCheckerData.qualityStatus == "Rework")
                    {
                        var stageName = request.QProcessCheckerData.stageName;
                        var data = request.QProcessCheckerData;

                        // ✅ Set status based on qualityStatus
                        string statusValue = request.QProcessCheckerData.qualityStatus == "Reject" ? "Rej" : "Rew";

                        if (stageName == "Stage1" || stageName == "Stage2")
                        {
                            var jobCode = data.JobCode;
                            var pcCode = data.pccode;
                            var partCode = data.partCode;

                            // Build list of valid serial numbers
                            var serialNumbers = new List<string>();

                            if (!string.IsNullOrWhiteSpace(data.EngSrNo))
                                serialNumbers.Add(data.EngSrNo);

                            if (!string.IsNullOrWhiteSpace(data.AltSrNo))
                                serialNumbers.Add(data.AltSrNo);

                            if (stageName == "Stage2")
                            {
                                if (!string.IsNullOrWhiteSpace(data.CpySrNo))
                                    serialNumbers.Add(data.CpySrNo);
                                if (!string.IsNullOrWhiteSpace(data.BatSrNo))
                                    serialNumbers.Add(data.BatSrNo);
                                if (!string.IsNullOrWhiteSpace(data.Bat2SrNo))
                                    serialNumbers.Add(data.Bat2SrNo);
                                if (!string.IsNullOrWhiteSpace(data.Bat3SrNo))
                                    serialNumbers.Add(data.Bat3SrNo);
                                if (!string.IsNullOrWhiteSpace(data.Bat4SrNo))
                                    serialNumbers.Add(data.Bat4SrNo);
                                if (!string.IsNullOrWhiteSpace(data.Bat5SrNo))
                                    serialNumbers.Add(data.Bat5SrNo);
                                if (!string.IsNullOrWhiteSpace(data.Bat6SrNo))
                                    serialNumbers.Add(data.Bat6SrNo);
                            }

                            if (serialNumbers.Any())
                            {
                                var serialNumbersParam = string.Join("','", serialNumbers);

                                var records = await _context.JobCardDetailsSubs
                                    .FromSqlRaw($@"SELECT jds.* FROM JobCardDetailsSub jds 
                               INNER JOIN JobCard j ON j.JobCode = jds.JobCode
                               WHERE j.JobCode = {{0}}
                               AND j.PCCode = {{1}}
                               AND jds.PartCode = {{2}}
                               AND jds.SerialNo IN ('{serialNumbersParam}')",
                                                 jobCode, pcCode, partCode)
                                    .ToListAsync();

                                foreach (var record in records)
                                {
                                    if (stageName == "Stage1")
                                    {
                                        record.Stage1Qastatus = statusValue;  // ✅ "Rej" or "Rew"
                                    }
                                    else if (stageName == "Stage2")
                                    {
                                        record.Stage3Qastatus = statusValue;  // ✅ "Rej" or "Rew"
                                    }
                                }

                                await _context.SaveChangesAsync();
                            }
                        }
                        else if (stageName == "Stage3")
                        {
                            var pfbCode = data.PFBCode;
                            var pcCode = data.pccode;
                            var partCode = data.partCode;

                            // Build list of valid serial numbers for Stage 3
                            var serialNumbers = new List<string>();

                            if (!string.IsNullOrWhiteSpace(data.Engine))
                                serialNumbers.Add(data.Engine);
                            if (!string.IsNullOrWhiteSpace(data.Alternator))
                                serialNumbers.Add(data.Alternator);
                            if (!string.IsNullOrWhiteSpace(data.Canopy))
                                serialNumbers.Add(data.Canopy);
                            if (!string.IsNullOrWhiteSpace(data.ControlPanel1))
                                serialNumbers.Add(data.ControlPanel1);
                            if (!string.IsNullOrWhiteSpace(data.ControlPanel2))
                                serialNumbers.Add(data.ControlPanel2);
                            if (!string.IsNullOrWhiteSpace(data.Battery1))
                                serialNumbers.Add(data.Battery1);
                            if (!string.IsNullOrWhiteSpace(data.Battery2))
                                serialNumbers.Add(data.Battery2);
                            if (!string.IsNullOrWhiteSpace(data.Battery3))
                                serialNumbers.Add(data.Battery3);
                            if (!string.IsNullOrWhiteSpace(data.Battery4))
                                serialNumbers.Add(data.Battery4);
                            if (!string.IsNullOrWhiteSpace(data.Battery5))
                                serialNumbers.Add(data.Battery5);
                            if (!string.IsNullOrWhiteSpace(data.Battery6))
                                serialNumbers.Add(data.Battery6);
                            if (!string.IsNullOrWhiteSpace(data.Krm))
                                serialNumbers.Add(data.Krm);

                            if (serialNumbers.Any())
                            {
                                var serialNumbersParam = string.Join("','", serialNumbers);

                                var processFeedback = await _context.ProcessFeedbackDetailsSubs
                                    .FromSqlRaw($@"SELECT pfds.* FROM ProcessFeedbackDetailsSub pfds 
                               INNER JOIN ProcessFeedBack pf ON pf.Pfbcode = pfds.Pfbcode
                               WHERE pf.Pfbcode = {{0}}
                               AND pf.ProfitCenterCode = {{1}}
                               AND pf.PartCode = {{2}}
                               AND pfds.SerialNo IN ('{serialNumbersParam}')",
                                                 pfbCode, pcCode, partCode)
                                    .ToListAsync();

                                foreach (var record in processFeedback)
                                {
                                    record.Qpcstatus = statusValue;  // ✅ "Rej" or "Rew"
                                }

                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                // 🔹 4. Commit transaction
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error saving Stage Wise Quality Check List", ex);
            }
        }
        #endregion

        #region Employee Methods

        /// <summary>
        /// Retrieves list of active employees for ESP dropdown.
        /// </summary>
        /// <returns>
        /// A list of <see cref="EmployeeListForRaiseESPResponseDTO"/> containing:
        /// - ECode: Employee code
        /// - EmployeeName: Full name (First + Last)
        /// </returns>
        /// <exception cref="Exception">Thrown when database operation fails.</exception>
        public async Task<List<EmployeeListForRaiseESPResponseDTO>> FetchEmployeeListAsync()
        {
            try
            {
                var empList = await _context.Employees
                    .Where(e => e.Active == true)  // Optional: filter active employees only
                    .Select(e => new EmployeeListForRaiseESPResponseDTO
                    {
                        ECode = e.Ecode,
                        EmployeeName = (e.Fname + " " + e.Lname).Trim()
                    })
                    .OrderBy(e => e.EmployeeName)
                    .ToListAsync();

                return empList;
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new Exception("Error fetching employee list: " + ex.Message);
            }
        }
        #endregion

        #region 6M Methods

        /// <summary>
        /// Retrieves 6M categories for dropdown.
        /// </summary>
        /// <returns>
        /// An IQueryable of <see cref="Select6MResponseDTO"/> containing:
        /// - Id: Category ID
        /// - Name: Category name (Man/Machine/Method/Material/Measurement/Mother Nature)
        /// </returns>
        /// <remarks>
        /// 6M refers to the six categories used in root cause analysis:

        public IQueryable<Select6MResponseDTO> Select6MFromDB()
        {
            return _context._6ms
                .Select(x => new Select6MResponseDTO
                {
                    Id = x.Id,
                    Name = x.Name
                });
        }
        #endregion
    }
}
