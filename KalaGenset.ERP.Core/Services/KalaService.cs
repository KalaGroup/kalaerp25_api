using System.Data;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace KalaGenset.ERP.Core.Services
{
    public class KalaService : IKalaService
    {
        private readonly KalaDbContext _context;
        private readonly IConfiguration _configuration;

        public KalaService(KalaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetPendingSiteVisitAsync(string Ecode)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "assignDataService_sp";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@user_id", Ecode));

                    if (conn.State == ConnectionState.Closed)
                        await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                            data.Add(row);
                        }
                    }
                }
            }

            return data;
        }

        public async Task<string> SubmitCustomerFeedbackAsyc(KalaServiceCustomerFeedbackSubmitRequest submitCustomerFeedbackReq)
        {
            string maxNo = "0";
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    string? PCANo = "0";
                    string? YearEnd = "";
                    string GetMaxValue = "0";
                    string MaxSrNo = "0";
                   

                    if (submitCustomerFeedbackReq.actno.Trim().Substring(0, 3) == "ACT")
                    {
                        PCANo = _context.ActionTakens
                            .Where(a => a.Actno == submitCustomerFeedbackReq.actno.Trim())
                            .Select(a => a.Pcacode)
                            .FirstOrDefault();
                    }
                    else
                    {
                        PCANo = submitCustomerFeedbackReq.actno.Trim();
                    }

                    YearEnd = await _context.YearEnds
                        .Select(y => $"{y.StartDate:yy}-{y.EndDate:yy}")
                        .FirstOrDefaultAsync();

                    // ============================================================
                    // INTEGRATED GetMaxNo LOGIC - Get and Increment MaxValue
                    // ============================================================
                    var recordGetMaxCodes = await _context.GetMaxCodes
                        .FirstOrDefaultAsync(x => x.Prefix == "CFS" &&
                                                 x.TblName == "CustomerFeedbackService" &&
                                                 x.CompCode == submitCustomerFeedbackReq.companyId &&
                                                 x.Yr == YearEnd);

                    int intmax = 0;

                    if (recordGetMaxCodes != null)
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            @"UPDATE GetMaxCode SET MaxValue = MaxValue + 1 WHERE Prefix = @Prefix 
                          AND TblName = @TblName AND CompCode = @CompCode AND Yr = @Yr",
                            new SqlParameter("@Prefix", "CFS"),
                            new SqlParameter("@TblName", "CustomerFeedbackService"),
                            new SqlParameter("@CompCode", submitCustomerFeedbackReq.companyId),
                            new SqlParameter("@Yr", YearEnd)
                        );

                        intmax = recordGetMaxCodes.MaxValue + 1;
                    }

                    // Format the number with leading zeros
                    if (intmax == 0)
                        GetMaxValue = "000001";
                    else if (intmax < 10)
                        GetMaxValue = "00000" + intmax;
                    else if (intmax < 100)
                        GetMaxValue = "0000" + intmax;
                    else if (intmax < 1000)
                        GetMaxValue = "000" + intmax;
                    else if (intmax < 10000)
                        GetMaxValue = "00" + intmax;
                    else if (intmax < 100000)
                        GetMaxValue = "0" + intmax;
                    else
                        GetMaxValue = intmax.ToString();
                    // ============================================================

                    MaxSrNo = submitCustomerFeedbackReq.companyId + GetMaxValue;
                    maxNo = "CFS/" + YearEnd.Trim() + "/" + MaxSrNo;

                    // Insert main feedback record
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC InsertUpdateCustomerFeedbackService @SearchType, @SerFeedbackNo, @Dt, @MaxSrNo, @Yr, @ACTNo, @Remark, @CompanyCode",
                        new SqlParameter("@SearchType", "S"),
                        new SqlParameter("@SerFeedbackNo", maxNo.Trim()),
                        new SqlParameter("@Dt", DateTime.Now),
                        new SqlParameter("@MaxSrNo", MaxSrNo.Trim()),
                        new SqlParameter("@Yr", YearEnd.Trim()),
                        new SqlParameter("@ACTNo", PCANo.Trim()),
                        new SqlParameter("@Remark", "record saved from app"),
                        new SqlParameter("@CompanyCode", submitCustomerFeedbackReq.companyId.Trim())
                    );

                    // Insert feedback details
                    var feedbackMappings = new[]
                    {
            new { Type = "PRODUCT", Rating = submitCustomerFeedbackReq.products },
            new { Type = "ORDER", Rating = submitCustomerFeedbackReq.promptness },
            new { Type = "TECHNICAL", Rating = submitCustomerFeedbackReq.technical },
            new { Type = "DELIVERY", Rating = submitCustomerFeedbackReq.delivery },
            new { Type = "COMMUNICATION", Rating = submitCustomerFeedbackReq.communication }
        };

                    foreach (var feedback in feedbackMappings)
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "EXEC InsertUpdateCustomerFeedbackServiceDetails @SearchType, @SerFeedbackNo, @Type, @Rating",
                            new SqlParameter("@SearchType", "S"),
                            new SqlParameter("@SerFeedbackNo", maxNo.Trim()),
                            new SqlParameter("@Type", feedback.Type),
                            new SqlParameter("@Rating", feedback.Rating?.Trim() ?? string.Empty)
                        );
                    }

                    // File upload
                    if (submitCustomerFeedbackReq.file != null && submitCustomerFeedbackReq.file.Length > 0)
                    {
                        string fileName = submitCustomerFeedbackReq.file.FileName;
                        int lastDotIndex = fileName.LastIndexOf('.');

                        string extension = lastDotIndex >= 0 && lastDotIndex < fileName.Length - 1
                            ? fileName.Substring(lastDotIndex).ToLower()
                            : string.Empty;

                        // Validate extension
                        var allowedExtensions = new[] { ".3gp", ".jpg", ".jpeg", ".pdf", ".wmv", ".flv", ".mp4", ".avi", ".mpg", ".wav", ".mpeg", ".dat", ".png" };
                        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                        {
                            throw new Exception("Invalid file format");
                        }

                        string finalFileName = YearEnd.Trim() + submitCustomerFeedbackReq.companyId.Trim() + MaxSrNo.Trim() + extension;
                        string mainFilePath = GetMainFilePath("CustFeedbackServicefile");
                        string savedFileName = Path.Combine(mainFilePath, finalFileName);

                        using (var stream = new FileStream(savedFileName, FileMode.Create))
                        {
                            await submitCustomerFeedbackReq.file.CopyToAsync(stream);
                        }

                        await _context.Database.ExecuteSqlRawAsync(
                            "EXEC InsertUpdateCustFeedbackServiceFile @SearchType, @SerFeedbackNo, @AttachFile",
                            new SqlParameter("@SearchType", "S"),
                            new SqlParameter("@SerFeedbackNo", maxNo.Trim()),
                            new SqlParameter("@AttachFile", finalFileName.Trim())
                        );
                    }

                    // Update action taken records
                    var actionTakenRecord = await _context.ActionTakens
                        .Where(a => a.Pcacode == PCANo && a.FeedBackStatus != "C")
                        .ToListAsync();

                    if (actionTakenRecord.Any())
                    {
                        foreach (var rec in actionTakenRecord)
                        {
                            rec.FeedBackStatus = "C";
                            rec.CustAckDt = DateTime.Now;
                        }
                        await _context.SaveChangesAsync();
                    }

                    // User Activity Log
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC InsertLoginTransactionDetails @TransactionDtTime, @EmpID, @TransactionType, @TransactionFrom, @TransactionNo, @CompanyCode",
                        new SqlParameter("@TransactionDtTime", DateTime.Now),
                        new SqlParameter("@EmpID", submitCustomerFeedbackReq.ecode.Trim()),
                        new SqlParameter("@TransactionType", "S"),
                        new SqlParameter("@TransactionFrom", "Customer Feedback"),
                        new SqlParameter("@TransactionNo", maxNo.Trim()),
                        new SqlParameter("@CompanyCode", submitCustomerFeedbackReq.companyId.Trim())
                    );

                    // Commit transaction
                    await transaction.CommitAsync();                   
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
            return maxNo;
        }

        public async Task<string> GetMaxNo(string prefix, string compCode, string tblName, string yearEnd)
        {
            string strmax = "";
            string NewTransCode = "";
            int intmax = 0;
            //string? yearEnd = _context.YearEnds
            //                         .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" + (y.EndDate.Year % 100).ToString("00"))
            //                         .FirstOrDefault();
            string query = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode WHERE TblName = @TableName
                            AND CompCode = @CompCode
                            AND Prefix = @Prefix
                            AND Yr = @YearEnd";

            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();


            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add(new SqlParameter("@CompCode", compCode));
                cmd.Parameters.Add(new SqlParameter("@Prefix", prefix));
                cmd.Parameters.Add(new SqlParameter("@YearEnd", yearEnd));
                cmd.Parameters.Add(new SqlParameter("@TableName", tblName));

                // Attach existing EF Core transaction if exists
                var currentTransaction = _context.Database.CurrentTransaction;
                if (currentTransaction != null)
                    cmd.Transaction = currentTransaction.GetDbTransaction();

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    intmax = Convert.ToInt32(result);
                }
            }

            if (intmax == 0)
                strmax = "000001";
            else if (intmax < 9)
                strmax = "00000" + (intmax + 1);
            else if (intmax < 99)
                strmax = "0000" + (intmax + 1);
            else if (intmax < 999)
                strmax = "000" + (intmax + 1);
            else if (intmax < 9999)
                strmax = "00" + (intmax + 1);
            else if (intmax < 99999)
                strmax = "0" + (intmax + 1);
            else
                strmax = Convert.ToString(intmax + 1);

            return strmax;
        }

        public string GetMainFilePath(string fileFolder)
        {
            var now = DateTime.Now;
            var year = now.Year.ToString();
            var month = now.ToString("MM MMMM"); // "12 December"

            // Build complete path
            string fullPath = Path.Combine("D:\\Attachments", year, month, fileFolder.Trim());

            // Create all directories in one call
            Directory.CreateDirectory(fullPath); // Creates all missing directories in the path

            return fullPath;
        }

        public async Task<string> SubmitSiteVisitDetailsAsync(SubmitSiteVisitDetailsRequest submitSiteVisitDetailsRequest)
        {
            string actNo = "";
            string finalResult = "";
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    string GetMaxValue = "0"; ;

                    var parameters = new[]
                    {
                      new SqlParameter("@pca_code", submitSiteVisitDetailsRequest.pca_code.Trim())
                    };

                    FeedbackSaveResponseDto chkFeedbackStatus = _context.Database
                        .SqlQueryRaw<FeedbackSaveResponseDto>("EXEC feedbackSaveOrNo @pca_code", parameters)
                        .ToList()
                        .FirstOrDefault();

                    if (chkFeedbackStatus?.RecName == "CustFeedPen")
                    {
                        // ✅ Rollback not needed here since no changes made yet
                        finalResult = "Customer Feedback Pending";
                        return;
                    }

                    // Get Year End
                    string? YearEnd = await _context.YearEnds
                        .Select(y => $"{y.StartDate:yy}-{y.EndDate:yy}")
                        .FirstOrDefaultAsync();

                    // Get Max Value
                    GetMaxValue = await GetMaxAsync("ActionTaken", "MaxValue", YearEnd, submitSiteVisitDetailsRequest.comp_code.Trim());

                    // Generate ACT Number
                    actNo = $"ACT/{YearEnd.Trim()}/{submitSiteVisitDetailsRequest.comp_code.Trim()}{GetMaxValue}";

                    // ✅ Update MaxValue with verification
                    var result = await _context.Database.ExecuteSqlRawAsync(@"UPDATE GetMaxCode SET MaxValue = MaxValue + 1 OUTPUT INSERTED.MaxValue
                                                                        WHERE Prefix = @Prefix AND TblName = @TblName AND CompCode = @CompCode AND Yr = @Yr",
                        new SqlParameter("@Prefix", "ACT"),
                        new SqlParameter("@TblName", "ActionTaken"),
                        new SqlParameter("@CompCode", submitSiteVisitDetailsRequest.comp_code.Trim()),
                        new SqlParameter("@Yr", YearEnd.Trim())
                    );

                    // Verify update happened
                    if (result == 0)
                    {
                        throw new Exception("Failed to update MaxValue - no matching record found");
                    }

                    // Save signature filename
                    string file_name = $"{actNo.Substring(10, 8)}.jpg";

                    // ✅ Insert ActionTaken
                    await _context.Database.ExecuteSqlRawAsync(@"EXEC InsertUpdateActionTaken 
                @SearchType, @ACTNo, @Dt, @SolvedDt, @MaxSrNo, @Yr, @PCACode, 
                @EngSrNo, @GenSrNo, @DGHours, @NosOfStart, @ActionStatus, 
                @NextDt, @CommissioningDt, @Remark, @FeedBackStatus, @QualityStatus, 
                @MailStatus, @MailCC, @CustAckDt, @CustAckName, @CustAckSign",
                        new SqlParameter("@SearchType", "S"),
                        new SqlParameter("@ACTNo", actNo),
                        new SqlParameter("@Dt", DateTime.Now),
                        new SqlParameter("@SolvedDt", DateTime.Parse($"{submitSiteVisitDetailsRequest.work_date.Trim()} {DateTime.Now:HH:mm:ss}")),
                        new SqlParameter("@MaxSrNo", actNo.Substring(10, 8)),
                        new SqlParameter("@Yr", YearEnd),
                        new SqlParameter("@PCACode", submitSiteVisitDetailsRequest.pca_code.Trim()),
                        new SqlParameter("@EngSrNo", submitSiteVisitDetailsRequest.eng_sr_no.Trim()),
                        new SqlParameter("@GenSrNo", string.Empty),
                        new SqlParameter("@DGHours", submitSiteVisitDetailsRequest.dg_hour.Trim()),
                        new SqlParameter("@NosOfStart", submitSiteVisitDetailsRequest.no_of_start.Trim()),
                        new SqlParameter("@ActionStatus", submitSiteVisitDetailsRequest.action_status.Trim()),
                        new SqlParameter("@NextDt", DateTime.Now.Date),
                        new SqlParameter("@CommissioningDt", DateTime.Now.Date),
                        new SqlParameter("@Remark", "record save from apps"),
                        new SqlParameter("@FeedBackStatus", "P"),
                        new SqlParameter("@QualityStatus", "P"),
                        new SqlParameter("@MailStatus", "C"),
                        new SqlParameter("@MailCC", string.Empty),
                        new SqlParameter("@CustAckDt", DateTime.Now),
                        new SqlParameter("@CustAckName", submitSiteVisitDetailsRequest.name.Trim()),
                        new SqlParameter("@CustAckSign", file_name)
                    );

                    // ✅ Save signature file
                    if (!string.IsNullOrEmpty(submitSiteVisitDetailsRequest.sign))
                    {
                        string base64Data = submitSiteVisitDetailsRequest.sign.Trim();
                        if (base64Data.Contains(","))
                        {
                            base64Data = base64Data.Split(',')[1];
                        }

                        byte[] imageBytes = Convert.FromBase64String(base64Data);
                        if (imageBytes.Length > 0)
                        {
                            string directory = GetMainFilePath("ActionTakenfile");
                            Directory.CreateDirectory(directory);
                            string filePath = Path.Combine(directory, file_name);
                            await File.WriteAllBytesAsync(filePath, imageBytes);
                        }
                    }

                    // ✅ Insert ActionTaken Details
                    await _context.Database.ExecuteSqlRawAsync(@"EXEC InsertUpdateActionTakenDetails @SearchType, @ACTNo, @SrNo, @ProblemCode, @SubProblemcode, 
                                                           @ActionTaken, @ServiceAnalysis, @RootCause, @PreventiveAction",
                        new SqlParameter("@SearchType", "S"),
                        new SqlParameter("@ACTNo", actNo),
                        new SqlParameter("@SrNo", "1"),
                        new SqlParameter("@ProblemCode", submitSiteVisitDetailsRequest.problem_code.Trim()),
                        new SqlParameter("@SubProblemcode", submitSiteVisitDetailsRequest.problem_sub_code.Trim()),
                        new SqlParameter("@ActionTaken", submitSiteVisitDetailsRequest.corrective_action.Trim()),
                        new SqlParameter("@ServiceAnalysis", string.Empty),
                        new SqlParameter("@RootCause", string.Empty),
                        new SqlParameter("@PreventiveAction", submitSiteVisitDetailsRequest.preventive_action.Trim())
                    );

                    // ✅ Insert photos
                    if (submitSiteVisitDetailsRequest.photos != null && submitSiteVisitDetailsRequest.photos.Count > 0)
                    {
                        string photoDirectory = GetMainFilePath("ActionTakenfile");
                        Directory.CreateDirectory(photoDirectory);

                        for (int j = 0; j < submitSiteVisitDetailsRequest.photos.Count; j++)
                        {
                            var photo = submitSiteVisitDetailsRequest.photos[j];

                            if (photo.Length > 0)
                            {
                                // Get file type
                                string fileType = submitSiteVisitDetailsRequest.file_types != null &&
                                                 j < submitSiteVisitDetailsRequest.file_types.Count
                                    ? submitSiteVisitDetailsRequest.file_types[j]
                                    : "photo";

                                // Generate filename
                                string fileName = $"{actNo.Substring(4, 5)}{actNo.Substring(10, 8)}-{j + 1}.jpg";
                                string filePath = Path.Combine(photoDirectory, fileName);

                                // Save to disk
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await photo.CopyToAsync(stream);
                                }

                                // Insert to database
                                await _context.Database.ExecuteSqlRawAsync(
                                    @"EXEC InsertUpdateActionTakenFileDetails 
                            @SearchType, @ACTNo, @PSrNo, @FileType, @FSRNo, @FileName",
                                    new SqlParameter("@SearchType", "S"),
                                    new SqlParameter("@ACTNo", actNo),
                                    new SqlParameter("@PSrNo", j + 1),
                                    new SqlParameter("@FileType", fileType),
                                    new SqlParameter("@FSRNo", "-"),
                                    new SqlParameter("@FileName", fileName)
                                );
                            }
                        }
                    }

                    // ✅ Update PrimaryCompAssign Status
                    if (submitSiteVisitDetailsRequest.action_status.Trim() == "P")
                    {
                        var record = await _context.PrimaryCompAssigns
                            .FirstOrDefaultAsync(x =>
                                x.Pcacode == submitSiteVisitDetailsRequest.pca_code.Trim() &&
                                x.CompNo == submitSiteVisitDetailsRequest.comp_code.Trim());

                        if (record != null)
                        {
                            record.Astatus = "I"; // In-Progress
                        }
                    }
                    else if (submitSiteVisitDetailsRequest.action_status.Trim() == "F")
                    {
                        var record = await _context.PrimaryCompAssigns
                            .FirstOrDefaultAsync(x =>
                                x.Pcacode == submitSiteVisitDetailsRequest.pca_code.Trim() &&
                                x.CompNo == submitSiteVisitDetailsRequest.comp_code.Trim());

                        if (record != null)
                        {
                            record.Astatus = "C"; // Completed
                        }
                    }

                    await _context.PrimaryCompAssignDetails.Where(x => x.Pcacode == submitSiteVisitDetailsRequest.pca_code.Trim())
                                                           .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.RecSavedAppStatus, 1));

                    // ✅ Save all EF Core changes
                    await _context.SaveChangesAsync();

                    // ✅ Log user activity
                    await _context.Database.ExecuteSqlRawAsync(@"EXEC InsertLoginTransactionDetails 
                                                           @TransactionDtTime, @EmpID, @TransactionType, @TransactionFrom, @TransactionNo, @CompanyCode",
                        new SqlParameter("@TransactionDtTime", DateTime.Now),
                        new SqlParameter("@EmpID", submitSiteVisitDetailsRequest.user_code.Trim()),
                        new SqlParameter("@TransactionType", "S"),
                        new SqlParameter("@TransactionFrom", "Site Visit"),
                        new SqlParameter("@TransactionNo", actNo),
                        new SqlParameter("@CompanyCode", submitSiteVisitDetailsRequest.comp_code.Trim()));

                    // ✅ Commit transaction - ALL operations successful
                    await transaction.CommitAsync();
                    finalResult = actNo;
                }
                catch (Exception ex)
                {
                    // ✅ Rollback transaction on any error
                    await transaction.RollbackAsync();
                    throw; // Re-throw to let controller handle the response
                }
            });

            return finalResult; // ✅ Return ACT number instead of generic message
        }
        private async Task<string> GetMaxAsync(string tablename, string fieldname, string yr, string compid)
        {
            string max = "000001";

            try
            {
                int intmax = 0;

                if (tablename.Trim() == "ActionTaken")
                {
                    // Query GetMaxCode table
                    var result = await _context.GetMaxCodes
                        .Where(x => x.TblName == tablename &&
                                   x.CompCode == compid &&
                                   x.Prefix == "ACT")
                        .Select(x => x.MaxValue)
                        .FirstOrDefaultAsync();

                    intmax = result;
                }
                else
                {
                    // Dynamic query using raw SQL with parameters
                    string query = $"SELECT MAX(SUBSTRING({fieldname}, 13, 7)) FROM {tablename} WHERE yr = @yr AND CompanyCode = @compid";

                    var connection = _context.Database.GetDbConnection();
                    if (connection.State != ConnectionState.Open)
                        await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = query;
                        cmd.Parameters.Add(new SqlParameter("@yr", yr));
                        cmd.Parameters.Add(new SqlParameter("@compid", compid));

                        var result = await cmd.ExecuteScalarAsync();
                        intmax = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
                    }
                }

                // Increment and format
                intmax++;

                if (intmax < 10)
                    max = "00000" + intmax;
                else if (intmax < 100)
                    max = "0000" + intmax;
                else if (intmax < 1000)
                    max = "000" + intmax;
                else if (intmax < 10000)
                    max = "00" + intmax;
                else if (intmax < 100000)
                    max = "0" + intmax;
                else
                    max = intmax.ToString();
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error in GetMaxAsync: {ex.Message}");
                throw;
            }

            return max;
        }
    }
}
