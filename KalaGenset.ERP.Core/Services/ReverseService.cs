using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace KalaGenset.ERP.Core.Services
{
    public class ReverseService : IReverse
    {
        private readonly KalaDbContext _db;
        private readonly string _connStr;
        private readonly CommonCon _com;

        public ReverseService(KalaDbContext context, ICommonService common, ILogger<ReverseService> logger, IConfiguration config, CommonCon com)
        {
            _db = context;
            _connStr = config.GetConnectionString("KalaDbContext")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _com = com;
        }


        public async Task<List<Dictionary<string, object>>> GetRevPCCodeAsync(string strTransType, string catId)
        {
            await using var con = new SqlConnection(_connStr);

            using var cmd = new SqlCommand("RevPCCode_NewERP", con)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 0            // LEGACY parity: 0 = no timeout
            };
            cmd.Parameters.Add("@ddlType", SqlDbType.Char).Value = strTransType;
            cmd.Parameters.Add("@CatId", SqlDbType.NVarChar, 50).Value = catId ?? "";

            await con.OpenAsync();

            var rows = new List<Dictionary<string, object>>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>(reader.FieldCount);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }
            return rows;
        }


        public async Task<List<Dictionary<string, object>>> LoadRevPrcDtsAsync(string strPCCode, string catId)
        {
            await using var con = new SqlConnection(_connStr);

            // LEGACY: SqlDataAdapter dAd = new SqlDataAdapter("RevTransCPY", con);
            using var cmd = new SqlCommand("RevTransCPY_NewERP", con)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 0            // LEGACY parity: 0 = no timeout
            };
            cmd.Parameters.Add("@PCCode", SqlDbType.Char).Value = strPCCode;
            cmd.Parameters.Add("@CatId", SqlDbType.Char).Value = catId;

            // LEGACY: the if/else chain only ADDS @PCCodeNext when a branch matches —
            //         unmatched PCCodes must NOT send the parameter, so the proc's
            //         default kicks in. Do not make this unconditional.
            string pcCodeNext = GetNextPCCode(strPCCode, catId);
            if (pcCodeNext != null)
            {
                cmd.Parameters.Add("@PCCodeNext", SqlDbType.Char).Value = pcCodeNext;
            }

            await con.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            return await MapRowsAsync(reader);
        }

        /// <summary>
        /// Process-flow routing: which stage a reversal at this PC moves to next.
        /// 1:1 with the legacy if/else chain — every branch and comment kept.
        /// Returns null when there is no mapping (parameter is then not sent).
        /// </summary>
        private static string GetNextPCCode(string strPCCode, string catId)
        {
            if (strPCCode == "01.095")// CNC A
                return "01.098";// bending A

            if (strPCCode == "01.096")// CNC B
                return "01.099";// bending B

            if (strPCCode == "01.097")// CNC C
                return "01.100";// bending C

            if (strPCCode == "03.066" && (catId == "038" || catId == "084")) // U 4 CNC A
                return "03.070";
            if (strPCCode == "03.067" && (catId == "038" || catId == "084")) // U 4 CNC B
                return "03.071";
            if (strPCCode == "03.068" && (catId == "038" || catId == "084")) // U 4 CNC C
                return "03.072";


            if (strPCCode == "01.098" && catId == "029") //Bending A
                return "01.101"; //fabrication A
            if (strPCCode == "01.099" && catId == "029")//Bending B
                return "01.102";//fabrication B
            if (strPCCode == "01.100" && catId == "029")//Bending C
                return "01.103";//fabrication C

            if (strPCCode == "03.070" && (catId == "038" || catId == "084")) // U4 Bending A
                return "03.073";//fabrication A
            if (strPCCode == "03.071" && (catId == "038" || catId == "084")) // U4 Bending B
                return "03.074"; //fabrication B
            if (strPCCode == "03.072" && (catId == "038" || catId == "084")) // U4 Bending C
                return "03.075"; //fabrication C


            if (strPCCode == "01.101" && catId == "029") //Bending A
                return "01.116"; //fabrication A
            if (strPCCode == "01.102" && catId == "029")//Bending B
                return "01.116";//fabrication B
            if (strPCCode == "01.103" && catId == "029")//Bending C
                return "01.116";//fabrication C

            if (strPCCode == "03.073" && (catId == "038" || catId == "084")) // U4 Bending A
                return "01.116";//fabrication A
            if (strPCCode == "03.074" && (catId == "038" || catId == "084")) // U4 Bending B
                return "01.116"; //fabrication B
            if (strPCCode == "03.075" && (catId == "038" || catId == "084")) // U4 Bending C
                return "01.116"; //fabrication C
            return null;
        }

        // </summary>
        private static async Task<List<Dictionary<string, object>>> MapRowsAsync(SqlDataReader reader)
        {
            var rows = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>(reader.FieldCount);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }
            return rows;
        }




        //public async Task<string> SubmitRevCpyTransAsync(CpyRevRequest CpyRevReq)
        //{
        //    string PrcNo = "";

        //    if (CpyRevReq.Details == null || CpyRevReq.Details.Count == 0)
        //    {
        //        return "Please Select At Least One Check Box !";
        //    }

        //    string pcCode_Act = (CpyRevReq.PCCode_Act ?? "").Trim();
        //    string pcCode = (CpyRevReq.PCCode ?? "").Trim();
        //    string transType = (CpyRevReq.TransType ?? "").Trim();

        //    // LEGACY: the if/else chain setting strNextPCCode + strPlanStatus.
        //    //         strNextPCCode was a SPLICED multi-value fragment
        //    //         ("01.076','01.002','01.008") — now a proper string array.
        //    var (nextPCCodes, planStatusSet) = GetReverseRouting(pcCode_Act);

        //    // LEGACY: an unmatched PCCode produced "UPDATE ... SET  WHERE ..."
        //    //         (empty SET → SqlException → rollback + stack trace). Guarded
        //    //         with a clear message instead — only matters for IndividualCode,
        //    //         since AllCode never uses the routing.
        //    if (transType == "IndividualCode" && nextPCCodes == null)
        //    {
        //        return "No Reverse Routing Configured For PCCode " + pcCode_Act + " !";
        //    }

        //    // LEGACY: if(open){close}else{open} + finally{ con.Close(); }
        //    //         → 'await using' + OpenAsync handles both.
        //    await using var con = new SqlConnection(_connStr);
        //    SqlTransaction tran = null;
        //    var sb = new StringBuilder();
        //    SqlCommand cmd;

        //    try
        //    {
        //        await con.OpenAsync();
        //        tran = (SqlTransaction)await con.BeginTransactionAsync();

        //        // LEGACY called this per row / per statement — value can't change
        //        // inside the transaction, so it's hoisted.
        //        string yr = _com.yearEnd(con, tran);
        //        string companyCode = pcCode_Act.Substring(0, 2);

        //        foreach (var d in CpyRevReq.Details)
        //        {
        //            string cp = (d.CPCode ?? "").Trim();       // legacy Dts[0]
        //            string product = (d.ProductCode ?? "").Trim();  // legacy Dts[1]
        //            string catId = (d.CatId ?? "").Trim();        // legacy Dts[2]

        //            // Mst Entry
        //            #region Mst Entry
        //            PrcNo = await _com.GetmaxPrcAsync("CpyrevTrans", "REVCode", yr, companyCode, con, tran);

        //            sb.Remove(0, sb.Length);
        //            sb.Append("INSERT INTO CpyrevTrans(REVCode,MaxSrNo,Dt,Yr,PCCode,PCCode_Act,TransType,CPCode,ProductCode,CompanyCode) ");
        //            sb.Append("VALUES(@REVCode,@MaxSrNo,@Dt,@Yr,@PCCode,@PCCode_Act,@TransType,@CPCode,@ProductCode,@CompanyCode)");
        //            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //            cmd.Parameters.AddWithValue("@REVCode", PrcNo.Trim());
        //            cmd.Parameters.AddWithValue("@MaxSrNo", PrcNo.Substring(10, 8));
        //            cmd.Parameters.AddWithValue("@Dt", DateTime.Now);   // LEGACY inserted a formatted string
        //            cmd.Parameters.AddWithValue("@Yr", yr);
        //            cmd.Parameters.AddWithValue("@PCCode", pcCode);
        //            cmd.Parameters.AddWithValue("@PCCode_Act", pcCode_Act);
        //            cmd.Parameters.AddWithValue("@TransType", transType);
        //            cmd.Parameters.AddWithValue("@CPCode", cp);
        //            cmd.Parameters.AddWithValue("@ProductCode", product);
        //            cmd.Parameters.AddWithValue("@CompanyCode", companyCode);
        //            await cmd.ExecuteNonQueryAsync();
        //            await cmd.DisposeAsync();
        //            #endregion




        //            // Update Prc Dts
        //            #region Update Prc Dts
        //            if (transType == "IndividualCode")
        //            {
        //                // StockWip
        //                #region StockWip
        //                // For StkWip issue Individual
        //                sb.Remove(0, sb.Length);
        //                sb.Append("DELETE FROM Stockwip WHERE Issuecode IN (SELECT PFBCode FROM ProcessFeedback ");
        //                sb.Append("WHERE PCCode_Act=@PCCode AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
        //                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //                cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
        //                cmd.Parameters.AddWithValue("@CPCode", cp);
        //                cmd.Parameters.AddWithValue("@ProductCode", product);
        //                await cmd.ExecuteNonQueryAsync();
        //                await cmd.DisposeAsync();

        //                // For StkWip issue Next
        //                sb.Remove(0, sb.Length);
        //                cmd = new SqlCommand() { Connection = con, Transaction = tran };
        //                sb.Append("DELETE FROM Stockwip WHERE Issuecode IN (SELECT PFBCode FROM ProcessFeedback ");
        //                sb.Append("WHERE PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") ");
        //                sb.Append("AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
        //                cmd.CommandText = sb.ToString();
        //                cmd.Parameters.AddWithValue("@CPCode", cp);
        //                cmd.Parameters.AddWithValue("@ProductCode", product);
        //                await cmd.ExecuteNonQueryAsync();
        //                await cmd.DisposeAsync();

        //                // For StkWip Received Individual
        //                sb.Remove(0, sb.Length);
        //                sb.Append("DELETE FROM Stockwip WHERE ReceivedCode IN (SELECT PFBCode FROM ProcessFeedback ");
        //                sb.Append("WHERE PCCode_Act=@PCCode AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
        //                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //                cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
        //                cmd.Parameters.AddWithValue("@CPCode", cp);
        //                cmd.Parameters.AddWithValue("@ProductCode", product);
        //                await cmd.ExecuteNonQueryAsync();
        //                await cmd.DisposeAsync();

        //                // For StkWip Received Next
        //                sb.Remove(0, sb.Length);
        //                cmd = new SqlCommand() { Connection = con, Transaction = tran };
        //                sb.Append("DELETE FROM Stockwip WHERE ReceivedCode IN (SELECT PFBCode FROM ProcessFeedback ");
        //                sb.Append("WHERE PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") ");
        //                sb.Append("AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
        //                cmd.CommandText = sb.ToString();
        //                cmd.Parameters.AddWithValue("@CPCode", cp);
        //                cmd.Parameters.AddWithValue("@ProductCode", product);
        //                await cmd.ExecuteNonQueryAsync();
        //                await cmd.DisposeAsync();
        //                #endregion

        //                // PrdWip
        //                #region PrdWip
        //                if (catId == "029")
        //                {
        //                    // For PrdWip issue Individual
        //                    sb.Remove(0, sb.Length);
        //                    sb.Append("DELETE FROM ProductWip WHERE IssueCode IN (SELECT PFBCode FROM ProcessFeedback ");
        //                    sb.Append("WHERE PCCode_Act=@PCCode AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
        //                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //                    cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
        //                    cmd.Parameters.AddWithValue("@CPCode", cp);
        //                    cmd.Parameters.AddWithValue("@ProductCode", product);
        //                    await cmd.ExecuteNonQueryAsync();
        //                    await cmd.DisposeAsync();

        //                    // For PrdWip issue Next
        //                    sb.Remove(0, sb.Length);
        //                    cmd = new SqlCommand() { Connection = con, Transaction = tran };
        //                    sb.Append("DELETE FROM ProductWip WHERE IssueCode IN (SELECT PFBCode FROM ProcessFeedback ");
        //                    sb.Append("WHERE PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") ");
        //                    sb.Append("AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
        //                    cmd.CommandText = sb.ToString();
        //                    cmd.Parameters.AddWithValue("@CPCode", cp);
        //                    cmd.Parameters.AddWithValue("@ProductCode", product);
        //                    await cmd.ExecuteNonQueryAsync();
        //                    await cmd.DisposeAsync();

        //                    // For PrdWip Received Individual
        //                    sb.Remove(0, sb.Length);
        //                    sb.Append("DELETE FROM ProductWip WHERE ReceivedCode IN (SELECT PFBCode FROM ProcessFeedback ");
        //                    sb.Append("WHERE PCCode_Act=@PCCode AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
        //                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //                    cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
        //                    cmd.Parameters.AddWithValue("@CPCode", cp);
        //                    cmd.Parameters.AddWithValue("@ProductCode", product);
        //                    await cmd.ExecuteNonQueryAsync();
        //                    await cmd.DisposeAsync();

        //                    // For PrdWip Received Next
        //                    sb.Remove(0, sb.Length);
        //                    cmd = new SqlCommand() { Connection = con, Transaction = tran };
        //                    sb.Append("DELETE FROM ProductWip WHERE ReceivedCode IN (SELECT PFBCode FROM ProcessFeedback ");
        //                    sb.Append("WHERE PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") ");
        //                    sb.Append("AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
        //                    cmd.CommandText = sb.ToString();
        //                    cmd.Parameters.AddWithValue("@CPCode", cp);
        //                    cmd.Parameters.AddWithValue("@ProductCode", product);
        //                    await cmd.ExecuteNonQueryAsync();
        //                    await cmd.DisposeAsync();
        //                }
        //                #endregion

        //                // Inactive Process
        //                #region Inactive Process
        //                // Partial Prc For Individual Prc
        //                sb.Remove(0, sb.Length);
        //                sb.Append("UPDATE ProcessFeedback SET Active='0' ");
        //                sb.Append("WHERE CanopyPlanCode=@CPCode AND ProductCode=@ProductCode ");
        //                sb.Append("AND PCCode_Act=@PCCode AND CatID=@CatId");
        //                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //                cmd.Parameters.AddWithValue("@CPCode", cp);
        //                cmd.Parameters.AddWithValue("@ProductCode", product);
        //                cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
        //                cmd.Parameters.AddWithValue("@CatId", catId);
        //                await cmd.ExecuteNonQueryAsync();
        //                await cmd.DisposeAsync();

        //                // Partial Prc For Nxt Prc
        //                sb.Remove(0, sb.Length);
        //                cmd = new SqlCommand() { Connection = con, Transaction = tran };
        //                sb.Append("UPDATE ProcessFeedback SET Active='0' ");
        //                sb.Append("WHERE CanopyPlanCode=@CPCode AND ProductCode=@ProductCode ");
        //                sb.Append("AND PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") AND CatID=@CatId");
        //                cmd.CommandText = sb.ToString();
        //                cmd.Parameters.AddWithValue("@CPCode", cp);
        //                cmd.Parameters.AddWithValue("@ProductCode", product);
        //                cmd.Parameters.AddWithValue("@CatId", catId);
        //                await cmd.ExecuteNonQueryAsync();
        //                await cmd.DisposeAsync();
        //                #endregion

        //                // Update Plan
        //                #region Update Plan
        //                // planStatusSet is a server-side constant from GetReverseRouting —
        //                // safe to splice; the row keys stay parameterized.

        //                    sb.Remove(0, sb.Length);
        //                    sb.Append("UPDATE canopyPlanDtsSub SET " + planStatusSet + " ");
        //                    sb.Append("WHERE CPCode=@CPCode AND CpyPartCode=@ProductCode AND CatID=@CatId");
        //                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //                    cmd.Parameters.AddWithValue("@CPCode", cp);
        //                    cmd.Parameters.AddWithValue("@ProductCode", product);
        //                    cmd.Parameters.AddWithValue("@CatId", catId);
        //                    await cmd.ExecuteNonQueryAsync();
        //                    await cmd.DisposeAsync();


        //                if (pcCode_Act == "01.095" || pcCode_Act == "01.096" || pcCode_Act == "01.097" || pcCode_Act == "03.066" || pcCode_Act == "03.067" || pcCode_Act == "03.068")
        //                {
        //                    sb.Remove(0, sb.Length);
        //                    sb.Append("UPDATE TurretKitForPrc SET PrcStatus='P',PartcutStatus='P' ");
        //                    sb.Append("WHERE CPCode=@CPCode AND CanopyPartCode=@ProductCode AND CatID=@CatId");
        //                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //                    cmd.Parameters.AddWithValue("@CPCode", cp);
        //                    cmd.Parameters.AddWithValue("@ProductCode", product);
        //                    cmd.Parameters.AddWithValue("@CatId", catId);
        //                    await cmd.ExecuteNonQueryAsync();
        //                    await cmd.DisposeAsync();
        //                }


        //                if (pcCode_Act == "01.101" || pcCode_Act == "01.102" || pcCode_Act == "01.103" || pcCode_Act == "03.073" || pcCode_Act == "03.074" || pcCode_Act == "03.075")
        //                {
        //                    sb.Remove(0, sb.Length);
        //                    sb.Append("UPDATE CanopyPlanOSDetails SET OSFQty='0',OSFStatus='P' ");
        //                    sb.Append("WHERE CPCode=@CPCode AND CpyPartCode=@ProductCode");
        //                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
        //                    cmd.Parameters.AddWithValue("@CPCode", cp);
        //                    cmd.Parameters.AddWithValue("@ProductCode", product);
        //                    await cmd.ExecuteNonQueryAsync();
        //                    await cmd.DisposeAsync();
        //                }
        //                #endregion
        //            }
        //            #endregion

        //            //****************User Activity****************
        //            cmd = new SqlCommand("InsertLoginTransactionDetails", con);
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
        //            cmd.Parameters.AddWithValue("@EmpID", (CpyRevReq.EmpCode ?? "").Trim());
        //            cmd.Parameters.AddWithValue("@TransactionType", "S");
        //            cmd.Parameters.AddWithValue("@TransactionFrom", "Canopy Assembly Process");   // LEGACY verbatim
        //            cmd.Parameters.AddWithValue("@TransactionNo", PrcNo.Trim());
        //            cmd.Parameters.AddWithValue("@CompanyCode", companyCode);
        //            cmd.Transaction = tran;
        //            await cmd.ExecuteNonQueryAsync();
        //            await cmd.DisposeAsync();
        //        }

        //        //await tran.CommitAsync();
        //         await tran.RollbackAsync();   
        //        // ← dry-run toggle: swap with CommitAsync to test without saving

        //        // LEGACY message verbatim (PrcNo = last generated REVCode)
        //        PrcNo = "ReverseCode=" + PrcNo + " For Reverse Saved SuccessFully ";
        //        return PrcNo;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (tran != null)
        //            await tran.RollbackAsync();
        //        return ("StackTrace " + ex.StackTrace + " Message " + ex.Message);
        //    }
        //}

        /// <summary>
        /// Reverse routing per profit centre: downstream PCs to clean up + the plan
        /// status columns to reset. 1:1 with the legacy strNextPCCode/strPlanStatus
        /// chain, including the commented-out branches. Returns (null, null) when
        /// the PCCode has no routing.
        ///
        /// NOTE: legacy strNextPCCode was a spliced fragment like
        /// "01.076','01.002','01.008" — here each value is a real array element.
        /// </summary>
        private static (string[] NextPCCodes, string PlanStatusSet) GetReverseRouting(string pcCode_Act)
        {
            switch (pcCode_Act)
            {
                case "01.095":
                    return (new[] { "01.098", "01.101" },
                        "CPTQty=0,CPTStatus='P',CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

                case "01.096":
                    return (new[] { "01.099", "01.102" },
                        "CPTQty=0,CPTStatus='P',CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

                case "01.097":
                    return (new[] { "01.100", "01.103" },
                        "CPTQty=0,CPTStatus='P',CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

                case "03.066":
                    return (new[] { "03.070", "03.073" },
                        "CPTQty=0,CPTStatus='P',CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

                case "03.067":
                    return (new[] { "03.071", "03.074" },
                        "CPTQty=0,CPTStatus='P',CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

                case "03.068":
                    return (new[] { "03.072", "03.075" },
                        "CPTQty=0,CPTStatus='P',CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");


                case "01.098":
                    return (new[] { "01.101" }, "CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");
                case "01.099":
                    return (new[] { "01.102" }, "CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");
                case "01.100":
                    return (new[] { "01.103" }, "CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

                case "03.070":
                    return (new[] { "03.073" }, "CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");
                case "03.071":
                    return (new[] { "03.074" }, "CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");
                case "03.072":
                    return (new[] { "03.075" }, "CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");



                case "01.101":
                    return (new[] { "01.116" }, "CPFQty=0,CPFStatus='P',CPPCQty=0,CPPCStatus='P'");
                case "01.102":
                    return (new[] { "01.116" }, "CPFQty=0,CPFStatus='P',CPPCQty=0,CPPCStatus='P'");
                case "01.103":
                    return (new[] { "01.116" }, "CPFQty=0,CPFStatus='P',CPPCQty=0,CPPCStatus='P'");


                case "03.073":
                    return (new[] { "01.116" }, "CPFQty=0,CPFStatus='P'");
                case "03.074":
                    return (new[] { "01.116" }, "CPFQty=0,CPFStatus='P'");
                case "03.075":
                    return (new[] { "01.116" }, "CPFQty=0,CPFStatus='P'");
                default:
                    return (null, null);
            }
        }


        //private static (string[] NextPCCodes, string PlanStatusSet) GetReverseRoutingPCCode(string pcCode)
        //{
        //    switch (pcCode)
        //    {
        //        case "01.009":
        //            return (new[] { "01.076", "01.002", "01.008" },
        //                "CPTQty=0,CPTStatus='P',CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

        //        case "03.061":
        //            return (new[] { "01.076", "03.004", "03.002" },
        //                "CPTQty=0,CPTStatus='P',CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

        //        // LEGACY (commented out):
        //        // case "01.076":
        //        //     return (new[] { "01.002", "01.008", "01.007", "01.005" },
        //        //         "CPPartCutQty=0,CPPartCutStatus='P',CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P',CPPCQty=0,CPPCStatus='P'");

        //        case "01.002":
        //            return (new[] { "01.008" }, "CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

        //        case "03.004":
        //            return (new[] { "03.002" }, "CPBQty=0,CPBStatus='P',CPFQty=0,CPFStatus='P'");

        //        case "01.008":
        //            return (new[] { "01.007", "01.005" }, "CPFQty=0,CPFStatus='P',CPPCQty=0,CPPCStatus='P'");

        //        case "03.002":
        //            return (new[] { "01.007" }, "CPFQty=0,CPFStatus='P'");

        //        // LEGACY older variant (commented out): 01.007 → "01.005"
        //        case "01.007":
        //            return (new[] { "01.093" }, "CPPCQty=0,CPPCStatus='P'");
        //        // ⚠ LoadPrcDts routes 01.007 → 01.005 — confirm which is current.

        //        case "03.001":
        //            return (new[] { "03.038" }, "CPPCQty=0,CPPCStatus='P'");

        //        case "01.005":
        //            return (new[] { "01.005" }, "CpyWopQty='0',CpyWopStatus='P',CpyWipQty='0',CpyWipStatus='P'");

        //        case "03.038":
        //            return (new[] { "03.038" }, "CpyWopQty='0',CpyWopStatus='P',CpyWipQty='0',CpyWipStatus='P'");

        //        default:
        //            return (null, null);
        //    }
        //}
        /// <summary>
        /// Adds one parameter per value to the command and returns the placeholder
        /// list ("@Next0,@Next1,...") for use inside IN (...). A single @Next
        /// parameter would compare against the literal joined string and match
        /// nothing — this is why the legacy code spliced the fragment.
        /// </summary>
        private static string BuildInClause(SqlCommand cmd, string[] values)
        {
            var names = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                names[i] = "@Next" + i;
                cmd.Parameters.AddWithValue(names[i], values[i]);
            }
            return string.Join(",", names);
        }

        private static async Task ExecProcAsync( SqlConnection con, SqlTransaction tran, CancellationToken ct,string procName, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(procName, con, tran) { CommandType = CommandType.StoredProcedure };
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);


        }


        public async Task<string> SubmitRevCpyTransAsync(CpyRevRequest CpyRevReq, CancellationToken ct = default)
        {
            string PrcNo = "";

            if (CpyRevReq.Details == null || CpyRevReq.Details.Count == 0)
            {
                return "Please Select At Least One Check Box !";
            }

            string pcCode_Act = (CpyRevReq.PCCode_Act ?? "").Trim();
            string pcCode = (CpyRevReq.PCCode ?? "").Trim();
            string transType = (CpyRevReq.TransType ?? "").Trim();

            // LEGACY: the if/else chain setting strNextPCCode + strPlanStatus.
            //         strNextPCCode was a SPLICED multi-value fragment
            //         ("01.076','01.002','01.008") — now a proper string array.
            var (nextPCCodes, planStatusSet) = GetReverseRouting(pcCode_Act);

            // LEGACY: an unmatched PCCode produced "UPDATE ... SET  WHERE ..."
            //         (empty SET → SqlException → rollback + stack trace). Guarded
            //         with a clear message instead — only matters for IndividualCode,
            //         since AllCode never uses the routing.
            if (transType == "IndividualCode" && nextPCCodes == null)
            {
                return "No Reverse Routing Configured For PCCode " + pcCode_Act + " !";
            }

            // LEGACY: if(open){close}else{open} + finally{ con.Close(); }
            //         → 'await using' + OpenAsync handles both.
            await using var con = new SqlConnection(_connStr);
            SqlTransaction tran = null;
            var sb = new StringBuilder();
            SqlCommand cmd;

            try
            {
                await con.OpenAsync();
                tran = (SqlTransaction)await con.BeginTransactionAsync();

                // LEGACY called this per row / per statement — value can't change
                // inside the transaction, so it's hoisted.
                string yr = _com.yearEnd(con, tran);
                string companyCode = pcCode_Act.Substring(0, 2);

                foreach (var d in CpyRevReq.Details)
                {
                    string cp = (d.CPCode ?? "").Trim();            // legacy Dts[0]
                    string product = (d.ProductCode ?? "").Trim();  // legacy Dts[1]
                    string catId = (d.CatId ?? "").Trim();          // legacy Dts[2]

                    // Mst Entry
                    #region Mst Entry
                    PrcNo = await _com.GetmaxPrcAsync("CpyrevTrans", "REVCode", yr, companyCode, con, tran);

                    sb.Remove(0, sb.Length);
                    sb.Append("INSERT INTO CpyrevTrans(REVCode,MaxSrNo,Dt,Yr,PCCode,PCCode_Act,TransType,CPCode,ProductCode,CompanyCode) ");
                    sb.Append("VALUES(@REVCode,@MaxSrNo,@Dt,@Yr,@PCCode,@PCCode_Act,@TransType,@CPCode,@ProductCode,@CompanyCode)");
                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                    cmd.Parameters.AddWithValue("@REVCode", PrcNo.Trim());
                    cmd.Parameters.AddWithValue("@MaxSrNo", PrcNo.Substring(10, 8));
                    cmd.Parameters.AddWithValue("@Dt", DateTime.Now);   // LEGACY inserted a formatted string
                    cmd.Parameters.AddWithValue("@Yr", yr);
                    cmd.Parameters.AddWithValue("@PCCode", pcCode);
                    cmd.Parameters.AddWithValue("@PCCode_Act", pcCode_Act);
                    cmd.Parameters.AddWithValue("@TransType", transType);
                    cmd.Parameters.AddWithValue("@CPCode", cp);
                    cmd.Parameters.AddWithValue("@ProductCode", product);
                    cmd.Parameters.AddWithValue("@CompanyCode", companyCode);
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();
                    #endregion

                    // ── NEW: 6M Dts Entry — SheetMetal 6M checker pattern, fed from the
                    //         structured ProductionDetails list.
                    #region 6M Dts Entry
                    foreach (var m in CpyRevReq.ProductionDetails)
                    {
                        string sixM = (m.SixM ?? "").Trim();
                        string assignTo = (m.AssignTo ?? "").Trim();

                        // skip rows with no 6M name or no assignee ("" or "0")
                        if (assignTo != "" || assignTo != "0" || assignTo != "None")
                        {

                            await ExecProcAsync(con, tran, ct, "InsertSheetMetal6MChecker_Detail",
                                ("@PlanCode", cp),
                                ("@SixMName", sixM),
                                ("@Description", (m.Description ?? "").Trim()),
                                ("@AssignTo", assignTo),
                                ("@CorReqNo", "0"),
                                ("@Status", "P"));
                        }
                    }
                    #endregion

                    // Update Prc Dts
                    #region Update Prc Dts
                    if (transType == "IndividualCode")
                    {
                        // StockWip
                        #region StockWip
                        // For StkWip issue Individual
                        sb.Remove(0, sb.Length);
                        sb.Append("DELETE FROM Stockwip WHERE Issuecode IN (SELECT PFBCode FROM ProcessFeedback ");
                        sb.Append("WHERE PCCode_Act=@PCCode AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                        cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
                        cmd.Parameters.AddWithValue("@CPCode", cp);
                        cmd.Parameters.AddWithValue("@ProductCode", product);
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        // For StkWip issue Next
                        sb.Remove(0, sb.Length);
                        cmd = new SqlCommand() { Connection = con, Transaction = tran };
                        sb.Append("DELETE FROM Stockwip WHERE Issuecode IN (SELECT PFBCode FROM ProcessFeedback ");
                        sb.Append("WHERE PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") ");
                        sb.Append("AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
                        cmd.CommandText = sb.ToString();
                        cmd.Parameters.AddWithValue("@CPCode", cp);
                        cmd.Parameters.AddWithValue("@ProductCode", product);
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        // For StkWip Received Individual
                        sb.Remove(0, sb.Length);
                        sb.Append("DELETE FROM Stockwip WHERE ReceivedCode IN (SELECT PFBCode FROM ProcessFeedback ");
                        sb.Append("WHERE PCCode_Act=@PCCode AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                        cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
                        cmd.Parameters.AddWithValue("@CPCode", cp);
                        cmd.Parameters.AddWithValue("@ProductCode", product);
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        // For StkWip Received Next
                        sb.Remove(0, sb.Length);
                        cmd = new SqlCommand() { Connection = con, Transaction = tran };
                        sb.Append("DELETE FROM Stockwip WHERE ReceivedCode IN (SELECT PFBCode FROM ProcessFeedback ");
                        sb.Append("WHERE PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") ");
                        sb.Append("AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
                        cmd.CommandText = sb.ToString();
                        cmd.Parameters.AddWithValue("@CPCode", cp);
                        cmd.Parameters.AddWithValue("@ProductCode", product);
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();
                        #endregion

                        // PrdWip
                        #region PrdWip
                        if (catId == "029")
                        {
                            // For PrdWip issue Individual
                            sb.Remove(0, sb.Length);
                            sb.Append("DELETE FROM ProductWip WHERE IssueCode IN (SELECT PFBCode FROM ProcessFeedback ");
                            sb.Append("WHERE PCCode_Act=@PCCode AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
                            cmd.Parameters.AddWithValue("@CPCode", cp);
                            cmd.Parameters.AddWithValue("@ProductCode", product);
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();

                            // For PrdWip issue Next
                            sb.Remove(0, sb.Length);
                            cmd = new SqlCommand() { Connection = con, Transaction = tran };
                            sb.Append("DELETE FROM ProductWip WHERE IssueCode IN (SELECT PFBCode FROM ProcessFeedback ");
                            sb.Append("WHERE PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") ");
                            sb.Append("AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
                            cmd.CommandText = sb.ToString();
                            cmd.Parameters.AddWithValue("@CPCode", cp);
                            cmd.Parameters.AddWithValue("@ProductCode", product);
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();

                            // For PrdWip Received Individual
                            sb.Remove(0, sb.Length);
                            sb.Append("DELETE FROM ProductWip WHERE ReceivedCode IN (SELECT PFBCode FROM ProcessFeedback ");
                            sb.Append("WHERE PCCode_Act=@PCCode AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
                            cmd.Parameters.AddWithValue("@CPCode", cp);
                            cmd.Parameters.AddWithValue("@ProductCode", product);
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();

                            // For PrdWip Received Next
                            sb.Remove(0, sb.Length);
                            cmd = new SqlCommand() { Connection = con, Transaction = tran };
                            sb.Append("DELETE FROM ProductWip WHERE ReceivedCode IN (SELECT PFBCode FROM ProcessFeedback ");
                            sb.Append("WHERE PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") ");
                            sb.Append("AND CanopyPlanCode=@CPCode AND ProductCode=@ProductCode)");
                            cmd.CommandText = sb.ToString();
                            cmd.Parameters.AddWithValue("@CPCode", cp);
                            cmd.Parameters.AddWithValue("@ProductCode", product);
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();
                        }
                        #endregion

                        // Inactive Process
                        #region Inactive Process
                        // Partial Prc For Individual Prc
                        sb.Remove(0, sb.Length);
                        sb.Append("UPDATE ProcessFeedback SET Active='0' ");
                        sb.Append("WHERE CanopyPlanCode=@CPCode AND ProductCode=@ProductCode ");
                        sb.Append("AND PCCode_Act=@PCCode AND CatID=@CatId");
                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                        cmd.Parameters.AddWithValue("@CPCode", cp);
                        cmd.Parameters.AddWithValue("@ProductCode", product);
                        cmd.Parameters.AddWithValue("@PCCode", pcCode_Act);
                        cmd.Parameters.AddWithValue("@CatId", catId);
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        // Partial Prc For Nxt Prc
                        sb.Remove(0, sb.Length);
                        cmd = new SqlCommand() { Connection = con, Transaction = tran };
                        sb.Append("UPDATE ProcessFeedback SET Active='0' ");
                        sb.Append("WHERE CanopyPlanCode=@CPCode AND ProductCode=@ProductCode ");
                        sb.Append("AND PCCode_Act IN (" + BuildInClause(cmd, nextPCCodes) + ") AND CatID=@CatId");
                        cmd.CommandText = sb.ToString();
                        cmd.Parameters.AddWithValue("@CPCode", cp);
                        cmd.Parameters.AddWithValue("@ProductCode", product);
                        cmd.Parameters.AddWithValue("@CatId", catId);
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();
                        #endregion

                        // Update Plan
                        #region Update Plan
                        // planStatusSet is a server-side constant from GetReverseRouting —
                        // safe to splice; the row keys stay parameterized.
                        sb.Remove(0, sb.Length);
                        sb.Append("UPDATE canopyPlanDtsSub SET " + planStatusSet + " ");
                        sb.Append("WHERE CPCode=@CPCode AND CpyPartCode=@ProductCode AND CatID=@CatId");
                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                        cmd.Parameters.AddWithValue("@CPCode", cp);
                        cmd.Parameters.AddWithValue("@ProductCode", product);
                        cmd.Parameters.AddWithValue("@CatId", catId);
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        if (pcCode_Act == "01.095" || pcCode_Act == "01.096" || pcCode_Act == "01.097" || pcCode_Act == "03.066" || pcCode_Act == "03.067" || pcCode_Act == "03.068")
                        {
                            sb.Remove(0, sb.Length);
                            sb.Append("UPDATE TurretKitForPrc SET PrcStatus='P',PartcutStatus='P' ");
                            sb.Append("WHERE CPCode=@CPCode AND CanopyPartCode=@ProductCode AND CatID=@CatId");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            cmd.Parameters.AddWithValue("@CPCode", cp);
                            cmd.Parameters.AddWithValue("@ProductCode", product);
                            cmd.Parameters.AddWithValue("@CatId", catId);
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();
                        }

                        if (pcCode_Act == "01.101" || pcCode_Act == "01.102" || pcCode_Act == "01.103" || pcCode_Act == "03.073" || pcCode_Act == "03.074" || pcCode_Act == "03.075")
                        {
                            sb.Remove(0, sb.Length);
                            sb.Append("UPDATE CanopyPlanOSDetails SET OSFQty='0',OSFStatus='P' ");
                            sb.Append("WHERE CPCode=@CPCode AND CpyPartCode=@ProductCode");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            cmd.Parameters.AddWithValue("@CPCode", cp);
                            cmd.Parameters.AddWithValue("@ProductCode", product);
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();
                        }
                        #endregion
                    }
                    #endregion

                    //****************User Activity****************
                    cmd = new SqlCommand("InsertLoginTransactionDetails", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EmpID", (CpyRevReq.EmpCode ?? "").Trim());
                    cmd.Parameters.AddWithValue("@TransactionType", "S");
                    cmd.Parameters.AddWithValue("@TransactionFrom", "Canopy Assembly Process");   // LEGACY verbatim
                    cmd.Parameters.AddWithValue("@TransactionNo", PrcNo.Trim());
                    cmd.Parameters.AddWithValue("@CompanyCode", companyCode);
                    cmd.Transaction = tran;
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();
                }

                await tran.CommitAsync();
               // await tran.RollbackAsync();
                // ← dry-run toggle: swap with CommitAsync to test without saving

                // LEGACY message verbatim (PrcNo = last generated REVCode)
                PrcNo = "ReverseCode=" + PrcNo + " For Reverse Saved SuccessFully ";
                return PrcNo;
            }
            catch (Exception ex)
            {
                if (tran != null)
                    await tran.RollbackAsync();
                return ("StackTrace " + ex.StackTrace + " Message " + ex.Message);
            }
        }
    }
}
