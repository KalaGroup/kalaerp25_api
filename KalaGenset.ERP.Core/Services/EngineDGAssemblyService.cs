using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using AutoMapper;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.RequestDTO;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace KalaGenset.ERP.Core.Services
{
    public class EngineDGAssemblyService : IEngineDGAssembly
    {

        private readonly KalaDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        string _getDGRate = "";

        public EngineDGAssemblyService(KalaDbContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _secretKey = configuration["JwtSettings:SecretKey"];
        }
        public async Task<BaseGetStageScanDts?> GetStageScanDetailsByQrSrNo(EngineDetailsRequest reqParam)
        {
            try
            {
                string decodeQRSrNo = HttpUtility.UrlDecode(reqParam.SerialNo);

                if (reqParam.Stage == "0") //stage 1 start
                {
                    var parameters = new[]
                           {
                           new SqlParameter("@strSrNo",decodeQRSrNo),
                           new SqlParameter("@strPartCode", reqParam.PartCode),
                           new SqlParameter("@strCat", reqParam.Category),
                           new SqlParameter("@strStage", reqParam.Stage),
                           new SqlParameter("@PCCode",reqParam.PCCode)
                    };

                    var results = await _context.Database
                        .SqlQueryRaw<GetStageFirstStartDts>("EXEC GetStageScanDts @strSrNo,@strPartCode,@strCat,@strStage,@PCCode", parameters)
                        .ToListAsync();

                    return results.FirstOrDefault();
                }
                else if (reqParam.Stage == "1")  // stage 1 End
                {
                    var parameters = new[]
                        {
                          new SqlParameter("@strSrNo",decodeQRSrNo),
                          new SqlParameter("@strPartCode", reqParam.PartCode),
                          new SqlParameter("@strCat", reqParam.Category),
                          new SqlParameter("@strStage", reqParam.Stage),
                          new SqlParameter("@PCCode",reqParam.PCCode)
                    };

                    var results = await _context.Database
                        .SqlQueryRaw<GetStageFirstEndDts>("EXEC GetStageScanDts @strSrNo,@strPartCode,@strCat,@strStage,@PCCode", parameters)
                        .ToListAsync();

                    return results.FirstOrDefault();
                }
                else if (reqParam.Stage == "3") // stage 2 -- no start, end for second stage. fetch data at once.
                {
                    var parameters = new[]
                    {
                          new SqlParameter("@strSrNo",decodeQRSrNo),
                          new SqlParameter("@strPartCode", reqParam.PartCode),
                          new SqlParameter("@strCat", reqParam.Category),
                          new SqlParameter("@strStage", reqParam.Stage),
                          new SqlParameter("@PCCode",reqParam.PCCode)
                    };

                    var results = await _context.Database
                       .SqlQueryRaw<GetSecondStageDts>("EXEC GetStageScanDts @strSrNo,@strPartCode,@strCat,@strStage,@PCCode", parameters)
                       .ToListAsync();

                    return results.FirstOrDefault();
                }
                else if (reqParam.Stage == "4") //stage 3 Start
                {
                    var parameters = new[]
                      {
                     new SqlParameter("@strSrNo",decodeQRSrNo),
                          new SqlParameter("@strPartCode", reqParam.PartCode),
                          new SqlParameter("@strCat", reqParam.Category),
                          new SqlParameter("@strStage", reqParam.Stage),
                          new SqlParameter("@PCCode",reqParam.PCCode)
                    };

                    var results = await _context.Database
                       .SqlQueryRaw<GetStageThirdStartDts>("EXEC GetStageScanDts @strSrNo,@strPartCode,@strCat,@strStage,@PCCode", parameters)
                       .ToListAsync();

                    return results.FirstOrDefault();
                }
                else if (reqParam.Stage == "5") //stage 3 End
                {
                    var parameters = new[]
                      {
                     new SqlParameter("@strSrNo",decodeQRSrNo),
                          new SqlParameter("@strPartCode", reqParam.PartCode),
                          new SqlParameter("@strCat", reqParam.Category),
                          new SqlParameter("@strStage", reqParam.Stage),
                          new SqlParameter("@PCCode",reqParam.PCCode)
                    };

                    var results = await _context.Database
                       .SqlQueryRaw<GetStageThirdEndDts>("EXEC GetStageScanDts @strSrNo,@strPartCode,@strCat,@strStage,@PCCode", parameters)
                       .ToListAsync();

                    return results.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return null;
        }
  
        public async Task<TestReportScanDts?> GetTestReportScanDetails(TestReportDetailsRequest testReportDetailsRequestDTO)
        {
            try
            {
                var result = new TestReportScanDts();

                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync(); 

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "GetTRScanDts";
                        command.CommandType = CommandType.StoredProcedure;

                        var strSrNoParam = new SqlParameter("@strSrNo", SqlDbType.VarChar) { Value = testReportDetailsRequestDTO.strSrNo ?? (object)DBNull.Value };
                        var strCatParam = new SqlParameter("@strCat", SqlDbType.VarChar) { Value = testReportDetailsRequestDTO.strCat ?? (object)DBNull.Value };
                        var DGSrNoParam = new SqlParameter("@DGSrNo", SqlDbType.VarChar) { Value = testReportDetailsRequestDTO.strDGSrNo ?? (object)DBNull.Value };
                        var PCCodeParam = new SqlParameter("@PCCode", SqlDbType.VarChar) { Value = testReportDetailsRequestDTO.strPCCode ?? (object)DBNull.Value };

                        command.Parameters.Add(strSrNoParam);
                        command.Parameters.Add(strCatParam);
                        command.Parameters.Add(DGSrNoParam);
                        command.Parameters.Add(PCCodeParam);

                        // Execute the command and read the results
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                result = new TestReportScanDts
                                {
                                    KVA = reader["KVA"] != DBNull.Value ? Convert.ToDouble(reader["KVA"]) : 0,
                                    PFBCode = reader["PFBCode"]?.ToString(),
                                    Dt = reader["Dt"]?.ToString(),
                                    SerialNo = reader["SerialNo"]?.ToString(),
                                    Partcode = reader["Partcode"]?.ToString(),
                                    Partdesc = reader["Partdesc"]?.ToString(),
                                    PanelType = reader["PanelType"]?.ToString(),
                                    Engdts = reader["Engdts"]?.ToString(),
                                    Altdts = reader["Altdts"]?.ToString(),
                                    DieselQty = reader["DieselQty"] != DBNull.Value ? Convert.ToDouble(reader["DieselQty"]) : 0,
                                    DieselPart = reader["DieselPart"]?.ToString(),
                                    DieselRate = reader["DieselRate"] != DBNull.Value ? Convert.ToDouble(reader["DieselRate"]) : 0,
                                    TRCode = reader["TRCode"]?.ToString(),
                                    TRStartTime = reader["TRStartTime"]?.ToString(),
                                    TREndTime = reader["TREndTime"]?.ToString(),
                                    DGStartTime = reader["DGStartTime"]?.ToString(),
                                    DGEndTime = reader["DGEndTime"]?.ToString(),
                                    QAStatus = reader["QAStatus"]?.ToString(),
                                    TRStatus = reader["TRStatus"]?.ToString()
                                };
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error fetching Test Report Scan Details", ex);
            }
        }

        public async Task<List<Dictionary<string, object?>>> GetPackingSlipScanDtsAsync(PackingSlipDetailsRequest req)
        {
            var result = new List<Dictionary<string, object?>>();

            using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            using (var command = new SqlCommand("GetPSScanDts", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@strSrNo", req.strSrNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DGSrNo", req.StrDGSrNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@strCat", req.strCat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@strCPBatCnt", req.strCPBatCnt ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PCCode", req.StrPCCode ?? (object)DBNull.Value);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) 
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            row[columnName] = value;
                        }

                        result.Add(row);
                    }
                }
            }

            return result;
        }

        public async Task<List<Dictionary<string, object?>>> GetJobCard1RptAsync(JobCardRequest req)
        {
            var result = new List<Dictionary<string, object?>>();

            using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            using (var command = new SqlCommand("getJobCard1Rpt_sp", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@Type", req.Type ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Code", req.Code ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FromDt", req.FromDt ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ToDt", req.ToDt ?? (object)DBNull.Value);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            row[columnName] = value;
                        }

                        result.Add(row);
                    }
                }
            }

            return result;
        }

        public async Task<List<Dictionary<string, object?>>> GetJobCard2RptAsync(JobCardRequest req)
        {
            var result = new List<Dictionary<string, object?>>();

            using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            using (var command = new SqlCommand("getJobCard2Rpt_sp", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@Type", req.Type ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Code", req.Code ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FromDt", req.FromDt ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ToDt", req.ToDt ?? (object)DBNull.Value);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            row[columnName] = value;
                        }

                        result.Add(row);
                    }
                }
            }

            return result;
        }

        public async Task<List<Dictionary<string, object?>>> GetJobCardDGDtsAsync(string strJobCardType, string strcompID)
        {
            var result = new List<Dictionary<string, object?>>();

            using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            using (var command = new SqlCommand("GetJobCardDGDts", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@JobCardType", strJobCardType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CompCode", strcompID ?? (object)DBNull.Value);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            row[columnName] = value;
                        }

                        result.Add(row);
                    }
                }
            }

            return result;
        }

        public async Task<string> ExecuteGetCPPartcodeAsync(string cpTypeValue)
        {
            // Ensure cpTypeValue is not null or empty to prevent SQL errors
            if (string.IsNullOrWhiteSpace(cpTypeValue))
            {
                throw new ArgumentException("CP Type value cannot be null or empty.", nameof(cpTypeValue));
            }

            var parameters = new[] { new SqlParameter("@cpType", cpTypeValue.Trim()) };

            var result = await _context.Database
                .SqlQueryRaw<GetCPPartcode>("EXEC GetCPPartcode @cpType, '0'", parameters)
                .FirstOrDefaultAsync(); // Fetch the first record

            return result?.PanelTypePartcode ?? string.Empty;
        }

        public async Task<List<GetTRPrcChkDts>> FetchProcessCheckpointsFromDB(string stageName, string statusName)
        {
            var parameters = new[]
            {
               new SqlParameter ("@stageName",stageName),
               new SqlParameter ("@status",statusName)
            };
            var results = await _context.Database
                .SqlQueryRaw<GetTRPrcChkDts>("EXEC GetTRPrcChkDts @stageName,@status", parameters)
                .ToListAsync();
            return results;
        }

        public IQueryable<Select6MResponseDTO> Select6MFromDB()
        {
            //var context = new KalaDbContext(); 
            return _context._6ms
                .Select(x => new Select6MResponseDTO
                {
                    Id = x.Id,
                    Name = x.Name
                });
        }

        public async Task<bool> CheckStageStatus(string JBCode, string EngSrNo, int StageNo)
        {
            if (StageNo == 0)
            {
                int count = await _context.JobCardDetailsSubs
                    .Where(j => j.JobCode == JBCode &&
                                j.SerialNo == EngSrNo &&
                                j.Stage1StartStatus == "D")
                    .CountAsync();

                return count > 0; // Return true if count > 0, otherwise false
            }
            else if (StageNo == 1)
            {
                int count = await _context.JobCardDetailsSubs
                     .Where(j => j.JobCode == JBCode &&
                                 j.SerialNo == EngSrNo &&
                                 j.Stage1Status == "D")
                     .CountAsync();

                return count > 0;
            }
            else if (StageNo == 3)
            {
                int count = await _context.JobCardDetailsSubs
                     .Where(j => j.JobCode == JBCode &&
                                 j.SerialNo == EngSrNo &&
                                 j.Stage3Status == "D")
                     .CountAsync();

                return count > 0;
            }
            else if (StageNo == 4)
            {
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "Select isNull(Count(pfd.SerialNo),0) as CntEngSRNo " +
                                          "From ProcessFeedBack Pf " +
                                          "Inner Join ProcessFeedBackDetailsSub Pfd on Pf.PfbCode=Pfd.PfbCode " +
                                          "where TRFcode=@JobCode and pfd.SerialNo=@EngSrNo";
                  
                    command.Parameters.Add(new SqlParameter("@JobCode", JBCode));
                    command.Parameters.Add(new SqlParameter("@EngSrNo", EngSrNo));

                    await _context.Database.OpenConnectionAsync();  

                    int count = Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);

                    return count > 0;
                }
  
            }
            return false;
        }

        public async Task<List<GetDGKitDetails>> GetDGKitDetailsFromDB(string strPrdPartCode, string strPCCode)
        {
            List<GetDGKitDetails> _dgKitDetails = new List<GetDGKitDetails>();
            string? strBOMCode = await _context.Boms
                                .Where(b => b.PartCode == strPrdPartCode.Trim() && b.Active == true && b.Discard == true)
                                .Select(b => b.Bomcode + "-->" + b.WgtHr.ToString() + "-->" + b.WgtCr.ToString())
                                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(strBOMCode))
            {
                return new List<GetDGKitDetails>();
            }

            string[] prodDts = strBOMCode.Trim().Split("-->");

            // Generate Profit Center Code Mapping
            string ratePCCode = strPCCode.Trim() switch
            {
                "01.005" => "01.007,01.005,03.008",
                "03.038" => "03.001,03.038,03.008",
                "01.004" or "04.001" or "14.001" or "03.051" => "01.005,01.004,04.001,01.064,14.001,01.023,03.040,03.051",
                "01.023" => "01.007,01.023,01.064",
                "03.040" => "01.007,01.023,03.040,01.064",
                _ => strPCCode.Trim()
            };

            string formattedRatePCCode = "'" + string.Join("','", ratePCCode.Split(',')) + "'";
            try
            {
                var parameters = new[]
                      {
                           new SqlParameter("@strPCCode",strPCCode),
                           new SqlParameter("@RatePCCode",formattedRatePCCode),
                           new SqlParameter("@ProdDts",prodDts[0]),
                           new SqlParameter("@strPrdPartCode",strPrdPartCode),
                    };

                _dgKitDetails = await _context.Database
                      .SqlQueryRaw<GetDGKitDetails>("EXEC GetDGKitDetails @strPCCode,@RatePCCode,@ProdDts,@strPrdPartCode", parameters)
                      .ToListAsync();

                var partCodes = _dgKitDetails.Select(k => k.PartCode?.Trim()).Distinct().ToList();
                Dictionary<string, double> stockData = await GetAllWIPStock(partCodes, strPCCode.Trim());

                double gridAmt = 0;
                
                foreach (var kitDetail in _dgKitDetails)
                {
                    // Ensure Quantity is not null
                    double qty = kitDetail.Qty ?? 0;

                    // Calculate TotalQty
                    kitDetail.TotalQty = (1 * qty).ToString();

                    // Get Rate (Safe conversion & method call)
                    kitDetail.Rate = Math.Round(
                        await GetRate4ProcessAsync(
                            kitDetail.PartCode?.Trim() ?? "",
                            strPCCode.Trim(),
                            kitDetail.ConversionValue,
                            kitDetail.UOMCode?.Trim() ?? "",
                            kitDetail.Mob?.Trim() ?? "",
                            strPrdPartCode.Trim(),
                            "N",
                            "",
                            prodDts[0].Trim()
                        ),
                        2
                    );

                    // Get StockQty (as double, not string)
                    // double stockQty = Math.Round(await GetWIPStock(kitDetail.PartCode?.Trim() ?? "", strPCCode.Trim()), 2);
                    //kitDetail.StockQty = stockQty.ToString();  // Store as string if needed
                    double stockQty = stockData.TryGetValue(kitDetail.PartCode?.Trim() ?? "", out double value) ? value : 0;
                    kitDetail.StockQty = stockQty.ToString();

                    // Calculate QAP
                    if (double.TryParse(kitDetail.TotalQty, out double totalQty))
                    {
                        kitDetail.QAP = Math.Round(stockQty - totalQty, 2);
                    }
                    else
                    {
                        kitDetail.QAP = 0;  // Default to 0 if parsing fails
                    }
                    
                    kitDetail.Amount = Math.Round(kitDetail.Rate * totalQty, 2);                   
                    gridAmt += kitDetail.Amount;
                }
            }
            catch (Exception)
            {
                throw;
            }
          
            return _dgKitDetails;
        }

        public async Task<List<GetTRKitDetails>> GetTRKitDetailsFromDB(string strPartcode, string strDGSrNo, string strPfbCode)
        {
            List<GetTRKitDetails> _TRKitDetails = new List<GetTRKitDetails>();
            try
            {
                strPfbCode = HttpUtility.UrlDecode(strPfbCode);
                Console.Write(strPfbCode);
                var parameters = new[]
                     {
                           new SqlParameter("@strPartcode",strPartcode),
                           new SqlParameter("@strDGSrNo",strDGSrNo),
                           new SqlParameter("@strPfbCode",strPfbCode)
                    };

                _TRKitDetails = await _context.Database
                      .SqlQueryRaw<GetTRKitDetails>("EXEC GetTRProdDts @strPartcode,@strDGSrNo,@strPfbCode", parameters)
                      .ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
            return _TRKitDetails;
        }

        public async Task<List<MOFAddPartDetailsResponseDTO>> GetMOFAdditionalPartDtsFromDB(string strMOFCode)
        {
            strMOFCode = HttpUtility.UrlDecode(strMOFCode);
            List<MOFAddPartDetailsResponseDTO> _mofDetails = new List<MOFAddPartDetailsResponseDTO>();
            try
            {
                var parameter = new SqlParameter("@strMOFCode", strMOFCode);

                _mofDetails = await _context.Database
                    .SqlQueryRaw<MOFAddPartDetailsResponseDTO>("EXEC GetMOFAddPartDts @strMOFCode", parameter)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
            return _mofDetails;
        }

        public async Task<double> GetRate4ProcessAsync(string strPartCode, string PCCode, double ConvValue, string struom, string strMOB, string strKIT, string RateType, string strRateFor, string strBOmCode)
        {
            string strOriginalPCCode = PCCode;
            string strField = "Rate";
            double prate = 0;
            string query = string.Empty;

            if (PCCode == "01.005" || PCCode == "03.038") // Canopy Assembly
            {
                if (strPartCode.Trim().StartsWith("005")) PCCode = "01.064";
                else if (strPartCode.Trim().StartsWith("00712") || strPartCode.Trim().StartsWith("00002060211") || strPartCode.Trim().StartsWith("012")) PCCode = "03.008";
                else if ((PCCode == "01.005" && strPartCode.Trim().StartsWith("004")) ||
                         (strPartCode.Length > 11 && (strPartCode[11] == '0' || strPartCode[11] == '1') && strPartCode[10] == '1')) PCCode = "01.005";
                else if ((PCCode == "03.038" && strPartCode.Trim().StartsWith("004")) ||
                         (strPartCode.Length > 11 && (strPartCode[11] == '0' || strPartCode[11] == '1') && strPartCode[10] == '1')) PCCode = "03.038";
                else if ((PCCode == "01.005" && strPartCode.Trim().StartsWith("004")) &&
                         (strPartCode.Length > 10 && (strPartCode[10] == '4' || strPartCode[10] == '5'))) PCCode = "01.007";
                else if ((PCCode == "03.038" && strPartCode.Trim().StartsWith("004")) &&
                         (strPartCode.Length > 10 && (strPartCode[10] == '4' || strPartCode[10] == '5'))) PCCode = "03.001";
                else if (PCCode == "01.005" && strPartCode.Trim().StartsWith("004")) PCCode = "01.007"; // PowderCoating U1
            }
            else if (new[] { "03.040", "01.004", "05.001", "04.001", "14.001", "03.051" }.Contains(PCCode)) // DG Assembly
            {
                if (strPartCode.Trim().StartsWith("005")) PCCode = "01.064"; // GoemKit
                else if (strPartCode.Trim().StartsWith("40")) PCCode = "01.005"; // Canopy
                else if (strPartCode.Trim().StartsWith("004") && strPartCode.Length > 11 && (strPartCode[11] == '5' || strPartCode[11] == '6')) PCCode = "01.005','01.007','01.023"; // CP stand
                else PCCode = "01.004','01.023','01.005','01.007','03.051','03.040";
            }

            try
            {
                if (strMOB.Trim() == "B")
                {
                    if (strPartCode.Trim().StartsWith("006"))
                    {
                        query = @"SELECT TOP 1 ISNULL(Rate, 0) AS Rate 
                          FROM SupplierPriceListChanged 
                          WHERE PartCode = @PartCode 
                          AND ChangeDateTime = (
                              SELECT MAX(ChangeDateTime) 
                              FROM SupplierPriceListChanged SPC 
                              INNER JOIN PurchaseCosting PC ON SPC.CostingCode = PC.PCCode
                              WHERE SPC.PartCode = @PartCode 
                              AND (RateSelected = 'M' OR RateSelected = 'T') 
                              AND PC.Active = '1' 
                              AND PC.Discard = '1' 
                              AND ChangeDateTime <= GETDATE())";
                    }
                    else
                    {
                        query = @"SELECT TOP 1 ISNULL(SuppRate + PrcCostUnitKG, 0) AS Rate 
                          FROM BOMdetails WHERE PartCode = @PartCode";
                    }
                }
                else if (strMOB.Trim() == "M" || strMOB.Trim() == "O")
                {
                    var pcCodeList = PCCode.Split(',').Select(code => code.Trim().Replace("'", "")).ToList();
                    string formattedPCCode = string.Join(",", pcCodeList.Select(x => $"'{x}'"));

                    query = $@"SELECT TOP 1 ISNULL(Rate, 0) AS Rate 
                       FROM ProfitcenterPLdetails 
                       WHERE ProfitCenterCode IN ({formattedPCCode}) 
                       AND PartCode = @PartCode";
                }

                if (!string.IsNullOrEmpty(query))
                {
                        await _context.Database.OpenConnectionAsync();
                        using (var command = _context.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = query;
                            command.CommandType = CommandType.Text;

                            // Add parameters
                            var param = command.CreateParameter();
                            param.ParameterName = "@PartCode";
                            param.Value = strPartCode ?? (object)DBNull.Value;
                            command.Parameters.Add(param);

                            var result = await command.ExecuteScalarAsync();

                            prate = result != null ? Convert.ToDouble(result) : 0; // Assign default 0 if no value is found
                        }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRate4ProcessAsync: {ex.Message}");
            }

            return prate;
        }

        public async Task<Dictionary<string, double>> GetAllWIPStock(List<string> partCodes, string strPCCode)
        {
            if (partCodes == null || partCodes.Count == 0)
            {
                return new Dictionary<string, double>();
            }

            // Convert partCodes list into a SQL-safe format
            string partCodesString = string.Join("','", partCodes.Select(p => p.Replace("'", "''"))); // Handle escaping

            // Construct raw SQL query
            string query = $@"
               SELECT PartCode, 
               SUM(CASE WHEN ToProfitCenterCode = @p0 THEN ReceivedQty ELSE 0 END) -
               SUM(CASE WHEN FromProfitCenterCode = @p0 THEN IssueQty ELSE 0 END) AS StockQty
               FROM Stockwip
               WHERE PartCode IN ('{partCodesString}')
               AND (ToProfitCenterCode = @p0 OR FromProfitCenterCode = @p0)
               GROUP BY PartCode";

            // Execute raw SQL query and store results in a dictionary
            var result = await _context.Database.SqlQueryRaw<DgKitStockResultDTO>(query, strPCCode).ToListAsync();

            return result.ToDictionary(x => x.PartCode, x => x.StockQty);

        }

        public async Task<List<InternalTOCResult>> GetInternalTOCResults(string PCCode)
        {
            List<InternalTOCResult> results = new List<InternalTOCResult>();

            try
            {
                    var conn = _context.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open)
                        await conn.OpenAsync();
                    
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "InternalTOCReq";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@PCCode", PCCode.Trim()));

                    var currentTransaction = _context.Database.CurrentTransaction;
                        if (currentTransaction != null)
                        {
                            cmd.Transaction = currentTransaction.GetDbTransaction();
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new InternalTOCResult
                                {
                                    Partcode = reader["Partcode"].ToString(),
                                    MOQ = reader["MOQ"] != DBNull.Value ? Convert.ToDecimal(reader["MOQ"]) : 0,
                                    Poper = reader["Poper"] != DBNull.Value ? Convert.ToDecimal(reader["Poper"]) : 0,
                                    Stk = reader["Stk"] != DBNull.Value ? Convert.ToDecimal(reader["Stk"]) : 0,
                                    PndReq = reader["PndReq"] != DBNull.Value ? Convert.ToDecimal(reader["PndReq"]) : 0,
                                    Req = reader["Req"] != DBNull.Value ? Convert.ToDecimal(reader["Req"]) : 0,
                                    Flag = reader["Flag"] != DBNull.Value ? Convert.ToInt32(reader["Flag"]) : 0, 
                                    RaiseReqQty = reader["RaiseReqQty"] != DBNull.Value ? Convert.ToDecimal(reader["RaiseReqQty"]) : 0,
                                    FromPC = reader["FromPC"].ToString(),
                                    ToPCCode = reader["ToPCCode"].ToString()
                                });
                            }
                        }
                    }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }

            return results;
        }

        public async Task SubmitDGAssemblyDetails(DGAssemblySubmitRequest dgStageScanReq)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string _getKVA = await GetKVAFromPartTable(dgStageScanReq.ProductCode);

                if (dgStageScanReq.StageNo == 0) //Stage I Start
                {
                    var sqlQuery = @"INSERT INTO StockWIP 
                               (FromProfitCenterCode, PartCode, ReceivedCode, ReceivedDate, ReceivedQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                              VALUES
                              (@PCCode, @ProductCode, @ReceivedCode, CAST(@ReceivedDate AS DATETIME), @ReceivedQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                    var parameters = new[]
                    {
                       new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value), // NVARCHAR
                       new SqlParameter("@ProductCode", dgStageScanReq.ProductCode?.Trim() ?? (object)DBNull.Value), // NVARCHAR
                       new SqlParameter("@ReceivedCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"), // NVARCHAR
                       new SqlParameter("@ReceivedDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime }, // DATETIME with proper type
                       new SqlParameter("@ReceivedQty", 1) { SqlDbType = SqlDbType.Float }, // FLOAT
                       new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value), // NVARCHAR
                       new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                       new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                       new SqlParameter("@StageName", "StageI") // VARCHAR
                    };
                    await _context.Database.ExecuteSqlRawAsync(sqlQuery, parameters);

                    if (dgStageScanReq.EngPartCode.Trim().Substring(0, 3) == "001")
                    {
                        try
                        {
                            var jobCardDetails = await _context.JobCardDetailsSubs
                                .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim() &&
                                            j.PartCode == dgStageScanReq.ProductCode.Trim() &&
                                            j.SerialNo == dgStageScanReq.EngSrNo.Trim() &&
                                            j.SrNoPartCode == dgStageScanReq.EngPartCode)
                                .ToListAsync();

                            if (jobCardDetails.Any())
                            {
                                foreach (var jobCardDetail in jobCardDetails)
                                {
                                    jobCardDetail.Stage1StartStatus = "D";
                                    jobCardDetail.Stage1StartPlay = dgStageScanReq.EngPlay?.Trim();
                                    jobCardDetail.Stage1StartDate = DateTime.Now;
                                }

                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                Console.WriteLine("No matching JobCardDetailssub records found to update.");
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }

                        try
                        {
                            var sqlQuery1 = "";
                             sqlQuery1 = @"INSERT INTO StockWIP 
                               (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                              VALUES
                              (@PCCode, @EnginePartCode, @IssueCode, CAST(@IssueDate AS DATETIME), @IssueQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                            var parameters1 = new[]
                            {
                              new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value), // NVARCHAR
                              new SqlParameter("@EnginePartCode", dgStageScanReq.EngPartCode?.Trim() ?? (object)DBNull.Value), // NVARCHAR
                              new SqlParameter("@IssueCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"), // NVARCHAR
                              new SqlParameter("@IssueDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime }, // DATETIME with proper type
                              new SqlParameter("@IssueQty", 1) { SqlDbType = SqlDbType.Float }, // FLOAT
                              new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value), // NVARCHAR
                              new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                              new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                              new SqlParameter("@StageName", "StageI") // VARCHAR
                            };
                            await _context.Database.ExecuteSqlRawAsync(sqlQuery1, parameters1);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }

                    if (dgStageScanReq.AltPartcode.Trim().Substring(0, 3) == "002")
                    {
                        try
                        {
                            var jobCardDetails = await _context.JobCardDetailsSubs
                                .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim() &&
                                            j.PartCode == dgStageScanReq.ProductCode.Trim() &&
                                            j.SerialNo == dgStageScanReq.AltSrno.Trim() &&
                                            j.SrNoPartCode == dgStageScanReq.AltPartcode)
                                .ToListAsync();

                            if (jobCardDetails.Any())
                            {
                                foreach (var jobCardDetail in jobCardDetails)
                                {
                                    jobCardDetail.Stage1StartStatus = "D";
                                    jobCardDetail.Stage1StartPlay = dgStageScanReq.EngPlay?.Trim();
                                    jobCardDetail.Stage1StartDate = DateTime.Now;
                                }

                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                Console.WriteLine("No matching JobCardDetailssub records found to update.");
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }

                        try
                        {
                            var sqlQuery1 = "";
                            sqlQuery1 = @"INSERT INTO StockWIP 
                               (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                              VALUES
                              (@PCCode, @AltPartCode, @IssueCode, CAST(@IssueDate AS DATETIME), @IssueQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                            var parameters1 = new[]
                            {
                              new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value), // NVARCHAR
                              new SqlParameter("@AltPartCode", dgStageScanReq.AltPartcode?.Trim() ?? (object)DBNull.Value), // NVARCHAR
                              new SqlParameter("@IssueCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"), // NVARCHAR
                              new SqlParameter("@IssueDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime }, // DATETIME with proper type
                              new SqlParameter("@IssueQty", 1) { SqlDbType = SqlDbType.Float }, // FLOAT
                              new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value), // NVARCHAR
                              new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                              new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                              new SqlParameter("@StageName", "StageI") // VARCHAR
                            };
                            await _context.Database.ExecuteSqlRawAsync(sqlQuery1, parameters1);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }

                    string ChkDGRate = "0";
                    ChkDGRate = await GetTransName(dgStageScanReq.PCCode, dgStageScanReq.ProductCode, "StageI");

                    if (ChkDGRate == "0")
                    {
                        _getDGRate = await GetTotalSuppRateAsync(dgStageScanReq.ProductCode);

                        try
                        {
                            var sqlQuery2 = @"Insert Into PCStageWiseRate(Dt,PCCode,PartCode,StageName,JobCode,Rate)
                                          values(@Date,@PCCode,@ProductCode,@StageName,@JBCode,@Rate)";

                            var parameters2 = new[]
                            {
                            new SqlParameter("@Date", DateTime.Now){ SqlDbType = SqlDbType.DateTime },
                            new SqlParameter("@PCCode", dgStageScanReq.PCCode),
                            new SqlParameter("@ProductCode",dgStageScanReq.ProductCode),
                            new SqlParameter("@StageName","StageI"),
                            new SqlParameter("@JBCode", dgStageScanReq.JBCode),
                            new SqlParameter("@Rate", double.Parse(_getDGRate.Trim()))
                        };
                            await _context.Database.ExecuteSqlRawAsync(sqlQuery2, parameters2);
                        }
                        catch (Exception)
                        {
                            throw;
                        }

                        try
                        {
                            var sqlQuery3 = @"Insert Into PCStageWiseRateChange(Dt,PCCode,PartCode,StageName,JobCode,Rate)
                                          values(@Date,@PCCode,@ProductCode,@StageName,@JBCode,@Rate)";

                            var parameters3 = new[]
                            {
                            new SqlParameter("@Date", DateTime.Now){ SqlDbType = SqlDbType.DateTime },
                            new SqlParameter("@PCCode", dgStageScanReq.PCCode),
                            new SqlParameter("@ProductCode",dgStageScanReq.ProductCode),
                            new SqlParameter("@StageName","StageI"),
                            new SqlParameter("@JBCode", dgStageScanReq.JBCode),
                            new SqlParameter("@Rate", double.Parse(_getDGRate.Trim()))
                        };
                            await _context.Database.ExecuteSqlRawAsync(sqlQuery3, parameters3);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        _getDGRate = await GetTotalSuppRateAsync(dgStageScanReq.ProductCode);

                        try
                        {
                            var PCStageWiseRateDetails = await _context.PcstageWiseRates
                                .Where(p =>
                                p.Pccode == dgStageScanReq.PCCode &&
                                           p.PartCode == dgStageScanReq.ProductCode &&
                                           p.StageName == "StageI")
                                .ToListAsync();

                            if (PCStageWiseRateDetails.Any())
                            {
                                foreach (var PCStageDetail in PCStageWiseRateDetails)
                                {
                                    PCStageDetail.Rate = double.Parse(_getDGRate);
                                    PCStageDetail.Dt = DateTime.Now;
                                    PCStageDetail.JobCode = dgStageScanReq.JBCode.Trim();
                                }
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                Console.WriteLine("No Matching PCStageWiseRateDetails Found");
                            }

                        }
                        catch (Exception)
                        {
                            throw;
                        }

                        try
                        {
                            var sqlQuery3 = @"Insert Into PCStageWiseRateChange(Dt,PCCode,PartCode,StageName,JobCode,Rate)
                                          values(@Date,@PCCode,@ProductCode,@StageName,@JBCode,@Rate)";

                            var parameters3 = new[]
                            {
                            new SqlParameter("@Date", DateTime.Now){ SqlDbType = SqlDbType.DateTime },
                            new SqlParameter("@PCCode", dgStageScanReq.PCCode),
                            new SqlParameter("@ProductCode",dgStageScanReq.ProductCode),
                            new SqlParameter("@StageName","StageI"),
                            new SqlParameter("@JBCode", dgStageScanReq.JBCode),
                            new SqlParameter("@Rate", double.Parse(_getDGRate.Trim()))
                            };

                            await _context.Database.ExecuteSqlRawAsync(sqlQuery3, parameters3);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
                else if (dgStageScanReq.StageNo == 1) //Stage1 End
                {
                    //method for save recorded audio and video path to DB
                    await SaveRecordedFiles(dgStageScanReq);

                    var sqlQuery = @"INSERT INTO StockWIP 
                               (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                              VALUES
                              (@PCCode, @ProductCode, @IssueCode, CAST(@IssueDate AS DATETIME), @IssueQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                    var parameters = new[]
                    {
                       new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                       new SqlParameter("@ProductCode", dgStageScanReq.ProductCode?.Trim() ?? (object)DBNull.Value),
                       new SqlParameter("@IssueCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"),
                       new SqlParameter("@IssueDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                       new SqlParameter("@IssueQty", 1) { SqlDbType = SqlDbType.Float },
                       new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                       new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                       new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                       new SqlParameter("@StageName", "StageI")
                    };
                    await _context.Database.ExecuteSqlRawAsync(sqlQuery, parameters);

                    var sqlQuery1 = "";
                    sqlQuery1 = @"INSERT INTO StockWIP 
                               (FromProfitCenterCode, PartCode, ReceivedCode, ReceivedDate, ReceivedQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                              VALUES
                              (@PCCode, @ProductCode, @ReceivedCode, CAST(@ReceivedDate AS DATETIME), @ReceivedQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                    var parameters1 = new[]
                    {
                       new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                       new SqlParameter("@ProductCode", dgStageScanReq.ProductCode?.Trim() ?? (object)DBNull.Value),
                       new SqlParameter("@ReceivedCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"),
                       new SqlParameter("@ReceivedDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                       new SqlParameter("@ReceivedQty", 1) { SqlDbType = SqlDbType.Float },
                       new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                       new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                       new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                       new SqlParameter("@StageName", "StageIII")
                    };
                    await _context.Database.ExecuteSqlRawAsync(sqlQuery1, parameters1);

                    if (dgStageScanReq.EngPartCode.Trim().Substring(0, 3) == "001")
                    {
                        var jobCard = await _context.JobCardDetailsSubs
                            .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim()
                             && j.SerialNo == dgStageScanReq.EngSrNo.Trim()
                             && j.SrNoPartCode == dgStageScanReq.EngPartCode.Trim()
                             && j.PartCode == dgStageScanReq.ProductCode.Trim())
                            .FirstOrDefaultAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            jobCard.Stage1Status = "D";
                            jobCard.Stage1Date = DateTime.Now;
                            jobCard.Stage2Status = "D";
                            jobCard.Stage1EndPlay = dgStageScanReq.EngPlay.Trim();
                            jobCard.Stage2Date = DateTime.Now;
                        }
                        else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejection")
                        {
                            jobCard.Stage1Status = "R";
                        }
                        await _context.SaveChangesAsync();
                    }
                    if (dgStageScanReq.AltPartcode.Trim().Substring(0, 3) == "002")
                    {
                        var jobCard = await _context.JobCardDetailsSubs
                           .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim()
                            && j.SerialNo == dgStageScanReq.AltSrno.Trim()
                            && j.SrNoPartCode == dgStageScanReq.AltPartcode.Trim()
                            && j.PartCode == dgStageScanReq.ProductCode.Trim())
                           .FirstOrDefaultAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            jobCard.Stage1Status = "D";
                            jobCard.Stage1Date = DateTime.Now;
                            jobCard.Stage2Status = "D";
                            jobCard.Stage2Date = DateTime.Now;
                        }
                        else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejection")
                        {
                            jobCard.Stage1Status = "R";
                            jobCard.Stage2Status = "R";
                        }
                        await _context.SaveChangesAsync();
                    }

                    string _getDGStartTime = "";
                    _getDGStartTime = await GetDGStartTime(dgStageScanReq.JBCode, dgStageScanReq.EngSrNo);

                    foreach (var chekpoint in dgStageScanReq.PrcChkDts)
                    {
                        var sqlquery = @"INSERT INTO PrcChkDetails (TransCode, Dt, MainSerialNo, PrcName, ChkPointId, PrcChkPoints, PrcStatus, DGStartTime, QA6M)
                                      VALUES
                                       (@TransCode,@Date,@MainSerialNo,@PrcName,@ChkPointId,@PrcChkPoints,@PrcStatus,@DGStartTime,@QA6M)";
                        var Parameters = new[]
                        {
                           new SqlParameter("@TransCode",dgStageScanReq.JBCode.Trim()),
                           new SqlParameter("@Date",DateTime.Now),
                           new SqlParameter("@MainSerialNo",dgStageScanReq.EngSrNo),
                           new SqlParameter("@PrcName","DG Stage1"),
                           new SqlParameter("@ChkPointId",chekpoint.PrcId),
                           new SqlParameter("@PrcChkPoints",chekpoint.Remark),
                           new SqlParameter("@PrcStatus",dgStageScanReq.PrcStatus),
                           new SqlParameter("@DGStartTime",_getDGStartTime),
                           new SqlParameter("@QA6M",dgStageScanReq.QA6M)
                        };
                        await _context.Database.ExecuteSqlRawAsync(sqlquery, Parameters);
                    }

                    string cntStageStatus = "0";
                    string query = @"select isnull(Count(Stage1Status),0) as Stage1Status from JobCardDetailssub where Stage1Status='P' and jobCode=@JobCode";
                    cntStageStatus = await GetStageStatusCount(query, dgStageScanReq.JBCode);

                    if (cntStageStatus == "0")
                    {
                        var jobcard = await _context.JobCards
                             .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim())
                             .FirstOrDefaultAsync();

                        if (jobcard != null)
                        {
                            jobcard.Stage1Status = "D";
                            jobcard.Stage2Status = "D";
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                else if (dgStageScanReq.StageNo == 3)//stage two(2)
                {
                    var sqlQuery = @"INSERT INTO StockWIP 
                               (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                              VALUES
                              (@PCCode, @ProductCode, @IssueCode, CAST(@IssueDate AS DATETIME), @IssueQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                    var parameters = new[]
                    {
                       new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                       new SqlParameter("@ProductCode", dgStageScanReq.ProductCode?.Trim() ?? (object)DBNull.Value),
                       new SqlParameter("@IssueCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"),
                       new SqlParameter("@IssueDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                       new SqlParameter("@IssueQty", 1) { SqlDbType = SqlDbType.Float },
                       new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                       new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                       new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                       new SqlParameter("@StageName", "StageIII")
                    };
                    await _context.Database.ExecuteSqlRawAsync(sqlQuery, parameters);

                    //var sqlQuery1 = "";
                    //sqlQuery1 = @"INSERT INTO StockWIP 
                    //           (FromProfitCenterCode, PartCode, ReceivedCode, ReceivedDate, ReceivedQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                    //          VALUES
                    //          (@PCCode, @ProductCode, @ReceivedCode, CAST(@ReceivedDate AS DATETIME), @ReceivedQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                    //var parameters1 = new[]
                    //{
                    //   new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                    //   new SqlParameter("@ProductCode", dgStageScanReq.ProductCode?.Trim() ?? (object)DBNull.Value),
                    //   new SqlParameter("@ReceivedCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"),
                    //   new SqlParameter("@ReceivedDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                    //   new SqlParameter("@ReceivedQty", 1) { SqlDbType = SqlDbType.Float },
                    //   new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                    //   new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                    //   new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                    //   new SqlParameter("@StageName", "StageIV  ")
                    //};
                    //await _context.Database.ExecuteSqlRawAsync(sqlQuery1, parameters1);

                    if (dgStageScanReq.EngPartCode.Trim().Substring(0, 3) == "001")
                    {
                        var jobCard1E = await _context.JobCardDetailsSubs
                              .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim()
                               && j.SerialNo == dgStageScanReq.EngSrNo.Trim()
                               && j.SrNoPartCode == dgStageScanReq.EngPartCode.Trim()
                               && j.PartCode == dgStageScanReq.ProductCode.Trim())
                              .FirstOrDefaultAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            jobCard1E.Stage3Status = "D";
                            jobCard1E.Stage3Date = DateTime.Now;
                        }
                        else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejection")
                        {
                            jobCard1E.Stage3Status = "R";
                        }
                        await _context.SaveChangesAsync();
                    }

                    if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                    {
                        var jobcard2E = await _context.Jobcard2DetailsSubs
                            .Where(j => j.SerialNo == dgStageScanReq.EngSrNo.Trim()
                            && j.SrNoPartCode == dgStageScanReq.EngPartCode.Trim()
                            && j.PartCode == dgStageScanReq.ProductCode.Trim())
                            .FirstOrDefaultAsync();

                        jobcard2E.Stage3Status = "D";
                        jobcard2E.JobCard1 = dgStageScanReq.JBCode;

                        await _context.SaveChangesAsync();
                    }

                    if (dgStageScanReq.AltPartcode.Trim().Substring(0, 3) == "002")
                    {
                        var jobCard1A = await _context.JobCardDetailsSubs
                                .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim()
                                 && j.SerialNo == dgStageScanReq.AltSrno.Trim()
                                 && j.SrNoPartCode == dgStageScanReq.AltPartcode.Trim()
                                 && j.PartCode == dgStageScanReq.ProductCode.Trim())
                                .FirstOrDefaultAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            jobCard1A.Stage3Status = "D";
                            jobCard1A.Stage3Date = DateTime.Now;
                        }
                        else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejection")
                        {
                            jobCard1A.Stage3Status = "R";
                        }
                        await _context.SaveChangesAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            var jobcard2A = await _context.Jobcard2DetailsSubs
                                .Where(j => j.SerialNo == dgStageScanReq.AltSrno.Trim()
                                && j.SrNoPartCode == dgStageScanReq.AltPartcode.Trim()
                                && j.PartCode == dgStageScanReq.ProductCode.Trim())
                                .FirstOrDefaultAsync();
                            if (jobcard2A != null)
                            {
                                jobcard2A.Stage3Status = "D";
                                jobcard2A.JobCard1 = dgStageScanReq.JBCode;
                            }

                            await _context.SaveChangesAsync();
                        }
                    }

                    if (dgStageScanReq.CpyPartcode.Trim().Substring(0, 2) == "40")
                    {
                        var jobCard1C = await _context.JobCardDetailsSubs
                                 .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim()
                                  && j.SerialNo == dgStageScanReq.CpySrno.Trim()
                                  && j.SrNoPartCode == dgStageScanReq.CpyPartcode.Trim()
                                  && j.PartCode == dgStageScanReq.ProductCode.Trim())
                                 .FirstOrDefaultAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            jobCard1C.Stage3Status = "D";
                            jobCard1C.Stage3Date = DateTime.Now;
                        }
                        else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejection")
                        {
                            jobCard1C.Stage3Status = "R";
                        }

                        await _context.SaveChangesAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            var jobcard2C = await _context.Jobcard2DetailsSubs
                                 .Where(j => j.SerialNo == dgStageScanReq.CpySrno.Trim()
                                 && j.SrNoPartCode == dgStageScanReq.CpyPartcode.Trim()
                                 && j.PartCode == dgStageScanReq.ProductCode.Trim())
                                 .FirstOrDefaultAsync();

                            if (jobcard2C != null)
                            {
                                jobcard2C.Stage3Status = "D";
                                jobcard2C.JobCard1 = dgStageScanReq.JBCode; 
                            }

                            await _context.SaveChangesAsync();
                        }

                        var SqlQuerycpy = @"INSERT INTO StockWIP 
                                       (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                                       VALUES
                                       (@PCCode, @ProductCode, @IssueCode, CAST(@IssueDate AS DATETIME), @IssueQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                        var Parameterscpy = new[]
                        {
                             new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                             new SqlParameter("@ProductCode", dgStageScanReq.CpyPartcode?.Trim() ?? (object)DBNull.Value),
                             new SqlParameter("@IssueCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"),
                             new SqlParameter("@IssueDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                             new SqlParameter("@IssueQty", 1) { SqlDbType = SqlDbType.Float },
                             new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                             new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                             new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                             new SqlParameter("@StageName", "StageIII")
                          };
                        await _context.Database.ExecuteSqlRawAsync(SqlQuerycpy, Parameterscpy);
                    }

                    if (dgStageScanReq.BatPartcode.Trim().Substring(0, 3) == "001")
                    {
                        var jobCard1B = await _context.JobCardDetailsSubs
                                  .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim()
                                   && j.SerialNo == dgStageScanReq.BatSrno.Trim()
                                   && j.SrNoPartCode == dgStageScanReq.BatPartcode.Trim()
                                   && j.PartCode == dgStageScanReq.ProductCode.Trim())
                                  .FirstOrDefaultAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            jobCard1B.Stage3Status = "D";
                            jobCard1B.Stage3Date = DateTime.Now;
                        }
                        else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejection")
                        {
                            jobCard1B.Stage3Status = "R";
                        }

                        await _context.SaveChangesAsync();

                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            var jobcard2B = await _context.Jobcard2DetailsSubs
                                 .Where(j => j.SerialNo == dgStageScanReq.BatSrno.Trim()
                                 && j.SrNoPartCode == dgStageScanReq.BatPartcode.Trim()
                                 && j.PartCode == dgStageScanReq.ProductCode.Trim())
                                 .FirstOrDefaultAsync();

                            if (jobcard2B != null)
                            {
                                jobcard2B.Stage3Status = "D";
                                jobcard2B.JobCard1 = dgStageScanReq.JBCode;

                            }
                            await _context.SaveChangesAsync();
                        }

                        var SqlQueryEng = @"INSERT INTO StockWIP 
                                       (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType, PanelTypeId, StageName)
                                       VALUES
                                       (@PCCode, @ProductCode, @IssueCode, CAST(@IssueDate AS DATETIME), @IssueQty, @ToPCCode, @StockType, @PanelTypeId, @StageName)";

                        var ParametersEng = new[]
                        {
                             new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                             new SqlParameter("@ProductCode", dgStageScanReq.BatPartcode?.Trim() ?? (object)DBNull.Value),
                             new SqlParameter("@IssueCode", $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"),
                             new SqlParameter("@IssueDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                             new SqlParameter("@IssueQty", 1) { SqlDbType = SqlDbType.Float },
                             new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                             new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                             new SqlParameter("@PanelTypeId", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int }, // Force 0
                             new SqlParameter("@StageName", "StageIII")
                          };
                        await _context.Database.ExecuteSqlRawAsync(SqlQueryEng, ParametersEng);
                    }

                    if (double.Parse(_getKVA) > 160)
                    {
                        if (dgStageScanReq.Bat2Partcode.Trim().Length > 2)
                        {
                            if (dgStageScanReq.Bat2Partcode.Trim().Substring(0, 3) == "010")
                            {
                                var jobCard1B2 = await _context.JobCardDetailsSubs
                                   .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim()
                                    && j.SerialNo == dgStageScanReq.Bat2Srno.Trim()
                                    && j.SrNoPartCode == dgStageScanReq.Bat2Partcode.Trim()
                                    && j.PartCode == dgStageScanReq.ProductCode.Trim())
                                   .FirstOrDefaultAsync();

                                if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                                {
                                    jobCard1B2.Stage3Status = "D";
                                    jobCard1B2.Stage3Date = DateTime.Now;
                                }
                                else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejection")
                                {
                                    jobCard1B2.Stage3Status = "R";
                                }

                                await _context.SaveChangesAsync();

                                if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                                {
                                    var jobcard2B2 = await _context.Jobcard2DetailsSubs
                                         .Where(j => j.SerialNo == dgStageScanReq.Bat2Srno.Trim()
                                         && j.SrNoPartCode == dgStageScanReq.Bat2Partcode.Trim()
                                         && j.PartCode == dgStageScanReq.ProductCode.Trim())
                                         .FirstOrDefaultAsync();

                                    if (jobcard2B2 != null)
                                    {
                                        jobcard2B2.Stage3Status = "D";
                                        jobcard2B2.JobCard1 = dgStageScanReq.JBCode; 
                                    }

                                    await _context.SaveChangesAsync();
                                }

                                var _stockWip = await _context.Stockwips
                                    .Where(s => s.FromProfitCenterCode == dgStageScanReq.PCCode.Trim()
                                    && s.IssueCode == $"{dgStageScanReq.JBCode}-->{dgStageScanReq.EngSrNo}"
                                    && s.StageName == "StageIII"
                                    && s.PartCode == dgStageScanReq.BatPartcode.Trim()
                                    && s.ToProfitCenterCode == dgStageScanReq.PCCode.Trim())
                                    .FirstOrDefaultAsync();

                                if (_stockWip != null)
                                {
                                    _stockWip.IssueQty = _stockWip.IssueQty + 1;
                                }

                                await _context.SaveChangesAsync();
                            }
                        }
                    }

                    foreach (var chekpoint in dgStageScanReq.PrcChkDts)
                    {
                        var sqlqueryprc = @"INSERT INTO PrcChkDetails (TransCode, Dt, MainSerialNo, PrcName, ChkPointId, PrcChkPoints, PrcStatus, DGStartTime, QA6M)
                                      VALUES
                                       (@TransCode,@Date,@MainSerialNo,@PrcName,@ChkPointId,@PrcChkPoints,@PrcStatus,@DGStartTime,@QA6M)";
                        var Parametersprc = new[]
                        {
                           new SqlParameter("@TransCode",dgStageScanReq.JBCode.Trim()),
                           new SqlParameter("@Date",DateTime.Now),
                           new SqlParameter("@MainSerialNo",dgStageScanReq.EngSrNo),
                           new SqlParameter("@PrcName","DG Stage3"),
                           new SqlParameter("@ChkPointId",chekpoint.PrcId),
                           new SqlParameter("@PrcChkPoints",chekpoint.Remark),
                           new SqlParameter("@PrcStatus",dgStageScanReq.PrcStatus),
                           new SqlParameter("@DGStartTime",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                           new SqlParameter("@QA6M",dgStageScanReq.QA6M)
                        };
                        await _context.Database.ExecuteSqlRawAsync(sqlqueryprc, Parametersprc);
                    }

                    if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                    {
                        var jobcardDetails = await _context.JobCardDetails
                            .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim()
                            && j.PartCode == dgStageScanReq.ProductCode.Trim())
                            .FirstOrDefaultAsync();

                        if (jobcardDetails != null)
                        {
                            jobcardDetails.Stage3Qty = jobcardDetails.Stage3Qty + 1;
                        }

                        await _context.SaveChangesAsync();
                    }

                    string cntStageStatus = "0";
                    string query = @"select isnull(Count(Stage3Status),0) as Stage3Status from JobCardDetailssub where Stage3Status ='P' and jobCode=@JobCode";
                    cntStageStatus = await GetStageStatusCount(query, dgStageScanReq.JBCode);
                    if (cntStageStatus != "0")
                    {
                        var jobcards = await _context.JobCards
                            .Where(j => j.JobCode == dgStageScanReq.JBCode.Trim())
                            .FirstOrDefaultAsync();

                        if (jobcards != null)
                        {
                            jobcards.Stage3Status = "D";
                        }

                        await _context.SaveChangesAsync();
                    }

                    string chkDGRate = "0";
                    chkDGRate = await GetTransName(dgStageScanReq.PCCode, dgStageScanReq.ProductCode, "StageIV");
                    if (chkDGRate == "0")
                    {
                        _getDGRate = "0";
                        _getDGRate = await GetTotalSuppRateAsync(dgStageScanReq.ProductCode, dgStageScanReq.CpyPartcode);

                        var SqlQueryDgrate = @"Insert Into PCStageWiseRate(Dt,PCCode,PartCode,StageName,JobCode,Rate)
                                          values(@Date,@PCCode,@ProductCode,@StageName,@JBCode,@Rate)";

                        var ParametersDgrate = new[]
                        {
                            new SqlParameter("@Date", DateTime.Now){ SqlDbType = SqlDbType.DateTime },
                            new SqlParameter("@PCCode", dgStageScanReq.PCCode),
                            new SqlParameter("@ProductCode",dgStageScanReq.ProductCode),
                            new SqlParameter("@StageName","StageIV"),
                            new SqlParameter("@JBCode", dgStageScanReq.JBCode),
                            new SqlParameter("@Rate", double.Parse(_getDGRate.Trim()))
                        };
                        await _context.Database.ExecuteSqlRawAsync(SqlQueryDgrate, ParametersDgrate);

                        var sqlQuery2 = "";
                        sqlQuery2 = @"Insert Into PCStageWiseRateChange(Dt,PCCode,PartCode,StageName,JobCode,Rate)
                                          values(@Date,@PCCode,@ProductCode,@StageName,@JBCode,@Rate)";

                        var Parameters1 = new[]
                        {
                            new SqlParameter("@Date", DateTime.Now){ SqlDbType = SqlDbType.DateTime },
                            new SqlParameter("@PCCode", dgStageScanReq.PCCode),
                            new SqlParameter("@ProductCode",dgStageScanReq.ProductCode),
                            new SqlParameter("@StageName","StageIV"),
                            new SqlParameter("@JBCode", dgStageScanReq.JBCode),
                            new SqlParameter("@Rate", double.Parse(_getDGRate.Trim()))
                        };
                        await _context.Database.ExecuteSqlRawAsync(sqlQuery2, Parameters1);
                    }
                    else
                    {
                        _getDGRate = "0";
                        _getDGRate = await GetTotalSuppRateAsync(dgStageScanReq.ProductCode, dgStageScanReq.CpyPartcode);

                        var pcStageWiseRate = await _context.PcstageWiseRates
                            .Where(p => p.Pccode == dgStageScanReq.PCCode.Trim()
                            && p.PartCode == dgStageScanReq.ProductCode.Trim()
                            && p.StageName == "StageIV")
                            .FirstOrDefaultAsync();

                        if (pcStageWiseRate != null)
                        {
                            pcStageWiseRate.Rate = double.Parse(_getDGRate.Trim());

                            await _context.SaveChangesAsync();
                        }

                        var SqlQuery1 = "";
                        SqlQuery1 = @"Insert Into PCStageWiseRateChange(Dt,PCCode,PartCode,StageName,JobCode,Rate)
                                          values(@Date,@PCCode,@ProductCode,@StageName,@JBCode,@Rate)";

                        var Parameters1 = new[]
                        {
                            new SqlParameter("@Date", DateTime.Now){ SqlDbType = SqlDbType.DateTime },
                            new SqlParameter("@PCCode", dgStageScanReq.PCCode),
                            new SqlParameter("@ProductCode",dgStageScanReq.ProductCode),
                            new SqlParameter("@StageName","StageIV"),
                            new SqlParameter("@JBCode", dgStageScanReq.JBCode),
                            new SqlParameter("@Rate", double.Parse(_getDGRate.Trim()))
                        };
                        await _context.Database.ExecuteSqlRawAsync(SqlQuery1, Parameters1);
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task<string> SubmitDGAssemblyStage4Details(DGAssemblySubmitRequest  dgStageScanReq)
        {
            string? StrBOMCode = "", GetMaxValue = "", PrcNo = "", StrDGRate = "0", StrCPRate = "0", StrCP2Rate = "0", StrCRRate = "", StrHRRate = "0", DGCFM = "", DGNo = "";
            using var transaction = await _context.Database.BeginTransactionAsync();

            if (dgStageScanReq.Remark == "Start")
            {
                string strkVA = "";
                string? CladdingRate = "0";
                string[] CPType = Regex.Split(dgStageScanReq.CPType.Trim(), "-->");

                var partDetails = _context.Parts
                   .Where(p => p.PartCode == dgStageScanReq.ProductCode.Trim())
                   .Select(p => new { p.Kva, p.Model })
                   .FirstOrDefault();

                if (partDetails != null)
                {
                    CladdingRate = _context.SilCladdingRates
                        .Where(s => s.Kva == partDetails.Kva.ToString().Trim() &&
                                    s.Model == partDetails.Model &&
                                    s.CompanyCode == dgStageScanReq.PCCode.Trim().Substring(0, 2) &&
                                    s.Active == true &&
                                    s.Discard == true &&
                                    s.Auth == true)
                        .Select(s => s.Rate.ToString())
                        .FirstOrDefault();

                    strkVA = partDetails.Kva.ToString().Trim();
                }

                DGCFM = _context.Parts
                  .Where(p => p.PartCode == dgStageScanReq.ProductCode.Trim() &&
                               p.Active == true)
                  .Select(p => p.Cfm)
                  .FirstOrDefault();


                #region validation for serial Numbers

                string engSrNo = dgStageScanReq.EngSrNo.Trim();
                string altSrNo = dgStageScanReq.AltSrno.Trim();
                string cpySrNo = dgStageScanReq.CpySrno.Trim();

                bool IsInvalidSerial(string serial) => string.IsNullOrEmpty(serial) || serial == "0";

                if (IsInvalidSerial(engSrNo)) return "Please Scan Engine SerialNo";
                if (IsInvalidSerial(altSrNo)) return "Please Scan Alternator SerialNo";
                if (IsInvalidSerial(cpySrNo)) return "Please Scan Canopy SerialNo";


                if (double.Parse(CPType[1].Trim()) != 0 && double.Parse(CPType[1].Trim()) != 150)
                {
                    if (dgStageScanReq.CPSrno.Trim() == "0" || dgStageScanReq.CPSrno.Trim() == "")
                    {
                        PrcNo = "Please Scan Control Panel(1) SerialNo";
                        return PrcNo;
                    }
                }
                if (double.Parse(CPType[1].Trim()) == 0 && DGCFM.Trim() == "STD")
                {
                    if (dgStageScanReq.CPSrno.Trim() == "0" || dgStageScanReq.CPSrno.Trim() == "")
                    {
                        PrcNo = "Please Scan Control Panel(1) SerialNo";
                        return PrcNo;
                    }
                }
                if (double.Parse(strkVA.Trim()) >= 180)
                {
                    if (dgStageScanReq.BatSrno.Trim() == "0" || dgStageScanReq.BatSrno.Trim() == "")
                    {
                        PrcNo = "Please Scan Battery(1) SerialNo";
                        return PrcNo;
                    }
                }
                
                foreach (var item in dgStageScanReq.DGKitDetails)
                {
                    if (double.Parse(item.StockQty) < 0)
                    {
                        PrcNo = $"Insufficient Stock For Part= {item.PartCode.Trim()}";
                        return PrcNo;
                    }
                }
                #endregion

                StrBOMCode = _context.Boms
                        .Where(b => b.PartCode == dgStageScanReq.ProductCode.Trim() &&
                        b.Active == true &&
                        b.Discard == true)
                       .Select(b => $"{b.Bomcode}-->{b.Wgt}-->{b.Sqft}-->{b.WgtHr}-->{b.WgtCr}")
                       .FirstOrDefault();

                string[] ProdDts = StrBOMCode.Trim().Split(new[] { "-->" }, StringSplitOptions.None);
                StrDGRate = _context.ProfitCenterPldetails
                           .Where(p => p.ProfitCenterCode == dgStageScanReq.PCCode.Trim() &&
                           p.PartCode == dgStageScanReq.ProductCode.Trim())
                           .Select(p => p.Rate)
                           .FirstOrDefault() // Retrieves the first matching record or null
                           .ToString() ?? "0";

                if (double.Parse(CPType[1].Trim()) != 0 && double.Parse(CPType[1].Trim()) != 150)
                {
                    if (dgStageScanReq.CPSrno.Trim() != "0")
                    {
                        StrCPRate = _context.ProfitCenterPldetails
                           .Where(p => p.ProfitCenterCode == dgStageScanReq.PCCode.Trim() &&
                           p.PartCode == dgStageScanReq.CPPartcode.Trim())
                           .Select(p => p.Rate)
                           .FirstOrDefault()
                           .ToString();
                    }
                    if (dgStageScanReq.CP2Srno.Trim() != "0")
                    {
                        StrCP2Rate = _context.ProfitCenterPldetails
                           .Where(p => p.ProfitCenterCode == dgStageScanReq.PCCode.Trim() &&
                           p.PartCode == dgStageScanReq.CP2Partcode.Trim())
                           .Select(p => p.Rate)
                           .FirstOrDefault()
                           .ToString();
                    }
                }

                if (double.Parse(CPType[1].Trim()) == 0 && DGCFM.Trim() == "STD")
                {
                    if (dgStageScanReq.CPSrno.Trim() != "0")
                    {
                        StrCPRate = _context.ProfitCenterPldetails
                           .Where(p => p.ProfitCenterCode == dgStageScanReq.PCCode.Trim() &&
                           p.PartCode == dgStageScanReq.CPPartcode.Trim())
                           .Select(p => p.Rate)
                           .FirstOrDefault()
                           .ToString();
                    }
                }

                StrCRRate = _context.Bomdetails
                           .Where(b => b.Bomcode == ProdDts[0].Trim() &&
                          b.Thickness <= 1.5 &&
                          b.SteelRate > 0)
                          .Select(b => b.SteelRate)
                          .FirstOrDefault() // Returns the first matching SteelRate or default value
                          .ToString();

                StrHRRate = _context.Bomdetails
                           .Where(b => b.Bomcode == ProdDts[0].Trim() &&
                          b.Thickness > 1.5 &&
                          b.SteelRate > 0)
                          .Select(b => b.SteelRate)
                          .FirstOrDefault() // Returns the first matching SteelRate or default value
                          .ToString();

                if (double.Parse(StrDGRate.Trim()) == 0)
                {
                    PrcNo = "DG Rate Cannot Be Zero Please Contact CIA/Document Control Dept";
                    return PrcNo;
                }

                if (double.Parse(CPType[1].Trim()) != 0 && double.Parse(CPType[1].Trim()) != 150)
                {
                    if (double.Parse(StrCPRate.Trim()) == 0)
                    {
                        PrcNo = "Control Panel Rate Cannot Be Zero Please Contact CIA/DOcument Control Dept";
                        return PrcNo;
                    }
                }

                if (double.Parse(CPType[1].Trim()) == 0 && DGCFM.Trim() == "STD")
                {
                    if (double.Parse(StrCPRate.Trim()) == 0)
                    {
                        PrcNo = "Control Panel Rate Cannot Be Zero Please Contact CIA/DOcument Control Dept";
                        return PrcNo;
                    }
                }

                if (double.Parse(CPType[1].Trim()) != 0 && double.Parse(CPType[1].Trim()) != 150)
                {
                    string panelTypePartcode = await ExecuteGetCPPartcodeAsync(CPType[1]);

                    if (panelTypePartcode != null)
                    {
                        if (double.Parse(CPType[1].Trim()) != 0)
                        {
                            if (dgStageScanReq.CP2Srno.Trim() == "0" || dgStageScanReq.CP2Srno.Trim() == "")
                            {
                                PrcNo = "Please Scan Control Panel(2) SerialNo";
                                return PrcNo;
                            }
                        }
                        if (double.Parse(StrCP2Rate.Trim()) == 0)
                        {
                            PrcNo = "Control Panel(2) Rate Cannot Be Zero Please Contact CIA/DOcument Control Dept";
                            return PrcNo;
                        }
                    }
                }

                try
                {
                    string? yearEnd = _context.YearEnds
                                     .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" + (y.EndDate.Year % 100).ToString("00"))
                                     .FirstOrDefault();

                    DGNo = await GetDGNoAsync(dgStageScanReq.PCCode.Trim().Substring(0, 2), dgStageScanReq.PCCode.Trim(), yearEnd);
                    if (DGNo == "0")
                    {
                        PrcNo = "DG Serial No Creation Problem";
                        return PrcNo;
                    }
                    PrcNo = await GetMaxPrcAsync(yearEnd, dgStageScanReq.PCCode.Trim().Substring(0, 2));

                    var SqlQuery = @"
                                    INSERT INTO processfeedback (
                                    GroupPFBCode, PFBCode, MaxSrNo, Dt, Yr, MachineCode, SerialNo, ProfitCenterCode, TurretKitCode,
                                    PartCode, PPWCode, MOFCode, VersionCode, ProcessQty, PKitQty, PLength, PWidth, PThickness, 
                                    NstWtPerUt, NstSqftPerUt, WtPerUt, SqftPerUt, CRWt, HRWt, CRRate, HRRate, CompanyCode,
                                    PPDIRStatus, TRStatus, PFBType, PFBRate, SilCladdingRate, Remark, PrcBOMCode, QPCStatus
                                    ) 
                                    VALUES (
                                    @PrcNo, @PrcNo, @MaxSrNo, @Date, @Year, @MachineCode, @SerialNo, @PCCode, '0', @ProductCode, '0', 
                                    'MOF', @CPType1, '1', '0', '0', '0', '0', @NstWtPerUt, @NstSqftPerUt, @WtPerUt, @SqftPerUt, 
                                    @CRWt, @HRWt, @CRRate, @HRRate, @CompanyCode, 'P', 'P', @PFBType, @PFBRate, @SilCladdingRate, 
                                     @Remark, @PrcBOMCode, 'P')";

                    var Parameters = new[]
                    {
                          new SqlParameter("@PrcNo", PrcNo.Trim()),
                          new SqlParameter("@MaxSrNo", PrcNo.Substring(10, 8)),
                          new SqlParameter("@Date", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                          new SqlParameter("@Year", yearEnd.Trim()),
                          new SqlParameter("@MachineCode", DGNo.Trim()),
                          new SqlParameter("@SerialNo", DGNo.Substring(8, 4).Trim()),
                          new SqlParameter("@PCCode", dgStageScanReq.PCCode.Trim()),
                          new SqlParameter("@ProductCode", dgStageScanReq.ProductCode.Trim()),
                          new SqlParameter("@CPType1", CPType[1].Trim()),
                          new SqlParameter("@NstWtPerUt", Convert.ToDouble(ProdDts[1].Trim())),
                          new SqlParameter("@NstSqftPerUt", Convert.ToDouble(ProdDts[2].Trim())),
                          new SqlParameter("@WtPerUt", Convert.ToDouble(ProdDts[1].Trim())),
                          new SqlParameter("@SqftPerUt", Convert.ToDouble(ProdDts[2].Trim())),
                          new SqlParameter("@CRWt", ProdDts[4].Trim()),
                          new SqlParameter("@HRWt", ProdDts[3].Trim()),
                          new SqlParameter("@CRRate", StrCRRate.Trim()),
                          new SqlParameter("@HRRate", StrCRRate.Trim()),
                          new SqlParameter("@CompanyCode", dgStageScanReq.PCCode.Trim().Substring(0, 2)),
                          new SqlParameter("@PFBRate", double.Parse(StrDGRate.Trim())),
                          new SqlParameter("@SilCladdingRate", (CladdingRate ?? "0").Trim()),
                          new SqlParameter("@Remark", dgStageScanReq.Remark.Trim()),
                          new SqlParameter("@PrcBOMCode", ProdDts[0].Trim())
                    };

                    // Conditional parameter for PFBType
                    if (double.Parse(CPType[1].Trim()) == 0)
                    {
                        //Parameters = Parameters.Append(new SqlParameter("@PFBType", DBNull.Value)).ToArray();
                        Parameters = Parameters.Append(new SqlParameter("@PFBType", "")).ToArray();
                    }
                    else
                    {
                        Parameters = Parameters.Append(new SqlParameter("@PFBType", CPType[0].Trim())).ToArray();
                    }

                    await _context.Database.ExecuteSqlRawAsync(SqlQuery, Parameters);

                    var sqlQuery = @"INSERT INTO StockWIP 
                                       (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType, StageName)
                                       VALUES
                                       (@PCCode, @ProductCode, @IssueCode, CAST(@IssueDate AS DATETIME), @IssueQty, @ToPCCode, @StockType, @StageName)";

                    var parameters = new[]
                    {
                             new SqlParameter("@PCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                             new SqlParameter("@ProductCode", dgStageScanReq.ProductCode?.Trim() ?? (object)DBNull.Value),
                             new SqlParameter("@IssueCode", PrcNo.Trim()),
                             new SqlParameter("@IssueDate", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                             new SqlParameter("@IssueQty", 1) { SqlDbType = SqlDbType.Float },
                             new SqlParameter("@ToPCCode", dgStageScanReq.PCCode ?? (object)DBNull.Value),
                             new SqlParameter("@StockType", (object)0 ?? DBNull.Value) { SqlDbType = SqlDbType.Int },
                             new SqlParameter("@StageName", "StageIV")
                          };
                    await _context.Database.ExecuteSqlRawAsync(sqlQuery, parameters);

                    int SrNo = 0;
                    foreach (var item in dgStageScanReq.DGKitDetails)
                    {
                        SrNo += 1;
                        string processFeedbackQuery = @"INSERT INTO processfeedbackdetails (
                                                        PFBCode, SrNo, PartCode, KITQty, TotQty, StockQty, 
                                                        PFBRate, PLength, PWidth, PThickness, PLossWt, PHeight, 
                                                        PLength1, PLength2, PWidth1, PWidth2, PLossSqft, PCatagoryCode) 
                                                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)";

                        await _context.Database.ExecuteSqlRawAsync(
                            processFeedbackQuery,
                            PrcNo.Trim(), SrNo, item.PartCode,
                            Convert.ToDouble(item.KITQty.Trim()), Convert.ToDouble(item.TotalQty.Trim()),
                            Convert.ToDouble(item.StockQty.Trim()), Convert.ToDouble(item.PFBRate.Trim())
                        );

                        // Insert into StockWIP
                        string stockWipQuery = @"INSERT INTO StockWIP (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType) 
                                                VALUES ({0}, {1}, {2}, GETDATE(), {3}, {4}, 0)";

                        await _context.Database.ExecuteSqlRawAsync(
                            stockWipQuery,
                            dgStageScanReq.PCCode.Trim(), item.PartCode.Trim(), PrcNo.Trim(),
                            double.Parse(item.TotalQty.Trim()), dgStageScanReq.PCCode.Trim()
                        );
                    }
                    //For Engine
                    var sqlInsertQueryEngine = @"INSERT INTO ProcessFeedbackDetailsSub (PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, TrfCode, Status, QPCStatus, RWStatus) 
                                 VALUES 
                                (@PFBCode, @SrNo, @PartCode, @SerialNo, @PFBBOTSerialNo, @TrfCode, @Status, @QPCStatus, @RWStatus)";

                    var parametersEngine = new[]
                    {
                       new SqlParameter("@PFBCode", PrcNo.Trim()) { SqlDbType = SqlDbType.VarChar },
                       new SqlParameter("@SrNo", 1) { SqlDbType = SqlDbType.Int },  // Hardcoded value '1'
                       new SqlParameter("@PartCode", dgStageScanReq.EngPartCode.Trim()) { SqlDbType = SqlDbType.VarChar },
                       new SqlParameter("@SerialNo", dgStageScanReq.EngSrNo.Trim()) { SqlDbType = SqlDbType.VarChar },
                       new SqlParameter("@PFBBOTSerialNo", dgStageScanReq.EngSrNo.Trim()) { SqlDbType = SqlDbType.VarChar },
                       new SqlParameter("@TrfCode", dgStageScanReq.JobCardCode.Trim()) { SqlDbType = SqlDbType.VarChar },
                       new SqlParameter("@Status", "P") { SqlDbType = SqlDbType.VarChar },  // Hardcoded value 'P'
                       new SqlParameter("@QPCStatus", "OK") { SqlDbType = SqlDbType.VarChar },  // Hardcoded value 'OK'
                       new SqlParameter("@RWStatus", "OK") { SqlDbType = SqlDbType.VarChar }   // Hardcoded value 'OK'
                    };

                    await _context.Database.ExecuteSqlRawAsync(sqlInsertQueryEngine, parametersEngine);
                    //For Engine
                    var jobCardDetailsEnine = await _context.Jobcard2DetailsSubs
                               .Where(j => j.SerialNo == dgStageScanReq.EngSrNo.Trim() &&
                                           j.JobCode == dgStageScanReq.JobCardCode &&
                                           j.SrNoPartCode == dgStageScanReq.EngPartCode &&
                                           j.PartCode == dgStageScanReq.ProductCode)
                               .ToListAsync();
                    if (jobCardDetailsEnine.Any())
                    {
                        foreach (var jobcard in jobCardDetailsEnine)
                        {
                            jobcard.PrcStatus = "D";
                        }

                        await _context.SaveChangesAsync();
                    }

                    //For Alternator
                    var sqlInsertQueryAlternator = @"INSERT INTO ProcessFeedbackDetailsSub (PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, TrfCode, Status, QPCStatus, RWStatus) 
                                         VALUES 
                                         (@PFBCode, @SrNo, @PartCode, @SerialNo, @PFBBOTSerialNo, @TrfCode, @Status, @QPCStatus, @RWStatus)";

                    var sqlParamsAlternator = new object[]
                    {
                         new SqlParameter("@PFBCode", PrcNo.Trim()),
                         new SqlParameter("@SrNo", 1),
                         new SqlParameter("@PartCode", dgStageScanReq.AltPartcode.Trim()),
                         new SqlParameter("@SerialNo", dgStageScanReq.AltSrno.Trim()),
                         new SqlParameter("@PFBBOTSerialNo", dgStageScanReq.AltSrno.Trim()),
                         new SqlParameter("@TrfCode", dgStageScanReq.JobCardCode.Trim()),
                         new SqlParameter("@Status", "P"),
                         new SqlParameter("@QPCStatus", "OK"),
                         new SqlParameter("@RWStatus", "OK")
                    };

                    await _context.Database.ExecuteSqlRawAsync(sqlInsertQueryAlternator, sqlParamsAlternator);
                    //For Alternator
                    var jobCardDetailsAlternator = await _context.Jobcard2DetailsSubs
                              .Where(j => j.SerialNo == dgStageScanReq.AltSrno.Trim() &&
                                          j.JobCode == dgStageScanReq.JobCardCode &&
                                          j.SrNoPartCode == dgStageScanReq.AltPartcode &&
                                          j.PartCode == dgStageScanReq.ProductCode)
                              .ToListAsync();
                    if (jobCardDetailsAlternator.Any())
                    {
                        foreach (var jobcard in jobCardDetailsAlternator)
                        {
                            jobcard.PrcStatus = "D";
                        }

                        await _context.SaveChangesAsync();
                    }

                    //For Canopy
                    var sqlInsertQueryCanopy = @"INSERT INTO ProcessFeedbackDetailsSub (PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, TrfCode, Status, QPCStatus, RWStatus) 
                                         VALUES 
                                         (@PFBCode, @SrNo, @PartCode, @SerialNo, @PFBBOTSerialNo, @TrfCode, @Status, @QPCStatus, @RWStatus)";

                    var sqlParamsCanopy = new object[]
                    {
                         new SqlParameter("@PFBCode", PrcNo.Trim()),
                         new SqlParameter("@SrNo", 1),
                         new SqlParameter("@PartCode", dgStageScanReq.CpyPartcode.Trim()),
                         new SqlParameter("@SerialNo", dgStageScanReq.CpySrno.Trim()),
                         new SqlParameter("@PFBBOTSerialNo", dgStageScanReq.CpySrno.Trim()),
                         new SqlParameter("@TrfCode", dgStageScanReq.JobCardCode.Trim()),
                         new SqlParameter("@Status", "P"),
                         new SqlParameter("@QPCStatus", "OK"),
                         new SqlParameter("@RWStatus", "OK")
                    };

                    await _context.Database.ExecuteSqlRawAsync(sqlInsertQueryCanopy, sqlParamsCanopy);

                    //for canopy
                    var jobCardDetailsCanopy = await _context.Jobcard2DetailsSubs
                             .Where(j => j.SerialNo == dgStageScanReq.CpySrno.Trim() &&
                                         j.JobCode == dgStageScanReq.JobCardCode &&
                                         j.SrNoPartCode == dgStageScanReq.CpyPartcode &&
                                         j.PartCode == dgStageScanReq.ProductCode)
                             .ToListAsync();
                    if (jobCardDetailsCanopy.Any())
                    {
                        foreach (var jobcard in jobCardDetailsCanopy)
                        {
                            jobcard.PrcStatus = "D";
                        }

                        await _context.SaveChangesAsync();
                    }

                    //Control Panel Related Operations
                    if (double.Parse(CPType[1].Trim()) != 0 && double.Parse(CPType[1].Trim()) != 150)
                    {
                        var sqlInsertQuery = @"INSERT INTO processfeedbackdetails(PFBCode, SrNo, PartCode, KITQty, TotQty, StockQty, 
                                              PFBRate, PLength, PWidth, PThickness, PLossWt, PHeight,PLength1, PLength2, PWidth1, PWidth2, PLossSqft, PCatagoryCode) 
                                              VALUES 
                                              (@PFBCode, @SrNo, @PartCode, @KITQty, @TotQty, @StockQty,@PFBRate, @PLength, @PWidth, @PThickness, @PLossWt, @PHeight, 
                                              @PLength1, @PLength2, @PWidth1, @PWidth2, @PLossSqft, @PCatagoryCode)";

                        var sqlParams = new object[]
                        {
                          new SqlParameter("@PFBCode", PrcNo.Trim()),
                          new SqlParameter("@SrNo", SrNo),
                          new SqlParameter("@PartCode", dgStageScanReq.CPPartcode.Trim()),
                          new SqlParameter("@KITQty", 1),
                          new SqlParameter("@TotQty", 1),
                          new SqlParameter("@StockQty", 1),
                          new SqlParameter("@PFBRate", double.Parse(StrCPRate.Trim())),
                          new SqlParameter("@PLength", 0),
                          new SqlParameter("@PWidth", 0),
                          new SqlParameter("@PThickness", 0),
                          new SqlParameter("@PLossWt", 0),
                          new SqlParameter("@PHeight", 0),
                          new SqlParameter("@PLength1", 0),
                          new SqlParameter("@PLength2", 0),
                          new SqlParameter("@PWidth1", 0),
                          new SqlParameter("@PWidth2", 0),
                          new SqlParameter("@PLossSqft", 0),
                          new SqlParameter("@PCatagoryCode", 0)
                        };

                        await _context.Database.ExecuteSqlRawAsync(sqlInsertQuery, sqlParams);

                        var sqlInsertQuery1 = @"INSERT INTO StockWIP(FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType) 
                                             VALUES 
                                             (@FromProfitCenterCode, @PartCode, @IssueCode, GETDATE(), @IssueQty, @ToProfitCenterCode, @StockType)";

                        var sqlParams1 = new object[]
                        {
                          new SqlParameter("@FromProfitCenterCode", dgStageScanReq.PCCode.Trim()),
                          new SqlParameter("@PartCode", dgStageScanReq.CPPartcode.Trim()),
                          new SqlParameter("@IssueCode", PrcNo.Trim()),
                          new SqlParameter("@IssueQty", 1),
                          new SqlParameter("@ToProfitCenterCode", dgStageScanReq.PCCode.Trim()),
                          new SqlParameter("@StockType", 0)
                        };

                        await _context.Database.ExecuteSqlRawAsync(sqlInsertQuery1, sqlParams1);

                        var sqlInsertQuery2 = @"INSERT INTO ProcessFeedbackDetailsSub(PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, TrfCode, Status, QPCStatus, RWStatus) 
                                             VALUES 
                                             (@PFBCode, @SrNo, @PartCode, @SerialNo, @PFBBOTSerialNo, @TrfCode, @Status, @QPCStatus, @RWStatus)";

                        var sqlParams2 = new object[]
                        {
                           new SqlParameter("@PFBCode", PrcNo.Trim()),
                           new SqlParameter("@SrNo", 1),
                           new SqlParameter("@PartCode", dgStageScanReq.CPPartcode.Trim()),
                           new SqlParameter("@SerialNo", dgStageScanReq.CPSrno.Trim()),
                           new SqlParameter("@PFBBOTSerialNo", dgStageScanReq.CPSrno.Trim()),
                           new SqlParameter("@TrfCode", dgStageScanReq.JobCardCode.Trim()),
                           new SqlParameter("@Status", "P"),
                           new SqlParameter("@QPCStatus", "OK"),
                           new SqlParameter("@RWStatus", "OK")
                        };

                        await _context.Database.ExecuteSqlRawAsync(sqlInsertQuery, sqlParams);

                        var jobCardDetails = await _context.Jobcard2DetailsSubs
                            .Where(j => j.SerialNo == dgStageScanReq.CPSrno.Trim() &&
                                        j.JobCode == dgStageScanReq.JobCardCode &&
                                        j.SrNoPartCode == dgStageScanReq.CPPartcode &&
                                        j.PartCode == dgStageScanReq.ProductCode)
                            .ToListAsync();
                        if (jobCardDetails.Any())
                        {
                            foreach (var jobcard in jobCardDetails)
                            {
                                jobcard.PrcStatus = "D";
                            }

                            await _context.SaveChangesAsync(); 
                        }
                    }

                    if (double.Parse(CPType[1].Trim()) == 0 && DGCFM == "STD")
                    {
                        var sqlquery = @"INSERT INTO processfeedbackdetails(PFBCode, SrNo, PartCode, KITQty, TotQty, StockQty, 
                                                  PFBRate, PLength, PWidth, PThickness, PLossWt, PHeight,PLength1, PLength2, PWidth1, PWidth2, PLossSqft, PCatagoryCode) 
                                                  VALUES 
                                                  (@PFBCode, @SrNo, @PartCode, @KITQty, @TotQty, @StockQty,@PFBRate, @PLength, @PWidth, @PThickness, @PLossWt, @PHeight, 
                                                   @PLength1, @PLength2, @PWidth1, @PWidth2, @PLossSqft, @PCatagoryCode)";

                        var sqlParameters = new object[]
                        {
                                  new SqlParameter("@PFBCode", PrcNo.Trim()),
                                  new SqlParameter("@SrNo", SrNo),
                                  new SqlParameter("@PartCode", dgStageScanReq.CPPartcode.Trim()),
                                  new SqlParameter("@KITQty", 1),
                                  new SqlParameter("@TotQty", 1),
                                  new SqlParameter("@StockQty", 1),
                                  new SqlParameter("@PFBRate", double.Parse(StrCPRate.Trim())),
                                  new SqlParameter("@PLength", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PWidth", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PThickness", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PLossWt", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PHeight", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PLength1", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PLength2", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PWidth1", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PWidth2", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PLossSqft", SqlDbType.Int) { Value = 0 },
                                  new SqlParameter("@PCatagoryCode", SqlDbType.Int) { Value = 0 }
                        };


                        await _context.Database.ExecuteSqlRawAsync(sqlquery, sqlParameters);

                        var _sqlInsertQuery = @"INSERT INTO StockWIP(FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType) 
                                                  VALUES 
                                                  (@FromProfitCenterCode, @PartCode, @IssueCode, GETDATE(), @IssueQty, @ToProfitCenterCode, @StockType)";

                        var sqlParameter = new object[]
                        {
                                new SqlParameter("@FromProfitCenterCode", dgStageScanReq.PCCode.Trim()),
                                new SqlParameter("@PartCode", dgStageScanReq.CPPartcode.Trim()),
                                new SqlParameter("@IssueCode", PrcNo.Trim()),
                                new SqlParameter("@IssueQty", 1),
                                new SqlParameter("@ToProfitCenterCode", dgStageScanReq.PCCode.Trim()),
                                new SqlParameter("@StockType",SqlDbType.Int) { Value = 0 }
                        };

                        await _context.Database.ExecuteSqlRawAsync(_sqlInsertQuery, sqlParameter);

                        var _sqlInsertQuery1 = @"INSERT INTO ProcessFeedbackDetailsSub(PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, TrfCode, Status, QPCStatus, RWStatus) 
                                                    VALUES 
                                                  (@PFBCode, @SrNo, @PartCode, @SerialNo, @PFBBOTSerialNo, @TrfCode, @Status, @QPCStatus, @RWStatus)";

                        var _sqlParams = new object[]
                        {
                                new SqlParameter("@PFBCode", PrcNo.Trim()),
                                new SqlParameter("@SrNo", 1),
                                new SqlParameter("@PartCode", dgStageScanReq.CPPartcode.Trim()),
                                new SqlParameter("@SerialNo", dgStageScanReq.CPSrno.Trim()),
                                new SqlParameter("@PFBBOTSerialNo", dgStageScanReq.CPSrno.Trim()),
                                new SqlParameter("@TrfCode", dgStageScanReq.JobCardCode.Trim()),
                                new SqlParameter("@Status", "P"),
                                new SqlParameter("@QPCStatus", "OK"),
                                new SqlParameter("@RWStatus", "OK")
                        };

                        await _context.Database.ExecuteSqlRawAsync(_sqlInsertQuery1, _sqlParams);

                        var _jobCardDetails = await _context.Jobcard2DetailsSubs
                        .Where(j => j.SerialNo == dgStageScanReq.CPSrno.Trim() &&
                                    j.JobCode == dgStageScanReq.JobCardCode &&
                                    j.SrNoPartCode == dgStageScanReq.CPPartcode &&
                                    j.PartCode == dgStageScanReq.ProductCode)
                        .ToListAsync();
                        if (_jobCardDetails.Any())
                        {
                            foreach (var jobcard in _jobCardDetails)
                            {
                                jobcard.PrcStatus = "D";
                            }

                            await _context.SaveChangesAsync();
                        }
                    }
                    //CP2 Related operations
                    if (dgStageScanReq.CP2Srno.Trim() != "0")
                    {
                        if (double.Parse(CPType[2].Trim()) > 1)
                        {
                            var sqlquery = @"INSERT INTO processfeedbackdetails(PFBCode, SrNo, PartCode, KITQty, TotQty, StockQty, 
                                                  PFBRate, PLength, PWidth, PThickness, PLossWt, PHeight,PLength1, PLength2, PWidth1, PWidth2, PLossSqft, PCatagoryCode) 
                                                  VALUES 
                                                  (@PFBCode, @SrNo, @PartCode, @KITQty, @TotQty, @StockQty,@PFBRate, @PLength, @PWidth, @PThickness, @PLossWt, @PHeight, 
                                                   @PLength1, @PLength2, @PWidth1, @PWidth2, @PLossSqft, @PCatagoryCode)";

                            var sqlParameters = new object[]
                            {
                                  new SqlParameter("@PFBCode", PrcNo.Trim()),
                                  new SqlParameter("@SrNo", SrNo),
                                  new SqlParameter("@PartCode", dgStageScanReq.CP2Partcode.Trim()),
                                  new SqlParameter("@KITQty", 1),
                                  new SqlParameter("@TotQty", 1),
                                  new SqlParameter("@StockQty", 1),
                                  new SqlParameter("@PFBRate", double.Parse(StrCP2Rate.Trim())),
                                  new SqlParameter("@PLength", 0),
                                  new SqlParameter("@PWidth", 0),
                                  new SqlParameter("@PThickness", 0),
                                  new SqlParameter("@PLossWt", 0),
                                  new SqlParameter("@PHeight", 0),
                                  new SqlParameter("@PLength1", 0),
                                  new SqlParameter("@PLength2", 0),
                                  new SqlParameter("@PWidth1", 0),
                                  new SqlParameter("@PWidth2", 0),
                                  new SqlParameter("@PLossSqft", 0),
                                 new SqlParameter("@PCatagoryCode", 0)
                            };

                            await _context.Database.ExecuteSqlRawAsync(sqlquery, sqlParameters);

                            var _sqlInsertQuery = @"INSERT INTO StockWIP(FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType) 
                                                  VALUES 
                                                  (@FromProfitCenterCode, @PartCode, @IssueCode, GETDATE(), @IssueQty, @ToProfitCenterCode, @StockType)";

                            var sqlParameter = new object[]
                            {
                                new SqlParameter("@FromProfitCenterCode", dgStageScanReq.PCCode.Trim()),
                                new SqlParameter("@PartCode", dgStageScanReq.CP2Partcode.Trim()),
                                new SqlParameter("@IssueCode", PrcNo.Trim()),
                                new SqlParameter("@IssueQty", 1),
                                new SqlParameter("@ToProfitCenterCode", dgStageScanReq.PCCode.Trim()),
                                new SqlParameter("@StockType", 0)
                            };

                            await _context.Database.ExecuteSqlRawAsync(_sqlInsertQuery, sqlParameter);

                            var _sqlInsertQuery1 = @"INSERT INTO ProcessFeedbackDetailsSub(PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, TrfCode, Status, QPCStatus, RWStatus) 
                                                    VALUES 
                                                  (@PFBCode, @SrNo, @PartCode, @SerialNo, @PFBBOTSerialNo, @TrfCode, @Status, @QPCStatus, @RWStatus)";

                            var _sqlParams = new object[]
                            {
                                new SqlParameter("@PFBCode", PrcNo.Trim()),
                                new SqlParameter("@SrNo", 1),
                                new SqlParameter("@PartCode", dgStageScanReq.CP2Partcode.Trim()),
                                new SqlParameter("@SerialNo", dgStageScanReq.CP2Srno.Trim()),
                                new SqlParameter("@PFBBOTSerialNo", dgStageScanReq.CP2Srno.Trim()),
                                new SqlParameter("@TrfCode", dgStageScanReq.JobCardCode.Trim()),
                                new SqlParameter("@Status", "P"),
                                new SqlParameter("@QPCStatus", "OK"),
                                new SqlParameter("@RWStatus", "OK")
                            };

                            await _context.Database.ExecuteSqlRawAsync(_sqlInsertQuery1, _sqlParams);

                            var _jobCardDetails = await _context.Jobcard2DetailsSubs
                           .Where(j => j.SerialNo == dgStageScanReq.CP2Srno.Trim() &&
                                       j.JobCode == dgStageScanReq.JobCardCode &&
                                       j.SrNoPartCode == dgStageScanReq.CP2Partcode &&
                                       j.PartCode == dgStageScanReq.ProductCode)
                           .ToListAsync();
                            if (_jobCardDetails.Any())
                            {
                                foreach (var jobcard in _jobCardDetails)
                                {
                                    jobcard.PrcStatus = "D";
                                }

                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    //battery related operations
                    var batteries = new List<(string? PartCode, string? SrNo)>
                    {
                          (dgStageScanReq.BatPartcode, dgStageScanReq.BatSrno),
                          (dgStageScanReq.Bat2Partcode, dgStageScanReq.Bat2Srno),
                          (dgStageScanReq.Bat3Partcode, dgStageScanReq.Bat3Srno),
                          (dgStageScanReq.Bat4Partcode, dgStageScanReq.Bat4Srno)
                    };

                    var validBatteries = batteries.Where(b => b.PartCode != null || b.SrNo != null).ToList();

                    int batteryIndex = 0; // Track battery position

                    foreach (var battery in validBatteries)
                    {
                        if (!string.IsNullOrWhiteSpace(battery.SrNo) && battery.SrNo.Trim() != "0")
                        {
                            // Apply the condition only from the second battery onwards
                            if (batteryIndex > 0 && double.Parse(strkVA) < 160)
                            {
                                batteryIndex++;
                                continue; // Skip battery processing if strkVA is < 160 (only for batteries beyond the first)
                            }

                            var insertQuery = @"INSERT INTO ProcessFeedbackDetailsSub (PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, TrfCode, Status, QPCStatus, RWStatus)
                                              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})";

                            await _context.Database.ExecuteSqlRawAsync(
                                insertQuery,
                                PrcNo.Trim(),
                                "1",
                                battery.PartCode?.Trim(),
                                battery.SrNo?.Trim(),
                                battery.SrNo?.Trim(),
                                dgStageScanReq.JobCardCode?.Trim(),
                                "P",
                                "OK",
                                "OK"
                            );

                            // Update Query using LINQ
                            var updateRecords = _context.Jobcard2DetailsSubs
                                .Where(j => j.SerialNo == battery.SrNo.Trim()
                                         && j.JobCode == dgStageScanReq.JobCardCode
                                         && j.SrNoPartCode == battery.PartCode.Trim()
                                         && j.PartCode == dgStageScanReq.ProductCode);

                            await updateRecords.ForEachAsync(j => j.PrcStatus = "D");
                            await _context.SaveChangesAsync();
                        }

                        batteryIndex++; // Move to next battery
                    }

                    //KRM Related Operations
                    if (dgStageScanReq.KRMSrno.Trim() != "0")
                    {
                        var sqlInsertQuery = @"INSERT INTO ProcessFeedbackDetailsSub(PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, TrfCode, Status, QPCStatus, RWStatus) 
                                             VALUES 
                                             (@PFBCode, @SrNo, @PartCode, @SerialNo, @PFBBOTSerialNo, @TrfCode, @Status, @QPCStatus, @RWStatus)";

                        var sqlParams = new object[]
                        {
                           new SqlParameter("@PFBCode", PrcNo.Trim()),
                           new SqlParameter("@SrNo", 1),
                           new SqlParameter("@PartCode", dgStageScanReq.KRMPartcode.Trim()),
                           new SqlParameter("@SerialNo", dgStageScanReq.KRMSrno.Trim()),
                           new SqlParameter("@PFBBOTSerialNo", dgStageScanReq.KRMSrno.Trim()),
                           new SqlParameter("@TrfCode", dgStageScanReq.JobCardCode.Trim()),
                           new SqlParameter("@Status", "P"),
                           new SqlParameter("@QPCStatus", "OK"),
                           new SqlParameter("@RWStatus", "OK")
                        };

                        await _context.Database.ExecuteSqlRawAsync(sqlInsertQuery, sqlParams);

                        var recordToUpdate = await _context.Jobcard2DetailsSubs
                                             .FirstOrDefaultAsync(j => j.SerialNo == dgStageScanReq.KRMSrno.Trim()
                                              && j.JobCode == dgStageScanReq.JobCardCode
                                              && j.SrNoPartCode == dgStageScanReq.KRMPartcode.Trim()
                                              && j.PartCode == dgStageScanReq.ProductCode);

                        if (recordToUpdate != null)
                        {
                            recordToUpdate.PrcStatus = "D";
                            await _context.SaveChangesAsync();
                        }

                    }

                    #region Kanban Related Operations
                    List<InternalTOCResult> dsKanBan = new List<InternalTOCResult>();

                    string strKanBan = "";
                    dsKanBan = await GetInternalTOCResults(dgStageScanReq.PCCode);

                    if (dsKanBan.Count > 0)
                    {
                        string query1 = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode 
                             WHERE TblName = @TableName AND CompCode = @CompCode 
                             AND Prefix = @Prefix 
                             AND Yr = @YearEnd";

                        GetMaxValue = await GetMaxNo("REQ", dgStageScanReq.PCCode.Trim().Substring(0, 2), query1, "MaterialRequisitionWithOutPlan");
                        strKanBan = GetMaxValue;
                        string? yearEnds = _context.YearEnds
                                    .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" + (y.EndDate.Year % 100).ToString("00"))
                                    .FirstOrDefault();
                        string query = @"INSERT INTO MaterialRequisitionWithOutPlan(REQCode, MaxSrNo, Dt, Yr, ProfitCenterCode, ToProfitCenterCode, ClassCode, CompanyCode, ActNo, 
                                         REQStatus, ReqType, Remark, Discard, Active, Auth, SourceCode) 
                                         VALUES (@REQCode, @MaxSrNo, @Dt, @Yr, @ProfitCenterCode, @ToProfitCenterCode, @ClassCode, @CompanyCode, 
                                         @ActNo, @REQStatus, @ReqType, @Remark, @Discard, @Active, @Auth, @SourceCode)";

                        await _context.Database.ExecuteSqlRawAsync(query,
                            new SqlParameter("@REQCode", strKanBan.Trim()),
                            new SqlParameter("@MaxSrNo", GetMaxValue.Substring(10, 8)),
                            new SqlParameter("@Dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt")),
                            new SqlParameter("@Yr", yearEnds),
                            new SqlParameter("@ProfitCenterCode", dgStageScanReq.PCCode.Trim()),
                            new SqlParameter("@ToProfitCenterCode", "23.001"),
                            new SqlParameter("@ClassCode", dgStageScanReq.ProductCode),
                            new SqlParameter("@CompanyCode", dgStageScanReq.PCCode.Substring(0, 2)),
                            new SqlParameter("@ActNo", "1"),
                            new SqlParameter("@REQStatus", "P"),
                            new SqlParameter("@ReqType", "WIP"),
                            new SqlParameter("@Remark", $"Auto Req For Plan No: {dgStageScanReq.ProductCode} and Prc No: {PrcNo}"),
                            new SqlParameter("@Discard", "1"),
                            new SqlParameter("@Active", "1"),
                            new SqlParameter("@Auth", "1"),
                            new SqlParameter("@SourceCode", "KanBan")
                        );

                        int SrNoReq = 0;
                        foreach (var item in dsKanBan)
                        {
                            SrNoReq = SrNoReq + 1;
                            string _query = "EXEC insertMaterialRequisitionWithOutPlanDetails @REQCode, @SrNo, @PartCode, @Qty, @REQStatus";

                            await _context.Database.ExecuteSqlRawAsync(_query,
                                new SqlParameter("@REQCode", strKanBan),
                                new SqlParameter("@SrNo", SrNoReq),
                                new SqlParameter("@PartCode", item.Partcode.ToString().Trim()),
                                new SqlParameter("@Qty", double.Parse(item.RaiseReqQty.ToString().Trim())),
                                new SqlParameter("@REQStatus", "P")
                            );
                        }
                    }
                    #endregion
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                PrcNo = $"Process Started For ProcessCode={PrcNo} and DG SrNo={DGNo.Trim()}";
            }
            else if (dgStageScanReq.Remark == "End")
            {
                try
                {
                    int SrNo = 0;
                    string _DGStartTime = "";
                    _DGStartTime = await GetDGStartTimeStage4(dgStageScanReq.PfbCode.Trim());

                    foreach (var item in dgStageScanReq.PrcChkDts)
                    {
                        SrNo += 1;
                        string query = @"INSERT INTO PrcChkDetails (TransCode, Dt, MainSerialNo, PrcName, ChkPointId, PrcChkPoints, PrcStatus, DGStartTime, QA6M)
                                    VALUES (@TransCode, @Dt, @MainSerialNo, @PrcName, @ChkPointId, @PrcChkPoints, @PrcStatus, @DGStartTime, @QA6M)";

                        var parameters = new[]
                        {
                         new SqlParameter("@TransCode", dgStageScanReq.PfbCode.Trim()),
                         new SqlParameter("@Dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                         new SqlParameter("@MainSerialNo", dgStageScanReq.EngSrNo.Trim()),
                         new SqlParameter("@PrcName", "DG Stage4"),
                         new SqlParameter("@ChkPointId", item.PrcId),
                         new SqlParameter("@PrcChkPoints", item.Remark),
                         new SqlParameter("@PrcStatus", dgStageScanReq.PrcStatus),
                         new SqlParameter("@DGStartTime", _DGStartTime),
                         new SqlParameter("@QA6M", dgStageScanReq.QA6M)
                    };
                        await _context.Database.ExecuteSqlRawAsync(query, parameters);
                    }

                    var record = await _context.ProcessFeedBacks
                             .Where(p => p.Pfbcode == dgStageScanReq.JobCardCode)
                             .FirstOrDefaultAsync();

                    if (record != null)
                    {
                        if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                        {
                            record.Edt = DateTime.Now;
                        }
                        else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejected")
                        {
                            record.Edt = null;
                        }

                        await _context.SaveChangesAsync();
                    }

                    if (dgStageScanReq.PrcStatus == "Accepted(OK)")
                    {
                        PrcNo = $"Process Ended For ProcessCode={dgStageScanReq.JobCardCode}";
                    }
                    else if (dgStageScanReq.PrcStatus == "Rework" || dgStageScanReq.PrcStatus == "Rejected")
                    {
                        PrcNo = $"Process {dgStageScanReq.PrcStatus} For ProcessCode={dgStageScanReq.JobCardCode}";
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            return PrcNo;
        }

        public async Task<string> SubmitTestReportDetails(TestReportSubmitDetails testReportSubmitDetailsDTO)
        {
            string StrTRCode = "";
            string StrPDIRCode = "";
            string strTimefield = "";
            string TRQAStatus = "";
            string ChkDupTR = "";

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (testReportSubmitDetailsDTO.TRTime == "TRStart")
                {
                    int trCnt = _context.TestReports
                                .Where(tr => tr.ProcessCode == testReportSubmitDetailsDTO.PFBCode.Trim() && tr.Active == true)
                                .Count();

                    if (trCnt > 0)
                    {
                        return StrTRCode = "TRStart For This Process Already Done";
                    }
                }

                if (testReportSubmitDetailsDTO.TRCode != null)
                {
                    TRQAStatus = _context.TestReports
                                         .Where(tr => tr.Trcode == testReportSubmitDetailsDTO.TRCode.Trim())
                                         .Select(tr => tr.Qastatus)
                                         .FirstOrDefault(); 
                }

                if (testReportSubmitDetailsDTO.TRTime == "TRStart")
                {
                    strTimefield = "TRStartTime";
                }
                else if (testReportSubmitDetailsDTO.TRTime == "TREnd")
                {
                    strTimefield = "TREndTime";
                }
                else if (testReportSubmitDetailsDTO.TRTime == "DGStart")
                {
                    strTimefield = "DGStartTime";
                }
                else if (testReportSubmitDetailsDTO.TRTime == "DGEnd")
                {
                    strTimefield = "DGEndTime";
                }

                if (strTimefield == "TRStartTime")
                {
                    string query1 = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode 
                             WHERE TblName = @TableName AND CompCode = @CompCode 
                             AND Prefix = @Prefix 
                             AND Yr = @YearEnd";

                    StrTRCode = await GetMaxNo("TRC", testReportSubmitDetailsDTO.PFBCode.Trim().Substring(10, 2), query1, "TestReport");

                    string yearEnd = _context.YearEnds
                                             .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" + (y.EndDate.Year % 100).ToString("00"))
                                             .FirstOrDefault();

                    string SqlQuery = @"INSERT INTO TestReport (TRCode, Dt, Yr, MaxSrNo, ProcessCode, MachineNo, Remark,
                                RevTRCode, BalQty, EngineModel, HP, KW, Speed, Alternator, RatedKVA, RatedVolt, RatedAMPS,
                                Ph, PF, Frequency, AMBTemp, RY, YB, BR, VoltageRegulation, RoomTempreture, RPMRegulation,
                                LLOP, HWT, HCT, OSD, DieselRate, PerUnitQty, PDIRStatus, CompanyCode, " + strTimefield + @")  
                                VALUES (@TRCode, @Dt, @Yr, @MaxSrNo, @ProcessCode, @MachineNo, @Remark,
                                '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '10',
                                 '11', '12', '13', '14', @DieselRate, @PerUnitQty, 'P', @CompanyCode, @TimeFieldValue
                                )";

                    var Parameters = new[]
                    {
                                new SqlParameter("@TRCode", StrTRCode.Trim()),
                                new SqlParameter("@Dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                                new SqlParameter("@Yr", yearEnd),
                                new SqlParameter("@MaxSrNo", StrTRCode.Substring(10, 8)),
                                new SqlParameter("@ProcessCode", testReportSubmitDetailsDTO.PFBCode.Trim()),
                                new SqlParameter("@MachineNo", testReportSubmitDetailsDTO.DGSrNo.Trim()),
                                new SqlParameter("@Remark", testReportSubmitDetailsDTO.Remark.Trim()),
                                new SqlParameter("@DieselRate", testReportSubmitDetailsDTO.DieselRate),
                                new SqlParameter("@PerUnitQty", testReportSubmitDetailsDTO.DieselQty),
                                new SqlParameter("@CompanyCode", testReportSubmitDetailsDTO.PFBCode.Trim().Substring(10, 2)),
                                new SqlParameter("@TimeFieldValue", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                  };

                    await _context.Database.ExecuteSqlRawAsync(SqlQuery, Parameters);

                    int SrNo = 0;
                    foreach (var item in testReportSubmitDetailsDTO.TRDGKitDetails)
                    {
                        SrNo += 1;

                        string SqlQuery1 = @"INSERT INTO TestReportSerialNoDetails (TRCode, SrNo, PartCode, SerialNo, SerialStatus, Qty, GiirCode)
                                            VALUES (@TRCode, @SrNo, @PartCode, @SerialNo, 'D', 1, @GiirCode)";

                        var parameters = new[]
                        {
                              new SqlParameter("@TRCode", StrTRCode.Trim()),
                              new SqlParameter("@SrNo", SrNo),
                              new SqlParameter("@PartCode", item.PartCode.Trim()),
                              new SqlParameter("@SerialNo", item.SerialNo.Trim()),
                              new SqlParameter("@GiirCode", testReportSubmitDetailsDTO.PFBCode.Trim())
                        };

                        await _context.Database.ExecuteSqlRawAsync(SqlQuery1, parameters);
                    }

                    //TrserialStatus change for ProcessFeedbackDetailsSub Table
                    var processFeedbackDetails = await _context.ProcessFeedbackDetailsSubs
                                                    .Where(p => p.Pfbcode == testReportSubmitDetailsDTO.PFBCode.Trim())
                                                    .ToListAsync();

                    foreach (var record in processFeedbackDetails)
                    {
                        record.TrserialStatus = "D";
                    }

                    //Trstatus change for ProcessFeedBacks Table
                    var processFeedback = await _context.ProcessFeedBacks
                                                    .Where(p => p.Pfbcode == testReportSubmitDetailsDTO.PFBCode.Trim())
                                                    .ToListAsync();

                    foreach (var record in processFeedback)
                    {
                        record.Trstatus = "D";
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {

                    if (strTimefield == "DGEndTime")
                    {
                        int SrNo = 0;
                        var dgStartTime = await _context.TestReports
                                         .Where(t => t.Trcode == testReportSubmitDetailsDTO.TRCode.Trim())
                                         .Select(t => t.DgstartTime.HasValue ? t.DgstartTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : null)
                                         .FirstOrDefaultAsync();

                        foreach (var item in testReportSubmitDetailsDTO.TRPrcChkDts)
                        {
                            SrNo += 1;
                            string insertQuery = @"INSERT INTO PrcChkDetails(TransCode, Dt, MainSerialNo, QA6M, PrcName, ChkPointId, PrcChkPoints, PrcStatus, DGStartTime) 
                                              VALUES(@TransCode, @Dt, @MainSerialNo, @QA6M, @PrcName, @ChkPointId, @PrcChkPoints, @PrcStatus, @DGStartTime)";

                            var parameters = new[]
                            {
                              new SqlParameter("@TransCode", testReportSubmitDetailsDTO.TRCode.Trim()),
                              new SqlParameter("@Dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                              new SqlParameter("@MainSerialNo", testReportSubmitDetailsDTO.DGSrNo.Trim()),
                              new SqlParameter("@QA6M", testReportSubmitDetailsDTO.QA6M),
                              new SqlParameter("@PrcName", "DG TestReport"),
                              new SqlParameter("@ChkPointId", item.PrcId),
                              new SqlParameter("@PrcChkPoints", item.Remark.Trim()),
                              new SqlParameter("@PrcStatus", testReportSubmitDetailsDTO.QAStatus),
                              new SqlParameter("@DGStartTime", dgStartTime.Trim())
                        };

                            await _context.Database.ExecuteSqlRawAsync(insertQuery, parameters);
                        }

                        // Determine the records to update
                        var testReport = await _context.TestReports
                            .Where(tr => tr.ProcessCode == testReportSubmitDetailsDTO.PFBCode.Trim() && tr.MachineNo == testReportSubmitDetailsDTO.DGSrNo.Trim())
                            .FirstOrDefaultAsync();

                        if (testReport != null)
                        {
                            if (testReportSubmitDetailsDTO.QAStatus == "Rework")
                            {
                                // Set specified fields to NULL and QAStatus to 'P'
                                typeof(TestReport).GetProperty(strTimefield)?.SetValue(testReport, null);
                                testReport.DgstartTime = null;
                                testReport.Qastatus = "P";
                            }
                            else
                            {
                                // Set dynamic time field to current datetime and QAStatus to 'D'
                                typeof(TestReport).GetProperty(strTimefield, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                                ?.SetValue(testReport, DateTime.Now);
                                testReport.Qastatus = "D";
                            }

                            if (double.TryParse(testReportSubmitDetailsDTO.DieselQty, out double perUnitQty))
                            {
                                testReport.PerUnitQty = perUnitQty;
                            }
                            else
                            {
                                testReport.PerUnitQty = 0;
                            }

                            await _context.SaveChangesAsync();
                        }


                    }
                    else if (strTimefield == "DGStartTime" && TRQAStatus.Trim() == "Rework")
                    {
                        var testReportDetails = await _context.TestReports
                                                .FirstOrDefaultAsync(tr => tr.ProcessCode == testReportSubmitDetailsDTO.PFBCode.Trim()
                                                && tr.MachineNo == testReportSubmitDetailsDTO.DGSrNo.Trim());

                        if (testReportDetails != null)
                        {
                            typeof(TestReport).GetProperty(strTimefield, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                           ?.SetValue(testReportDetails, DateTime.Now);


                            testReportDetails.Qastatus = "P";

                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        var testReports = await _context.TestReports
                                          .Where(tr => tr.ProcessCode == testReportSubmitDetailsDTO.PFBCode.Trim()
                                          && tr.MachineNo == testReportSubmitDetailsDTO.DGSrNo.Trim()).ToListAsync();

                        foreach (var report in testReports)
                        {
                            typeof(TestReport).GetProperty(strTimefield, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                            ?.SetValue(report, DateTime.Now);

                        }

                        await _context.SaveChangesAsync();
                    }
                }

                if (strTimefield == "TRStartTime")
                {
                    StrTRCode = $"TestReport Code={StrTRCode}.";
                }
                else
                {
                    StrTRCode = $"TestReport = {testReportSubmitDetailsDTO.TRCode.Trim()} Updated With {strTimefield} Timings";
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return StrTRCode;
        }

        public async Task<string> SubmitPackingSlipDetails(PackingSlipSubmitDetails packingSlipSubmitDetailsReq)
        {
            string StrPSLCode = "";

            string StrPCCodeStkIssue = "";

            string StrPCCodeStkRecieved = "";

            int SrNo = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                    if (packingSlipSubmitDetailsReq.PSTime == "PSStartTime")
                    {
                        if (!string.IsNullOrEmpty(packingSlipSubmitDetailsReq.PSStartTime))
                        {
                            StrPSLCode = $"PSStart Process Already Completed,Please Continue For PSEnd Process..!";
                            return StrPSLCode;
                        }
                        foreach (var item in packingSlipSubmitDetailsReq.MOFAddParts)
                        {
                            if (item.Qty - item.WIPStock < 0)
                            {
                                StrPSLCode = $"Insufficient Stock(Additional Part) For: {item.PartCode}";
                                return StrPSLCode;
                            }
                        }

                        string PC_CompanyCode = "";
                        string trcode = packingSlipSubmitDetailsReq.TRCode.Trim();
                        string StrTPSStatus = _context.TestReports
                                              .Where(tr => tr.Trcode == trcode && tr.Active == true)
                                              .Select(tr => tr.Tpsstatus)
                                              .FirstOrDefault() ?? "0";

                        if (packingSlipSubmitDetailsReq.TRCode.Trim().Substring(10, 2).Trim() == "01" && StrTPSStatus.Trim() == "P")
                        {
                            StrPCCodeStkIssue = "01.004";
                            StrPCCodeStkRecieved = "01.018";
                            PC_CompanyCode = packingSlipSubmitDetailsReq.TRCode.Trim().Substring(10, 2).Trim();
                        }
                        else if (packingSlipSubmitDetailsReq.TRCode.Trim().Substring(10, 2).Trim() == "28" && StrTPSStatus.Trim() == "P")
                        {
                            StrPCCodeStkIssue = "28.001";
                            StrPCCodeStkRecieved = "28.005";
                            PC_CompanyCode = packingSlipSubmitDetailsReq.TRCode.Trim().Substring(10, 2).Trim();
                        }
                        else
                        {
                            StrPCCodeStkIssue = "03.051";
                            StrPCCodeStkRecieved = "03.019";
                            PC_CompanyCode = "03";
                        }

                        string query = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode 
                             WHERE TblName = @TableName AND CompCode = @CompCode 
                             AND Prefix = @Prefix 
                             AND Yr = @YearEnd";
                        string yearEnd = _context.YearEnds
                                        .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" + (y.EndDate.Year % 100).ToString("00"))
                                        .FirstOrDefault();

                        StrPSLCode = await GetMaxNo("PSL", PC_CompanyCode , query, "Packingslip");

                        string insertSql = @"INSERT INTO Packingslip(PSCode, Dt, Yr, MaxSrNo, PCCode, SOFCode, PSStartTime, PDICode, SrNo, Remark,
                                           PCCodeStkIssue, BatteryTerminals, BatteryLead, ExhaustPipe, DCBulb, CanopyKey, FuelCapKey, Rate, RubberPad, FunnelPad, PrdManual, CompanyCode)
                                           VALUES
                                          (@PSCode, @Dt, @Yr, @MaxSrNo, @PCCode, @SOFCode, @PSStartTime, @PDICode, @SrNo, @Remark,
                                           @PCCodeStkIssue, @BatteryTerminals, @BatteryLead, @ExhaustPipe, @DCBulb,
                                           @CanopyKey, @FuelCapKey, @Rate, @RubberPad, @FunnelPad, @PrdManual, @CompanyCode)";

                        var sqlParams = new[]
                        {
                               new SqlParameter("@PSCode",            StrPSLCode.Trim()),
                               new SqlParameter("@Dt",                DateTime.Now),                   // datetime handled natively
                               new SqlParameter("@Yr",                yearEnd),
                               new SqlParameter("@MaxSrNo",           StrPSLCode.Substring(10, 8)),
                               new SqlParameter("@PCCode",            packingSlipSubmitDetailsReq.TRCode.Trim()),
                               new SqlParameter("@SOFCode",           packingSlipSubmitDetailsReq.DiNo.Trim()),
                               new SqlParameter("@PSStartTime",       DateTime.Now),
                               new SqlParameter("@PDICode",           packingSlipSubmitDetailsReq.PDICode.Trim()),
                               new SqlParameter("@SrNo",              packingSlipSubmitDetailsReq.DGSrNo.Trim()),
                               new SqlParameter("@Remark",            packingSlipSubmitDetailsReq.Remark.Trim()),
                               new SqlParameter("@PCCodeStkIssue",    StrPCCodeStkIssue.Trim()),
                               new SqlParameter("@BatteryTerminals",  Convert.ToInt32(packingSlipSubmitDetailsReq.BatTer.Trim())),
                               new SqlParameter("@BatteryLead",       Convert.ToInt32(packingSlipSubmitDetailsReq.BatLead.Trim())),
                               new SqlParameter("@ExhaustPipe",       Convert.ToInt32(packingSlipSubmitDetailsReq.ExhPipe.Trim())),
                               new SqlParameter("@DCBulb",            Convert.ToInt32(packingSlipSubmitDetailsReq.DCBulb.Trim())),
                               new SqlParameter("@CanopyKey",         Convert.ToInt32(packingSlipSubmitDetailsReq.CanopyKey.Trim())),
                               new SqlParameter("@FuelCapKey",        Convert.ToInt32(packingSlipSubmitDetailsReq.FuelCapKey.Trim())),
                               new SqlParameter("@Rate",              1),                              // literal value 1
                               new SqlParameter("@RubberPad",         Convert.ToInt32(packingSlipSubmitDetailsReq.RubberPad.Trim())),
                               new SqlParameter("@FunnelPad",         Convert.ToInt32(packingSlipSubmitDetailsReq.FunnelPad.Trim())),
                               new SqlParameter("@PrdManual",         Convert.ToInt32(packingSlipSubmitDetailsReq.PrdManual.Trim())),
                               new SqlParameter("@CompanyCode",       PC_CompanyCode.Trim())
                        };
                        await _context.Database.ExecuteSqlRawAsync(insertSql, sqlParams);

                        if (packingSlipSubmitDetailsReq.EngPartCode.Substring(0, 3) == "001")
                        {
                            SrNo += 1;
                            var sql = @"INSERT INTO PackingslipDetails(PSCode, SrNo, PartCode, SerialNo, Qty, Rate)
                                       VALUES(@PSCode, @SrNo, @PartCode, @SerialNo, @Qty, @Rate)";

                            var parameters = new[]
                            {
                                   new SqlParameter("@PSCode",   StrPSLCode.Trim()),
                                   new SqlParameter("@SrNo",     SrNo),
                                   new SqlParameter("@PartCode", packingSlipSubmitDetailsReq.EngPartCode.Trim()),
                                   new SqlParameter("@SerialNo", packingSlipSubmitDetailsReq.EngSrNo.Trim()),
                                   new SqlParameter("@Qty",      1),
                                   new SqlParameter("@Rate",     1)
                            };

                            await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                        }

                        if (packingSlipSubmitDetailsReq.AltPartcode.Substring(0, 3) == "002")
                        {
                            SrNo += 1;
                            string insertSqlAlt = @"INSERT INTO PackingslipDetails(PSCode, SrNo, PartCode, SerialNo, Qty, Rate)
                                               VALUES(@PSCode, @SrNo, @PartCode, @SerialNo, @Qty, @Rate)";

                            var parameters = new[]
                            {
                               new SqlParameter("@PSCode",   StrPSLCode.Trim()),
                               new SqlParameter("@SrNo",     SrNo),
                               new SqlParameter("@PartCode", packingSlipSubmitDetailsReq.AltPartcode.Trim()),
                               new SqlParameter("@SerialNo", packingSlipSubmitDetailsReq.AltSrno.Trim()),
                               new SqlParameter("@Qty",      1),
                               new SqlParameter("@Rate",     1)
                            };
                            await _context.Database.ExecuteSqlRawAsync(insertSqlAlt, parameters);
                        }

                        if (packingSlipSubmitDetailsReq.CpyPartcode.Substring(0, 3) == "401")
                        {
                            SrNo += 1;
                            var insertsqlCpy = @"INSERT INTO PackingslipDetails(PSCode, SrNo, PartCode, SerialNo, Qty, Rate) 
                                      VALUES(@PSCode, @SrNo, @PartCode, @SerialNo, @Qty, @Rate)";

                            var parameters = new[]
                            {
                               new SqlParameter("@PSCode",   StrPSLCode.Trim()),
                               new SqlParameter("@SrNo",     SrNo),
                               new SqlParameter("@PartCode", packingSlipSubmitDetailsReq.CpyPartcode.Trim()),
                               new SqlParameter("@SerialNo", packingSlipSubmitDetailsReq.CpySrno.Trim()),
                               new SqlParameter("@Qty",      1),
                               new SqlParameter("@Rate",     1)
                             };
                            await _context.Database.ExecuteSqlRawAsync(insertsqlCpy, parameters);
                        }

                        var batteries = new List<(string? PartCode, string? SrNo)>
                        {
                          (packingSlipSubmitDetailsReq.BatPartcode, packingSlipSubmitDetailsReq.BatSrno),
                          (packingSlipSubmitDetailsReq.Bat2Partcode, packingSlipSubmitDetailsReq.Bat2Srno),
                          (packingSlipSubmitDetailsReq.Bat3Partcode, packingSlipSubmitDetailsReq.Bat3Srno),
                          (packingSlipSubmitDetailsReq.Bat4Partcode, packingSlipSubmitDetailsReq.Bat4Srno)
                        };

                        var validBatteries = batteries.Where(b => (!string.IsNullOrWhiteSpace(b.PartCode) && b.PartCode.Trim().Length > 2) ||
                                                        (!string.IsNullOrWhiteSpace(b.SrNo) && b.SrNo.Trim().Length > 2)).ToList();
                        int batteryIndex = 0;
                        foreach (var battery in validBatteries)
                        {
                            SrNo += 1;
                            // Apply the condition only from the second battery onwards
                            if (batteryIndex > 0 && double.Parse(packingSlipSubmitDetailsReq.KVA) <= 180)
                            {
                                batteryIndex++;
                                continue; // Skip battery processing if strkVA is < 160 (only for batteries beyond the first)
                            }

                            string insertQuery = @"INSERT INTO PackingslipDetails (PSCode, SrNo, PartCode, SerialNo, Qty, Rate)
                                                   VALUES (@PSCode, @SrNo, @PartCode, @SerialNo, @Qty, @Rate)";
                            var parameters = new[]
                            {
                                 new SqlParameter("@PSCode", StrPSLCode.Trim()),
                                 new SqlParameter("@SrNo", SrNo),
                                 new SqlParameter("@PartCode", battery.PartCode),
                                 new SqlParameter("@SerialNo", battery.SrNo),
                                 new SqlParameter("@Qty", 1),
                                 new SqlParameter("@Rate", 1)
                            };
                            await _context.Database.ExecuteSqlRawAsync(insertQuery, parameters);

                            batteryIndex++;
                        }

                        //CP1 & CP2 Related Operations
                        string[] CPTYpe = Regex.Split(packingSlipSubmitDetailsReq.CPType.Trim(), "-->");
                        var cpData = new[]
                        {
                                new {
                                        Index = 1,
                                        PartCode = packingSlipSubmitDetailsReq.CPPartcode,
                                        SerialNo = packingSlipSubmitDetailsReq.CPSrno
                                },
                                new {
                                        Index = 2,
                                        PartCode = packingSlipSubmitDetailsReq.CP2Partcode,
                                        SerialNo = packingSlipSubmitDetailsReq.CP2Srno
                                }
                        };

                        foreach (var cp in cpData)
                        {
                            if (double.Parse(CPTYpe[cp.Index].Trim()) > 0)
                            {
                                SrNo += 1;
                                // Insert PackingslipDetails if PartCode starts with "003"
                                if (cp.PartCode.StartsWith("003"))
                                {
                                    var insertQuery = @"INSERT INTO PackingslipDetails(PSCode, SrNo, PartCode, SerialNo, Qty, Rate)
                                                      VALUES(@PSCode, @SrNo, @PartCode, @SerialNo, @Qty, @Rate)";

                                    var parameters = new[]
                                    {
                                       new SqlParameter("@PSCode", StrPSLCode.Trim()),
                                       new SqlParameter("@SrNo", SrNo),
                                       new SqlParameter("@PartCode", cp.PartCode.Trim()),
                                       new SqlParameter("@SerialNo", cp.SerialNo.Trim()),
                                       new SqlParameter("@Qty", 1),
                                       new SqlParameter("@Rate", 1)
                                    };

                                    await _context.Database.ExecuteSqlRawAsync(insertQuery, parameters);
                                }

                                // Inline JobCardStatus Update for 3 tables related CP1 & CP2
                                var serial = cp.SerialNo.Trim();
                                var part = cp.PartCode.Trim();

                                var processList = await _context.ProcessFeedbackDetailsSubs
                                                    .Where(p => p.SerialNo == serial)
                                                    .ToListAsync();
                                foreach (var p in processList)
                                    p.JobCardStatus = "J";

                                var mtfList = await _context.MtfdetailsSubs
                                                .Where(m => m.SerialNo == serial && m.PartCode == part)
                                                .ToListAsync();
                                foreach (var m in mtfList)
                                    m.JobCardStatus = "J";

                                var giirList = await _context.GiirdetailsSubs
                                                 .Where(g => g.SerialNo == serial)
                                                 .ToListAsync();
                                foreach (var g in giirList)
                                    g.JobCardStatus = "J";
                            }
                        }
                        await _context.SaveChangesAsync();

                        //KRM Related Operations
                        if (packingSlipSubmitDetailsReq.KRMPartcode.Trim() != "0")
                        {
                            SrNo += 1;
                            var insertQuery = @"INSERT INTO PackingslipDetails (PSCode, SrNo, PartCode, SerialNo, Qty, Rate)
                                              VALUES (@PSCode, @SrNo, @PartCode, @SerialNo, @Qty, @Rate)";

                            var parameters = new[]
                            {
                               new SqlParameter("@PSCode", StrPSLCode.Trim()),
                               new SqlParameter("@SrNo", SrNo),
                               new SqlParameter("@PartCode", packingSlipSubmitDetailsReq.KRMPartcode.Trim()),
                               new SqlParameter("@SerialNo", packingSlipSubmitDetailsReq.KRMSrno.Trim()),
                               new SqlParameter("@Qty", 1),
                               new SqlParameter("@Rate", 1)
                            };
                            await _context.Database.ExecuteSqlRawAsync(insertQuery, parameters);
                        }

                        //Mst 
                        var dispatchRecords = await _context.DispatchInstructionDetails
                                              .Where(d => d.Dino == packingSlipSubmitDetailsReq.DiNo.Trim() && d.Rdgcode == packingSlipSubmitDetailsReq.TRCode.Trim())
                                             .ToListAsync();

                        foreach (var record in dispatchRecords)
                        {
                            record.Psldstatus = "C";
                        }
                        await _context.SaveChangesAsync();

                        int count = await _context.DispatchInstructionDetails
                                   .Where(d => d.Psldstatus != "C" && d.Dino == packingSlipSubmitDetailsReq.DiNo.Trim())
                                   .CountAsync();

                        string cntDIStatus = count.ToString();
                        if (cntDIStatus == "0")
                        {
                            var dispatchInstruction = await _context.DispatchInstructions
                                                     .Where(d => d.Dino == packingSlipSubmitDetailsReq.DiNo.Trim())
                                                     .ToListAsync();
                            foreach (var item in dispatchInstruction)
                            {
                                item.Pslstatus = "C";
                            }

                            await _context.SaveChangesAsync();
                        }

                        foreach (var item in packingSlipSubmitDetailsReq.MOFAddParts)
                        {
                            if (item.PartCode.Trim() != "0")
                            {
                                SrNo += 1;
                                string insertStockWipSql = @"INSERT INTO StockWIP(FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType)
                                                            VALUES(@FromPC, @PartCode, @IssueCode, @IssueDate, @IssueQty, @ToPC, @StockType);";
                                var parameters = new[]
                                {
                                    new SqlParameter("@FromPC", StrPCCodeStkIssue.Trim()),   
                                    new SqlParameter("@PartCode", item.PartCode.Trim()),
                                    new SqlParameter("@IssueCode", StrPSLCode.Trim()),  
                                    new SqlParameter("@IssueDate", DateTime.Now), 
                                    new SqlParameter("@IssueQty", item.Qty),
                                    new SqlParameter("@ToPC", StrPCCodeStkRecieved.Trim()),
                                    new SqlParameter("@StockType", (object)0 ?? DBNull.Value){ SqlDbType = SqlDbType.Int }
                                };
                                await _context.Database.ExecuteSqlRawAsync(insertStockWipSql, parameters);

                                string insertPackingslipSql = @"INSERT INTO PackingslipDetails (PSCode, SrNo, PartCode, SerialNo, Qty, Rate)
                                                    VALUES (@PSCode, @SrNo, @PartCode, @SerialNo, @Qty, @Rate)";
                                var parametersPackingSlip = new[]
                                {
                                    new SqlParameter("@PSCode", StrPSLCode.Trim()),
                                    new SqlParameter("@SrNo", SrNo),
                                    new SqlParameter("@PartCode", item.PartCode.Trim()),
                                    new SqlParameter("@SerialNo", "-"),
                                    new SqlParameter("@Qty", 1),
                                    new SqlParameter("@Rate", 1)
                                };
                                await _context.Database.ExecuteSqlRawAsync(insertPackingslipSql, parametersPackingSlip);
                            }
                        }

                        await transaction.CommitAsync();

                       return StrPSLCode = $"PSStart Process Completed Successfully For = {StrPSLCode}";
                    }
                    else if(packingSlipSubmitDetailsReq.PSTime == "PSEndTime")
                    {
                    if (packingSlipSubmitDetailsReq.PSCode == null)
                    {
                        StrPSLCode = $"Please Complete PSStart Process And Then Continue For PSEnd Process..!";
                        return StrPSLCode;
                    }
                    else if (packingSlipSubmitDetailsReq.PSEndTime != null && packingSlipSubmitDetailsReq.PSEndTime != "")
                    {
                        StrPSLCode = $"PSEnd Process Already Complete For Packing Slip SrNo: {packingSlipSubmitDetailsReq.strSrNo}";
                        return StrPSLCode;
                    }
                        string PC_CompanyCode = "";
                      string trcode = packingSlipSubmitDetailsReq.TRCode.Trim();
                      string StrTPSStatus = _context.TestReports
                                            .Where(tr => tr.Trcode == trcode && tr.Active == true)
                                            .Select(tr => tr.Tpsstatus)
                                            .FirstOrDefault() ?? "0";

                      if (packingSlipSubmitDetailsReq.TRCode.Trim().Substring(10, 2).Trim() == "01" && StrTPSStatus.Trim() == "P")
                      {
                        PC_CompanyCode = packingSlipSubmitDetailsReq.TRCode.Trim().Substring(10, 2).Trim();
                      }
                      else if (packingSlipSubmitDetailsReq.TRCode.Trim().Substring(10, 2).Trim() == "28" && StrTPSStatus.Trim() == "P")
                      {       
                        PC_CompanyCode = packingSlipSubmitDetailsReq.TRCode.Trim().Substring(10, 2).Trim();
                      }
                      else
                      {
                        PC_CompanyCode = "03";
                      }

                       string query = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode 
                                      WHERE TblName = @TableName AND CompCode = @CompCode 
                                      AND Prefix = @Prefix 
                                      AND Yr = @YearEnd";
                       StrPSLCode = await GetMaxNo("PSL", PC_CompanyCode, query, "Packingslip");

                        var psCode = packingSlipSubmitDetailsReq.PSCode.Trim();
                        var columnName = packingSlipSubmitDetailsReq.PSTime.Trim();
                        var dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string updateSqlQuery = $"UPDATE PackingSlip SET {columnName} = @p0 WHERE PSCode = @p1";
                        await _context.Database.ExecuteSqlRawAsync(updateSqlQuery, dateTimeNow, psCode);

                      await transaction.CommitAsync();

                      return StrPSLCode = $"PSEnd Process Completed Successfully For = {StrPSLCode}";
                    }

                 // await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                  await transaction.RollbackAsync();
                  throw;
            }
            return StrPSLCode;        
        }

        public async Task<string> SubmitJobCard2Details(Jobcard2SubmitDetails jobcard2SubmitDetailsReq)
        {
            int strEngQty = 0, strAltQty = 0, strCpyQty = 0, strBatQty = 0, strCPQty = 0;

            string JobCardNo = "";
            string panelTypeId = "0";

            #region Checking For Part Description
            List<GetJobCardSrNo> jobCardSrNo = new List<GetJobCardSrNo>();
            List<GetJobCardSrNo> dsDetailsSub = new List<GetJobCardSrNo>();
            foreach (var item in jobcard2SubmitDetailsReq.JobCard2Dts)
            {
                if (item.PartCode == "0")
                {
                    var partDesc = await _context.Parts
                                  .Where(p => p.PartCode == item.PartCode)
                                  .Select(p => p.PartDesc)
                                  .FirstOrDefaultAsync();
                    JobCardNo = $"Panel Not Selected For DG {partDesc}";
                    return JobCardNo;
                }
                else
                {
                    var parameters = new[]
                       {
                          new SqlParameter("@JobCodeType", "DGWIP"),
                          new SqlParameter("@Partcode", item.PartCode),
                          new SqlParameter("@Qty", item.Jobcard2Qty),
                          new SqlParameter("@CompCode",jobcard2SubmitDetailsReq.PCCode.Trim().Substring(0, 2))
                    };
                    jobCardSrNo = await _context.Database
                              .SqlQueryRaw<GetJobCardSrNo>("EXEC GetJobCardSrNo @JobCodeType, @Partcode, @Qty, @CompCode", parameters)
                              .ToListAsync();
                    if (jobCardSrNo.Count > 0)
                    {
                        foreach (var jb in jobCardSrNo)
                        {

                        }
                    }
                    else
                    {
                        var partDesc = await _context.Parts
                                  .Where(p => p.PartCode == item.PartCode)
                                  .Select(p => p.PartDesc)
                                  .FirstOrDefaultAsync();

                        JobCardNo = $"JobCard1(Without Panel) Not available For DG {partDesc}";
                        return JobCardNo;
                    }
                }
            }
            #endregion

            using var transaction = await _context.Database.BeginTransactionAsync();
            //Check Srno Availability
            try
            {
                string? yearEnd = _context.YearEnds
                                     .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" + (y.EndDate.Year % 100).ToString("00"))
                                     .FirstOrDefault();
                string query = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode 
                                      WHERE TblName = @TableName AND CompCode = @CompCode 
                                      AND Prefix = @Prefix 
                                      AND Yr = @YearEnd";

                JobCardNo = await GetMaxNo("JCP", jobcard2SubmitDetailsReq.PCCode.Trim().Substring(0, 2), query, "JobCard2");

                string sqlJobCard2 = @"INSERT INTO JobCard2 (JobCode, Dt, Yr, MaxSrNo, PCCode, Remark, CompanyCode, Active, Auth) 
                                     VALUES (@JobCode, GETDATE(), @Yr, @MaxSrNo, @PCCode, @Remark, @CompanyCode, @Active, @Auth)";
                var parameters = new[]
                {
                        new SqlParameter("@JobCode", JobCardNo.Trim()),
                        new SqlParameter("@Yr", yearEnd),
                        new SqlParameter("@MaxSrNo", JobCardNo.Substring(10, 8)),
                        new SqlParameter("@PCCode", jobcard2SubmitDetailsReq.PCCode.Trim()),
                        new SqlParameter("@Remark", jobcard2SubmitDetailsReq.Remark.Trim()),
                        new SqlParameter("@CompanyCode", jobcard2SubmitDetailsReq.PCCode.Trim().Substring(0, 2)),
                        new SqlParameter("@Active", 1),
                        new SqlParameter("@Auth", 1),
                };
                await _context.Database.ExecuteSqlRawAsync(sqlJobCard2, parameters);

                int SrNo = 0;
                string? strModel = "", strPhase = "";

                foreach (var item in jobcard2SubmitDetailsReq.JobCard2Dts)
                {
                    SrNo += 1;
                    panelTypeId = "0";
                    if (item.DGPanel == "SPL")
                    {
                        panelTypeId = "150";
                    }
                    else
                    {
                        strModel = ""; strPhase = "";
                        strModel = await _context.Parts
                                  .Where(p => p.PartCode == item.PartCode)
                                  .Select(p => p.Model.Substring(0, 5))
                                  .FirstOrDefaultAsync();

                        strPhase = await _context.Parts
                                  .Where(p => p.PartCode == item.PartCode)
                                  .Select(p => p.Phase)
                                  .FirstOrDefaultAsync();

                        var dgType = await _context.Parts
                                     .Where(p => p.PartCode == item.PartCode)
                                     .Select(p => p.Model != null && p.Model.Length >= 6
                                      ? p.Model.Substring(p.Model.Length - 6, 6): null)
                                     .FirstOrDefaultAsync();

                        var partDetails = await _context.Parts
                                         .Where(p => p.PartCode == item.PartCode.Trim())
                                         .Select(p => new { p.Kva, p.Phase })
                                         .FirstOrDefaultAsync();

                        if (partDetails != null)
                        {
                            string targetType = dgType == "iGreen" ? "iGreen" : "KG";

                            int? tempPanelTypeId = await _context.PanelTypeKits
                                .Where(pt => pt.PanelTypeName == item.DGPanel.Trim()
                                    && pt.DgkVa == partDetails.Kva
                                    && pt.Dgphase == Convert.ToInt32(partDetails.Phase)
                                    && pt.Dgtype == targetType)
                                .Select(pt => (int?)pt.PanelTypeId)
                                .FirstOrDefaultAsync();

                            panelTypeId = tempPanelTypeId?.ToString();
                        }
                    }

                       string insertJobcard2 = @"INSERT INTO JobCard2Details (JobCode, SrNo, BOMCode, PartCode, Qty, PanelType)
                                               VALUES (@JobCode, @SrNo, @BOMCode, @PartCode, @Qty, @PanelType)";
                       var paraJobcard2 = new[]
                       {
                              new SqlParameter("@JobCode", JobCardNo.Trim()),
                              new SqlParameter("@SrNo", SrNo),
                              new SqlParameter("@BOMCode", item.BOMCode.Trim()),
                              new SqlParameter("@PartCode", item.PartCode.Trim()),
                              new SqlParameter("@Qty", Convert.ToInt32(item.Jobcard2Qty.Trim())),
                              new SqlParameter("@PanelType", string.IsNullOrEmpty(panelTypeId) ? (object)DBNull.Value : panelTypeId)
                       };

                     await _context.Database.ExecuteSqlRawAsync(insertJobcard2, paraJobcard2);

                    var paramdsDetailsSub = new[]
                       {
                          new SqlParameter("@JobCodeType", "DGWIP"),
                          new SqlParameter("@Partcode", item.PartCode),
                          new SqlParameter("@Qty", item.Jobcard2Qty),
                          new SqlParameter("@CompCode",jobcard2SubmitDetailsReq.PCCode.Trim().Substring(0, 2))
                    };
                    dsDetailsSub = await _context.Database
                              .SqlQueryRaw<GetJobCardSrNo>("EXEC GetJobCardSrNo @JobCodeType, @Partcode, @Qty, @CompCode", paramdsDetailsSub)
                              .ToListAsync();

                    if (dsDetailsSub.Count > 0)
                    {
                        strEngQty = 0; strAltQty = 0; strCpyQty = 0; strBatQty = 0;
                        int SrNok = 0;

                        foreach (var dsdetail in dsDetailsSub)
                        {
                            string? jobCard1;
                            string? j2Priority;
                            if (dsdetail.SrNoPartcode.ToString().Trim().StartsWith("00002"))
                            {
                                // Fetch JobCard1
                                jobCard1 = await _context.JobCardDetailsSubs
                                            .Where(j => _context.GiirdetailsSubs
                                                          .Where(g => g.Krmno == dsdetail.SerialNo.Trim())
                                                          .Select(g => g.SerialNo)
                                                          .Contains(j.SerialNo))
                                            .Select(j => j.JobCode)
                                            .FirstOrDefaultAsync();

                                // Fetch J2Priority
                                j2Priority = await _context.JobCardDetailsSubs
                                            .Where(j => _context.GiirdetailsSubs
                                                          .Where(g => g.Krmno == dsdetail.SerialNo.Trim())
                                                          .Select(g => g.SerialNo)
                                                          .Contains(j.SerialNo))
                                            .Select(j => j.Jpriority.ToString())
                                            .FirstOrDefaultAsync();
                            }
                            else
                            {
                                jobCard1 = dsdetail.JobCode.Trim();
                                j2Priority = dsdetail.JPriority.ToString();
                            }

                            jobCard1 ??= "";
                            j2Priority ??= "";

                            string insertJob2DetailsSub = @"INSERT INTO JobCard2DetailsSub(JobCode, SrNo, PartCode, PanelType, TransCode, SrNoPartCode, SerialNo, Stage3Status, JobCard1, J2Priority)
                                                          VALUES
                                                          (@JobCode, @SrNo, @PartCode, @PanelType, @TransCode, @SrNoPartCode, @SerialNo, @Stage3Status, @JobCard1, @J2Priority)";

                            var paramjob2details = new[]
                            {
                                 new SqlParameter("@JobCode", JobCardNo.Trim()),
                                 new SqlParameter("@SrNo", SrNok),
                                 new SqlParameter("@PartCode", item.PartCode.Trim()),
                                 new SqlParameter("@PanelType", string.IsNullOrEmpty(panelTypeId) ? (object)DBNull.Value : panelTypeId),
                                 new SqlParameter("@TransCode", dsdetail.JobCode.Trim()),
                                 new SqlParameter("@SrNoPartCode", dsdetail.SrNoPartcode.Trim()),
                                 new SqlParameter("@SerialNo", dsdetail.SerialNo.Trim()),
                                 new SqlParameter("@Stage3Status", dsdetail.Stage3Status.Trim()),
                                 new SqlParameter("@JobCard1", jobCard1),
                                 new SqlParameter("@J2Priority", j2Priority)
                            };

                            await _context.Database.ExecuteSqlRawAsync(insertJob2DetailsSub, paramjob2details);

                            //Updating Jobcard status to J from JobCardDetailsSub
                            await _context.JobCardDetailsSubs
                                  .Where(j => j.SerialNo == dsdetail.SerialNo.Trim()
                                  && j.JobCode == dsdetail.JobCode.Trim()
                                  && j.SrNoPartCode == dsdetail.SrNoPartcode.Trim())
                                  .ExecuteUpdateAsync(update => update.SetProperty(j => j.JobCard2Status, "J"));

                            //Updating jobcard2Qty value
                            if (dsdetail.SrNoPartcode.Trim().StartsWith("001"))
                            {
                                await _context.JobCardDetails
                                    .Where(j => j.JobCode == dsdetail.JobCode.Trim()
                                             && j.PartCode == item.PartCode.Trim())
                                    .ExecuteUpdateAsync(update => update.SetProperty(j => j.JobCard2Qty, j => j.JobCard2Qty + 1));
                            }

                            //Cheked SrNo To JobCardQty
                            string srNoPartcodePrefix = dsdetail.SrNoPartcode.ToString().Trim();
                            if (srNoPartcodePrefix.StartsWith("001"))
                                strEngQty++;
                            else if (srNoPartcodePrefix.StartsWith("002"))
                                strAltQty++;
                            else if (srNoPartcodePrefix.StartsWith("010"))
                                strBatQty++;
                            else if (srNoPartcodePrefix.StartsWith("40"))
                                strCpyQty++;
                        }
                        //Verified SrNo To JobCardQty
                        string partDesc = await _context.Parts
                                         .Where(p => p.PartCode == item.PartCode.Trim())
                                         .Select(p => p.PartDesc)
                                         .FirstOrDefaultAsync() ?? "Unknown Part";

                        int requiredQty = int.TryParse(item.Jobcard2Qty?.Trim(), out var parsedQty) ? parsedQty : 0;
                        if (requiredQty > strEngQty)
                            return $"Engine SrNo Not available For DG {partDesc}";

                        if (requiredQty > strAltQty)
                            return $"Alternator SrNo Not available For DG {partDesc}";

                        if (requiredQty > strBatQty)
                            if (await CheckTranBOMForBat(item.PartCode.Trim()))
                                return $"Battery SrNo Not available For DG {partDesc}";

                        if (requiredQty > strCpyQty)
                            return $"Canopy SrNo Not available For DG {partDesc}";
                    }

                    //For Job Priority
                    if (double.Parse(panelTypeId) != 150)
                    {
                        var dsDetailsSubP = await _context.Jobcard2DetailsSubs
                                          .Where(j => j.JobCode == JobCardNo.Trim() && j.SrNoPartCode.StartsWith("001"))
                                          .Select(j => new
                                          {
                                                   j.J2priority,
                                                   j.JobCard1
                                          })
                                         .ToListAsync();
                        if (dsDetailsSubP.Count > 0)
                        {
                            foreach (var dsdetail in dsDetailsSubP)
                            {
                                List<PanelTypePartcodeDto> dsDetailsCP = new List<PanelTypePartcodeDto>();
                                if (double.Parse(panelTypeId) != 150)
                                {
                                    if (jobcard2SubmitDetailsReq.PCCode.Trim().Substring(0, 2) == "28")
                                    {
                                        if (double.Parse(panelTypeId) == 0)
                                        {
                                            dsDetailsCP = await GetCPPartcodeBangaloreAsync(panelTypeId, item.PartCode.Trim(), transaction);
                                        }
                                        else
                                        {
                                            dsDetailsCP = await GetCPPartcodeBangaloreAsync(panelTypeId, "0", transaction);
                                        }
                                    }
                                    else 
                                    {
                                        if (double.Parse(panelTypeId) == 0)
                                        {
                                            dsDetailsCP = await GetCPPartcodeAsync(panelTypeId, item.PartCode.Trim(), transaction);
                                        }
                                        else
                                        {
                                            dsDetailsCP = await GetCPPartcodeAsync(panelTypeId, "0", transaction);
                                        }
                                    }
                                }

                                if (dsDetailsCP.Count > 0)
                                {
                                    strCPQty = 0;
                                    List<PanelTypePartcodeDto> dsDetailsCPSRNo = new List<PanelTypePartcodeDto>();
                                    foreach (var sdDetail in dsDetailsCP)
                                    {
                                        if (jobcard2SubmitDetailsReq.PCCode.Trim().Substring(0, 2) == "28")
                                        {
                                            dsDetailsCPSRNo = await GetCPPartcodeBangaloreAsync("1", sdDetail.PanelTypePartcode.Trim(), transaction);
                                        }
                                        else 
                                        {
                                            dsDetailsCPSRNo = await GetCPPartcodeAsync("1", sdDetail.PanelTypePartcode.Trim(), transaction);
                                        }
                                        if (dsDetailsCPSRNo != null && dsDetailsCPSRNo.Count > 0)
                                        {
                                            int SrNoP = 0;

                                            foreach (var cpSrNo in dsDetailsCPSRNo)
                                            {
                                                SrNoP += 1;
                                                string sql1 = @"INSERT INTO JobCard2DetailsSub(JobCode, SrNo, PartCode, PanelType, JobCard1, J2Priority, TransCode, Stage3Status, SrNoPartCode, SerialNo)
                                                               VALUES 
                                                             (@JobCode, @SrNo, @PartCode, @PanelType, @JobCard1, @J2Priority, @TransCode, @Stage3Status, @SrNoPartCode, @SerialNo)";
                                                var param1 = new[]
                                                {
                                                    new SqlParameter("@JobCode", JobCardNo.Trim()),
                                                    new SqlParameter("@SrNo", SrNoP),
                                                    new SqlParameter("@PartCode", item.PartCode.Trim()),
                                                    new SqlParameter("@PanelType", panelTypeId),
                                                    new SqlParameter("@JobCard1", dsdetail.JobCard1?.Trim() ?? string.Empty),
                                                    new SqlParameter("@J2Priority", dsdetail.J2priority),
                                                    new SqlParameter("@TransCode", cpSrNo.GCode?.Trim() ?? string.Empty),
                                                    new SqlParameter("@Stage3Status", cpSrNo.TRFStatus?.Trim() ?? string.Empty),
                                                    new SqlParameter("@SrNoPartCode", cpSrNo.PartCode?.Trim() ?? string.Empty),
                                                    new SqlParameter("@SerialNo", cpSrNo.SerialNo?.Trim() ?? string.Empty)

                                                }; await _context.Database.ExecuteSqlRawAsync(sql1, param1);

                                                if (cpSrNo.GCode.Trim().Substring(0, 3) == "PSH")
                                                {
                                                    var pfDetails = await _context.ProcessFeedbackDetailsSubs
                                                                   .Where(pf => pf.Pfbcode == cpSrNo.GCode.Trim()
                                                                   && pf.SerialNo == cpSrNo.SerialNo.Trim()
                                                                   && pf.PartCode == cpSrNo.PartCode.Trim())
                                                                   .ToListAsync();

                                                    foreach (var detail in pfDetails)
                                                    {
                                                        detail.JobCardStatus = "J";
                                                    }

                                                    await _context.SaveChangesAsync();

                                                }
                                                else if (cpSrNo.GCode.Trim().Substring(0, 3) == "MTF")
                                                {
                                                    var mtfDetails = await _context.MtfdetailsSubs
                                                                     .Where(mtf => mtf.Mtfcode == cpSrNo.GCode.Trim()
                                                                      && mtf.SerialNo == cpSrNo.SerialNo.Trim()
                                                                      && mtf.PartCode == cpSrNo.PartCode.Trim())
                                                                     .ToListAsync();

                                                    foreach (var detail in mtfDetails)
                                                    {
                                                        detail.JobCardStatus = "J";
                                                    }

                                                    await _context.SaveChangesAsync();

                                                }
                                                else if (cpSrNo.GCode.Trim().Substring(0, 3) == "GIR")
                                                {
                                                    var giirDetails = await _context.GiirdetailsSubs
                                                                     .Where(g => g.Giircode == cpSrNo.GCode.Trim()
                                                                      && g.SerialNo == cpSrNo.SerialNo.Trim()
                                                                      && g.PartCode == cpSrNo.PartCode.Trim())
                                                                     .ToListAsync();

                                                    foreach (var detail in giirDetails)
                                                    {
                                                        detail.JobCardStatus = "J";
                                                        detail.Trfstatus = "D";
                                                    }

                                                    await _context.SaveChangesAsync();

                                                }
                                                else if (cpSrNo.GCode.Trim().Substring(0, 3) == "GRI")
                                                {
                                                    var gateReceiptDetails = await _context.GatereceiptInternalDetailsSubs
                                                                             .Where(g => g.Gricode == cpSrNo.GCode.Trim()
                                                                             && g.SerialNo == cpSrNo.SerialNo.Trim()
                                                                             && g.PartCode == cpSrNo.PartCode.Trim())
                                                                             .ToListAsync();

                                                    foreach (var detail in gateReceiptDetails)
                                                    {
                                                        detail.JobcardStatus = "J";
                                                        detail.Trfstatus = "D";
                                                    }

                                                    await _context.SaveChangesAsync();
                                                }
                                                strCPQty = strCPQty + 1;
                                            }
                                        }
                                        else 
                                        {
                                            var partDesc = await _context.Parts
                                                           .Where(p => p.PartCode == item.PartCode)
                                                           .Select(p => p.PartDesc)
                                                           .FirstOrDefaultAsync();
                                            JobCardNo = $"CP SrNo Not available For DG {partDesc} and CP Type {item.PanelType}";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return JobCardNo;
        }

        private async Task<List<PanelTypePartcodeDto>> GetCPPartcodeBangaloreAsync(string panelTypeId, string partCode, IDbContextTransaction transaction)
        {
            List<PanelTypePartcodeDto> cpPartcodeResult = new List<PanelTypePartcodeDto>();

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.Transaction = transaction.GetDbTransaction();
                command.CommandText = "GetCPPartcode_Bangalore";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@PanelTypeId", SqlDbType.Int)
                {
                    Value = double.Parse(panelTypeId) == 0 ? 0 : Convert.ToInt32(panelTypeId)
                });

                command.Parameters.Add(new SqlParameter("@PartCode", SqlDbType.NVarChar, 20)
                {
                    Value = double.Parse(panelTypeId) == 0 ? partCode.Trim() : "0"
                });

                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (double.Parse(panelTypeId) == 0)
                        {
                            cpPartcodeResult.Add(new PanelTypePartcodeDto
                            {
                                PanelTypePartcode = reader["PanelTypePartcode"].ToString()
                            });
                        }
                        else
                        {
                            cpPartcodeResult.Add(new PanelTypePartcodeDto
                            {
                                SerialNo = reader["SerialNo"].ToString(),
                                GCode = reader["GCode"].ToString(),
                                PartCode = reader["PartCode"].ToString(),
                                QDt = reader["QDt"] != DBNull.Value ? Convert.ToDateTime(reader["QDt"]) : (DateTime?)null,
                                TRFStatus = reader["TRFStatus"].ToString()
                            });
                        }
                    }
                }
            }

            return cpPartcodeResult;
        }
        private async Task<List<PanelTypePartcodeDto>> GetCPPartcodeAsync(string panelTypeId, string partCode, IDbContextTransaction transaction)
        {
            List<PanelTypePartcodeDto> cpPartcodeResult = new List<PanelTypePartcodeDto>();

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.Transaction = transaction.GetDbTransaction();
                command.CommandText = "GetCPPartcode";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@PanelTypeId", SqlDbType.Int)
                {
                    Value = double.Parse(panelTypeId) == 0 ? 0 : Convert.ToInt32(panelTypeId)
                });

                command.Parameters.Add(new SqlParameter("@PartCode", SqlDbType.NVarChar, 20)
                {
                    Value = double.Parse(panelTypeId) == 0 ? partCode.Trim() : "0"
                });

                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (double.Parse(panelTypeId) == 0)
                        {
                            cpPartcodeResult.Add(new PanelTypePartcodeDto
                            {
                                PanelTypePartcode = reader["PanelTypePartcode"].ToString()
                            });
                        }
                        else
                        {
                            cpPartcodeResult.Add(new PanelTypePartcodeDto
                            {
                                SerialNo = reader["SerialNo"].ToString(),
                                GCode = reader["GCode"].ToString(),
                                PartCode = reader["PartCode"].ToString(),
                                QDt = reader["QDt"] != DBNull.Value ? Convert.ToDateTime(reader["QDt"]) : (DateTime?)null,
                                TRFStatus = reader["TRFStatus"].ToString()
                            });
                        }
                    }
                }
            }

            return cpPartcodeResult;
        }

        private async Task<bool> CheckTranBOMForBat(string productCode)
        {
            int batPartCount = await _context.Bomdetails
                .Where(b => b.Kitcode == productCode && b.PartCode.StartsWith("010"))
                .CountAsync();

            return batPartCount > 0;
        }

        public async Task<string> GetMaxNo(string prefix, string compCode, string query, string tblName)
        {
            string strmax = "";
            string NewTransCode = "";
            int intmax = 0;
            string? yearEnd = _context.YearEnds
                                     .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" + (y.EndDate.Year % 100).ToString("00"))
                                     .FirstOrDefault();

            //string query = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode 
            //                 WHERE TblName = @TableName AND CompCode = @CompCode 
            //                 AND Prefix = @Prefix 
            //                 AND Yr = @YearEnd";

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

            NewTransCode = $"{prefix}/{yearEnd}/{compCode}{strmax}";

            var record = _context.GetMaxCodes
                        .FirstOrDefault(g => g.Prefix == prefix
                         && g.TblName == tblName
                         && g.CompCode == compCode
                         && g.Yr == yearEnd);

            if (record != null)
            {
                record.MaxValue = Convert.ToInt32(strmax); // Update the MaxValue
                _context.SaveChanges();   // Commit changes to the database
            }

            return NewTransCode;
        }
        public async Task<string> GetDGNoAsync(string companyCode, string profitCenterCode, string year)
        {
            string query = @"SELECT CAST(YEAR(GETDATE()) AS CHAR(4)) + '.' +
               RIGHT('0' + RTRIM(MONTH(GETDATE())), 2) + '.' +
               RIGHT('000' + RTRIM((ISNULL(MAX(SerialNo), 0) + 1)), 4) AS DGNo
               FROM processfeedback
               WHERE companycode = {0} 
               AND profitcentercode = {1} 
               AND yr = {2}";

            var result = await _context.Database
                .SqlQueryRaw<string>(query, companyCode, profitCenterCode, year)
                .ToListAsync();

            return result.FirstOrDefault(); // Returns the first result or null if no data found
        }

        public async Task<string> GetMaxPrcAsync(string Yr, string CompCode)
        {
            // Ensure values are properly trimmed
            Yr = Yr.Trim();
            CompCode = CompCode.Trim();

            // Define the SQL query
            string query = @"SELECT MAX(SUBSTRING(PFbCode, 13, 7)) FROM ProcessFeedback 
                           WHERE Yr = @Yr AND CompanyCode = @CompCode";

            var connection = _context.Database.GetDbConnection();
           

            var dbTransaction = _context.Database.CurrentTransaction?.GetDbTransaction();

            // using (var command = _context.Database.GetDbConnection().CreateCommand())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = System.Data.CommandType.Text;
                command.Transaction = dbTransaction;
                command.Parameters.Add(new SqlParameter("@Yr", Yr));
                command.Parameters.Add(new SqlParameter("@CompCode", CompCode));

                // ✅ Open connection only if it's closed
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();
                string formattedmax;
                if (result == DBNull.Value || result == null)
                {
                    formattedmax = "000001";
                }
                else
                {
                    int val = Convert.ToInt32(result) + 1;
                    formattedmax = val.ToString().PadLeft(6, '0');
                }
                return $"PSH/{Yr}/{CompCode}{formattedmax}";
            }
        }

        public async Task<string> GetTransName(string PCCode, string ProductCode, string StageName)
        {
            int PartCnt = await _context.PcstageWiseRates
                            .Where(p => p.Pccode == PCCode &&
                                       p.PartCode == ProductCode &&
                                       p.StageName == StageName)
                            .CountAsync();
            return PartCnt.ToString();
        }

        public async Task<string> GetTotalSuppRateAsync(string productCode, string cpyPartCode)
        {
            string query = @"SELECT ISNULL(ROUND(SUM(Rate), 0), 0) AS Rate 
                           FROM 
                           (
                             SELECT ISNULL(SUM(Qty * SuppRate), 0) AS Rate 
                             FROM BOMDetails 
                             WHERE Kitcode = @productCode 
                             AND SUBSTRING(Partcode, 1, 3) IN ('001', '002', '010') 

                             UNION ALL 

                             SELECT TOP 1 Rate 
                             FROM ProfitCenterplDetailsChanged PC 
                             INNER JOIN 
                             (SELECT BOMCode, Partcode FROM BOMDetails 
                             WHERE Kitcode = @productCode 
                             AND SUBSTRING(Partcode, 1, 3) IN ('401')) AS B 
                             ON PC.PartCode = B.Partcode AND PC.BomCode = B.BOMCode 
                             WHERE ProfitCenterCode = '01.005' 
                             AND PC.Partcode = @cpyPartCode
                             ) AS T";
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(new SqlParameter("@productCode", productCode));
                command.Parameters.Add(new SqlParameter("@cpyPartCode", cpyPartCode));

               // await _context.Database.OpenConnectionAsync();

                var currentTransaction = _context.Database.CurrentTransaction;
                if (currentTransaction != null)
                {
                    command.Transaction = currentTransaction.GetDbTransaction();
                }

                var result = await command.ExecuteScalarAsync();

                return result.ToString();
            }

        }

        public async Task<string> GetTotalSuppRateAsync(string productCode)
        {
            string query = @" SELECT ISNULL(SUM(SuppRate), 0) AS SuppRate FROM BOMDetails WHERE Kitcode = @ProductCode 
                           AND SUBSTRING(Partcode, 1, 3) IN ('001', '002')";

            var productCodeParam = new SqlParameter("@ProductCode", productCode);

            var result = await _context.Bomdetails
                .FromSqlRaw(query, productCodeParam)
                .Select(x => x.SuppRate)
                .FirstOrDefaultAsync();

            return result.ToString();
        }

        public async Task<string> GetDGStartTime(string JobCode, string EngSrNo)
        {
            try
            {
                string query = @"Select convert(nvarchar(20),Stage1StartDate,120) as Stage1StartDate from JobCardDetailssub 
                           Where JobCode=@JobCode And SerialNo=@EngSrNo";
                var parameters = new[] {
                 new SqlParameter("@JobCode",JobCode),
                 new SqlParameter("@EngSrNo",EngSrNo)
                 };
                var result = await _context.JobCardDetailsSubs
             .FromSqlRaw(query, parameters)
             .Select(x => x.Stage1StartDate.ToString())  // Ensure it’s treated as a string
             .FirstOrDefaultAsync();
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<string> GetDGStartTimeStage4(string pFBCode)
        {
            try
            {
                var result = await _context.ProcessFeedBacks
                             .Where(p => p.Pfbcode == pFBCode)
                             .Select(p => new { Stage4StartDate = p.Dt.ToString("yyyy-MM-dd HH:mm:ss") })
                             .FirstOrDefaultAsync();

                return result?.Stage4StartDate ?? string.Empty;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<string> GetStageStatusCount(string query, string JobCode)
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(new SqlParameter("@JobCode", JobCode));

                //await _context.Database.OpenConnectionAsync();
                var currentTransaction = _context.Database.CurrentTransaction;
                if (currentTransaction != null)
                {
                    command.Transaction = currentTransaction.GetDbTransaction();
                }
                var result = await command.ExecuteScalarAsync();

                return result.ToString();
            }

        }

        public async Task<string> GetKVAFromPartTable(string productcode)
        {
            string query = @"Select Kva from Part where PartCode=@productcode";
            var parameters = new[] {
                new SqlParameter("@productcode", productcode)
            };

            var result = await _context.Parts
                .FromSqlRaw(query, parameters)
                .Select(x => x.Kva.ToString())
                .FirstOrDefaultAsync();
            return result;
        }

        private async Task<(string audioFilePath, string videoFilePath)> SaveRecordedFiles(DGAssemblySubmitRequest dgStageScanReq)
        {
            string audioFilePath = null;
            string videoFilePath = null;

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // Ensure the uploads directory exists
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // ✅ Save Audio File
            if (dgStageScanReq.RecordedAudioFile != null)
            {
                string audioFileName = $"audio_{Guid.NewGuid()}.mp3"; // Change extension if needed
                audioFilePath = Path.Combine(uploadsFolder, audioFileName);

                using (var stream = new FileStream(audioFilePath, FileMode.Create))
                {
                    await dgStageScanReq.RecordedAudioFile.CopyToAsync(stream);
                }
            }

            // ✅ Save Video File
            if (dgStageScanReq.RecordedVideoFile != null)
            {
                string videoFileName = $"video_{Guid.NewGuid()}.mp4"; // Change extension if needed
                videoFilePath = Path.Combine(uploadsFolder, videoFileName);

                using (var stream = new FileStream(videoFilePath, FileMode.Create))
                {
                    await dgStageScanReq.RecordedVideoFile.CopyToAsync(stream);
                }
            }

            return (audioFilePath, videoFilePath);
        }


        public async Task<string> UploadVideoAndPDFAsync(UploadVideopdfDGAssemblyRequest uploadVideopdfDGAssemblyReq)
        {
            string location = "";
            string file_name = "";

            // Start transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check for existing TestReport
                if (uploadVideopdfDGAssemblyReq.UploadFor == "TestReport")
                {
                    string? chkEngNoSave = await _context.Videos
                        .Where(v => v.EngSrNo == uploadVideopdfDGAssemblyReq.EngSrNo
                               && v.TrVideoType == "TR"
                               && v.Active == true)
                        .Select(v => v.EngSrNo)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(chkEngNoSave))
                    {
                        return $"Test Report Already Uploaded For Engine SrNo: {uploadVideopdfDGAssemblyReq.EngSrNo}.";
                    }
                }

                // Check for existing PDIR
                if (uploadVideopdfDGAssemblyReq.UploadFor == "PDIR")
                {
                    string? chkEngNoSave = await _context.Videos
                        .Where(v => v.EngSrNo == uploadVideopdfDGAssemblyReq.EngSrNo
                               && v.PdirVideoType == "PD"
                               && v.Active == true)
                        .Select(v => v.EngSrNo)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(chkEngNoSave))
                    {
                        return $"PDIR Already Uploaded For Engine SrNo: {uploadVideopdfDGAssemblyReq.EngSrNo}.";
                    }
                }

                // Validate file
                if (uploadVideopdfDGAssemblyReq.File == null || uploadVideopdfDGAssemblyReq.File.Length == 0)
                {
                    return "Please select a file.";
                }

                string extension = Path.GetExtension(uploadVideopdfDGAssemblyReq.File.FileName).ToLower();

                // Validate file extension
                string[] allowedExtensions = { ".jpg", ".jpeg", ".wmv", ".flv", ".mp4", ".avi", ".mpg", ".wav", ".mpeg", ".dat", ".pdf" };
                if (!allowedExtensions.Contains(extension))
                {
                    return "Invalid file type. Please select a valid file.";
                }

                // Generate filename and path
                file_name = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss") + extension;
                location = GetMainFilePath("TRAttachments") + "\\" + file_name;

                // Save file
                using (var stream = new FileStream(location, FileMode.Create))
                {
                    await uploadVideopdfDGAssemblyReq.File.CopyToAsync(stream);
                }

                // Set parameters based on Id (0 = new, else update)
                string saveType = uploadVideopdfDGAssemblyReq.Id == 0 ? "S" : "U";
                string videoId = uploadVideopdfDGAssemblyReq.Id.ToString();

                // Set parameters based on UploadFor (TestReport or PDIR)
                string trVideoType = "";
                string pdirVideoType = "";
                string trVideoName = "";
                string pdirVideoName = "";

                if (uploadVideopdfDGAssemblyReq.UploadFor == "TestReport")
                {
                    trVideoType = "TR";
                    pdirVideoType = "N";
                    trVideoName = file_name;
                    pdirVideoName = "";
                }
                else if (uploadVideopdfDGAssemblyReq.UploadFor == "PDIR")
                {
                    trVideoType = "N";
                    pdirVideoType = "PD";
                    trVideoName = "";
                    pdirVideoName = file_name;
                }

                // Call stored procedure using EF Core
                var parameters = new[]
                {
                      new SqlParameter("@save_type", SqlDbType.VarChar, 5) { Value = saveType },
                      new SqlParameter("@video_id", SqlDbType.VarChar, 10) { Value = videoId },
                      new SqlParameter("@tr_video_type", SqlDbType.VarChar, 10) { Value = trVideoType },       
                      new SqlParameter("@pdir_video_type", SqlDbType.VarChar, 10) { Value = pdirVideoType },  
                      new SqlParameter("@eng_sr_no", SqlDbType.VarChar, 50) { Value = uploadVideopdfDGAssemblyReq.EngSrNo.Trim() },  
                      new SqlParameter("@tr_video_name", SqlDbType.VarChar, 50) { Value = trVideoName },
                      new SqlParameter("@pdir_video_name", SqlDbType.VarChar, 50) { Value = pdirVideoName },
                      new SqlParameter("@video_path", SqlDbType.VarChar, 100) { Value = location },
                      new SqlParameter("@emp_code", SqlDbType.VarChar, 10) { Value = uploadVideopdfDGAssemblyReq.EmpCode ?? "" }
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC video_upload @save_type, @video_id, @tr_video_type, @pdir_video_type, @eng_sr_no, @tr_video_name, @pdir_video_name, @video_path, @emp_code",
                    parameters
                );

                //await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                return $"{uploadVideopdfDGAssemblyReq.UploadFor} File uploaded successfully for EngSrNo: {uploadVideopdfDGAssemblyReq.EngSrNo}"; 
            }
            catch (Exception ex)
            {
                // Rollback transaction
                await transaction.RollbackAsync();
                throw ex;
            }
        }

        public string GetMainFilePath(string fileFolder)
        {
            // Get Year from Database
            var year = _context.Database
                .SqlQueryRaw<string>("SELECT CAST(Year(GETDATE()) AS NVARCHAR(10)) AS Value")
                .FirstOrDefault();

            // Get Month from Database
            var month = _context.Database
                .SqlQueryRaw<string>(@"SELECT CASE 
            WHEN MONTH(GETDATE()) < 10 THEN '0' + CAST(MONTH(GETDATE()) AS NVARCHAR(10)) + ' ' + DATENAME(MONTH, GETDATE()) 
            WHEN MONTH(GETDATE()) >= 10 THEN CAST(MONTH(GETDATE()) AS NVARCHAR(10)) + ' ' + DATENAME(MONTH, GETDATE()) 
            END AS Value")
                .FirstOrDefault();

            // Build complete path
            string fullPath = Path.Combine("D:\\ERP", year, month, fileFolder.Trim());

            // Create all directories in one call
            Directory.CreateDirectory(fullPath);

            return fullPath;
        }
    }
}