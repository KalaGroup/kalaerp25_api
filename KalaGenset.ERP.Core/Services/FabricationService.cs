using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Services
{
    public class FabricationService:IFabrication
    {
        private readonly KalaDbContext _db;

        private readonly string _connStr;
        private readonly CommonCon ComCon;

        public FabricationService(
            KalaDbContext context,
            ICommonService common,
            ILogger<FabricationService> logger,
            IConfiguration config,
            CommonCon com)
        {
            _db = context;
            ComCon = com;
            _connStr = config.GetConnectionString("KalaDbContext")
                       ?? throw new InvalidOperationException("Connection string 'KalaDbContext' not found.");
        }

        public async Task<List<Dictionary<string, object>>> GetCpyPrcddlFabAsync(string pcCode, string machineCode, string kva, string model, string suppCode)
        {
            var parts = Regex.Split(machineCode?.Trim() ?? "", "-->");
            var machine = parts.Length > 0 ? parts[0].Trim() : "";
            var serialNo = parts.Length > 1 ? parts[1].Trim() : "";

            const string feedbackDate = "2020-07-10 00:00:00";

            // ---- Branch 1: KVA list ----
            if (kva == "0" && model == "0")
            {
                var sql = @"select P.KVA, P.KVA as KVA1
                     from processfeedback pf
                     inner join Part P on Pf.ProductCode = P.partcode
                     where PCCode_Act = @PCCode
                       and MachineCode = @Machine and SerialNo = @SerialNo
                       and Edt is null and Pf.Active = '1'
                       and SupplierCode = @SuppCode
                       and ProductCode like '401%' and Pf.Dt >= @FbDate
                     group by P.KVA";

                var rows = await QueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                    cmd.Parameters.Add(new SqlParameter("@SuppCode", suppCode));
                    cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                });

                return rows.Count > 0 ? rows : await GetddlCpyPrcFabAsync(pcCode, kva, model, suppCode);
            }

            // ---- Branch 2: Model list ----
            if (kva != "0" && model == "0")
            {
                var sql = @"select P.Model, P.Model as Model1
                     from processfeedback pf
                     inner join Part P on Pf.ProductCode = P.partcode
                     where pf.PCCode_Act = @PCCode
                       and MachineCode = @Machine and SerialNo = @SerialNo
                       and Edt is null and P.KVA = @KVA
                       and SupplierCode = @SuppCode
                       and Pf.Active = '1' and Pf.Dt >= @FbDate
                     group by P.Model";

                var rows = await QueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                    cmd.Parameters.Add(new SqlParameter("@KVA", kva));
                    cmd.Parameters.Add(new SqlParameter("@SuppCode", suppCode));
                    cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                });

                return rows.Count > 0 ? rows : await GetddlCpyPrcFabAsync(pcCode, kva, model, suppCode);
            }

            // ---- Branch 3: KVA+Model selected, fetch detail row (Top 1) ----
            if (kva != "0" && model != "0")
            {
                var sql = @"select Top 1 Convert(varchar(10),P.KVA)+'-->'+P.Model as KVAMod, KVA, Model,
                       CanopyPlanCode as CPCode, PF.Dt, PF.ProductCode as Partcode,
                       Partdesc+'-->'+PF.Partcode as Part, ProcessQty as CPQty,
                       isnull(PFBCode,0) as PFBCode, EDt, TurretKitCode as BOMCode, SupplierCode as SCode
                from processfeedback pf
                inner join Part P on Pf.ProductCode = P.partcode
                where pf.PCCode_Act = @PCCode and MachineCode = @Machine and SerialNo = @SerialNo
                  and Edt is null and P.KVA = @KVA and P.Model = @Model
                  and SupplierCode = @SuppCode
                  and Pf.Active = '1' and Pf.Dt >= @FbDate
                order by Dt desc";

                var rows = await QueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                    cmd.Parameters.Add(new SqlParameter("@KVA", kva));
                    cmd.Parameters.Add(new SqlParameter("@Model", model));
                    cmd.Parameters.Add(new SqlParameter("@SuppCode", suppCode));
                    cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                });

                return rows.Count > 0 ? rows : await GetddlCpyPrcFabAsync(pcCode, kva, model, suppCode);
            }

            // No branch matched -> empty result
            return new List<Dictionary<string, object>>();
        }

        // Runs an inline parameterized query (or stored proc) and returns rows as dictionaries.
        private async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Action<DbCommand> addParameters, CommandType commandType = CommandType.Text)
        {
            var data = new List<Dictionary<string, object>>();
            var conn = _db.Database.GetDbConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandType = commandType;
                cmd.CommandTimeout = 0;
                addParameters(cmd);

                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    data.Add(row);
                }
            }

            return data;
        }

        // Shared stored-proc fallback (GetddlCpyPrcFab_NewERP).
        private Task<List<Dictionary<string, object>>> GetddlCpyPrcFabAsync(string pcCode, string kva, string model, string suppCode)
        {
            return QueryAsync("GetddlCpyPrcFab_NewERP", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@PCCode", SqlDbType.Char) { Value = pcCode });
                cmd.Parameters.Add(new SqlParameter("@KVA", SqlDbType.Char) { Value = kva });
                cmd.Parameters.Add(new SqlParameter("@Model", SqlDbType.Char) { Value = model });
                cmd.Parameters.Add(new SqlParameter("@SuppCode", SqlDbType.Char) { Value = suppCode });
            }, CommandType.StoredProcedure);
        }

        public async Task<List<Dictionary<string, object>>> GetCpyKitFabAsync(string pcCode, string machineCode, string planCode,string partCode, string cpyKit, string suppCode)
        {
            var parts = Regex.Split(machineCode?.Trim() ?? "", "-->");
            var machine = parts.Length > 0 ? parts[0].Trim() : "";
            var serialNo = parts.Length > 1 ? parts[1].Trim() : "";

            const string feedbackDate = "2020-07-10 00:00:00";

            // ---- Branch 1: Kit list ----
            if (cpyKit == "0")
            {
                var sql = @"select AliseName as KitDesc,
                           Pf.Partcode + '-->' + PartDesc as KitCode,
                           PfbCode, EDt
            from processfeedback pf
            inner join Part P on Pf.partcode = P.partcode
            where pf.PCCode_Act = @PCCode
              and MachineCode = @Machine and SerialNo = @SerialNo
              and Edt is null and CanopyPlanCode = @PlanCode
              and Productcode = @Partcode and SupplierCode = @SuppCode
              and Pf.Active = '1' and Pf.Dt >= @FbDate
            order by Pf.Dt desc";

                var rows = await QueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                    cmd.Parameters.Add(new SqlParameter("@PlanCode", planCode));
                    cmd.Parameters.Add(new SqlParameter("@Partcode", partCode));
                    cmd.Parameters.Add(new SqlParameter("@SuppCode", suppCode));
                    cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                });

                return rows.Count > 0
                    ? rows
                    : await GetCpyKitFabSpAsync(pcCode, planCode, partCode, cpyKit, suppCode);
            }

            // ---- Branch 2: Balance for selected kit ----
            if (cpyKit != "0")
            {
                var sql = @"select isnull(ProcessQty, 0) as Bal
            from processfeedback pf
            inner join Part P on Pf.partcode = P.partcode
            where pf.PCCode_Act = @PCCode
              and MachineCode = @Machine and SerialNo = @SerialNo
              and Edt is null and CanopyPlanCode = @PlanCode
              and Productcode = @Partcode and SupplierCode = @SuppCode
              and Pf.Partcode = @CpyKit
              and Pf.Active = '1' and Pf.Dt >= @FbDate
            order by Pf.Dt desc";

                var rows = await QueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                    cmd.Parameters.Add(new SqlParameter("@PlanCode", planCode));
                    cmd.Parameters.Add(new SqlParameter("@Partcode", partCode));
                    cmd.Parameters.Add(new SqlParameter("@SuppCode", suppCode));
                    cmd.Parameters.Add(new SqlParameter("@CpyKit", cpyKit));
                    cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                });

                return rows.Count > 0
                    ? rows
                    : await GetCpyKitFabSpAsync(pcCode, planCode, partCode, cpyKit, suppCode);
            }

            // No branch matched -> empty result
            return new List<Dictionary<string, object>>();
        }

        // Shared stored-proc fallback (GetCpyKitFab_NewERP).
        private Task<List<Dictionary<string, object>>> GetCpyKitFabSpAsync(string pcCode, string planCode, string partCode, string cpyKit, string suppCode)
        {
            return QueryAsync("GetCpyKitFab_NewERP", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@PCCode", SqlDbType.Char) { Value = pcCode });
                cmd.Parameters.Add(new SqlParameter("@PlanCode", SqlDbType.Char) { Value = planCode });
                cmd.Parameters.Add(new SqlParameter("@Partcode", SqlDbType.Char) { Value = partCode });
                cmd.Parameters.Add(new SqlParameter("@CpyKit", SqlDbType.Char) { Value = cpyKit });
                cmd.Parameters.Add(new SqlParameter("@SuppCode", SqlDbType.Char) { Value = suppCode });
            }, CommandType.StoredProcedure);
        }

        public Task<List<Dictionary<string, object>>> CpyKitDtsAsync( string pcCode, int batchQty, string cpyKitCode, string bomCode, string pfbCode)
        {
            return QueryAsync("CpyKitDts_NewERP", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@PCCode", SqlDbType.Char) { Value = pcCode });
                cmd.Parameters.Add(new SqlParameter("@BatchQty", SqlDbType.Char) { Value = batchQty });
                cmd.Parameters.Add(new SqlParameter("@CpyKitcode", SqlDbType.Char) { Value = cpyKitCode });
                cmd.Parameters.Add(new SqlParameter("@BOMCode", SqlDbType.Char) { Value = bomCode });
                cmd.Parameters.Add(new SqlParameter("@PFBCode", SqlDbType.Char) { Value = pfbCode });
            }, CommandType.StoredProcedure);
        }


        public async Task<string> SubmitFabricationAsync(CpyPrcFabRequest CpyPrcFabReq, CancellationToken cancellationToken = default)
        {
            string PrcNo = "";
            string FabkitFlag = "No";

            StringBuilder sb = new StringBuilder();

            // Local connection from EF -> never null, never a stale field.
            SqlConnection con = (SqlConnection)_db.Database.GetDbConnection();
            bool openedHere = false;
            SqlTransaction tran = null;

            try
            {
                if (con.State != ConnectionState.Open)
                {
                    await con.OpenAsync();
                    openedHere = true;
                }

                // ---- PowderCoating PC mapping (Unit 01 Fab + Unit 04 Fab, all -> 01.116) ----
                string StrPowderCoating_PCCode = "0";
                if (CpyPrcFabReq.PCCode_Act.Trim() == "01.101") StrPowderCoating_PCCode = "01.116";       // A
                else if (CpyPrcFabReq.PCCode_Act.Trim() == "01.102") StrPowderCoating_PCCode = "01.116";  // B
                else if (CpyPrcFabReq.PCCode_Act.Trim() == "01.103") StrPowderCoating_PCCode = "01.116";  // C
                else if (CpyPrcFabReq.PCCode_Act.Trim() == "03.073") StrPowderCoating_PCCode = "01.116";  // A
                else if (CpyPrcFabReq.PCCode_Act.Trim() == "03.074") StrPowderCoating_PCCode = "01.116";  // B
                else if (CpyPrcFabReq.PCCode_Act.Trim() == "03.075") StrPowderCoating_PCCode = "01.116";  // C

                string StrPowderCoating_PCCodeold = "0";
                if (CpyPrcFabReq.PCCode.Trim() == "01.008") StrPowderCoating_PCCodeold = "01.007";       // unit1
                else if (CpyPrcFabReq.PCCode.Trim() == "03.002") StrPowderCoating_PCCodeold = "01.007"; //unit4


                // ---- Existence check (was FebSheetQty) ----
                string FebSheetQty = ComCon.getTranName("SELECT TOP 1 '1' AS PFBCode  FROM processfeedback  WITH (NOLOCK) WHERE canopyplancode = '" + CpyPrcFabReq.PlanCode + "' AND partcode = '" + CpyPrcFabReq.CpyKitcode + "'   AND PCCode_Act = '" + CpyPrcFabReq.PCCode_Act.Trim() + "'   AND SupplierCode = '" + CpyPrcFabReq.OSSupplierCode.Trim() + "'   and Active ='1' ", "tbl_PFFCode", "PFBCode", con, tran);
                if (FebSheetQty != "0" && CpyPrcFabReq.PFBCode.Substring(0, 3) == "NEW")
                {
                    PrcNo = "Process is already saved.";

                    tran = (SqlTransaction)await con.BeginTransactionAsync();

                    SqlCommand cmd;
                    sb.Remove(0, sb.Length);
                    sb.Append("UPDATE CanopyPlanDtsSub SET CPFStatus='D',CPFQty='" + CpyPrcFabReq.PrcQty + "' " +
                              "WHERE CpCode = '" + CpyPrcFabReq.PlanCode + "' " +
                              "AND partcode = '" + CpyPrcFabReq.CpyKitcode.Trim() + "' " +
                              "AND CatID = '" + CpyPrcFabReq.CatID.Trim() + "'");
                    cmd = new SqlCommand(sb.ToString(), con);
                    cmd.Transaction = tran;
                    await cmd.ExecuteNonQueryAsync();

                    await tran.CommitAsync();
                    return PrcNo;
                }
                else
                {

                    //Added Fab Lock ProductWip Stock 03/04/2026 (async - .NET 8)
                    string fabReqFlag = "";

                    if (CpyPrcFabReq.PFBCode.Substring(0, 3) == "NEW" && CpyPrcFabReq.CatID.ToString() == "029")
                    {
                        await using var cmd = new SqlCommand("Get_ProductWip_Ben_CanopyAssly_NewERP", con, tran)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@PCCode", CpyPrcFabReq.PCCode_Act.Trim());
                        cmd.Parameters.AddWithValue("@ProductCode", CpyPrcFabReq.ProductCode.Trim());

                        // C#
                        await using var reader = await cmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            int idx = reader.GetOrdinal("ClsQty");
                            double clsQty = reader.IsDBNull(idx) ? 0.0 : Convert.ToDouble(reader.GetValue(idx));
                            if (CpyPrcFabReq.PrcQty > clsQty)
                            {
                                string partDesc = reader["PartDesc"]?.ToString()?.Trim() ?? "";
                                fabReqFlag = string.IsNullOrEmpty(fabReqFlag) ? partDesc : $"{fabReqFlag}, {partDesc}";
                                if (!string.IsNullOrEmpty(fabReqFlag))
                                    return $"Insufficient Stock For Part(BR): {fabReqFlag}";
                            }
                        }
                    }
                    //END



                    if (CpyPrcFabReq.PFBCode.Substring(0, 3) == "NEW")
                    {
                        string[] strMachineNo = Regex.Split(CpyPrcFabReq.MachineCodeSrNo, "-->");

                        if (CpyPrcFabReq.OSSupplierCode == "0")
                        {
                            PrcNo = "Pl Select Supplier !!! ";
                            return PrcNo;
                        }

                        // tran still null -> these reads run with no active transaction, same as bending.
                        if (await CkhDoubleEntryAsync(con, tran,
                                CpyPrcFabReq.PCCode_Act.Trim(), CpyPrcFabReq.PlanCode.Trim(),
                                CpyPrcFabReq.ProductCode.Trim(), CpyPrcFabReq.OSSupplierCode.Trim(),
                                CpyPrcFabReq.CpyKitcode.Trim()) == false)
                        {
                            bool ChkforStartCPY = await GetChkforStartCpyAsync(con, tran, CpyPrcFabReq.PCCode_Act.Trim(), CpyPrcFabReq.PlanCode, CpyPrcFabReq.ProductCode, CpyPrcFabReq.CatID);
                            bool ChkforStart = await GetChkforStartAsync(con, tran, CpyPrcFabReq.PCCode_Act.Trim(), CpyPrcFabReq.PlanCode, CpyPrcFabReq.ProductCode, strMachineNo[1].ToString(), CpyPrcFabReq.CatID);

                            tran = (SqlTransaction)await con.BeginTransactionAsync();

                            SqlCommand cmd;

                            // ---------------- Mst Entry ----------------
                            PrcNo = await GetMaxPrcAsync(con, tran, "ProcessFeedback", "PFbCode", ComCon.yearEnd(con, tran), CpyPrcFabReq.PCCode_Act.Trim().Substring(0, 2));

                            string NstPart = "0";
                            string CpyStageType = "Line1";
                            string NstWt = "0";
                            if (CpyPrcFabReq.CpyKitcode.Trim().Substring(11, 1) == "1" || CpyPrcFabReq.CpyKitcode.Trim().Substring(11, 1) == "0")
                            {
                                NstPart = ComCon.getTranName("select KitCode from Bomdetails where BOMCode='" + CpyPrcFabReq.BOMcode + "' and Kitcode Like '004%' and  substring(Kitcode,11,1) in ('4') and Partcode='" + CpyPrcFabReq.CpyKitcode.Trim() + "'", "TblNstPartCode", "KitCode", con, tran);
                            }
                            else if (CpyPrcFabReq.CpyKitcode.Trim().Substring(11, 1) == "6")
                            {
                                NstPart = ComCon.getTranName("select KitCode from Bomdetails where BOMCode='" + CpyPrcFabReq.BOMcode + "' and Kitcode Like '004%' and  substring(Kitcode,11,1) in ('4') group by KitCode ", "TblNstPartCode", "KitCode", con, tran);
                            }
                            else if (CpyPrcFabReq.CpyKitcode.Trim().Substring(11, 1) == "2" || CpyPrcFabReq.CpyKitcode.Trim().Substring(11, 1) == "3")
                            {
                                NstPart = CpyPrcFabReq.CpyKitcode.Trim();
                                CpyStageType = "Line2";
                            }

                            NstWt = ComCon.getTranName("select Pwt from ProfitcenterPlDetails where ProfitcenterCode='01.008' and Partcode='" + NstPart + "'", "TblPartWt", "Pwt", con, tran);

                            sb.Remove(0, sb.Length);
                            sb.Append("insert into processfeedback(GroupPFBCode,PFBCode,MaxSrNo,Dt,EDt,Yr,MachineCode,SerialNo,ProfitCenterCode,SupplierCode,ProductCode,CanopyPlanCode,TurretKitCode,");
                            sb.Append("NestingforCode,NestingforQty,nstWtPerUt,nstSqftPerUt,WtperUt,SqftperUt,CpyStageType,");
                            sb.Append("PartCode,ProcessQty,CompanyCode,PFBRate,PPWCode,Remark,CatID,PCCode_Act)");
                            sb.Append(" values('" + PrcNo.Trim() + "', ");
                            sb.Append("'" + PrcNo.Trim() + "','" + (PrcNo.Substring(10, 8)) + "',");
                            if (ChkforStart == true)
                            {
                                sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',Null,");
                            }
                            //else if (ChkforStart == false)
                            //{
                            //    sb.Append("'" + ComCon.dateinyyyymmdd(await GetPrevPrcTimeAsync(con, tran, CpyPrcFabReq.PCCode, CpyPrcFabReq.PlanCode, CpyPrcFabReq.ProductCode, strMachineNo[1].ToString())) + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                            //}

                            else if (ChkforStart == false)
                            {
                                //local
                              //  sb.Append("'" + ComCon.dateinyyyymmdd(await GetPrevPrcTimeAsync(con, tran, CpyPrcFabReq.PCCode_Act, CpyPrcFabReq.PlanCode, CpyPrcFabReq.ProductCode, strMachineNo[1].ToString())) + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                                //server
                                sb.Append("'" + (await GetPrevPrcTimeAsync(con, tran, CpyPrcFabReq.PCCode_Act, CpyPrcFabReq.PlanCode, CpyPrcFabReq.ProductCode, strMachineNo[1].ToString())) + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");

                            }

                            //else if (ChkforStart == false)
                            //{
                            //    string prevPrcTime = await GetPrevPrcTimeAsync(
                            //        con, tran,
                            //        CpyPrcFabReq.PCCode_Act, CpyPrcFabReq.PlanCode,
                            //        CpyPrcFabReq.ProductCode, strMachineNo[1].ToString());

                            //    // GetPrevPrcTimeAsync returns "Null" when the previous process row has no EDt
                            //    // (started, not yet ended). dateinyyyymmdd can't parse that -> crash.
                            //    // No valid end-time to chain from, so fall back to start behaviour: Dt=now, EDt=Null.
                            //    if (prevPrcTime == "Null" || string.IsNullOrWhiteSpace(prevPrcTime))
                            //    {
                            //        sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',Null,");
                            //    }
                            //    else
                            //    {
                            //        sb.Append("'" + ComCon.dateinyyyymmdd(prevPrcTime) + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                            //    }
                            //}
                            sb.Append("'" + ComCon.yearEnd(con, tran) + "','" + strMachineNo[0].ToString() + "','" + strMachineNo[1].ToString() + "','" + CpyPrcFabReq.PCCode.Trim() + "','" + CpyPrcFabReq.OSSupplierCode.Trim() + "','" + CpyPrcFabReq.ProductCode.Trim() + "',");
                            sb.Append("'" + CpyPrcFabReq.PlanCode.Trim() + "','" + CpyPrcFabReq.BOMcode.Trim() + "',");
                            sb.Append("'" + NstPart + "','" + CpyPrcFabReq.BatchQty + "',");
                            sb.Append("'" + NstWt.Trim() + "','0',");
                            sb.Append("'" + CpyPrcFabReq.PWt + "','" + CpyPrcFabReq.PSqft + "','" + CpyStageType.Trim() + "',");
                            sb.Append("'" + CpyPrcFabReq.CpyKitcode.Trim() + "','" + CpyPrcFabReq.PrcQty + "','" + CpyPrcFabReq.PCCode_Act.Trim().Substring(0, 2) + "',");
                            sb.Append("'" + CpyPrcFabReq.PFBRate + "','" + CpyPrcFabReq.EmpCode.Trim() + "','Nil','" + CpyPrcFabReq.CatID + "','" + CpyPrcFabReq.PCCode_Act + "' )");
                            cmd = new SqlCommand(sb.ToString(), con);
                            cmd.Transaction = tran;
                            await cmd.ExecuteNonQueryAsync();

                            // ---------------- Action Taken File Attachment ----------------
                            if (!string.IsNullOrEmpty(CpyPrcFabReq.AttachFileDts.ToString().Trim()))
                            {
                                string[] strPlanDts = Regex.Split(CpyPrcFabReq.AttachFileDts, "@#@");
                                int SrNoA = 0;
                                foreach (String StrSub in strPlanDts)
                                {
                                    SrNoA += 1;
                                    string[] DtsA = Regex.Split(StrSub.ToString().Trim(), "-->");
                                    string FileName = PrcNo.ToString().Trim().Substring(4, 5).Trim() + PrcNo.ToString().Trim().Substring(10, 8).Trim() + "-" + (SrNoA) + Path.GetExtension(DtsA[1].ToString().Trim());
                                    string StrMpath = ComCon.getMainFilePath("TempPrcFab") + "/" + FileName.ToString().Trim();
                                    string StrTpath = "C:/TempERPFile/TempPrcFab/" + CpyPrcFabReq.EmpCode.Trim() + "/" + DtsA[1].ToString().Trim();
                                    string StrTempPath = "C:/TempERPFile/TempPrcFab/" + CpyPrcFabReq.EmpCode.Trim();
                                    if (Directory.Exists(StrTempPath) && File.Exists(StrTpath))
                                    {
                                        File.Copy(StrTpath, StrMpath);
                                    }
                                    sb.Remove(0, sb.Length);
                                    sb.Append("INSERT INTO ProcessFeedbackFiles");
                                    sb.Append("(GroupPFBCode,SrNo,FileName)");
                                    sb.Append(" VALUES('" + PrcNo.Trim() + "' ,'" + SrNoA + "','" + FileName.ToString().Trim() + "')");
                                    cmd = new SqlCommand(sb.ToString(), con);
                                    cmd.Transaction = tran;
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            // ---------------- Dts Entry ----------------
                            int recCount = ComCon.CountChars(CpyPrcFabReq.PrcDts, ",");
                            string[] strPrcDts = Regex.Split(CpyPrcFabReq.PrcDts, ",");
                            int SrNo = 0;
                            for (int cSub = 0; cSub <= recCount; cSub++)
                            {
                                SrNo += 1;
                                string[] Dts = Regex.Split(strPrcDts[cSub].ToString().Trim(), "-->");
                                sb.Remove(0, sb.Length);
                                sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,");
                                sb.Append("PFBRate,SaleRate,PLength,PWidth,PThickness,PLossWt,PCatagoryCode,WtPerUt,SqftPerUt)");
                                sb.Append("values('" + PrcNo.Trim() + "','" + SrNo + "',");
                                sb.Append("'" + Dts[0].Trim() + "',");
                                sb.Append("'" + Convert.ToDouble(Dts[1].Trim()) + "',");
                                sb.Append("'" + Convert.ToDouble(Dts[2].Trim()) + "',");
                                sb.Append("'" + Convert.ToDouble(Dts[3].Trim()) + "',");
                                sb.Append("'" + Convert.ToDouble(Dts[4].Trim()) + "',");
                                sb.Append("'" + Convert.ToDouble(Dts[5].Trim()) + "',");
                                sb.Append("'" + Convert.ToDouble(Dts[6].Trim()) + "',");
                                sb.Append("'" + Convert.ToDouble(Dts[7].Trim()) + "',");
                                sb.Append("'" + Dts[8].Trim() + "','" + Dts[9].Trim() + "','" + Math.Round(double.Parse(Dts[10].Trim()), 2) + "','" + Math.Round(double.Parse(Dts[11].Trim()), 2) + "')");
                                cmd = new SqlCommand(sb.ToString(), con);
                                cmd.Transaction = tran;
                                await cmd.ExecuteNonQueryAsync();

                                sb.Remove(0, sb.Length);
                                sb.Append("INSERT INTO StockWIP(FromProfitCenterCode,PartCode,IssueCode,IssueDate,IssueQty,ToProfitCenterCode,StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                sb.Append(" values('" + CpyPrcFabReq.PCCode.Trim() + "','" + Dts[0].ToString().Trim() + "',");
                                sb.Append("'" + PrcNo.Trim() + "',GetDate(),'" + double.Parse(Dts[2].ToString().Trim()) + "','" + CpyPrcFabReq.PCCode.Trim() + "',0,'" + CpyPrcFabReq.PCCode_Act.Trim() + "','" + CpyPrcFabReq.PCCode_Act.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con);
                                cmd.Transaction = tran;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // ---------------- OS / Plan status updates ----------------
                            sb.Remove(0, sb.Length);
                            sb.Append("Update CanopyPlanOSDetails set OSFQty=OSFQty + '" + CpyPrcFabReq.PrcQty + "' where CPCode='" + CpyPrcFabReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcFabReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcFabReq.CpyKitcode + "' and SCode='" + CpyPrcFabReq.OSSupplierCode + "' ");
                            cmd = new SqlCommand(sb.ToString(), con);
                            cmd.Transaction = tran;
                            await cmd.ExecuteNonQueryAsync();

                            string cntPrcQtyOS = ComCon.getTranName("select Qty-OSFQty as BalQty from CanopyPlanOSDetails where CPCode='" + CpyPrcFabReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcFabReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcFabReq.CpyKitcode + "'  and SCode='" + CpyPrcFabReq.OSSupplierCode + "' ", "FabPrc", "BalQty", con, tran);
                            if (cntPrcQtyOS == "0")
                            {
                                sb.Remove(0, sb.Length);
                                sb.Append("Update CanopyPlanOSDetails set OSFStatus='D' where CPCode='" + CpyPrcFabReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcFabReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcFabReq.CpyKitcode + "' and SCode='" + CpyPrcFabReq.OSSupplierCode + "' ");
                                cmd = new SqlCommand(sb.ToString(), con);
                                cmd.Transaction = tran;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            sb.Remove(0, sb.Length);
                            sb.Append("Update CanopyPlanDtsSub set CPFQty=CPFQty + '" + CpyPrcFabReq.PrcQty + "' where CPCode='" + CpyPrcFabReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcFabReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcFabReq.CpyKitcode + "' and CatID='" + CpyPrcFabReq.CatID + "' ");
                            cmd = new SqlCommand(sb.ToString(), con);
                            cmd.Transaction = tran;
                            await cmd.ExecuteNonQueryAsync();

                            string cntPrcQty = ComCon.getTranName("select CPQty-CPFQty as BalQty from CanopyPlanDtsSub where CPCode='" + CpyPrcFabReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcFabReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcFabReq.CpyKitcode + "' and CatID='" + CpyPrcFabReq.CatID + "' ", "FabPrc", "BalQty", con, tran);
                            if (cntPrcQty == "0")
                            {
                                sb.Remove(0, sb.Length);
                                sb.Append("Update CanopyPlanDtsSub set CPFStatus='D' where CPCode='" + CpyPrcFabReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcFabReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcFabReq.CpyKitcode + "'and CatID='" + CpyPrcFabReq.CatID + "' ");
                                cmd = new SqlCommand(sb.ToString(), con);
                                cmd.Transaction = tran;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            string cntBndStatus = ComCon.getTranName("select Count(CPFStatus) as CPFStatus from CanopyPlanDtsSub where CPCode='" + CpyPrcFabReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcFabReq.ProductCode.Trim() + "' and CatID='" + CpyPrcFabReq.CatID + "'  and  CPFStatus='P'  ", "FabPrc", "CPFStatus", con, tran);
                            if (cntBndStatus == "0")
                            {
                                string PlanQty = ComCon.getTranName("select Qty  from CanopyPlanDetails where CPCode='" + CpyPrcFabReq.PlanCode.Trim() + "' and Partcode='" + CpyPrcFabReq.ProductCode.Trim() + "' ", "TblCPQty", "Qty", con, tran);
                                if (CpyPrcFabReq.CatID.ToString() == "029")
                                {
                                    sb.Remove(0, sb.Length);
                                    sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode,IssueCode,IssueDate, IssueQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                    sb.Append(" values('" + CpyPrcFabReq.ProductCode.ToString().Trim() + "','" + CpyPrcFabReq.PCCode.Trim() + "','" + StrPowderCoating_PCCodeold.Trim() + "',");
                                    sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + double.Parse(PlanQty.Trim()) + "',0,'" + CpyPrcFabReq.PCCode_Act.Trim() + "','" + StrPowderCoating_PCCode.Trim() + "')");
                                    cmd = new SqlCommand(sb.ToString(), con);
                                    cmd.Transaction = tran;
                                    await cmd.ExecuteNonQueryAsync();

                                    sb.Remove(0, sb.Length);
                                    sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                    sb.Append(" values('" + CpyPrcFabReq.ProductCode.ToString().Trim() + "','" + CpyPrcFabReq.PCCode.Trim() + "','" + StrPowderCoating_PCCodeold.Trim() + "',");
                                    sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + double.Parse(PlanQty.Trim()) + "',0,'" + CpyPrcFabReq.PCCode_Act.Trim() + "','" + StrPowderCoating_PCCode.Trim() + "')");
                                    cmd = new SqlCommand(sb.ToString(), con);
                                    cmd.Transaction = tran;
                                    await cmd.ExecuteNonQueryAsync();

                                    sb.Remove(0, sb.Length);
                                    sb.Append("Update CanopyPlanSerialNo set CPFSerialStatus='D' where CPCode='" + CpyPrcFabReq.PlanCode.ToString().Trim() + "' and Partcode='" + CpyPrcFabReq.ProductCode.ToString().Trim() + "'  ");
                                    cmd = new SqlCommand(sb.ToString(), con);
                                    cmd.Transaction = tran;
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                //// Kanban region was commented out in source -> strKanBan stays empty, preserved.
                                //string strKanBan = "";

                                //// ---- User Activity (MaterialRequisitionWithoutPlan) ----
                                //cmd = new SqlCommand("insertLoginTransactionDetails", con);
                                //cmd.CommandType = CommandType.StoredProcedure;
                                //cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd"));
                                //cmd.Parameters.AddWithValue("@EmpID", CpyPrcFabReq.EmpCode);
                                //cmd.Parameters.AddWithValue("@TransactionType", "S");
                                //cmd.Parameters.AddWithValue("@TransactionFrom", "MaterialRequisitionWithoutPlan");
                                //cmd.Parameters.AddWithValue("@TransactionNo", strKanBan);
                                //cmd.Parameters.AddWithValue("@CompanyCode", CpyPrcFabReq.PCCode_Act.Substring(0, 2).Trim());
                                //cmd.Transaction = tran;
                                //await cmd.ExecuteNonQueryAsync();
                            }

                            // ---------------- User Activity (Fabrication Process) ----------------
                            cmd = new SqlCommand("InsertLoginTransactionDetails", con);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EmpID", CpyPrcFabReq.EmpCode.Trim());
                            cmd.Parameters.AddWithValue("@TransactionType", "S");
                            cmd.Parameters.AddWithValue("@TransactionFrom", "Fabrication Process");
                            cmd.Parameters.AddWithValue("@TransactionNo", PrcNo.Trim());
                            cmd.Parameters.AddWithValue("@CompanyCode", CpyPrcFabReq.PCCode_Act.Substring(0, 2).Trim());
                            cmd.Transaction = tran;
                            await cmd.ExecuteNonQueryAsync();

                            // ---------------- Prc Below 1000 ----------------
                            string GetMaxRatePartCode = ComCon.getTranName("Select top 1 PartCode From CanopyplandtsSub where CPCode='" + CpyPrcFabReq.PlanCode + "' and CpyPartcode='" + CpyPrcFabReq.ProductCode + "' and CatID='" + CpyPrcFabReq.CatID + "' order by rate desc ", "tbl_ChkForMaxRate", "PartCode", con, tran);
                            string PrcBelowRate = "";

                            if (GetMaxRatePartCode.Trim() == CpyPrcFabReq.CpyKitcode.Trim())
                            {
                                DataSet dsKitbelowRate = ComCon.procTranDS("select Pf.Partcode,Pl.rate,Pl.PurRate,Pwt,Psqft From CanopyPlanDtsSubBelowStdRate Pf Inner Join ProfitcenterPldetails Pl on  Pf.Partcode = Pl.partcode where CpyPartcode = '" + CpyPrcFabReq.ProductCode.ToString().Trim() + "' and CPCode = '" + CpyPrcFabReq.PlanCode + "' and CatID='" + CpyPrcFabReq.CatID + "' and ProfitcenterCode = '01.008' ", "tbl_KitbelowRate", con, tran);
                                if (dsKitbelowRate != null && dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows.Count > 0)
                                {
                                    for (int br = 0; br < dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows.Count; br++)
                                    {
                                        // ---- Mst Entry (below rate) — supplierCode + ProfitCenterCode both = PCCode (in-house) ----
                                        PrcBelowRate = await GetMaxPrcAsync(con, tran, "ProcessFeedback", "PFbCode", ComCon.yearEnd(con, tran), CpyPrcFabReq.PCCode_Act.Trim().Substring(0, 2));

                                        sb.Remove(0, sb.Length);
                                        sb.Append("insert into processfeedback(GroupPFBCode,PFBCode,CpyStageType,MaxSrNo,Dt,EDt,Yr,MachineCode,SerialNo,supplierCode,ProfitCenterCode,ProductCode,CanopyPlanCode,TurretKitCode,");
                                        sb.Append("NestingforCode,NestingforQty,nstWtPerUt,nstSqftPerUt,WtperUt,SqftperUt,");
                                        sb.Append("PartCode,ProcessQty,CompanyCode,PFBRate,PPWCode,Remark,CatID,PCCode_Act)");
                                        sb.Append(" values('" + PrcNo.Trim() + "','" + PrcBelowRate.Trim() + "','" + CpyStageType + "','" + (PrcBelowRate.Substring(10, 8)) + "', ");
                                        sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                                        sb.Append("'" + ComCon.yearEnd(con, tran) + "','" + strMachineNo[0].ToString() + "','" + strMachineNo[1].ToString() + "','" + CpyPrcFabReq.PCCode.Trim() + "','" + CpyPrcFabReq.PCCode.Trim() + "','" + CpyPrcFabReq.ProductCode.Trim() + "',");
                                        sb.Append("'" + CpyPrcFabReq.PlanCode.Trim() + "','" + CpyPrcFabReq.BOMcode.Trim() + "',");
                                        sb.Append("'" + NstPart + "','" + CpyPrcFabReq.BatchQty + "','" + NstWt.Trim() + "','0',  '" + double.Parse(dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Pwt"].ToString().Trim()) + "',  '" + double.Parse(dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["PSqft"].ToString().Trim()) + "',");
                                        sb.Append("'" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "','" + CpyPrcFabReq.PrcQty + "','" + CpyPrcFabReq.PCCode_Act.Trim().Substring(0, 2) + "', ");
                                        sb.Append("'" + ComCon.getTranName("Select Isnull(Max(Rate),0) as Rate From ProfitcenterPLDetails where ProfitcenterCode='01.008' and Partcode='" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "' ", "tbl_PLRate", "Rate", con, tran) + "'");
                                        sb.Append(", '" + CpyPrcFabReq.EmpCode + "','Nil','" + CpyPrcFabReq.CatID.Trim() + "','" + CpyPrcFabReq.PCCode_Act.Trim() + "')");
                                        cmd = new SqlCommand(sb.ToString(), con);
                                        cmd.Transaction = tran;
                                        await cmd.ExecuteNonQueryAsync();

                                        // ---- Dts Entry (below rate) ----
                                        int ChkStk = 0;
                                        DataSet dsKitbelowRatedts = ComCon.procTranDS("select Bd.Partcode,P.PartDesc,Qty,Purrate,rate,Pwt,Psqft,Bd.Length,bd.Width,bd.Thickness,bd.LossWgt,Bd.categoryID, " +
                                           " (select Round(Isnull(Sum(Recqty) - sum(IssueQty), 0), 00) as Stk From ( select Sum(ReceivedQty) as Recqty, " +
                                          " 0.00 as IssueQty from stockwip where ToProfitcenterCode_Act = '" + CpyPrcFabReq.PCCode_Act.Trim() + "' and StockType = '0' " +
                                         " and Partcode = Bd.Partcode and  ReceivedQty > 0   Union all " +
                                          " select 0.00 as Recqty, sum(IssueQty) as IssueQty from stockwip where FromProfitcenterCode_Act = '" + CpyPrcFabReq.PCCode_Act.Trim() + "' and StockType = '0' " +
                                          "  and Partcode = Bd.Partcode and  IssueQty > 0) as stk) as Stock " +
                                          "From BOMDetails Bd Inner Join ProfitcenterPLdetails Pl On   Bd.KitCode=Pl.Partcode  Inner Join part P On Bd.Partcode=P.partcode " +
                                            " where Bd.BOMCode='" + CpyPrcFabReq.BOMcode + "' and Bd.KitCode='" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "' " +
                                            " and Pl.ProfitcenterCode='01.008'  ", "tbl_KitbelowRatedts", con, tran);

                                        if (dsKitbelowRatedts != null && dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows.Count > 0)
                                        {
                                            SrNo = 0;
                                            for (int brd = 0; brd < dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows.Count; brd++)
                                            {
                                                if ((Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Qty"].ToString().Trim()) * CpyPrcFabReq.PrcQty) >
                                                Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Stock"].ToString().Trim()))
                                                {
                                                    if (ChkStk == 0)
                                                    {
                                                        PrcNo = dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["PartDesc"].ToString().Trim();
                                                        ChkStk = 1;
                                                    }
                                                    else
                                                    {
                                                        PrcNo = PrcNo + "," + dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["PartDesc"].ToString().Trim();
                                                    }
                                                }
                                                // NOTE: original had 'else' commented out -> this is a separate if, preserved.
                                                if (ChkStk == 0)
                                                {
                                                    SrNo += 1;
                                                    sb.Remove(0, sb.Length);
                                                    sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,");
                                                    sb.Append("PFBRate,SaleRate,WtPerUt,SqftPerUt,PLength,PWidth,PThickness,PLossWt,PCatagoryCode)");
                                                    sb.Append("values('" + PrcBelowRate.Trim() + "','" + SrNo + "',");
                                                    sb.Append("'" + dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Partcode"].ToString().Trim().Trim() + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Qty"].ToString().Trim()) + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Qty"].ToString().Trim()) * CpyPrcFabReq.PrcQty + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Purrate"].ToString().Trim()) + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["rate"].ToString().Trim()) + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Pwt"].ToString().Trim()) + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Psqft"].ToString().Trim()) + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Length"].ToString().Trim()) + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Width"].ToString().Trim()) + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Thickness"].ToString().Trim()) + "',");
                                                    sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["LossWgt"].ToString().Trim()) + "',");
                                                    sb.Append("'" + dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["categoryID"].ToString().Trim() + "')");
                                                    cmd = new SqlCommand(sb.ToString(), con);
                                                    cmd.Transaction = tran;
                                                    await cmd.ExecuteNonQueryAsync();

                                                    sb.Remove(0, sb.Length);
                                                    sb.Append("INSERT INTO StockWIP(FromProfitCenterCode,PartCode,IssueCode,IssueDate,IssueQty,ToProfitCenterCode,StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                                    sb.Append(" values('" + CpyPrcFabReq.PCCode.Trim() + "','" + dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Partcode"].ToString().Trim() + "',");
                                                    sb.Append("'" + PrcBelowRate.Trim() + "',GetDate(),'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Qty"].ToString()) * CpyPrcFabReq.PrcQty + "','" + CpyPrcFabReq.PCCode.Trim() + "',0,'" + CpyPrcFabReq.PCCode_Act.Trim() + "','" + CpyPrcFabReq.PCCode_Act.Trim() + "')");
                                                    cmd = new SqlCommand(sb.ToString(), con);
                                                    cmd.Transaction = tran;
                                                    await cmd.ExecuteNonQueryAsync();
                                                }
                                            }
                                            if (ChkStk > 0)
                                            {
                                                PrcNo = "Insufficient Stock For Part(BR): " + PrcNo;
                                                await tran.RollbackAsync();   // discard open transaction before returning
                                                return PrcNo;
                                            }
                                        }
                                    }
                                }

                                // ---------------- Fab kit (MOB B Part Lock, Get_FabKit_NewERP) ----------------
                                FabkitFlag = "";
                                DataSet dsFabKit = ComCon.procTranDS("Exec Get_FabKit_NewERP '" + CpyPrcFabReq.BOMcode.Trim() + "','" + CpyPrcFabReq.PCCode_Act.Trim() + "','" + CpyPrcFabReq.CatID.Trim() + "' ", "tbl_FabKit", con, tran);
                                if (dsFabKit != null && dsFabKit.Tables["tbl_FabKit"].Rows.Count > 0)
                                {
                                    for (int k = 0; k < dsFabKit.Tables["tbl_FabKit"].Rows.Count; k++)
                                    {
                                        if ((Convert.ToDouble(dsFabKit.Tables["tbl_FabKit"].Rows[k]["Qty"].ToString().Trim()) * CpyPrcFabReq.PrcQty) >
                                             Convert.ToDouble(dsFabKit.Tables["tbl_FabKit"].Rows[k]["StockQty"].ToString().Trim()))
                                        {
                                            FabkitFlag = FabkitFlag + ", " + dsFabKit.Tables["tbl_FabKit"].Rows[k]["PartDesc"].ToString().Trim();
                                        }
                                        else
                                        {
                                            sb.Remove(0, sb.Length);
                                            sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,SaleRate)");
                                            sb.Append("values('" + PrcNo.Trim() + "','" + (k + 2) + "',");
                                            sb.Append("'" + dsFabKit.Tables["tbl_FabKit"].Rows[k]["PartCode"].ToString().Trim() + "',");
                                            sb.Append("'" + dsFabKit.Tables["tbl_FabKit"].Rows[k]["Qty"].ToString().Trim() + "',");
                                            sb.Append("'" + double.Parse(dsFabKit.Tables["tbl_FabKit"].Rows[k]["Qty"].ToString().Trim()) * double.Parse(CpyPrcFabReq.PrcQty.ToString()) + "',");
                                            sb.Append("'" + dsFabKit.Tables["tbl_FabKit"].Rows[k]["SuppRate"].ToString().Trim() + "')");
                                            cmd = new SqlCommand(sb.ToString(), con);
                                            cmd.Transaction = tran;
                                            await cmd.ExecuteNonQueryAsync();

                                            sb.Remove(0, sb.Length);
                                            sb.Append("INSERT INTO StockWIP(FromProfitCenterCode,PartCode,IssueCode,IssueDate,IssueQty,ToProfitCenterCode,StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                            sb.Append(" values('" + CpyPrcFabReq.PCCode.Trim() + "','" + dsFabKit.Tables["tbl_FabKit"].Rows[k]["PartCode"].ToString().Trim() + "',");
                                            sb.Append("'" + PrcNo.Trim() + "',GetDate(),'" + double.Parse(dsFabKit.Tables["tbl_FabKit"].Rows[k]["Qty"].ToString().Trim()) * double.Parse(CpyPrcFabReq.PrcQty.ToString()) + "','" + CpyPrcFabReq.PCCode.Trim() + "',0,'" + CpyPrcFabReq.PCCode_Act.Trim() + "','" + CpyPrcFabReq.PCCode_Act.Trim() + "')");
                                            cmd = new SqlCommand(sb.ToString(), con);
                                            cmd.Transaction = tran;
                                            await cmd.ExecuteNonQueryAsync();
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(FabkitFlag))
                                    {
                                        FabkitFlag = "Insufficient Stock For Part(BR): " + FabkitFlag;
                                        await tran.RollbackAsync();   // discard open transaction before returning
                                        return FabkitFlag;
                                    }
                                }
                            }

                           await tran.CommitAsync();
                            //await tran.RollbackAsync();
                            PrcNo = "ProcessCode=" + PrcNo + " For Fabrication  Saved SuccessFully ";
                        }
                        else
                        {
                            PrcNo = "Fabrication Process For Part Already Saved ";
                            return PrcNo;
                        }
                    }
                    else if (CpyPrcFabReq.PFBCode.Substring(0, 3) == "PSH")
                    {
                        tran = (SqlTransaction)await con.BeginTransactionAsync();

                        SqlCommand cmd;
                        sb.Remove(0, sb.Length);
                        sb.Append("Update ProcessFeedBack set EDt = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'  where PFBCode='" + CpyPrcFabReq.PFBCode.Trim() + "' ");
                        cmd = new SqlCommand(sb.ToString(), con);
                        cmd.Transaction = tran;
                        await cmd.ExecuteNonQueryAsync();

                        string cntDatecQty = ComCon.getTranName("select count(Dt) as DT from ProcessFeedback where PFBCode='" + CpyPrcFabReq.PFBCode.Trim() + "' and Dt='1900-01-01 00:00:00.000' ", "Tbl_Dt", "Dt", con, tran);
                        if (cntDatecQty != "0")
                        {
                            sb.Remove(0, sb.Length);
                            sb.Append("Update ProcessFeedBack set Dt = '" + DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss") + "'  where PFBCode='" + CpyPrcFabReq.PFBCode.Trim() + "' ");
                            cmd = new SqlCommand(sb.ToString(), con);
                            cmd.Transaction = tran;
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await tran.CommitAsync();
                        //await tran.RollbackAsync();
                        PrcNo = "ProcessCode=" + CpyPrcFabReq.PFBCode.Trim() + " For Fabrication  End SuccessFully ";
                    }
                    return PrcNo;
                }
            }
            catch (Exception ex)
            {
                if (tran != null) await tran.RollbackAsync();
                return ("StackTrace " + ex.StackTrace.ToString() + " Message " + ex.Message.ToString());
            }
            finally
            {
                // Connection is owned by the DbContext; only close if we opened it here.
                if (openedHere && con.State == ConnectionState.Open) await con.CloseAsync();
            }
        }

        // ⚠️ INFERRED — replace this query with your real ckhDoubleEntry logic.
        // Returns true when an active fabrication entry already exists for this combo.
        private async Task<bool> CkhDoubleEntryAsync(SqlConnection con, SqlTransaction tran,string pcCode, string planCode, string productCode, string suppCode, string cpyKitcode,CancellationToken cancellationToken = default)
        {
            var sql = "Select isNull(Count(Productcode),0) as Cnt From processfeedback " +
                      "where PCCode_Act='" + pcCode + "' and CanopyPlanCode='" + planCode + "' " +
                      "and Productcode='" + productCode + "' and SupplierCode='" + suppCode + "' " +
                      "and Partcode='" + cpyKitcode + "' and Active='1' and Edt is null";

            using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            int cnt = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
            return cnt > 0;
        }

        // 1. Max process-code generator (single ExecuteScalar, padding simplified)
        private async Task<string> GetMaxPrcAsync(
            SqlConnection con, SqlTransaction tran,
            string tableName, string fieldName, string yr, string compCode,
            CancellationToken cancellationToken = default)
        {
            var sql = "select max(substring(" + fieldName + ",13,7)) as MX from " + tableName.Trim() +
                      " where yr='" + yr.Trim() + "' and CompanyCode='" + compCode.Trim() + "'";

            using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            var scalar = await cmd.ExecuteScalarAsync(cancellationToken);

            int next = (scalar == null || scalar == DBNull.Value) ? 1 : Convert.ToInt32(scalar) + 1;
            string max = compCode + next.ToString().PadLeft(6, '0');
            return "PSH/" + yr + "/" + max;
        }

        // 2. Start check WITH serial number (getChkforStart)
        private async Task<bool> GetChkforStartAsync(
            SqlConnection con, SqlTransaction tran,
            string pcCode, string planCode, string productCode, string machineNo, string catId,
            CancellationToken cancellationToken = default)
        {
            var sql = "Select isNull(Count(Productcode),0) as CntStart From processfeedback " +
                      "where PCCode_Act='" + pcCode + "' and CanopyPlanCode='" + planCode + "' " +
                      "and Productcode='" + productCode + "' and serialNo='" + machineNo + "' " +
                      "and CatID='" + catId + "' and Active='1'";

            using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            int cnt = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
            return cnt == 0;
        }

        // 3. Start check WITHOUT serial number (getChkforStartCpy)
        private async Task<bool> GetChkforStartCpyAsync(
            SqlConnection con, SqlTransaction tran,
            string pcCode, string planCode, string productCode, string catId,
            CancellationToken cancellationToken = default)
        {
            var sql = "Select isNull(Count(Productcode),0) as CntStart From processfeedback " +
                      "where PCCode_Act='" + pcCode + "' and CanopyPlanCode='" + planCode + "' " +
                      "and Productcode='" + productCode + "' and CatID='" + catId + "' and Active='1' ";

            using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            int cnt = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
            return cnt == 0;
        }

        // 4. Previous process end-time (GetPrevPrcTime)
        private async Task<string> GetPrevPrcTimeAsync(
            SqlConnection con, SqlTransaction tran,
            string pcCode, string planCode, string productCode, string machineNo,
            CancellationToken cancellationToken = default)
        {
            var sql = "Select Top 1 EDt From processfeedback " +
                      "where PCCode_Act='" + pcCode + "' and CanopyPlanCode='" + planCode + "' " +
                      "and Productcode='" + productCode + "' and SerialNo='" + machineNo + "' and Active='1' " +
                      "Order By Dt Desc ";

            using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            if (result == null || result == DBNull.Value)
                return "Null";
            return result.ToString().Trim();
        }



      public async Task<string> SubmitFabricationCheckerAsync(CpyPrcFabCheckerRequest req, CancellationToken ct = default)
        {
            string PrcNo = "";
            string strReqCode = "";          // never set in the AUTH path; kept for message parity
            string strReqCodeCPYAssly = "";  // referenced in original success message; kept as-is
            string strKanBan = "";

            await using var con = new SqlConnection(_connStr);
            await con.OpenAsync(ct);
            await using var tran = (SqlTransaction)await con.BeginTransactionAsync(ct);

            try
            {
                var strPlanDts = Regex.Split(req.ProductionDetails ?? "", "@@#@@");

                // Fabrication PC -> PowderCoating PC mapping (kept from original; NOT used downstream in the checker)
                string StrPowderCoatingPCCode = req.PCCode_Act.Trim() switch
                {
                    "01.101" => "01.116",   // A Unit 1
                    "01.102" => "01.116",   // B Unit 1
                    "01.103" => "01.116",   // C Unit 1
                    "03.073" => "01.116",   // A Unit 4
                    "03.074" => "01.116",   // B Unit 4
                    "03.075" => "01.116",   // C Unit 4
                    _ => "0"
                };

                if (req.Status.Trim() == "AUTH")
                {
                    // ---- UPDATE ProcessFeedBack (mark checked) ----
                    await ExecNonQueryAsync(con, tran, ct,
                        "UPDATE ProcessFeedBack SET Dt = @Dt, Checker1 = 1 " +
                        "WHERE CanopyPlanCode = @PlanCode and PFBCode = @PFBCode",
                        ("@Dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                        ("@PlanCode", req.PlanCode.Trim()),
                        ("@PFBCode", req.PFBCode.Trim()));

                    // ---- insert only the "unassigned" 6M lines (AssignTo == "0") ----
                    // Bounds check (DtsPlan.Length > 3) preserved from the original Fab fix.
                    foreach (var StrSub in strPlanDts)
                    {
                        var DtsPlan = Regex.Split(StrSub.Trim(), "@#@");
                        if (DtsPlan.Length > 3 && DtsPlan[3] != null && DtsPlan[3].Trim() == "0")
                        {
                            await ExecProcAsync(con, tran, ct, "InsertSheetMetal6MChecker_Detail",
                                ("@PlanCode", req.PlanCode.Trim()),
                                ("@SixMName", DtsPlan[1].Trim()),
                                ("@Description", DtsPlan[2].Trim()),
                                ("@AssignTo", DtsPlan[3].Trim()),
                                ("@CorReqNo", "0"),
                                ("@Status", req.Status.Trim()));
                        }
                    }

                    // ---- any fab rows still unchecked for this plan / product / cat / PC? ----
                    // NOTE: Fab filters on ProfitCenterCode (bending used PCCode_Act).
                    var cntfabStatus = await ComCon.GetScalarAsync(
                        "select isnull(Count(Checker1),0) as Checker1 from ProcessFeedBack " +
                        "where Checker1='0' and CanopyPlanCode=@PlanCode and ProductCode=@ProductCode " +
                        "and CatId=@CatId and PCCode_Act=@PCCode and Active='1'",
                        new Dictionary<string, object?>
                        {
                            ["@PlanCode"] = req.PlanCode.Trim(),
                            ["@ProductCode"] = req.ProductCode.Trim(),
                            ["@CatId"] = req.CatID.Trim(),
                            ["@PCCode"] = req.PCCode_Act.Trim()
                        }, con, tran);

                    if (cntfabStatus == "0")
                    {
                        // ===== KanBan Processing =====
                        strKanBan = "";
                        var dsKanBan = await ComCon.ExecuteToDataSetAsync(
                            "exec InternalTOCReq_NewERP @PCCode",
                            new Dictionary<string, object?> { ["@PCCode"] = req.PCCode_Act.Trim() },
                            "tbl_RaiseReqDtsKanBan", con, tran);

                        if (dsKanBan?.Tables["tbl_RaiseReqDtsKanBan"] is { Rows.Count: > 0 } kanTable)
                        {
                            var GetMaxValue = await ComCon.GetMaxNoAsync("MaterialRequisitionWithOutPlan", "REQ", req.PCCode_Act.Substring(0, 2), con, tran);
                            strKanBan = GetMaxValue;
                            var toPCCode = kanTable.Rows[0]["ToPCCode"].ToString()!.Trim();
                            await ExecNonQueryAsync(con, tran, ct,
                                "insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt,Yr,ProfitCenterCode,ToProfitCenterCode,ProfitCenterCode_Act,ToProfitCenterCode_Act," +
                                "ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) " +
                                "values(@REQCode,@MaxSrNo,@Dt,@Yr,@ProfitCenterCode,@ToProfitCenterCode,@ProfitCenterCode_Act,@ToProfitCenterCode_Act,@ClassCode,@CompanyCode,@ActNo,'P','WIP',@Remark,'1','1','1','KanBan','0')",
                                ("@REQCode", strKanBan.Trim()),
                                ("@MaxSrNo", GetMaxValue.Substring(10, 8)),
                                ("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")),
                                ("@Yr", await ComCon.YearEndAsync(con, tran)),
                                ("@ProfitCenterCode", req.PCCode.Trim()),
                                ("@ToProfitCenterCode", toPCCode),
                                ("@ProfitCenterCode_Act", req.PCCode_Act.Trim()),
                                ("@ToProfitCenterCode_Act", toPCCode),
                                ("@ClassCode", req.ProductCode),
                                ("@CompanyCode", req.PCCode_Act.Substring(0, 2)),
                                ("@ActNo", req.BatchQty.ToString().Trim()),
                                ("@Remark", "Auto Req For Plan No: " + req.ProductCode + " and Prc No: " + req.PFBCode)
                                //,("@PCCode_Act", req.PCCode_Act.ToString().Trim())
                                );

                            int SrNoReq = 0;
                            foreach (DataRow row in kanTable.Rows)
                            {
                                SrNoReq++;
                                await ExecProcAsync(con, tran, ct, "insertMaterialRequisitionWithOutPlanDetails_ERPNEW",
                                    ("@REQCode", strKanBan),
                                    ("@SrNo", SrNoReq),
                                    ("@PartCode", row["Partcode"].ToString()!.Trim()),
                                    ("@Qty", double.Parse(row["RaiseReqQty"].ToString()!.Trim())),
                                    ("@REQStatus", "P"));
                            }
                        }

                        // ---- activity log ----
                        await ExecProcAsync(con, tran, ct, "insertLoginTransactionDetails",
                            ("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                            ("@EmpID", req.EmpCode),
                            ("@TransactionType", "S"),
                            ("@TransactionFrom", "MaterialRequisitionWithoutPlan"),
                            ("@TransactionNo", strKanBan),
                            ("@CompanyCode", req.PCCode_Act.Substring(0, 2).Trim()));

                        await tran.CommitAsync(ct);
                        PrcNo = "ProcessCode:" + req.PFBCode.Trim() + " and ReqCode: " + strReqCode + "," + strReqCodeCPYAssly + " For Fabrication Saved SuccessFully ";
                        return PrcNo;
                    }

                    await tran.CommitAsync(ct);
                    PrcNo = "ProcessCode=" + req.PFBCode.Trim() + " For Fabrication  Saved SuccessFully ";
                    return PrcNo;
                }
                else
                {
                    // ---- Status is NOT "AUTH" (REJECT) ----
                    // NOTE: no bounds check here (matches original); accesses DtsPlan[1..4],
                    // so each line must split into at least 5 segments.
                    foreach (var StrSub in strPlanDts)
                    {
                        var DtsPlan = Regex.Split(StrSub.Trim(), "@#@");

                        var StrDispCode = await ComCon.GetMaxNoAsync("CorporateRequisition", "COR", req.CompCode.Trim(), con, tran);

                        var assigned = DtsPlan[3] != null && DtsPlan[3].Trim() != "0";

                        await ExecProcAsync(con, tran, ct, "InsertSheetMetal6MChecker_Detail",
                            ("@PlanCode", req.PlanCode.Trim()),
                            ("@SixMName", DtsPlan[1].Trim()),
                            ("@Description", DtsPlan[2].Trim()),
                            ("@AssignTo", DtsPlan[3].Trim()),
                            ("@CorReqNo", assigned ? StrDispCode.Trim() : "0"),
                            ("@Status", req.Status.Trim()));

                        if (assigned)
                        {
                            var ReqMsg = string.Format(
                                " Fabrication Checker PlanCode: {0}, PFBCode: {1}, 6MType: {2}, Description: {3}",
                                req.PlanCode.Trim(), req.PFBCode.Trim(), DtsPlan[1].Trim(), DtsPlan[2].Trim());

                            await ExecNonQueryAsync(con, tran, ct,
                                "INSERT INTO CorporateRequisition (ReqCode,Dt,Yr,MaxSrNo,EmpCode,FromPCCode,ToEmpCode,ToPCCode," +
                                "Priority,ReqMsg,CompanyCode,AssignStatus,Active,Discard) " +
                                "VALUES(@ReqCode,@Dt,@Yr,@MaxSrNo,@EmpCode,@FromPCCode,@ToEmpCode,@ToPCCode,'High Priority',@ReqMsg,@CompanyCode,'P','1','1')",
                                ("@ReqCode", StrDispCode.Trim()),
                                ("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")),
                                ("@Yr", StrDispCode.Substring(4, 5)),
                                ("@MaxSrNo", StrDispCode.Substring(10, 8)),
                                ("@EmpCode", req.EmpCode.Trim()),
                                ("@FromPCCode", req.PCCode_Act.Trim()),
                                ("@ToEmpCode", DtsPlan[3].Trim()),
                                ("@ToPCCode", DtsPlan[4].Trim()),
                                ("@ReqMsg", ReqMsg.Trim()),
                                ("@CompanyCode", req.CompCode));

                            // Original ran SELECT @@Identity via ExecuteScalar but never used the value -> dropped.
                            await ExecNonQueryAsync(con, tran, ct,
                                "INSERT INTO CorporateRequisitionActionTaken (Dt,ReqCode,AssignByCode,AssignToCode,ActionTaken," +
                                "Priority,ActionStatus,AssOrAction,Active,Discard) " +
                                "VALUES(@Dt,@ReqCode,@AssignByCode,@AssignToCode,'','High Priority','P','ASS','1','1')",
                                ("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")),
                                ("@ReqCode", StrDispCode.Trim()),
                                ("@AssignByCode", req.EmpCode.Trim()),
                                ("@AssignToCode", DtsPlan[3].Trim()));

                            await ExecNonQueryAsync(con, tran, ct,
                                "Update CorporateRequisition set AssignStatus='C' where ReqCode=@ReqCode",
                                ("@ReqCode", StrDispCode.Trim()));
                        }
                    }

                    await tran.CommitAsync(ct);
                    PrcNo = "ProcessCode:" + req.PFBCode.Trim() + " For Fabrication Saved SuccessFully ";
                    return PrcNo;
                }
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync(ct);
                return "StackTrace " + ex.StackTrace + " Message " + ex.Message;
            }
        }


        private static async Task ExecNonQueryAsync(
    SqlConnection con, SqlTransaction tran, CancellationToken ct,
    string sql, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(sql, con, tran);
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static async Task ExecProcAsync(
            SqlConnection con, SqlTransaction tran, CancellationToken ct,
            string procName, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(procName, con, tran) { CommandType = CommandType.StoredProcedure };
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }

    }
}
