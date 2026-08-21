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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Services
{
    public class PowderCoatingService : IPowderCoating
    {
        private readonly KalaDbContext _db;

        private readonly string _connStr;
        private readonly CommonCon ComCon;

        public PowderCoatingService(KalaDbContext context,ICommonService common, ILogger<PowderCoatingService> logger,IConfiguration config, CommonCon com)
        {
            _db = context;
            ComCon = com;
            _connStr = config.GetConnectionString("KalaDbContext")
                       ?? throw new InvalidOperationException("Connection string 'KalaDbContext' not found.");
        }

        public async Task<List<Dictionary<string, object>>> GetCpyKitPCAsync(string pcCode, string machineCode, string planCode,string partCode, string cpyKit, string kva)
        {
            var parts = Regex.Split(machineCode?.Trim() ?? "", "-->");
            var machine = parts.Length > 0 ? parts[0].Trim() : "";
            var serialNo = parts.Length > 1 ? parts[1].Trim() : "";

            const string feedbackDate = "2020-07-10 00:00:00";

            var sql = @"select P.AliseName as KitDesc, '1' as SelectPC, P1.KVA, P1.Model as ModelCPType,
                       '0.00-->0.00-->0.00-->0.00' as Sqft, NestingForQty as BatchQty, 0 as BatchBalQty,
                       ProcessQty as PrcQty, 0.00 as PrcSqft, 0.00 as TotSqft,
                       CanopyPlanCode as PlanCode, convert(varchar(10), C.dt, 103) as PlanDt,
                       pf.CatID, Pf.PartCode as KitCode, PfbCode, GroupPfbCode, EDt,
                       TurretKitCode as BOMCode, ProductCode
                from processfeedback pf
                inner join Part P  on Pf.partcode    = P.partcode
                inner join Part P1 on Pf.Productcode = P1.Partcode
                inner join CanopyPlan C on Pf.CanopyPlanCode = C.CPCode
                where pf.PCCode_Act = @PCCode
                  and MachineCode = @Machine and SerialNo = @SerialNo
                  and Edt is null and Pf.Active = '1'
                  and Pf.Partcode like '004%' and Pf.Dt >= @FbDate
                order by Pf.Dt desc";

            var rows = await QueryAsync(sql, cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
            });

            return rows.Count > 0
                ? rows
                : await GetCpyKitPcSpAsync(pcCode, planCode, partCode, cpyKit, kva);
        }

        // Shared stored-proc fallback (GetCpyKit_NewERP).
        private Task<List<Dictionary<string, object>>> GetCpyKitPcSpAsync( string pcCode, string planCode, string partCode, string cpyKit, string kva)
        {
            return QueryAsync("GetCpyKit_NewERP", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@PCCode", SqlDbType.Char) { Value = pcCode });
                cmd.Parameters.Add(new SqlParameter("@PlanCode", SqlDbType.Char) { Value = planCode });
                cmd.Parameters.Add(new SqlParameter("@Partcode", SqlDbType.Char) { Value = partCode });
                cmd.Parameters.Add(new SqlParameter("@CpyKit", SqlDbType.Char) { Value = cpyKit });
                cmd.Parameters.Add(new SqlParameter("@Kva", SqlDbType.Char) { Value = kva });
            }, CommandType.StoredProcedure);
        }

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


        public async Task<string> SubmitPowderCoatingAsync(CpyPrcPCRequest CpyPrcPCReq, CancellationToken cancellationToken = default)
        {
            string PrcNo = "";
            string PrcNos = "";
            string Trans = "";

            StringBuilder sb = new StringBuilder();

            SqlConnection con = (SqlConnection)_db.Database.GetDbConnection();
            bool openedHere = false;
            SqlTransaction tran = null;

            try
            {
                int recCount = ComCon.CountChars(CpyPrcPCReq.PrcDts, ",");
                string[] strPrcDts = Regex.Split(CpyPrcPCReq.PrcDts, ",");
                int SrNo = 0;
                string GrpPfbCode = "";
                string PCkitFlag = "No";
                string AllPrcCode = "";
                string NstPart = "0";
                string NstWtsqft = "0";
                string CpyStageType = "Line1";
                string strKanBan = "";   // never set (Kanban block commented in original); kept for parity

                if (con.State != ConnectionState.Open)
                {
                    await con.OpenAsync(cancellationToken);
                    openedHere = true;
                }

                tran = (SqlTransaction)await con.BeginTransactionAsync(cancellationToken);

                SqlCommand cmd;
                string[] strMachineNo = Regex.Split(CpyPrcPCReq.MachineCodeSrNo, "-->");

                for (int cSub = 0; cSub <= recCount; cSub++)
                {
                    SrNo = 0;
                    string[] Dts = Regex.Split(strPrcDts[cSub].ToString().Trim(), "-->");

                    string PCSheetQty = ComCon.getTranName("SELECT isnull(Count(PFBCode),0) as PFBCode FROM processfeedback WHERE canopyplancode = '" + Dts[0].Trim() + "' AND partcode = '" + Dts[3].Trim() + "' AND PCCode_Act = '" + CpyPrcPCReq.PCCode_Act + "' and Active ='1' ", "tbl_PFBCode", "PFBCode", con, tran);

                    if (PCSheetQty != "0" && Dts[10].Substring(0, 3) == "NEW")
                    {
                        PrcNo = "Process is already saved.";

                        sb.Remove(0, sb.Length);
                        sb.Append("UPDATE CanopyPlanDtsSub SET CPPCStatus='D',CPPCQty='" + Dts[9].Trim() + "' " +
                                  "WHERE CpCode = '" + Dts[0].Trim() + "' " +
                                  "AND partcode = '" + Dts[3].Trim() + "' " +
                                  "AND CatID = '" + Dts[13].Trim() + "'");
                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                        await cmd.ExecuteNonQueryAsync(cancellationToken);

                        await tran.CommitAsync(cancellationToken);   // FLAG: original left this uncommitted
                        return PrcNo;
                    }
                    else
                    {

                        //Added Powder coting  Lock ProductWip Stock 

                        string PCReqFlag = "";
                        if (Dts[10].Substring(0, 3) == "NEW" && CpyPrcPCReq.CatID.ToString() == "029")
                        {
                            var dsDetailsSub = ComCon.procTranDS(
                               "Exec Get_ProductWip_Ben_CanopyAssly_NewERP '" + CpyPrcPCReq.PCCode_Act.Trim() + "','" + Dts[1].Trim() + "'",
                               "tbl_ProductWip_Ben_CanopyAssly",
                               con,
                               tran
                           );
                            var table = dsDetailsSub.Tables["tbl_ProductWip_Ben_CanopyAssly"];
                            if (table.Rows.Count > 0)
                            {
                                int ClsQty = Convert.ToInt32(table.Rows[0]["ClsQty"]);

                                //  Correct condition for insufficient stock
                                if (Convert.ToInt32(Dts[9]) > ClsQty)
                                {
                                    string partDesc = table.Rows[0]["PartDesc"].ToString().Trim();

                                    if (!string.IsNullOrEmpty(PCReqFlag))
                                    {
                                        PCReqFlag += ", " + partDesc;
                                    }
                                    else
                                    {
                                        PCReqFlag = partDesc;
                                    }

                                    if (!string.IsNullOrEmpty(PCReqFlag))
                                    {
                                        PCReqFlag = "Insufficient Stock For Part(BR): " + PCReqFlag;
                                        return PCReqFlag;
                                    }
                                }
                            }
                        }

                        //END


                        if (Dts[10].Substring(0, 3) == "NEW")
                        {
                            Trans = "Start";

                            // ---------- Master Entry ----------
                            PrcNo = await GetMaxPrcAsync(con, tran, "ProcessFeedback", "PFbCode", ComCon.yearEnd(con, tran), CpyPrcPCReq.PCCode_Act.Trim().Substring(0, 2));
                            if (cSub == 0) { GrpPfbCode = PrcNo; AllPrcCode = PrcNo; }
                            else { AllPrcCode = AllPrcCode + "," + PrcNo; }

                            NstPart = "0";
                            NstWtsqft = "0";
                            if (Dts[3].Trim().Substring(11, 1) == "1" || Dts[3].Trim().Substring(11, 1) == "0")
                                NstPart = ComCon.getTranName("select KitCode from Bomdetails where BOMCode='" + Dts[2].Trim() + "' and Kitcode Like '004%' and  substring(Kitcode,11,1) in ('4') and Partcode='" + Dts[3].Trim() + "'", "TblNstPartCode", "KitCode", con, tran);
                            else if (Dts[3].Trim().Substring(11, 1) == "6")
                                NstPart = ComCon.getTranName("select KitCode from Bomdetails where BOMCode='" + Dts[2].Trim() + "' and Kitcode Like '004%' and  substring(Kitcode,11,1) in ('4') group by KitCode ", "TblNstPartCode", "KitCode", con, tran);
                            else if (Dts[3].Trim().Substring(11, 1) == "2" || Dts[3].Trim().Substring(11, 1) == "3")
                            {
                                NstPart = Dts[3].Trim();
                                CpyStageType = "Line2";
                            }

                            NstWtsqft = ComCon.getTranName("Select convert(varchar(10),Pwt)+'-->'+convert(varchar(10),PSqft ) as PwtSqft from ProfitcenterPlDetails where ProfitcenterCode='01.007' and Partcode='" + NstPart + "'", "TblPwtSqft", "PwtSqft", con, tran);
                            string[] strNstWtsqft = Regex.Split(NstWtsqft.Trim(), "-->");

                            sb.Remove(0, sb.Length);
                            sb.Append("insert into processfeedback(GroupPFBCode,PFBCode,MaxSrNo,Dt,EDt,Yr,MachineCode,SerialNo,ProfitCenterCode,SupplierCode,CanopyPlanCode,ProductCode,TurretKitCode,");
                            sb.Append("PartCode,NestingForCode,NestingForQty,SqftPerUt,WtPerUt,PFBRate,ProcessQty,NstWtPerUt,NstSqftPerUt,CpyStageType,PPWCode,CompanyCode,Remark,CatID,PCCode_Act)");
                            sb.Append(" values('" + GrpPfbCode.Trim() + "','" + PrcNo.Trim() + "','" + (PrcNo.Substring(10, 8)) + "', ");
                            sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',Null,");
                            sb.Append("'" + ComCon.yearEnd(con, tran) + "','" + strMachineNo[0].ToString() + "','" + strMachineNo[1].ToString() + "','" + CpyPrcPCReq.PCCode.Trim() + "','" + CpyPrcPCReq.SupplierCode.Trim() + "',");
                            sb.Append("'" + Dts[0].Trim() + "','" + Dts[1].Trim() + "','" + Dts[2].Trim() + "','" + Dts[3].Trim() + "','" + NstPart.Trim() + "','" + Dts[4].Trim() + "', ");
                            sb.Append("'" + Dts[5].Trim() + "','" + Dts[6].Trim() + "','" + Dts[7].Trim() + "','" + Dts[9].Trim() + "',");
                            sb.Append("'" + double.Parse(strNstWtsqft[0].Trim()) + "','" + double.Parse(strNstWtsqft[1]) + "','" + CpyStageType + "',");
                            sb.Append("'" + CpyPrcPCReq.EmpCode.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim().Substring(0, 2) + "','Nil','" + Dts[13].Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "')");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync(cancellationToken);

                            sb.Remove(0, sb.Length);
                            sb.Append("INSERT INTO StockWIP(FromProfitCenterCode,PartCode,IssueCode,IssueDate,IssueQty,ToProfitCenterCode,StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                            sb.Append(" values('" + CpyPrcPCReq.PCCode.Trim() + "','" + Dts[3].ToString().Trim() + "',");
                            sb.Append("'" + PrcNo.Trim() + "',GetDate(),'" + double.Parse(Dts[9].ToString().Trim()) + "','" + CpyPrcPCReq.PCCode.Trim() + "',1,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "')");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync(cancellationToken);

                            string StrKVA = ComCon.getTranName("Select Kva from Part where Partcode='" + Dts[1].ToString().Trim() + "' and active='1'", "PartKVA", "Kva", con, tran).ToString().Trim();
                            // (original had a commented-out StockWIP insert for KVA<200 here — omitted)

                            // ---------- Details Entry ----------
                            SrNo = 1;   // original wrote 'SrNo = +1;' (unary plus) -> same value
                            sb.Remove(0, sb.Length);
                            sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,");
                            sb.Append("PFBRate,SaleRate,WtPerUt,SqftPerUt)");
                            sb.Append("values('" + PrcNo.Trim() + "','" + SrNo + "',");
                            sb.Append("'" + Dts[3].Trim() + "',");
                            sb.Append("'1','" + Convert.ToDouble(Dts[9].Trim()) + "',");
                            sb.Append("'" + Convert.ToDouble(Dts[8].Trim()) + "',");
                            sb.Append("'" + Convert.ToDouble(Dts[7].Trim()) + "',");
                            sb.Append("'" + Convert.ToDouble(Dts[6].Trim()) + "',");
                            sb.Append("'" + Convert.ToDouble(Dts[5].Trim()) + "')");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync(cancellationToken);

                            // ---------- PC Asskit and KitBelow 1000 ----------
                            string PrcBelowRate = "";
                            PCkitFlag = "No";

                            string GetMaxRatePartCode = ComCon.getTranName("Select top 1 PartCode From CanopyplandtsSub where CPCode='" + Dts[0].Trim() + "' and CpyPartcode='" + Dts[1].Trim() + "' and CatID='" + Dts[13].Trim() + "' order by rate desc ", "tbl_ChkForMaxRate", "PartCode", con, tran);

                            if (GetMaxRatePartCode.Trim() == Dts[3].Trim())
                            {
                                // ----- PC Asskit -----
                                PCkitFlag = "Yes";
                                DataSet dsPCKit = ComCon.procTranDS("Exec GetPCKit_NewERP '" + Dts[2].Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "' , '" + Dts[13].Trim() + "' ", "tbl_PCKit", con, tran);
                                if (dsPCKit != null && dsPCKit.Tables["tbl_PCKit"].Rows.Count > 0)
                                {
                                    for (int k = 0; k < dsPCKit.Tables["tbl_PCKit"].Rows.Count; k++)
                                    {
                                        sb.Remove(0, sb.Length);
                                        sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,SaleRate)");
                                        sb.Append("values('" + PrcNo.Trim() + "','" + (k + 2) + "',");
                                        sb.Append("'" + dsPCKit.Tables["tbl_PCKit"].Rows[k]["PartCode"].ToString().Trim() + "',");
                                        sb.Append("'" + dsPCKit.Tables["tbl_PCKit"].Rows[k]["Qty"].ToString().Trim() + "',");
                                        sb.Append("'" + double.Parse(dsPCKit.Tables["tbl_PCKit"].Rows[k]["Qty"].ToString().Trim()) * double.Parse(Dts[9].Trim()) + "',");
                                        sb.Append("'" + dsPCKit.Tables["tbl_PCKit"].Rows[k]["SuppRate"].ToString().Trim() + "')");
                                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                        await cmd.ExecuteNonQueryAsync(cancellationToken);

                                        sb.Remove(0, sb.Length);
                                        sb.Append("INSERT INTO StockWIP(FromProfitCenterCode,PartCode,IssueCode,IssueDate,IssueQty,ToProfitCenterCode,StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                        sb.Append(" values('" + CpyPrcPCReq.PCCode.Trim() + "','" + dsPCKit.Tables["tbl_PCKit"].Rows[k]["PartCode"].ToString().Trim() + "',");
                                        sb.Append("'" + PrcNo.Trim() + "',GetDate(),'" + double.Parse(dsPCKit.Tables["tbl_PCKit"].Rows[k]["Qty"].ToString().Trim()) * double.Parse(Dts[9].Trim()) + "','" + CpyPrcPCReq.PCCode.Trim() + "',1,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "')");
                                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                                    }
                                }

                                // ----- KitBelow 1000 -----
                                DataSet dsKitbelowRate = ComCon.procTranDS("select P.AliseName, CD.Partcode,(select Convert(nvarchar(10),PurRate)+'-->'+Convert(nvarchar(10),Rate)+'-->'+Convert(nvarchar(10),PWt)+'-->'+Convert(nvarchar(10),PSqft) " +
                                    " from ProfitcenterPLDetails where  ProfitcenterCode = '01.007' and Partcode=cd.partcode) as PartDts ," +
                                    " (select Round(Isnull(Sum(Recqty) - sum(IssueQty), 0), 00) as Stk From ( select Sum(ReceivedQty) as Recqty, " +
                                    " 0.00 as IssueQty from stockwip where ToProfitcenterCode_Act = '01.116' and StockType = '1' " +
                                    " and Partcode = cd.Partcode and  ReceivedQty > 0   Union all " +
                                    " select 0.00 as Recqty, sum(IssueQty) as IssueQty from stockwip where FromProfitCenterCode_Act = '01.116' and StockType = '1' " +
                                    "  and Partcode = cd.Partcode and  IssueQty > 0) as stk) as Stock " +
                                    "From CanopyPlanDtsSubBelowStdRate cd  INNER JOIN Part P ON CD.PartCode = P.PartCode where CpyPartcode='" + Dts[1].Trim() + "' and CPCode='" + Dts[0].Trim() + "' and CatID='" + Dts[13].Trim() + "'  ", "tbl_KitbelowRate", con, tran);
                                if (dsKitbelowRate != null && dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows.Count > 0)
                                {
                                    int ChkStk = 0;
                                    for (int br = 0; br < dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows.Count; br++)
                                    {
                                        string[] DtsBR = Regex.Split(dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["PartDts"].ToString().Trim(), "-->");

                                        if (Convert.ToDouble(Dts[9].Trim()) > Convert.ToDouble(dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Stock"].ToString().Trim()))
                                        {
                                            if (ChkStk == 0) { PrcNos = dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["AliseName"].ToString().Trim(); ChkStk = 1; }
                                            else { PrcNos = PrcNos + "," + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["AliseName"].ToString().Trim(); }
                                        }
                                        else if (ChkStk == 0)
                                        {
                                            PrcBelowRate = await GetMaxPrcAsync(con, tran, "ProcessFeedback", "PFbCode", ComCon.yearEnd(con, tran), CpyPrcPCReq.PCCode_Act.Trim().Substring(0, 2));

                                            sb.Remove(0, sb.Length);
                                            sb.Append("insert into processfeedback(GroupPFBCode,PFBCode,CpyStageType,MOFCode,MaxSrNo,Dt,EDt,Yr,MachineCode,SerialNo,ProfitCenterCode,SupplierCode,CanopyPlanCode,ProductCode,TurretKitCode,");
                                            sb.Append("PartCode,NestingForCode,NestingForQty,SqftPerUt,WtPerUt,PFBRate,ProcessQty,NstWtPerUt,NstSqftPerUt,PPWCode,CompanyCode,Remark,CatID,PCCode_Act) ");
                                            sb.Append(" values('" + GrpPfbCode.Trim() + "','" + PrcBelowRate.Trim() + "','" + CpyStageType + "','" + PrcNo.Trim() + "','" + (PrcBelowRate.Substring(10, 8)) + "', ");
                                            sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',Null,");
                                            sb.Append("'" + ComCon.yearEnd(con, tran) + "','" + strMachineNo[0].ToString() + "','" + strMachineNo[1].ToString() + "','" + CpyPrcPCReq.PCCode.Trim() + "','" + CpyPrcPCReq.SupplierCode.Trim() + "',");
                                            sb.Append("'" + Dts[0].Trim() + "','" + Dts[1].Trim() + "','" + Dts[2].Trim() + "','" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "','" + NstPart.Trim() + "','" + Dts[4].Trim() + "', ");
                                            sb.Append("'" + DtsBR[3].Trim() + "','" + DtsBR[2].Trim() + "','" + DtsBR[1].Trim() + "','" + Dts[9].Trim() + "',");
                                            sb.Append("'" + double.Parse(strNstWtsqft[0].Trim()) + "','" + double.Parse(strNstWtsqft[1].Trim()) + "',");
                                            sb.Append("'" + CpyPrcPCReq.EmpCode.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim().Substring(0, 2) + "','Nil','" + Dts[13].Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "')");
                                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync(cancellationToken);

                                            sb.Remove(0, sb.Length);
                                            sb.Append("INSERT INTO StockWIP(FromProfitCenterCode,PartCode,IssueCode,IssueDate,IssueQty,ToProfitCenterCode,StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                            sb.Append(" values('" + CpyPrcPCReq.PCCode.Trim() + "','" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "',");
                                            sb.Append("'" + PrcBelowRate.Trim() + "',GetDate(),'" + Dts[9].Trim() + "','01.007',1,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "' )");
                                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync(cancellationToken);

                                            sb.Remove(0, sb.Length);
                                            sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,");
                                            sb.Append("PFBRate,SaleRate,WtPerUt,SqftPerUt)");
                                            sb.Append("values('" + PrcBelowRate.Trim() + "','" + SrNo + "',");
                                            sb.Append("'" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "',");
                                            sb.Append("'1',");
                                            sb.Append("'" + Convert.ToDouble(Dts[9].Trim()) + "',");
                                            sb.Append("'" + Convert.ToDouble(DtsBR[0].Trim()) + "',");
                                            sb.Append("'" + Convert.ToDouble(DtsBR[1].Trim()) + "',");
                                            sb.Append("'" + Convert.ToDouble(DtsBR[2].Trim()) + "',");
                                            sb.Append("'" + Convert.ToDouble(DtsBR[3].Trim()) + "')");
                                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                                        }
                                    }

                                    if (ChkStk > 0)
                                    {
                                        PrcNos = "Insufficient Stock For Part(BR): " + PrcNos;
                                        await tran.RollbackAsync(cancellationToken);
                                        return PrcNos;
                                    }
                                }
                            }

                            if (PCkitFlag == "Yes")
                            {
                                sb.Remove(0, sb.Length);
                                sb.Append("Update ProcessFeedBack set CanopyCode='PCKit' where PFBCode='" + PrcNo.Trim() + "'and CatID='" + Dts[13].Trim() + "' ");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                            }

                            sb.Remove(0, sb.Length);
                            sb.Append("Update CanopyPlanDtsSub set CPPCQty=CPPCQty + '" + double.Parse(Dts[9].ToString().Trim()) + "' where CPCode='" + Dts[0].ToString().Trim() + "' and CpyPartcode='" + Dts[1].ToString().Trim() + "' and Partcode='" + Dts[3].ToString().Trim() + "' and CatID='" + Dts[13].ToString().Trim() + "' ");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync(cancellationToken);

                            string cntPrcQty = ComCon.getTranName("select CPQty-CPPCQty as BalQty from CanopyPlanDtsSub where  CPCode='" + Dts[0].ToString().Trim() + "' and CpyPartcode='" + Dts[1].ToString().Trim() + "' and Partcode='" + Dts[3].ToString().Trim() + "' and CatID='" + Dts[13].ToString().Trim() + "' ", "PCPrc", "BalQty", con, tran);
                            if (cntPrcQty == "0")
                            {
                                sb.Remove(0, sb.Length);
                                sb.Append("Update CanopyPlanDtsSub set CPPCStatus='D' where CPCode='" + Dts[0].ToString().Trim() + "' and CpyPartcode='" + Dts[1].ToString().Trim() + "' and Partcode='" + Dts[3].ToString().Trim() + "' and CatID='" + Dts[13].ToString().Trim() + "' ");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                            }

                            string cntPCStatus = ComCon.getTranName("select Count(CPPCStatus) as CPPCStatus from CanopyPlanDtsSub where CPCode='" + Dts[0].ToString().Trim() + "' and CpyPartcode='" + Dts[1].ToString().Trim() + "'  and CatID='" + Dts[13].ToString().Trim() + "' and  CPPCStatus='P'  ", "BendingPrc", "CPPCStatus", con, tran);

                            if (CpyPrcPCReq.PCCode_Act.Trim() == "28.016")
                            {
                                if (cntPCStatus == "0")
                                {
                                    if (CpyPrcPCReq.CatID.ToString() == "029")   // NOTE: request CatID here…
                                    {
                                        sb.Remove(0, sb.Length);
                                        sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode,IssueCode,IssueDate, IssueQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                        sb.Append(" values('" + Dts[1].ToString().Trim() + "','" + CpyPrcPCReq.PCCode.Trim() + "','28.016',");
                                        sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + Dts[9].ToString().Trim() + "',0,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "')");
                                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                        await cmd.ExecuteNonQueryAsync(cancellationToken);

                                        if (double.Parse(StrKVA.ToString().Trim()) <= 58.5)
                                        {
                                            sb.Remove(0, sb.Length);
                                            sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                            sb.Append(" values('" + Dts[1].ToString().Trim() + "','" + CpyPrcPCReq.PCCode.Trim() + "','28.017',");
                                            sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + Dts[9].ToString().Trim() + "',0,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "')");
                                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                                        }
                                        else if (double.Parse(StrKVA.ToString().Trim()) > 58.5)
                                        {
                                            sb.Remove(0, sb.Length);
                                            sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                            sb.Append(" values('" + Dts[1].ToString().Trim() + "','" + CpyPrcPCReq.PCCode.Trim() + "','28.017',");
                                            sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + Dts[9].ToString().Trim() + "',0,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "')");
                                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                                        }

                                        sb.Remove(0, sb.Length);
                                        sb.Append("Update CanopyPlanSerialNo set CPPCSerialStatus='D' where CPCode='" + Dts[0].ToString().Trim() + "' and Partcode='" + Dts[1].ToString().Trim() + "'  ");
                                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                                    }

                                    sb.Remove(0, sb.Length);
                                    sb.Append("Update ProcessFeedbackDetailsSub set PCStatus='D' where PFBBOTSerialNo='" + Dts[0].Trim().Trim() + "' and Partcode='" + Dts[1].ToString().Trim() + "' ");
                                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else
                            {
                                if (cntPCStatus == "0")
                                {
                                    if (Dts[13].ToString() == "029")
                                    {
                                        sb.Remove(0, sb.Length);
                                        sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode,IssueCode,IssueDate, IssueQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                        sb.Append(" values('" + Dts[1].ToString().Trim() + "','" + CpyPrcPCReq.PCCode.Trim() + "','01.007',");
                                        sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + Dts[9].ToString().Trim() + "',0,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','" + CpyPrcPCReq.PCCode_Act.Trim() + "')");
                                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                                    }

                                    if (Dts[13].ToString() == "029")
                                    {
                                        if (double.Parse(StrKVA.ToString().Trim()) <= 58.5)
                                        {
                                            sb.Remove(0, sb.Length);
                                            sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                            sb.Append(" values('" + Dts[1].ToString().Trim() + "','" + CpyPrcPCReq.PCCode.Trim() + "','01.093',");
                                            sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + Dts[9].ToString().Trim() + "',0,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','01.124')");
                                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                                        }
                                        else if (double.Parse(StrKVA.ToString().Trim()) > 58.5 && double.Parse(StrKVA.ToString().Trim()) <= 250)
                                        {
                                            sb.Remove(0, sb.Length);
                                            sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                            sb.Append(" values('" + Dts[1].ToString().Trim() + "','" + CpyPrcPCReq.PCCode.Trim() + "','01.093',");
                                            sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + Dts[9].ToString().Trim() + "',0,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','01.125')");
                                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                                        }
                                        else if (double.Parse(StrKVA.ToString().Trim()) > 250)
                                        {
                                            sb.Remove(0, sb.Length);
                                            sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                            sb.Append(" values('" + Dts[1].ToString().Trim() + "','" + CpyPrcPCReq.PCCode.Trim() + "','01.093',");
                                            sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + Dts[9].ToString().Trim() + "',0,'" + CpyPrcPCReq.PCCode_Act.Trim() + "','01.126')");
                                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                                        }

                                        sb.Remove(0, sb.Length);
                                        sb.Append("Update CanopyPlanSerialNo set CPPCSerialStatus='D' where CPCode='" + Dts[0].ToString().Trim() + "' and Partcode='" + Dts[1].ToString().Trim() + "'  ");
                                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                                    }

                                    sb.Remove(0, sb.Length);
                                    sb.Append("Update ProcessFeedbackDetailsSub set PCStatus='D' where PFBBOTSerialNo='" + Dts[0].Trim().Trim() + "' and Partcode='" + Dts[1].ToString().Trim() + "' ");
                                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }

                            // ---------- User Activity (PowderCoating Process) ----------
                            cmd = new SqlCommand("InsertLoginTransactionDetails", con) { Transaction = tran };
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EmpID", CpyPrcPCReq.EmpCode.Trim());
                            cmd.Parameters.AddWithValue("@TransactionType", "S");
                            cmd.Parameters.AddWithValue("@TransactionFrom", "PowderCoating Process");
                            cmd.Parameters.AddWithValue("@TransactionNo", PrcNo.Trim());
                            cmd.Parameters.AddWithValue("@CompanyCode", CpyPrcPCReq.PCCode_Act.Substring(0, 2).Trim());
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                        }
                        else if (Dts[10].Substring(0, 3) == "PSH")
                        {
                            Trans = "End";
                            if (cSub == 0) { AllPrcCode = Dts[10].Trim(); }
                            else { AllPrcCode = AllPrcCode + "," + Dts[10].Trim(); }

                            string StrKVA = ComCon.getTranName("Select Kva from Part where Partcode='" + Dts[1].ToString().Trim() + "' and active='1'", "PartKVA", "Kva", con, tran).ToString().Trim();
                            // (original had a commented-out StockWIP insert for KVA>=200 here — omitted)

                            GrpPfbCode = ComCon.getTranName("select GroupPfbCode   from ProcessFeedBack where PFBCode='" + Dts[10].Trim() + "' group By GroupPfbCode ", "TblPCPrc", "GroupPfbCode", con, tran);

                            sb.Remove(0, sb.Length);
                            sb.Append("Update ProcessFeedBack set EDt='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'  where PFBCode='" + Dts[10].Trim() + "' ");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync(cancellationToken);

                            string cntDatecQty = ComCon.getTranName("select count(Dt) as DT from ProcessFeedback where PFBCode='" + Dts[10].Trim() + "' and Dt='1900-01-01 00:00:00.000' ", "Tbl_Dt", "Dt", con, tran);
                            if (cntDatecQty != "0")
                            {
                                sb.Remove(0, sb.Length);
                                sb.Append("Update ProcessFeedBack set Dt='" + DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss") + "'  where PFBCode='" + Dts[10].Trim() + "' ");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                            }
                        }
                    }
                }

                // ---------- For Stk Matching ----------
                if (Trans == "Start")
                {
                    DataSet dsStk = ComCon.procTranDS("Exec GetPrcDtsForStk_NewERP '" + GrpPfbCode + "'", "tbl_PCKitForStk", con, tran);
                    if (dsStk != null && dsStk.Tables["tbl_PCKitForStk"].Rows.Count > 0)
                    {
                        int stk = 0;
                        for (int s = 0; s < dsStk.Tables["tbl_PCKitForStk"].Rows.Count; s++)
                        {
                            if (double.Parse(dsStk.Tables["tbl_PCKitForStk"].Rows[s]["PrcQty"].ToString().Trim()) >
                                (double.Parse(dsStk.Tables["tbl_PCKitForStk"].Rows[s]["Stock"].ToString().Trim()) + double.Parse(dsStk.Tables["tbl_PCKitForStk"].Rows[s]["PrcQty"].ToString().Trim())))
                            {
                                if (stk == 0) { AllPrcCode = dsStk.Tables["tbl_PCKitForStk"].Rows[s]["Partdesc"].ToString().Trim(); stk = 1; }
                                else if (stk > 0) { AllPrcCode = AllPrcCode + "," + dsStk.Tables["tbl_PCKitForStk"].Rows[s]["Partdesc"].ToString().Trim(); }
                            }
                        }
                        if (stk > 0)
                        {
                            AllPrcCode = "In sufficient Stock For Part: " + AllPrcCode;
                            await tran.RollbackAsync(cancellationToken);
                            return AllPrcCode;
                        }
                    }
                }

                // ---------- Action Taken File Attachment ----------
                if (Trans == "End")
                {
                    if (!string.IsNullOrEmpty(CpyPrcPCReq.AttachFileDts.ToString().Trim()))
                    {
                        string[] strPlanDts = Regex.Split(CpyPrcPCReq.AttachFileDts, "@#@");
                        int SrNoA = 0;
                        foreach (String StrSub in strPlanDts)
                        {
                            SrNoA += 1;
                            string[] DtsA = Regex.Split(StrSub.ToString().Trim(), "-->");

                            string FileName = GrpPfbCode.ToString().Trim().Substring(4, 5).Trim() + GrpPfbCode.ToString().Trim().Substring(10, 8).Trim() + "-" + (SrNoA) + Path.GetExtension(DtsA[1].ToString().Trim());
                            string StrMpath = ComCon.getMainFilePath("TempPrcPC") + "/" + FileName.ToString().Trim();
                            string StrTpath = "C:/TempERPFile/TempPrcPC/" + CpyPrcPCReq.EmpCode.Trim() + "/" + DtsA[1].ToString().Trim();
                            string StrTempPath = "C:/TempERPFile/TempPrcPC/" + CpyPrcPCReq.EmpCode.Trim();
                            if (Directory.Exists(StrTempPath) && File.Exists(StrTpath))
                            {
                                File.Copy(StrTpath, StrMpath);
                            }

                            sb.Remove(0, sb.Length);
                            sb.Append("INSERT INTO ProcessFeedbackFiles");
                            sb.Append("(GroupPFBCode,SrNo,FileName)");
                            sb.Append(" VALUES('" + GrpPfbCode.ToString().Trim() + "' ,'" + SrNoA + "','" + FileName.ToString().Trim() + "')");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }

                await tran.CommitAsync(cancellationToken);

                if (Trans == "Start") AllPrcCode = "ProcessCode =" + AllPrcCode + " For PowderCoating  Started SuccessFully ";
                else if (Trans == "End") AllPrcCode = "ProcessCode =" + AllPrcCode + " For PowderCoating  Ended SuccessFully ";

                return AllPrcCode;
            }
            catch (Exception ex)
            {
                if (tran != null) await tran.RollbackAsync(cancellationToken);
                return ("StackTrace " + ex.StackTrace.ToString() + " Message " + ex.Message.ToString());
            }
            finally
            {
                if (openedHere && con.State == ConnectionState.Open) await con.CloseAsync();
            }
        }

        private async Task<string> GetMaxPrcAsync(SqlConnection con, SqlTransaction tran,string tableName, string fieldName, string yr, string compCode,CancellationToken cancellationToken = default)
        {
            var sql = "select max(substring(" + fieldName + ",13,7)) as MX from " + tableName.Trim() +
                      " where yr='" + yr.Trim() + "' and CompanyCode='" + compCode.Trim() + "'";

            await using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            var scalar = await cmd.ExecuteScalarAsync(cancellationToken);

            int next = (scalar == null || scalar == DBNull.Value) ? 1 : Convert.ToInt32(scalar) + 1;
            string max = compCode + next.ToString().PadLeft(6, '0');
            return "PSH/" + yr + "/" + max;
        }

        public async Task<string> SubmitPowderCoatingCheckerAsync(CpyPrcPCCheckerRequest req, CancellationToken ct = default)
        {
            string PrcNo = "";
            string strReqCode = "";          // NOTE: never assigned here (parity with original message).
            string strReqCodeCPYAssly = "";  //       Actual generated code is strKanBan — see note below.
            string strKanBan = "";

            await using var con = new SqlConnection(_connStr);
            await con.OpenAsync(ct);
            await using var tran = (SqlTransaction)await con.BeginTransactionAsync(ct);

            try
            {
                var strPlanDts = Regex.Split(req.ProductionDetails ?? "", "@@#@@");

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
                    // Bounds check (DtsPlan.Length > 3) preserved from the original PC fix.
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

                    // ---- any powder-coating rows still unchecked for this plan / product / cat / PC? ----
                    // DIFFERENCE: filters on ProfitCenterCode (Fab used PCCode_Act).
                    var cntPCStatus = await ComCon.GetScalarAsync(
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

                    if (cntPCStatus == "0")
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

                            // DIFFERENCE: no PCCode_Act column; ProfitCenterCode = req.PCCode.
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
                                ("@Remark", "Auto Req For Plan No: " + req.ProductCode + " and Prc No: " + req.PFBCode));

                            int SrNoReq = 0;
                            foreach (DataRow row in kanTable.Rows)
                            {
                                SrNoReq++;
                                await ExecProcAsync(con, tran, ct, "insertMaterialRequisitionWithOutPlanDetails",
                                    ("@REQCode", strKanBan),
                                    ("@SrNo", SrNoReq),
                                    ("@PartCode", row["Partcode"].ToString()!.Trim()),
                                    ("@Qty", double.Parse(row["RaiseReqQty"].ToString()!.Trim())),
                                    ("@REQStatus", "P"));
                            }
                        }

                        // ---- activity log (runs even if KanBan had no rows -> matches original) ----
                        await ExecProcAsync(con, tran, ct, "insertLoginTransactionDetails",
                            ("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                            ("@EmpID", req.EmpCode),
                            ("@TransactionType", "S"),
                            ("@TransactionFrom", "MaterialRequisitionWithoutPlan"),
                            ("@TransactionNo", strKanBan),
                            ("@CompanyCode", req.PCCode_Act.Substring(0, 2).Trim()));

                        await tran.CommitAsync(ct);
                        PrcNo = "ProcessCode:" + req.PFBCode.Trim() + " and ReqCode: " + strReqCode + "," + strReqCodeCPYAssly + " For Powder Coting  Saved SuccessFully ";
                        return PrcNo;
                    }

                    await tran.CommitAsync(ct);
                    PrcNo = "ProcessCode=" + req.PFBCode.Trim() + " For Powder Coting  Saved SuccessFully ";
                    return PrcNo;
                }
                else
                {
                    // ---- Status is NOT "AUTH" (REJECT) ----
                    // NOTE: no bounds check here (matches original); reads DtsPlan[1..4],
                    // so each line must split into at least 5 segments or this throws.
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
                            // NOTE: original PC code literally said "Fabrication Checker" here
                            // (copy-paste leftover). Kept for parity — change if that label is wrong.
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
                    PrcNo = "ProcessCode:" + req.PFBCode.Trim() + " For Powder Coting Saved SuccessFully ";
                    return PrcNo;
                }
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync(ct);
                return "StackTrace " + ex.StackTrace + " Message " + ex.Message;
            }
        }

        // ---- helpers (same as Fabrication service) ----
        private static async Task ExecNonQueryAsync(SqlConnection con, SqlTransaction tran, CancellationToken ct,string sql, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(sql, con, tran);
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static async Task ExecProcAsync( SqlConnection con, SqlTransaction tran, CancellationToken ct,string procName, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(procName, con, tran) { CommandType = CommandType.StoredProcedure };
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
