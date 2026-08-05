using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.Request.ControlPanel;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
    public class ControlPanelService : IControlPanel
    {
        private readonly KalaDbContext _db;
        private readonly string _connStr;
        private readonly CommonCon _com;

        public ControlPanelService(KalaDbContext context, ICommonService common, ILogger<ControlPanelService> logger, IConfiguration config, CommonCon com)
        {
            _db = context;
            _connStr = config.GetConnectionString("KalaDbContext")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _com = com;
        }

        public async Task<List<Dictionary<string, object>>> GetControlPanelAsync(string strJobCardType, string lineWisePC)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetJobCard_Cp_PlanDts_ERPNEW";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@Type", strJobCardType));
                    cmd.Parameters.Add(new SqlParameter("@lineWisePC", lineWisePC));

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


        public async Task<string> SubmitCPAsync(JobCard_CPRequest job_CPReq)
        {
            DataSet dsDetailsSub;
            DataSet dsTurretKitForPrc;
            DataSet dsTurretKitGKForPrc;
            string StrDisplayMsg = "";
            string StrDispCode_CPPlan = "";
            string[] strPlanDts;
            string[] Dts;
            int SrNo;
            string ParentPart = "";

            if (string.IsNullOrEmpty(job_CPReq.JobCard_CPDts))
            {
                return "Please Check Record !";
            }

            // Per-request connection/transaction (thread-safe — no shared fields).
            await using var con = new SqlConnection(_connStr);
            SqlTransaction tran = null;
            var sb = new StringBuilder();
            SqlCommand cmd;

            try
            {
                await con.OpenAsync();
                tran = (SqlTransaction)await con.BeginTransactionAsync();

                // Save Cpy Plan
                StrDispCode_CPPlan = _com.GetMaxNo("CanopyPlan", "CPY", job_CPReq.CompCode.Trim(), con, tran);

                string CurrentMnth = _com.getName("select Mon=case MONTH(GETDATE()) " +
                                                        "when 1 then '01' when 2 then '02' when 3 then '03' " +
                                                        "when 4 then '04' when 5 then '05' when 6 then '06' " +
                                                        "when 7 then '07' when 8 then '08' when 9 then '09' " +
                                                        "when 10 then '10' when 11 then '11' when  12 then '12' end", "tblM", "Mon");

                // ---------- For Mst Save ----------
                cmd = new SqlCommand("InsertCanopyPlan_ERPNEW", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CPCode", StrDispCode_CPPlan.Trim());
                cmd.Parameters.AddWithValue("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                cmd.Parameters.AddWithValue("@MaxSrNo", StrDispCode_CPPlan.Substring(10, 8).Trim());
                cmd.Parameters.AddWithValue("@Yr", StrDispCode_CPPlan.Substring(4, 5).Trim());
                cmd.Parameters.AddWithValue("@FromDt", DateTime.Now.ToString("yyyy-MM-dd 00:00:00"));
                cmd.Parameters.AddWithValue("@ToDt", DateTime.Now.ToString("yyyy-MM-dd 00:00:00"));
                cmd.Parameters.AddWithValue("@PlanPCCode", job_CPReq.PCCode.Trim());
                cmd.Parameters.AddWithValue("@PCCode_Act", job_CPReq.PCCode_Act.Trim());
                cmd.Parameters.AddWithValue("@CompanyCode", job_CPReq.CompCode.Trim());
                cmd.Parameters.AddWithValue("@PlanType", "G");
                cmd.Parameters.AddWithValue("@AutoFlg", "Yes");
                cmd.Transaction = tran;
                await cmd.ExecuteNonQueryAsync();
                await cmd.DisposeAsync();

                // ---------- For CPY Plan Details Save ----------
                //strPlanDts = Regex.Split(job_CPReq.JobCard_CPDts, "@@#@@");
                //SrNo = 0;

                //foreach (string StrSub in strPlanDts)
                //{
                //    SrNo += 1;
                //    Dts = Regex.Split(StrSub.ToString().Trim(), "@#@");

                //    // ----- Dts Save -----
                //    cmd = new SqlCommand("InsertCanopyPlanDetails", con);
                //    cmd.CommandType = CommandType.StoredProcedure;
                //    cmd.Parameters.AddWithValue("@CPCode", StrDispCode_CPPlan.Trim());
                //    cmd.Parameters.AddWithValue("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                //    cmd.Parameters.AddWithValue("@SrNo", SrNo);
                //    cmd.Parameters.AddWithValue("@PartCode", Dts[2].ToString().Trim());
                //    cmd.Parameters.AddWithValue("@BomCode", Dts[10].ToString().Trim());

                //    string PartCodeWOP = _com.getTranName(
                //        "SELECT Partcode FROM BOMdetails where BOMCode='" + Dts[10].ToString().Trim() + "' " +
                //        "AND KitCode='" + Dts[2].ToString().Trim() + "' AND Partcode like '004%' ",
                //        "tblBP", "Partcode", con, tran);

                //    cmd.Parameters.AddWithValue("@PartCodeWOP", PartCodeWOP.Trim());
                //    cmd.Parameters.AddWithValue("@Qty", int.Parse(Dts[8].ToString().Trim()));
                //    cmd.Parameters.AddWithValue("@PlanCode", Dts.Length > 11 ? Dts[11].ToString().Trim() : "");

                //    DateTime planDate = DateTime.MinValue;
                //    if (Dts.Length > 12 && !string.IsNullOrEmpty(Dts[12].ToString().Trim()))
                //    {
                //        if (DateTime.TryParse(Dts[12].ToString().Trim(), out planDate))
                //            cmd.Parameters.AddWithValue("@PlanDate", planDate.ToString("yyyy-MM-dd"));
                //        else
                //            cmd.Parameters.AddWithValue("@PlanDate", DBNull.Value);
                //    }
                //    else
                //    {
                //        cmd.Parameters.AddWithValue("@PlanDate", DBNull.Value);
                //    }

                //    int dayPlanQty = 0;
                //    if (Dts.Length > 13 && int.TryParse(Dts[13].ToString().Trim(), out dayPlanQty))
                //        cmd.Parameters.AddWithValue("@DayPlanQty", dayPlanQty);
                //    else
                //        cmd.Parameters.AddWithValue("@DayPlanQty", 0);

                //    // cmd.Parameters.AddWithValue("@ShiftType", job_Cpyreq.ShiftType.ToString().Trim());
                //    cmd.Transaction = tran;
                //    await cmd.ExecuteNonQueryAsync();
                //    await cmd.DisposeAsync();
                //}
               

                strPlanDts = Regex.Split(job_CPReq.JobCard_CPDts, "@@#@@");
                SrNo = 0;


                foreach (string StrSub in strPlanDts) //For Loop Start
                { // Main For Loop Start Bracket 

                    SrNo += 1;
                    Dts = Regex.Split(StrSub.ToString().Trim(), "@#@");

                    // Updated field mapping with new columns:
                    //     0           1           2           3           4           5           6               7           8           9           10          11          12          13
                    // item.KVA + "@#@" + item.Model + "@#@" + item.Partcode + "@#@" + item.FNorm + "@#@" + item.TotStk + "@#@" + item.WIPStk + "@#@" + item.PenPlanQty + "@#@" + item.PReq + "@#@" + item.PlanQty + "@#@" + item.BatchQty + "@#@" + item.Bomcode + "@#@" + item.PlanCode + "@#@" + item.PlanDate + "@#@" + item.DayPlanQty

                    //For Dts Save
                    #region
                    cmd = new SqlCommand("InsertCanopyPlanDetails", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CPCode", StrDispCode_CPPlan.Trim());
                    cmd.Parameters.AddWithValue("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                    cmd.Parameters.AddWithValue("@SrNo", SrNo);

                    cmd.Parameters.AddWithValue("@PartCode", Dts[3].ToString().Trim());
                    cmd.Parameters.AddWithValue("@BomCode", Dts[11].ToString().Trim());

                    string PartCodeWOP = "";
                    //PartCodeWOP = ComCon.getTranName("SELECT Partcode FROM BOMdetails where BOMCode='" + Dts[10].ToString().Trim() + "' " +
                    //"AND KitCode='" + Dts[2].ToString().Trim() + "' AND Partcode like '004%' ", "tblBP", "Partcode", con, tran);

                    PartCodeWOP = _com.getTranName("SELECT Partcode FROM BOMdetails where BOMCode='" + Dts[11].ToString().Trim() + "' " +
                                "AND KitCode='" + Dts[3].ToString().Trim() + "' AND Substring(Partcode,12,1)IN ('6') AND Partcode like '004%' ", "tblBP", "Partcode", con, tran);


                    cmd.Parameters.AddWithValue("@PartCodeWOP", PartCodeWOP.Trim());
                    cmd.Parameters.AddWithValue("@Qty", int.Parse(Dts[9].ToString().Trim()));

                    // New Monthly Plan Parameters
                    cmd.Parameters.AddWithValue("@PlanCode", Dts.Length > 11 ? Dts[12].ToString().Trim() : "");

                    // Handle PlanDate - convert string to DateTime
                    DateTime planDate = DateTime.MinValue;
                    if (Dts.Length > 12 && !string.IsNullOrEmpty(Dts[13].ToString().Trim()))
                    {
                        if (DateTime.TryParse(Dts[13].ToString().Trim(), out planDate))
                        {
                            cmd.Parameters.AddWithValue("@PlanDate", planDate.ToString("yyyy-MM-dd"));
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@PlanDate", DBNull.Value);
                        }
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PlanDate", DBNull.Value);
                    }

                    // Handle DayPlanQty
                    int dayPlanQty = 0;
                    if (Dts.Length > 13 && int.TryParse(Dts[14].ToString().Trim(), out dayPlanQty))
                    {
                        cmd.Parameters.AddWithValue("@DayPlanQty", dayPlanQty);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@DayPlanQty", 0);
                    }

                    cmd.Transaction = tran;
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();
                    #endregion


                }
            


                // ----- User Activity -----
                cmd = new SqlCommand("InsertLoginTransactionDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                cmd.Parameters.AddWithValue("@EmpID", job_CPReq.EmpCode.Trim());
                cmd.Parameters.AddWithValue("@TransactionType", "S");
                cmd.Parameters.AddWithValue("@TransactionFrom", "Control Panel Maker (Primary Plan)");
                cmd.Parameters.AddWithValue("@TransactionNo", StrDispCode_CPPlan.Trim());
                cmd.Parameters.AddWithValue("@CompanyCode", job_CPReq.CompCode.Trim());
                cmd.Transaction = tran;
                await cmd.ExecuteNonQueryAsync();
                await cmd.DisposeAsync();

                await tran.CommitAsync();
                // await tran.RollbackAsync();

                if (!string.IsNullOrEmpty(StrDispCode_CPPlan.Trim()))
                    StrDisplayMsg = "Saved Successfully With Control Panel Plan: " + StrDispCode_CPPlan.Trim() + "";

                return StrDisplayMsg;
            }
            catch (Exception ex)
            {
                if (tran != null)
                    await tran.RollbackAsync();
                return ("StackTrace " + ex.StackTrace + ", Message " + ex.Message);
            }
            // No finally needed: 'await using var con' closes/disposes automatically.
        }


        public async Task<List<Dictionary<string, object>>> GetCheckerCPLoad()
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetCheckerCPLoad_ERPNEW";
                    cmd.CommandType = CommandType.StoredProcedure;
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



        public async Task<List<Dictionary<string, object>>> GetJobCardCpyCheckerAsync(string strJobCardType, string strcompID, string planCode)
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetJobCard_CPChecker_PlanDts_NewERP";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@Type", strJobCardType));
                    cmd.Parameters.Add(new SqlParameter("@CompId", strcompID));
                    cmd.Parameters.Add(new SqlParameter("@PlanCode", planCode));
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


        public async Task<string> CPCheckerSubmitAsync(CP_JobCardCheckerRequest job_CPCheckerreq)
        {
            DataSet dsCanopyPlanDtsSub;
            string StrDisplayMsg = "";
            string StrDispCode_MaterialReq_WH_ALL_msg = "";
            string[] strPlanDts;
            string[] DtsPlan;
            int SrNo;

            if (string.IsNullOrEmpty(job_CPCheckerreq.ProductionDetails))
            {
                return "Please Check Record !";
            }

            await using var con = new SqlConnection(_connStr);
            SqlTransaction tran = null;
            var sb = new StringBuilder();
            SqlCommand cmd;

            try
            {
                await con.OpenAsync();
                tran = (SqlTransaction)await con.BeginTransactionAsync();

                strPlanDts = Regex.Split(job_CPCheckerreq.ProductionDetails, "@@#@@");
                SrNo = 0;
                string CpyPlan = _com.getName("SELECT CPCode FROM CanopyPlan WHERE CPCode='" + job_CPCheckerreq.PlanCode.ToString().Trim() + "'", "tblCpyPlan", "CPCode");

                if (job_CPCheckerreq.Status.ToString().Trim() == "AUTH")
                {
                    if (CpyPlan != null)
                    {
                        // ---- mark checker-1 done ----
                        sb.Remove(0, sb.Length);
                        sb.Append("UPDATE CanopyPlan SET ");
                        sb.Append("Dt = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', ");
                        sb.Append("Checker1 = 1 ");
                        sb.Append("WHERE CPCode = '" + job_CPCheckerreq.PlanCode.ToString().Trim() + "'");
                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        // ---- 6M checker rows (AssignTo == 0) ----
                        foreach (string StrSub in strPlanDts)
                        {
                            SrNo += 1;
                            DtsPlan = Regex.Split(StrSub.ToString().Trim(), "@#@");

                            if (DtsPlan[3] != null && DtsPlan[3].ToString().Trim() == "0")
                            {
                                cmd = new SqlCommand("InsertSheetMetal6MChecker_Detail", con)
                                {
                                    CommandType = CommandType.StoredProcedure,
                                    Transaction = tran
                                };
                                cmd.Parameters.AddWithValue("@PlanCode", job_CPCheckerreq.PlanCode.Trim());
                                cmd.Parameters.AddWithValue("@SixMName", DtsPlan[1].Trim());
                                cmd.Parameters.AddWithValue("@Description", DtsPlan[2].Trim());
                                cmd.Parameters.AddWithValue("@AssignTo", DtsPlan[3].Trim());
                                cmd.Parameters.AddWithValue("@CorReqNo", '0');
                                cmd.Parameters.AddWithValue("@Status", job_CPCheckerreq.Status.Trim());
                                await cmd.ExecuteNonQueryAsync();
                                await cmd.DisposeAsync();
                            }
                        }

                        // ================= Auto WH Req =================
                        string StrWH_PCCode = "0";
                        string StrWH_OLDPCCode = "0";
                        string RequisitionForPartCode = "";
                        string CatID = "";
                        string CompCode = "";

                        dsCanopyPlanDtsSub = _com.procTranDS(
                            "select PlanPCCode, PCCode_Act, CompanyCode from CanopyPlan C where C.CPCode='" + job_CPCheckerreq.PlanCode.ToString().Trim() + "'",
                            "tbl_CanopyPlan", con, tran);

                        if (dsCanopyPlanDtsSub.Tables["tbl_CanopyPlan"].Rows.Count > 0)
                        {
                            for (int m1 = 0; m1 < dsCanopyPlanDtsSub.Tables["tbl_CanopyPlan"].Rows.Count; m1++)
                            {
                                var r = dsCanopyPlanDtsSub.Tables["tbl_CanopyPlan"].Rows[m1];
                                string comp = r["CompanyCode"].ToString().Trim();
                                string PCplan_OLD = r["PlanPCCode"].ToString().Trim();
                                string PCplan_Act = r["PCCode_Act"].ToString().Trim();

                             

                                string StrDispCode_WHReq = _com.GetMaxNo("MaterialRequisitionWithOutPlan", "REQ", comp, con, tran);

                                StrDispCode_MaterialReq_WH_ALL_msg = string.IsNullOrEmpty(StrDispCode_MaterialReq_WH_ALL_msg.Trim())
                                    ? StrDispCode_WHReq.Trim()
                                    : StrDispCode_MaterialReq_WH_ALL_msg + ", " + StrDispCode_WHReq.Trim();

                                sb.Remove(0, sb.Length);
                                sb.Append("insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt, Yr,ProfitCenterCode,ToProfitCenterCode, ProfitCenterCode_Act,ToProfitCenterCode_Act,ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) ");
                                sb.Append("values('" + StrDispCode_WHReq.Trim() + "','" + StrDispCode_WHReq.Substring(10, 8) + "','" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                                sb.Append("'" + StrDispCode_WHReq.Substring(4, 5).Trim() + "','" + PCplan_OLD.Trim() + "','01.091','" + PCplan_Act.Trim() + "','01.168','" + job_CPCheckerreq.Partcode.ToString().Trim() + "','" + comp + "','" + job_CPCheckerreq.BatchQty.ToString().Trim() + "','P','WIP',");
                                sb.Append("'Auto Req For : " + job_CPCheckerreq.Kva.ToString().Trim() + " Kva " + job_CPCheckerreq.Model.ToString().Trim() + " ','1','1','1','" + job_CPCheckerreq.PlanCode.Trim() + "','" + RequisitionForPartCode.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync();
                                await cmd.DisposeAsync();

                                // ----- WH Req details -----
                                DataSet dsReqDts_WHReq = _com.procTranDS(
                                    "exec InternalReqLogisticsdetailsWHKIT_CP_NewERP '" + job_CPCheckerreq.Partcode.ToString().Trim() + "'",  // was Dts[3] (undefined)
                                    "tbl_ReqDts_WHReq", con, tran);

                                if (dsReqDts_WHReq != null && dsReqDts_WHReq.Tables["tbl_ReqDts_WHReq"].Rows.Count > 0)
                                {
                                    int SrNoReq_WPReq = 0;
                                    for (int cntd = 0; cntd < dsReqDts_WHReq.Tables["tbl_ReqDts_WHReq"].Rows.Count; cntd++)
                                    {
                                        SrNoReq_WPReq += 1;
                                        string part = dsReqDts_WHReq.Tables["tbl_ReqDts_WHReq"].Rows[cntd]["Partcode"].ToString().Trim();

                                        cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails_ERPNEW", con)
                                        {
                                            CommandType = CommandType.StoredProcedure,
                                            Transaction = tran
                                        };
                                        cmd.Parameters.AddWithValue("@REQCode", StrDispCode_WHReq.Trim());
                                        cmd.Parameters.AddWithValue("@SrNo", SrNoReq_WPReq);
                                        cmd.Parameters.AddWithValue("@PartCode", part);
                                        cmd.Parameters.AddWithValue("@Qty", double.Parse(dsReqDts_WHReq.Tables["tbl_ReqDts_WHReq"].Rows[cntd]["TotQty"].ToString().Trim()) * double.Parse(job_CPCheckerreq.BatchQty.ToString().Trim()));
                                        cmd.Parameters.AddWithValue("@REQStatus", "P");
                                        await cmd.ExecuteNonQueryAsync();
                                        await cmd.DisposeAsync();

                                        await GetReqDetailsSubAsync(con, tran, StrDispCode_WHReq.Trim(), part, 0, double.Parse(job_CPCheckerreq.BatchQty.ToString().Trim()));
                                    }
                                }
                            }
                            //Logistics

                            for (int m1 = 0; m1 < dsCanopyPlanDtsSub.Tables["tbl_CanopyPlan"].Rows.Count; m1++)
                            {
                                var r = dsCanopyPlanDtsSub.Tables["tbl_CanopyPlan"].Rows[m1];
                                string comp = r["CompanyCode"].ToString().Trim();
                                string PCplan_OLD = r["PlanPCCode"].ToString().Trim();
                                string PCplan_Act = r["PCCode_Act"].ToString().Trim();



                                string StrDispCode_Log = _com.GetMaxNo("MaterialRequisitionWithOutPlan", "REQ", comp, con, tran);

                                StrDispCode_MaterialReq_WH_ALL_msg = string.IsNullOrEmpty(StrDispCode_MaterialReq_WH_ALL_msg.Trim())
                                    ? StrDispCode_Log.Trim()
                                    : StrDispCode_MaterialReq_WH_ALL_msg + ", " + StrDispCode_Log.Trim();

                                sb.Remove(0, sb.Length);
                                sb.Append("insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt, Yr,ProfitCenterCode,ToProfitCenterCode, ProfitCenterCode_Act,ToProfitCenterCode_Act,ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) ");
                                sb.Append("values('" + StrDispCode_Log.Trim() + "','" + StrDispCode_Log.Substring(10, 8) + "','" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                                sb.Append("'" + StrDispCode_Log.Substring(4, 5).Trim() + "','" + PCplan_OLD.Trim() + "','23.001','" + PCplan_Act.Trim() + "','23.001','" + job_CPCheckerreq.Partcode.ToString().Trim() + "','" + comp + "','" + job_CPCheckerreq.BatchQty.ToString().Trim() + "','P','WIP',");
                                sb.Append("'Auto Req For : " + job_CPCheckerreq.Kva.ToString().Trim() + " Kva " + job_CPCheckerreq.Model.ToString().Trim() + " ','1','1','1','" + job_CPCheckerreq.PlanCode.Trim() + "','" + RequisitionForPartCode.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync();
                                await cmd.DisposeAsync();

                                // ----- CNC Req details -----
                                DataSet dsReqDts_LOGReq = _com.procTranDS(
                                    "exec InternalReqLogisticsdetailsCPKit_NewERP '" + job_CPCheckerreq.Partcode.ToString().Trim() + "'",  // was Dts[3] (undefined)
                                    "tbl_ReqDts_LogReq", con, tran);

                                if (dsReqDts_LOGReq != null && dsReqDts_LOGReq.Tables["tbl_ReqDts_LogReq"].Rows.Count > 0)
                                {
                                    int SrNoReq_LOGReq = 0;
                                    for (int cntd = 0; cntd < dsReqDts_LOGReq.Tables["tbl_ReqDts_LogReq"].Rows.Count; cntd++)
                                    {
                                        SrNoReq_LOGReq += 1;
                                        string part = dsReqDts_LOGReq.Tables["tbl_ReqDts_LogReq"].Rows[cntd]["Partcode"].ToString().Trim();

                                        cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails_ERPNEW", con)
                                        {
                                            CommandType = CommandType.StoredProcedure,
                                            Transaction = tran
                                        };
                                        cmd.Parameters.AddWithValue("@REQCode", StrDispCode_Log.Trim());
                                        cmd.Parameters.AddWithValue("@SrNo", SrNoReq_LOGReq);
                                        cmd.Parameters.AddWithValue("@PartCode", part);
                                        cmd.Parameters.AddWithValue("@Qty", double.Parse(dsReqDts_LOGReq.Tables["tbl_ReqDts_LogReq"].Rows[cntd]["RaiseReqQty"].ToString().Trim()) * double.Parse(job_CPCheckerreq.BatchQty.ToString().Trim()));
                                        cmd.Parameters.AddWithValue("@REQStatus", "P");
                                        await cmd.ExecuteNonQueryAsync();
                                        await cmd.DisposeAsync();

                                        await GetReqDetailsSubAsync(con, tran, StrDispCode_Log.Trim(), part, 0, double.Parse(job_CPCheckerreq.BatchQty.ToString().Trim()));
                                    }
                                }
                            }
                        }

                    }
                }
                else
                {
                    // ================= Corporate Requisition (non-AUTH) =================
                    string StrDispCode = "";
                    foreach (string StrSub in strPlanDts)
                    {
                        SrNo += 1;
                        DtsPlan = Regex.Split(StrSub.ToString().Trim(), "@#@");
                        StrDispCode = _com.GetMaxNo("CorporateRequisition", "COR", job_CPCheckerreq.CompCode.Trim(), con, tran);

                        cmd = new SqlCommand("InsertSheetMetal6MChecker_Detail", con)
                        {
                            CommandType = CommandType.StoredProcedure,
                            Transaction = tran
                        };
                        cmd.Parameters.AddWithValue("@PlanCode", job_CPCheckerreq.PlanCode.Trim());
                        cmd.Parameters.AddWithValue("@SixMName", DtsPlan[1].Trim());
                        cmd.Parameters.AddWithValue("@Description", DtsPlan[2].Trim());
                        cmd.Parameters.AddWithValue("@AssignTo", DtsPlan[3].Trim());
                        if (DtsPlan[3] != null && DtsPlan[3].ToString().Trim() != "0")
                            cmd.Parameters.AddWithValue("@CorReqNo", StrDispCode.Trim());
                        else
                            cmd.Parameters.AddWithValue("@CorReqNo", '0');
                        cmd.Parameters.AddWithValue("@Status", job_CPCheckerreq.Status.Trim());
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        if (DtsPlan[3] != null && DtsPlan[3].ToString().Trim() != "0")
                        {
                            string ReqMsg = string.Format(
                                " Sheet Metal Checker  JobCard  PlanCode: {0}, KVA: {1}, Model: {2}, 6MType: {3}, Description: {4}",
                                job_CPCheckerreq.PlanCode.Trim(), job_CPCheckerreq.Kva, job_CPCheckerreq.Model.Trim(), DtsPlan[1].Trim(), DtsPlan[2].Trim());

                            sb.Remove(0, sb.Length);
                            sb.Append("INSERT INTO CorporateRequisition ");
                            sb.Append("(ReqCode,Dt,Yr,MaxSrNo,EmpCode,FromPCCode,ToEmpCode,ToPCCode,Priority,ReqMsg,CompanyCode,AssignStatus,Active,Discard)");
                            sb.Append(" VALUES('" + StrDispCode.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                            sb.Append("'" + (StrDispCode.Substring(4, 5)) + "','" + (StrDispCode.Substring(10, 8)) + "',");
                            sb.Append("'" + job_CPCheckerreq.EmpCode.Trim() + "' ,'" + job_CPCheckerreq.PCCode.Trim() + "','" + DtsPlan[3].Trim() + "',");
                            sb.Append("'" + DtsPlan[4].Trim() + "' ,'High Priority','" + ReqMsg.Trim() + "',");
                            sb.Append("'" + job_CPCheckerreq.CompCode + "','P','1','1')");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();

                            sb.Remove(0, sb.Length);
                            sb.Append("INSERT INTO CorporateRequisitionActionTaken");
                            sb.Append("(Dt,ReqCode,AssignByCode,AssignToCode,ActionTaken,Priority,ActionStatus,AssOrAction,Active,Discard)");
                            sb.Append(" VALUES('" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                            sb.Append("'" + StrDispCode.Trim() + "',");
                            sb.Append("'" + job_CPCheckerreq.EmpCode.Trim() + "',");
                            sb.Append("'" + DtsPlan[3].Trim() + "',");
                            sb.Append(" '','High Priority','P','ASS','1','1');SELECT @@Identity;");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            object lblDispID = await cmd.ExecuteScalarAsync();
                            await cmd.DisposeAsync();

                            sb.Remove(0, sb.Length);
                            sb.Append("Update CorporateRequisition set AssignStatus='C'");
                            sb.Append(" where ReqCode='" + StrDispCode.Trim() + "'");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();
                        }
                    }
                }

                // ================= User Activity =================
                cmd = new SqlCommand("insertLoginTransactionDetails", con)
                {
                    CommandType = CommandType.StoredProcedure,
                    Transaction = tran
                };
                cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                cmd.Parameters.AddWithValue("@EmpID", job_CPCheckerreq.EmpCode.Trim());
                cmd.Parameters.AddWithValue("@TransactionType", "S");
                cmd.Parameters.AddWithValue("@TransactionFrom", "Sheet Metal Checker (Primary Plan)");
                cmd.Parameters.AddWithValue("@TransactionNo", job_CPCheckerreq.PlanCode.Trim());
                cmd.Parameters.AddWithValue("@CompanyCode", job_CPCheckerreq.CompCode.Trim());
                await cmd.ExecuteNonQueryAsync();
                await cmd.DisposeAsync();

                  await tran.CommitAsync();
               // await tran.RollbackAsync();

                if (!string.IsNullOrEmpty(StrDispCode_MaterialReq_WH_ALL_msg.Trim()))
                    StrDisplayMsg += " & WH&Log Req: " + StrDispCode_MaterialReq_WH_ALL_msg.Trim();
               

                return StrDisplayMsg;
            }
            catch (Exception ex)
            {
                if (tran != null)
                    await tran.RollbackAsync();
                return ("StackTrace " + ex.StackTrace + ", Message " + ex.Message);
            }
            // 'await using var con' closes/disposes automatically.
        }

        private async Task GetReqDetailsSubAsync(SqlConnection con, SqlTransaction tran, string reqCode, string partCode, int flag, double qty)
        {
            await Task.CompletedTask; // <-- replace with your converted logic
        }



    }

}
    