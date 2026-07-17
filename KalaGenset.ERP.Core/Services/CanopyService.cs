using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.ResponseDTO;

using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using static Azure.Core.HttpHeader;
using CanopyPlan = KalaGenset.ERP.Data.Models.CanopyPlan;
using CanopyPlanDtsSub = KalaGenset.ERP.Data.Models.CanopyPlanDtsSub;
using CanopyPlanDtsSubBelowStdRate = KalaGenset.ERP.Data.Models.CanopyPlanDtsSubBelowStdRate;
using CorporateRequisition = KalaGenset.ERP.Data.Models.CorporateRequisition;
using MaterialRequisitionWithOutPlan = KalaGenset.ERP.Data.Models.MaterialRequisitionWithOutPlan;


namespace KalaGenset.ERP.Core.Services
{
    public class CanopyService : ICanopy
    {
        private readonly KalaDbContext _db;
        private readonly string _connStr;
        private readonly CommonCon _com;

        public CanopyService(KalaDbContext context, ICommonService common, ILogger<CanopyService> logger, IConfiguration config, CommonCon com)
        {
            _db = context;
            _connStr = config.GetConnectionString("KalaDbContext")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _com = com;
        }

        public async Task<List<Dictionary<string, object>>> GetCanopyPlanAsync(string strJobCardType, string lineWisePC)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetJobCard_Cpy_PlanDts_ERPNEW";
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

        public async Task<List<Dictionary<string, object>>> GetLineByProcessAsync(string ProcessName, string compCode)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetLineByProcess_ERPNEW";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@ProcessName", ProcessName));
                    cmd.Parameters.Add(new SqlParameter("@CompCode",
                        string.IsNullOrEmpty(compCode) ? (object)DBNull.Value : compCode));

                    if (conn.State == ConnectionState.Closed)
                        await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                                row[reader.GetName(i)] =
                                    reader.IsDBNull(i) ? null : reader.GetValue(i);

                            data.Add(row);
                        }
                    }
                }
            }

            return data;
        }

        public async Task<string> SubmitAsync(JobCard_CpyRequest job_Cpyreq)
        {
            DataSet dsDetailsSub;
            DataSet dsTurretKitForPrc;
            DataSet dsTurretKitGKForPrc;
            string StrDisplayMsg = "";
            string StrDispCode_CPYPlan = "";
            string[] strPlanDts;
            string[] Dts;
            int SrNo;
            string ParentPart = "";

            if (string.IsNullOrEmpty(job_Cpyreq.JobCard_CpyDts))
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
                StrDispCode_CPYPlan = _com.GetMaxNo("CanopyPlan", "CPY", job_Cpyreq.CompCode.Trim(), con, tran);

                string CurrentMnth = _com.getName("select Mon=case MONTH(GETDATE()) " +
                                                        "when 1 then '01' when 2 then '02' when 3 then '03' " +
                                                        "when 4 then '04' when 5 then '05' when 6 then '06' " +
                                                        "when 7 then '07' when 8 then '08' when 9 then '09' " +
                                                        "when 10 then '10' when 11 then '11' when  12 then '12' end", "tblM", "Mon");

                // ---------- For Mst Save ----------
                cmd = new SqlCommand("InsertCanopyPlan_ERPNEW", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CPCode", StrDispCode_CPYPlan.Trim());
                cmd.Parameters.AddWithValue("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                cmd.Parameters.AddWithValue("@MaxSrNo", StrDispCode_CPYPlan.Substring(10, 8).Trim());
                cmd.Parameters.AddWithValue("@Yr", StrDispCode_CPYPlan.Substring(4, 5).Trim());
                cmd.Parameters.AddWithValue("@FromDt", DateTime.Now.ToString("yyyy-MM-dd 00:00:00"));
                cmd.Parameters.AddWithValue("@ToDt", DateTime.Now.ToString("yyyy-MM-dd 00:00:00"));
                cmd.Parameters.AddWithValue("@PlanPCCode", job_Cpyreq.PCCode.Trim());
                cmd.Parameters.AddWithValue("@PCCode_Act", job_Cpyreq.PCCode_Act.Trim());
                cmd.Parameters.AddWithValue("@CompanyCode", job_Cpyreq.CompCode.Trim());
                cmd.Parameters.AddWithValue("@PlanType", "G");
                cmd.Parameters.AddWithValue("@AutoFlg", "Yes");
                cmd.Transaction = tran;
                await cmd.ExecuteNonQueryAsync();
                await cmd.DisposeAsync();

                // ---------- For CPY Plan Details Save ----------
                strPlanDts = Regex.Split(job_Cpyreq.JobCard_CpyDts, "@@#@@");
                SrNo = 0;

                foreach (string StrSub in strPlanDts)
                {
                    SrNo += 1;
                    Dts = Regex.Split(StrSub.ToString().Trim(), "@#@");

                    // ----- Dts Save -----
                    cmd = new SqlCommand("InsertCanopyPlanDetails", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CPCode", StrDispCode_CPYPlan.Trim());
                    cmd.Parameters.AddWithValue("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                    cmd.Parameters.AddWithValue("@SrNo", SrNo);
                    cmd.Parameters.AddWithValue("@PartCode", Dts[2].ToString().Trim());
                    cmd.Parameters.AddWithValue("@BomCode", Dts[10].ToString().Trim());

                    string PartCodeWOP = _com.getTranName(
                        "SELECT Partcode FROM BOMdetails where BOMCode='" + Dts[10].ToString().Trim() + "' " +
                        "AND KitCode='" + Dts[2].ToString().Trim() + "' AND Partcode like '004%' ",
                        "tblBP", "Partcode", con, tran);

                    cmd.Parameters.AddWithValue("@PartCodeWOP", PartCodeWOP.Trim());
                    cmd.Parameters.AddWithValue("@Qty", int.Parse(Dts[8].ToString().Trim()));
                    cmd.Parameters.AddWithValue("@PlanCode", Dts.Length > 11 ? Dts[11].ToString().Trim() : "");

                    DateTime planDate = DateTime.MinValue;
                    if (Dts.Length > 12 && !string.IsNullOrEmpty(Dts[12].ToString().Trim()))
                    {
                        if (DateTime.TryParse(Dts[12].ToString().Trim(), out planDate))
                            cmd.Parameters.AddWithValue("@PlanDate", planDate.ToString("yyyy-MM-dd"));
                        else
                            cmd.Parameters.AddWithValue("@PlanDate", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PlanDate", DBNull.Value);
                    }

                    int dayPlanQty = 0;
                    if (Dts.Length > 13 && int.TryParse(Dts[13].ToString().Trim(), out dayPlanQty))
                        cmd.Parameters.AddWithValue("@DayPlanQty", dayPlanQty);
                    else
                        cmd.Parameters.AddWithValue("@DayPlanQty", 0);

                    // cmd.Parameters.AddWithValue("@ShiftType", job_Cpyreq.ShiftType.ToString().Trim());
                    cmd.Transaction = tran;
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();

                    string TxtCpBoxCode = _com.getName(
                        "select partcode from BomDetails where BomCode='" + Dts[10].ToString().Trim() +
                        "' and KitCode Like '003%' and PartCode LIKE '004%'", "tblCpy1", "partcode");

                    // ----- SrNo loop -----
                    for (int d = 0; d < int.Parse(Dts[8].ToString().Trim()); d++)
                    {
                        int GetCpyMax = Convert.ToInt32(_com.getTranName("SELECT ISNULL(MaxValue,0) as MaxValue FROM getMaxSerialNo WHERE CompCode='01' AND Prefix='CPY' AND Yr='" + _com.yearEnd(con, tran) + "' ", "tblMx", "MaxValue", con, tran));
                        int GetBfmMax = Convert.ToInt32(_com.getTranName("SELECT ISNULL(MaxValue,0) as MaxValue FROM getMaxSerialNo WHERE CompCode='01' AND Prefix='BFM' AND Yr='" + _com.yearEnd(con, tran) + "' ", "tblMx", "MaxValue", con, tran));
                        int GetFltMax = Convert.ToInt32(_com.getTranName("SELECT ISNULL(MaxValue,0) as MaxValue FROM getMaxSerialNo WHERE CompCode='01' AND Prefix='FTK' AND Yr='" + _com.yearEnd(con, tran) + "' ", "tblMx", "MaxValue", con, tran));
                        string strCpymax = "0", strBfmmax = "0", strFtkmax = "0";
                        string CpySerialNo = "", BfmSerialNo = "", FtkSerialNo = "";

                        // Cpy
                        if (GetCpyMax == 0) strCpymax = "0001";
                        else if (GetCpyMax <= 9) strCpymax = "000" + (GetCpyMax + 1);
                        else if (GetCpyMax <= 99) strCpymax = "00" + (GetCpyMax + 1);
                        else if (GetCpyMax <= 999) strCpymax = "0" + (GetCpyMax + 1);
                        else strCpymax = Convert.ToString(GetCpyMax + 1);

                        CpySerialNo = "CPY" + DateTime.Now.Year.ToString().Substring(2, 2) + CurrentMnth.Trim() + "01" + strCpymax;
                        if (double.Parse(strCpymax.Trim()) > 0)
                        {
                            sb.Clear();
                            sb.Append("UPDATE getMaxSerialNo SET MaxValue='" + (GetCpyMax + 1) + "' WHERE ");
                            sb.Append("CompCode='01' AND Yr='" + _com.yearEnd(con, tran) + "' AND Prefix='CPY' ");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();
                        }

                        // Base
                        if (GetBfmMax == 0) strBfmmax = "0001";
                        else if (GetBfmMax <= 9) strBfmmax = "000" + (GetBfmMax + 1);
                        else if (GetBfmMax <= 99) strBfmmax = "00" + (GetBfmMax + 1);
                        else if (GetBfmMax <= 999) strBfmmax = "0" + (GetBfmMax + 1);
                        else strBfmmax = Convert.ToString(GetBfmMax + 1);

                        BfmSerialNo = "BFM" + DateTime.Now.Year.ToString().Substring(2, 2) + CurrentMnth.Trim() + "01" + strBfmmax;
                        if (double.Parse(strBfmmax.Trim()) > 0)
                        {
                            sb.Clear();
                            sb.Append("UPDATE getMaxSerialNo SET MaxValue='" + (GetBfmMax + 1) + "' WHERE ");
                            sb.Append("CompCode='01' AND Yr='" + _com.yearEnd(con, tran) + "' AND Prefix='BFM' ");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();
                        }

                        // Fuel Tank
                        if (GetFltMax == 0) strFtkmax = "0001";
                        else if (GetFltMax <= 9) strFtkmax = "000" + (GetFltMax + 1);
                        else if (GetFltMax <= 99) strFtkmax = "00" + (GetFltMax + 1);
                        else if (GetFltMax <= 999) strFtkmax = "0" + (GetFltMax + 1);
                        else strFtkmax = Convert.ToString(GetFltMax + 1);

                        FtkSerialNo = "FTK" + DateTime.Now.Year.ToString().Substring(2, 2) + CurrentMnth.Trim() + "01" + strFtkmax;
                        if (double.Parse(strFtkmax.Trim()) > 0)
                        {
                            sb.Clear();
                            sb.Append("UPDATE getMaxSerialNo SET MaxValue='" + (GetFltMax + 1) + "' WHERE ");
                            sb.Append("CompCode='01' AND Yr='" + _com.yearEnd(con, tran) + "' AND Prefix='FTK' ");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();
                        }

                        sb.Clear();
                        sb.Append("Insert Into CanopyPlanSerialNo(CPCode, SrNo, PartCode, SerialNo,BFMSrNo,FLKSrNo,Status,QPCStatus, RWStatus) ");
                        sb.Append("Values('" + StrDispCode_CPYPlan.Trim() + "','" + (d + 1) + "','" + Dts[2].ToString().Trim() + "',");
                        sb.Append("'" + CpySerialNo.Trim() + "','" + BfmSerialNo.Trim() + "','" + FtkSerialNo.Trim() + "','P','OK','OK')");
                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();
                    }

                    // ----- TurretKit Mst + Dts -----
                    sb.Clear();
                    sb.Append("insert Into TurretKitForPrc(CPCode,BOMCode,TKITID,CanopyPartCode,TurretKitPartcode,SheetPartCode,KITType,TLength,TWidth,TThickness,SerialNo,SerialQty,CompCode,CatID) ");
                    sb.Append("select S.CPCode,S.BomCode,S.TKITID,S.CanopyPartCode,S.TurretKitPartcode,S.SheetPartCode,S.KITType,S.TLength	,S.TWidth,S.TThickness,	S.SerialNo,	S.SerialQty,S1.CompCode,s1.CatID  from (select '" + StrDispCode_CPYPlan.Trim() + "' as CPCode ,'" + Dts[10].ToString().Trim() + "'  as BomCode ,");
                    sb.Append("TKITID,'" + Dts[2].ToString().Trim() + "'  as CanopyPartCode,CanopyPartCode as TurretKitPartcode,SheetPartCode,KITType,TLength,TWidth,TThickness,SerialNo,SerialQty,CatID ");
                    sb.Append("from TurretKit where Auth='1' and Canopypartcode in ( select Partcode from BomDetails where BOMCode='" + Dts[10].ToString().Trim() + "' ");
                    sb.Append("and substring(partcode,1,4) in ('0121','0122') and substring(partcode,12,2) in ('15','25','35','45'))) as S" +
                        " inner join ( select  count(PPM.BracketID) as B ,PPM.CatID ,PPM.Location as CompCode  from ProductionPlanMaster PPM  where PPM.Active='1' " +
                        "group by PPM.Location,PPM.CatID) as S1 on S.CatID =S1.CatID  order By TKITid ");
                    cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();

                    string PrcNo = "";
                    sb.Clear();
                    sb.Append("insert Into TurretKitForPrcDts(CPCode,TKITID,SrNo,PartCode,Qty,TLength,TWidth,THeight,TTHickness,TLossWt,TLength1,TLength2,TWidth1,TWidth2,TLosssqft,TCatagorycode) ");
                    sb.Append("select '" + StrDispCode_CPYPlan.Trim() + "' as CPCode,TKITID,SrNo,PartCode,Qty,TLength,TWidth,THeight,TTHickness,TLossWt,TLength1,TLength2,");
                    sb.Append("TWidth1,TWidth2,TLosssqft,TCatagorycode from TurretKitdetails where TKITid in ( select TKITid ");
                    sb.Append("from TurretKit where Auth='1' and Canopypartcode in ( select Partcode from BomDetails where BOMCode='" + Dts[10].ToString().Trim() + "' and substring(partcode,1,4) in ('0121','0122') and substring(partcode,12,2) in ('15','25','35','45')))  order By TKITid");
                    cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();

                    dsTurretKitForPrc = _com.procTranDS("SELECT COUNT(TKITID) AS TKITID  FROM TurretKitForPrcDts WHERE TKITID IN (" +
                                                              "SELECT TKITID FROM TurretKitForPrc " +
                                                              "WHERE cpcode = '" + StrDispCode_CPYPlan.Trim() + "' " +
                                                              "AND CanopyPartCode = '" + Dts[2].ToString().Trim() + "'" +
                                                          ") " +
                                                          "AND PartCode IN (" +
                                                              "SELECT bdd.PartCode  FROM BOMDetails bdm " +
                                                              "INNER JOIN BOMDetails bdd ON bdm.PartCode = bdd.KitCode " +
                                                              "WHERE bdm.BOMCode = '" + Dts[10].ToString().Trim() + "' " +
                                                              "AND bdm.KITCode LIKE '003%' AND bdm.PartCode LIKE '004%' AND bdd.PartCode LIKE '004%' GROUP BY bdd.PartCode" +
                                                          ")",
                                                          "tbl_TurretKitForPrcb", con, tran);

                    int count = Convert.ToInt32(dsTurretKitForPrc.Tables["tbl_TurretKitForPrcb"].Rows[0]["TKITID"]);
                    double kva = 0;
                    if (Dts[0] != null && !Convert.IsDBNull(Dts[0]))
                        double.TryParse(Dts[0].ToString().Trim(), out kva);

                    if (count == 0 && kva >= 10 && kva <= 25)
                    {
                        dsTurretKitGKForPrc = _com.procTranDS("SELECT P.rating, B.BOMCode, B.PartCode " +
                                                                  "FROM BOM B " +
                                                                  "INNER JOIN part P ON B.partcode = P.partcode " +
                                                                  "WHERE B.BOMCode = '" + Dts[10].ToString().Trim() + "' " +
                                                                  "AND B.partcode  LIKE '101%' and  P.rating='GK' ",
                                                                  "tbl_BOMPartRating", con, tran);

                        bool hasGKRating = dsTurretKitGKForPrc.Tables["tbl_BOMPartRating"].Rows.Count > 0;
                        if (Dts[0] != null && !Convert.IsDBNull(Dts[0]))
                            double.TryParse(Dts[0].ToString().Trim(), out kva);

                        if (!hasGKRating && count == 0 && kva < 750)
                        {
                            PrcNo = "Nesting kit not saved!  '" + Dts[0].ToString().Trim() + "' KVA ";
                            return PrcNo; // 'await using' disposes con -> open transaction is rolled back
                        }
                    }

                    // ----- For DtsSub Save -----
                    if (kva < 750)
                    {
                        dsDetailsSub = _com.procTranDS(" select s.Partcode, s.PartDesc, s.Rate, s.KVA, s.Strokes,s1.CompCode,S1.CatID from " +
                            "(select DISTINCT  bd.Partcode,p.PartDesc," +
                            "(select Rate from ProfitcenterPLDetails where Partcode=bd.Partcode and ProfitcenterCode = '01.007') as Rate,  P1.KVA ," +
                               "isNull((select isNull(Max(MarAMT),0) as Strokes from ProfitcenterPLDetails where Partcode=bd.Partcode and ProfitcenterCode = '01.002'),0) as Strokes" +
                               " ,BM.BID as BracketId " +
                               " , CASE WHEN SUBSTRING(bd.Partcode, 12, 1) = '1' THEN '029' " +
                               " WHEN SUBSTRING(bd.Partcode, 12, 1) = '2' THEN '084'" +
                               " WHEN SUBSTRING(bd.Partcode, 12, 1) = '3' THEN '038'" +
                               " ELSE 'other'END AS CatCode" +
                               ",CASE WHEN SUBSTRING(bd.Partcode, 12, 1) = '1' THEN 'cpy'" +
                               " WHEN SUBSTRING(bd.Partcode, 12, 1) = '2' THEN 'BF'" +
                               " WHEN SUBSTRING(bd.Partcode, 12, 1) = '3' THEN 'FT' " +
                               " ELSE 'other'END AS CatName " +
                               " from BOM B Inner Join BOMDetails Bd on B.BOMCode = Bd.BOMCode " +
                               " inner Join Part P on bd.Partcode = P.Partcode " +
                               " inner Join Part P1 on b.Partcode = P1.Partcode	 " +
                               " inner Join BracketMst BM   ON p1.kva BETWEEN BM.fromkva AND BM.tokva " +
                               " where B.BOMCode = '" + Dts[10].ToString().Trim() + "' and " +
                               " B.Active = '1' and B.Auth = '1' and p.Kit = '1' and Bd.MOB = 'M' and  Bd.KitCode like '004%' and substring(Bd.KitCode,11,1) in ('4','5') and Bd.Partcode like '004%' ) as S  " +
                               " inner join ( select  count(PPM.BracketID) as B ,PPM.BracketID,PPM.CatID ,PPM.Location as CompCode from ProductionPlanMaster PPM" +
                               " where PPM.Active = '1' group by PPM.Location, PPM.BracketID, PPM.CatID) as S1 on S.CatCode = S1.CatID and S.BracketId = S1.BracketID" +
                                 " Union all " +
                                  " select s.Partcode, s.PartDesc, s.Rate, s.KVA, s.Strokes, '01' as CompCode, '029' as CatID from " +
                                  " (select DISTINCT  bd.Partcode, p.PartDesc, " +
                                  " (select Rate from ProfitcenterPLDetails where Partcode = bd.Partcode " +
                                  " and ProfitcenterCode = '01.007') as Rate, P1.KVA, " +
                                  " isNull((select isNull(Max(MarAMT), 0) as Strokes " +
                                  " from ProfitcenterPLDetails where Partcode = bd.Partcode and ProfitcenterCode = '01.002'), 0) as Strokes " +
                                  " from BOM B Inner Join BOMDetails Bd on B.BOMCode = Bd.BOMCode " +
                                  " inner Join Part P on bd.Partcode = P.Partcode " +
                                  " inner Join Part P1 on b.Partcode = P1.Partcode " +
                                  " where B.BOMCode = '" + Dts[10].ToString().Trim() + "'  and  " +
                                  " B.Active = '1' and B.Auth = '1' and p.Kit = '1' and Bd.MOB = 'M'  " +
                                  " and substring(Bd.KitCode,1,3) in ('003') and Bd.Partcode like '004%' ) as S  ", "tbl_RaiseReqDtsSub", con, tran);
                    }
                    else
                    {
                        dsDetailsSub = _com.procTranDS(" select s.Partcode, s.PartDesc, s.Rate, s.KVA, s.Strokes,s1.CompCode,S1.CatID from " +
                            "(select DISTINCT  bd.Partcode,p.PartDesc," +
                            "(select Rate from ProfitcenterPLDetails where Partcode=bd.Partcode and ProfitcenterCode = '01.007') as Rate,  P1.KVA ," +
                               "isNull((select isNull(Max(MarAMT),0) as Strokes from ProfitcenterPLDetails where Partcode=bd.Partcode and ProfitcenterCode = '01.002'),0) as Strokes" +
                               " ,BM.BID as BracketId " +
                               " , CASE WHEN SUBSTRING(bd.Partcode, 12, 1) = '1' THEN '029' " +
                               " WHEN SUBSTRING(bd.Partcode, 12, 1) = '2' THEN '084'" +
                               " WHEN SUBSTRING(bd.Partcode, 12, 1) = '3' THEN '038'" +
                               " ELSE 'other'END AS CatCode" +
                               ",CASE WHEN SUBSTRING(bd.Partcode, 12, 1) = '1' THEN 'cpy'" +
                               " WHEN SUBSTRING(bd.Partcode, 12, 1) = '2' THEN 'BF'" +
                               " WHEN SUBSTRING(bd.Partcode, 12, 1) = '3' THEN 'FT' " +
                               " ELSE 'other'END AS CatName " +
                               " from BOM B Inner Join BOMDetails Bd on B.BOMCode = Bd.BOMCode " +
                               " inner Join Part P on bd.Partcode = P.Partcode " +
                               " inner Join Part P1 on b.Partcode = P1.Partcode	 " +
                               " inner Join BracketMst BM   ON p1.kva BETWEEN BM.fromkva AND BM.tokva " +
                               " where B.BOMCode = '" + Dts[10].ToString().Trim() + "' and " +
                               " B.Active = '1' and B.Auth = '1' and p.Kit = '1' and Bd.MOB = 'M' and  Bd.KitCode like '004%' and substring(Bd.KitCode,11,1) in ('4','5') and Bd.Partcode like '004%' ) as S  " +
                               " inner join ( select  count(PPM.BracketID) as B ,PPM.BracketID,PPM.CatID ,PPM.Location as CompCode from ProductionPlanMaster PPM" +
                               " where PPM.Active = '1' group by PPM.Location, PPM.BracketID, PPM.CatID) as S1 on S.CatCode = S1.CatID and S.BracketId = S1.BracketID", "tbl_RaiseReqDtsSub", con, tran);
                    }

                    if (dsDetailsSub.Tables["tbl_RaiseReqDtsSub"].Rows.Count > 0)
                    {
                        for (int m = 0; m < dsDetailsSub.Tables["tbl_RaiseReqDtsSub"].Rows.Count; m++)
                        {
                            var row = dsDetailsSub.Tables["tbl_RaiseReqDtsSub"].Rows[m];

                            if (double.Parse(row["Strokes"].ToString().Trim()) > 0)
                            {
                                if (double.Parse(row["Rate"].ToString().Trim()) >= 1000)
                                {
                                    sb.Clear();
                                    sb.Append("insert Into CanopyPlanDtsSub(CPCode,CpyPartCode,SrNo,PartCode,CPQty,Rate,Strokes,CompCode,CatID)");
                                    sb.Append(" values ('" + StrDispCode_CPYPlan.Trim() + "','" + Dts[2].ToString().Trim() + "'," + (m + 1) + " ,");
                                    sb.Append(" '" + row["Partcode"].ToString().Trim() + "','" + int.Parse(Dts[8].ToString().Trim()) + "',");
                                    sb.Append("'" + double.Parse(row["Rate"].ToString().Trim()) + "','" + double.Parse(row["Strokes"].ToString().Trim()) + "' ,'" + row["CompCode"].ToString().Trim() + "' ,'" + row["CatID"].ToString().Trim() + "')");
                                    cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                                    await cmd.ExecuteNonQueryAsync();
                                    await cmd.DisposeAsync();

                                    // ----- OS Fab Plan -----
                                    if (int.Parse(Dts[8].ToString().Trim()) >= 1)
                                    {
                                        string curPart = row["Partcode"].ToString().Trim();
                                        string p11 = curPart.Substring(11, 1);

                                        if (p11 == "1" || p11 == "0" || p11 == "6") // CPY
                                        {
                                            ParentPart = _com.getTranName("select PartCode  as ParentPart from BOMDetails where KitCode='" + PartCodeWOP.Trim() + "' " +
                                             " and BOMCode = '" + Dts[10].ToString().Trim() + "'  " +
                                             " and Partcode like '004%' and substring(Partcode, 11, 1) = '4' ", "tblParentPart", "ParentPart", con, tran);

                                            sb.Clear();
                                            sb.Append("insert Into CanopyPlanOSDetails(CPCode,CpyPartCode,SrNo,PartCode,Scode,Qty,OSFqty,OSFStatus,ParentPart)");
                                            sb.Append(" values ('" + StrDispCode_CPYPlan.Trim() + "','" + Dts[2].ToString().Trim() + "'," + (m + 1) + " ,");
                                            sb.Append(" '" + curPart + "','02.13.01.01.23.0001','" + (double.Parse(Dts[8].ToString().Trim()) / 2) + "',");
                                            sb.Append("'0','P','" + ParentPart.Trim() + "' )");
                                            cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync();
                                            await cmd.DisposeAsync();

                                            sb.Clear();
                                            sb.Append("insert Into CanopyPlanOSDetails(CPCode,CpyPartCode,SrNo,PartCode,Scode,Qty,OSFqty,OSFStatus,ParentPart)");
                                            sb.Append(" values ('" + StrDispCode_CPYPlan.Trim() + "','" + Dts[2].ToString().Trim() + "'," + (m + 1) + " ,");
                                            sb.Append(" '" + curPart + "','02.04.01.01.01.0573','" + (double.Parse(Dts[8].ToString().Trim()) / 2) + "',");
                                            sb.Append("'0','P','" + ParentPart.Trim() + "' )");
                                            cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync();
                                            await cmd.DisposeAsync();
                                        }
                                        else if (p11 == "2") // BF
                                        {
                                            sb.Clear();
                                            sb.Append("insert Into CanopyPlanOSDetails(CPCode,CpyPartCode,SrNo,PartCode,Scode,Qty,OSFqty,OSFStatus,ParentPart)");
                                            sb.Append(" values ('" + StrDispCode_CPYPlan.Trim() + "','" + Dts[2].ToString().Trim() + "'," + (m + 1) + " ,");
                                            sb.Append(" '" + curPart + "','02.01.01.01.01.0305','" + (double.Parse(Dts[8].ToString().Trim()) / 2) + "',");
                                            sb.Append("'0','P','" + curPart + "' )");
                                            cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync();
                                            await cmd.DisposeAsync();

                                            sb.Clear();
                                            sb.Append("insert Into CanopyPlanOSDetails(CPCode,CpyPartCode,SrNo,PartCode,Scode,Qty,OSFqty,OSFStatus,ParentPart)");
                                            sb.Append(" values ('" + StrDispCode_CPYPlan.Trim() + "','" + Dts[2].ToString().Trim() + "'," + (m + 1) + " ,");
                                            sb.Append(" '" + curPart + "','02.01.01.01.23.0009','" + (double.Parse(Dts[8].ToString().Trim()) / 2) + "',");
                                            sb.Append("'0','P','" + curPart + "' )");
                                            cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync();
                                            await cmd.DisposeAsync();
                                        }
                                        else if (p11 == "3") // FT
                                        {
                                            sb.Clear();
                                            sb.Append("insert Into CanopyPlanOSDetails(CPCode,CpyPartCode,SrNo,PartCode,Scode,Qty,OSFqty,OSFStatus,ParentPart)");
                                            sb.Append(" values ('" + StrDispCode_CPYPlan.Trim() + "','" + Dts[2].ToString().Trim() + "'," + (m + 1) + " ,");
                                            sb.Append(" '" + curPart + "','02.01.01.01.01.0305','" + (double.Parse(Dts[8].ToString().Trim())) + "',");
                                            sb.Append("'0','P','" + curPart + "' )");
                                            cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                                            await cmd.ExecuteNonQueryAsync();
                                            await cmd.DisposeAsync();
                                        }
                                    }
                                }
                                else if (double.Parse(row["Rate"].ToString().Trim()) < 1000)
                                {
                                    sb.Clear();
                                    sb.Append("insert Into CanopyPlanDtsSubBelowStdRate(CPCode,CpyPartCode,SrNo,PartCode,CPQty,Rate,Strokes,CompCode,CatID)");
                                    sb.Append(" values ('" + StrDispCode_CPYPlan.Trim() + "','" + Dts[2].ToString().Trim() + "'," + (m + 1) + " ,");
                                    sb.Append(" '" + row["Partcode"].ToString().Trim() + "','" + int.Parse(Dts[8].ToString().Trim()) + "',");
                                    sb.Append("'" + double.Parse(row["Rate"].ToString().Trim()) + "','" + double.Parse(row["Strokes"].ToString().Trim()) + "','" + row["CompCode"].ToString().Trim() + "','" + row["CatID"].ToString().Trim() + "' )");
                                    cmd = new SqlCommand(sb.ToString(), con) { CommandTimeout = 0, Transaction = tran };
                                    await cmd.ExecuteNonQueryAsync();
                                    await cmd.DisposeAsync();
                                }
                            }
                            else
                            {
                                return row["PartDesc"].ToString().Trim() + "  This Part Strokes is 0.";
                            }
                        }
                    }
                } // foreach end

                // ----- User Activity -----
                cmd = new SqlCommand("InsertLoginTransactionDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                cmd.Parameters.AddWithValue("@EmpID", job_Cpyreq.EmpCode.Trim());
                cmd.Parameters.AddWithValue("@TransactionType", "S");
                cmd.Parameters.AddWithValue("@TransactionFrom", "Sheet Metal Maker (Primary Plan)");
                cmd.Parameters.AddWithValue("@TransactionNo", StrDispCode_CPYPlan.Trim());
                cmd.Parameters.AddWithValue("@CompanyCode", job_Cpyreq.CompCode.Trim());
                cmd.Transaction = tran;
                await cmd.ExecuteNonQueryAsync();
                await cmd.DisposeAsync();

                await tran.CommitAsync();
                //await tran.RollbackAsync();

                if (!string.IsNullOrEmpty(StrDispCode_CPYPlan.Trim()))
                    StrDisplayMsg = "Saved Successfully With Canopy Plan: " + StrDispCode_CPYPlan.Trim() + "";

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
        public async Task<List<Dictionary<string, object>>> GetCheckerCPPlanLoadAsync()
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetCheckerCPPlanLoad_ERPNEW";
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
                    cmd.CommandText = "GetJobCard_CpyChecker_PlanDts_NewERP";
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

        public async Task<List<Dictionary<string, object>>> GetJobCardCpyCheckerDoneAsync(string strJobCardType, string strcompID, string planCode)
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetJobCard_CpyChecker_PlanDts_NewERP";
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

        public async Task<List<Dictionary<string, object>>> GetStageSheetDataAsync(string cpCode, string partCode, string stage, string pcCode)
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetStageSheetData_NEWERP";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@CPCode", cpCode));
                    cmd.Parameters.Add(new SqlParameter("@CanopyPartCode", partCode));
                    cmd.Parameters.Add(new SqlParameter("@Stage", stage));
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode)); // cnc | bending | fabrication | powdercoating

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

        public async Task<List<Dictionary<string, object>>> Get6MTypesAsync()
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "Get6MJobCradNewERP";
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

        public async Task<List<Dictionary<string, object>>> JobcardCorReqEmpNameAsync()
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "JobcardCorReqEmpNameNewERP";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;

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

        public async Task<string> CheckerSubmitAsync(Canopy_JobCardCheckerRequest job_CpyCheckerreq)
        {
            DataSet dsCanopyPlanDtsSub;
            string StrDisplayMsg = "";
            string StrDispCode_MaterialReq_CNC_ALL_msg = "";
            string StrDispCode_MaterialReq_FAB_ALL_msg = "";
            string StrDispCode_MaterialReq_POC_ALL_msg = "";
            string[] strPlanDts;
            string[] DtsPlan;
            int SrNo;

            if (string.IsNullOrEmpty(job_CpyCheckerreq.ProductionDetails))
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

                strPlanDts = Regex.Split(job_CpyCheckerreq.ProductionDetails, "@@#@@");
                SrNo = 0;
                string CpyPlan = _com.getName("SELECT CPCode FROM CanopyPlan WHERE CPCode='" + job_CpyCheckerreq.PlanCode.ToString().Trim() + "'", "tblCpyPlan", "CPCode");

                if (job_CpyCheckerreq.Status.ToString().Trim() == "AUTH")
                {
                    if (CpyPlan != null)
                    {
                        sb.Remove(0, sb.Length);
                        sb.Append("UPDATE CanopyPlan SET ");
                        sb.Append("Dt = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', ");
                        sb.Append("Checker1 = 1");
                        sb.Append("WHERE CPCode = '" + job_CpyCheckerreq.PlanCode.ToString().Trim() + "'");
                        cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        foreach (string StrSub in strPlanDts)
                        {
                            SrNo += 1;
                            DtsPlan = Regex.Split(StrSub.ToString().Trim(), "@#@");

                            if (DtsPlan[3] != null && DtsPlan[3].ToString().Trim() == "0")
                            {
                                cmd = new SqlCommand("InsertSheetMetal6MChecker_Detail", con);
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@PlanCode", job_CpyCheckerreq.PlanCode.Trim());
                                cmd.Parameters.AddWithValue("@SixMName", DtsPlan[1].Trim());
                                cmd.Parameters.AddWithValue("@Description", DtsPlan[2].Trim());
                                cmd.Parameters.AddWithValue("@AssignTo", DtsPlan[3].Trim());
                                cmd.Parameters.AddWithValue("@CorReqNo", '0');
                                cmd.Parameters.AddWithValue("@Status", job_CpyCheckerreq.Status.Trim());
                                cmd.Transaction = tran;
                                await cmd.ExecuteNonQueryAsync();
                                await cmd.DisposeAsync();
                            }
                        }

                        // ================= Auto CNC Req =================
                        string StrDispCode_CNCReq = "";
                        string StrCNC_PCCode = "0";
                        string StrCNC_OLDPCCode = "0";
                        string RequisitionForPartCode = "";
                        string CatID = "";
                        string CompCode = "";

                        dsCanopyPlanDtsSub = _com.procTranDS("select  PCCode_Act,CatID,CompCode from CanopyPlanDtsSub CP inner join CanopyPlan  C  on CP.CPCode= C.CPCode   where CP.CPCode='" + job_CpyCheckerreq.PlanCode.ToString().Trim() + "'  group by  PCCode_Act,CatID, CompCode", "tbl_CanopyPlanDtsSub", con, tran);

                        if (dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows.Count > 0)
                        {
                            for (int m1 = 0; m1 < dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows.Count; m1++)
                            {
                                var r = dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows[m1];
                                string comp = r["CompCode"].ToString().Trim();
                                string cat = r["CatID"].ToString().Trim();
                                string plan = r["PCCode_Act"].ToString().Trim();

                                // ----- Canopy -----
                                if (comp == "01" && cat == "029" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrCNC_PCCode = "01.095"; CatID = "029"; CompCode = "01"; StrCNC_OLDPCCode = "01.009";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 11, 1) IN ('4') AND Partcode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "01" && cat == "029" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrCNC_PCCode = "01.096"; CatID = "029"; CompCode = "01"; StrCNC_OLDPCCode = "01.009";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 11, 1) IN ('4') AND Partcode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "01" && cat == "029" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrCNC_PCCode = "01.097"; CatID = "029"; CompCode = "01"; StrCNC_OLDPCCode = "01.009";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 11, 1) IN ('4') AND Partcode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                // ----- Base Frame -----
                                else if (comp == "03" && cat == "084" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrCNC_PCCode = "03.066"; CatID = "084"; CompCode = "03"; StrCNC_OLDPCCode = "03.061";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "084" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrCNC_PCCode = "03.067"; CatID = "084"; CompCode = "03"; StrCNC_OLDPCCode = "03.061";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "084" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrCNC_PCCode = "03.068"; CatID = "084"; CompCode = "03"; StrCNC_OLDPCCode = "03.061";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                // ----- Fuel Tank -----
                                else if (comp == "03" && cat == "038" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrCNC_PCCode = "03.066"; CatID = "038"; CompCode = "03"; StrCNC_OLDPCCode = "03.061";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "038" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrCNC_PCCode = "03.067"; CatID = "038"; CompCode = "03"; StrCNC_OLDPCCode = "03.061";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "038" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrCNC_PCCode = "03.068"; CatID = "038"; CompCode = "03"; StrCNC_OLDPCCode = "03.061";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }

                                // ----- CNC Req master (product + company wise) -----
                                StrDispCode_CNCReq = _com.GetMaxNo("MaterialRequisitionWithOutPlan", "REQ", comp, con, tran);

                                StrDispCode_MaterialReq_CNC_ALL_msg = string.IsNullOrEmpty(StrDispCode_MaterialReq_CNC_ALL_msg.Trim())
                                    ? StrDispCode_CNCReq.Trim()
                                    : StrDispCode_MaterialReq_CNC_ALL_msg + ", " + StrDispCode_CNCReq.Trim();

                                sb.Remove(0, sb.Length);
                                sb.Append("insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt, Yr,ProfitCenterCode,ToProfitCenterCode, ProfitCenterCode_Act,ToProfitCenterCode_Act,ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) ");
                                sb.Append("values('" + StrDispCode_CNCReq.Trim() + "','" + StrDispCode_CNCReq.Substring(10, 8).ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                                sb.Append("'" + StrDispCode_CNCReq.Substring(4, 5).Trim() + "','" + StrCNC_OLDPCCode.Trim() + "','23.001','" + StrCNC_PCCode.Trim() + "','23.001'  ,'" + job_CpyCheckerreq.Partcode.ToString().Trim() + "','" + comp + "','" + job_CpyCheckerreq.BatchQty.ToString().Trim() + "','P','WIP',");
                                sb.Append("'Auto Req For : " + job_CpyCheckerreq.Kva.ToString().Trim() + " Kva " + job_CpyCheckerreq.Model.ToString().Trim() + " ','1','1','1','" + job_CpyCheckerreq.PlanCode.Trim() + "','" + RequisitionForPartCode.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync();
                                await cmd.DisposeAsync();

                                // ----- CNC Req details -----
                                DataSet dsReqDts_CNCReq = _com.procTranDS("exec InternalReqLogisticsKit_ERPNEW '" + job_CpyCheckerreq.Partcode.ToString().Trim() + "' ,0 ,'" + cat + "'", "tbl_ReqDts_CNCReq", con, tran);
                                if (dsReqDts_CNCReq != null && dsReqDts_CNCReq.Tables["tbl_ReqDts_CNCReq"].Rows.Count > 0)
                                {
                                    int SrNoReq_CNCReq = 0;
                                    for (int cntd = 0; cntd < dsReqDts_CNCReq.Tables["tbl_ReqDts_CNCReq"].Rows.Count; cntd++)
                                    {
                                        SrNoReq_CNCReq += 1;
                                        string part = dsReqDts_CNCReq.Tables["tbl_ReqDts_CNCReq"].Rows[cntd]["Partcode"].ToString().Trim();
                                        cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails_ERPNEW", con);
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.Parameters.AddWithValue("@REQCode", StrDispCode_CNCReq.Trim());
                                        cmd.Parameters.AddWithValue("@SrNo", SrNoReq_CNCReq);
                                        cmd.Parameters.AddWithValue("@PartCode", part);
                                        cmd.Parameters.AddWithValue("@Qty", double.Parse(dsReqDts_CNCReq.Tables["tbl_ReqDts_CNCReq"].Rows[cntd]["RaiseReqQty"].ToString().Trim()) * double.Parse(job_CpyCheckerreq.BatchQty.ToString().Trim()));
                                        cmd.Parameters.AddWithValue("@REQStatus", "P");
                                        cmd.Transaction = tran;
                                        await cmd.ExecuteNonQueryAsync();
                                        await cmd.DisposeAsync();

                                        await GetReqDetailsSubAsync(con, tran, StrDispCode_CNCReq.Trim(), part, 0, double.Parse(job_CpyCheckerreq.BatchQty.ToString().Trim()));
                                    }
                                }
                            }
                        }
                        // ================= Auto FAB Req =================
                        string StrDispCode_FABReq = "";
                        string StrFAB_PCCode = "0";
                        string StrFAB_OLDPCCode = "0";

                        dsCanopyPlanDtsSub = _com.procTranDS("select  PCCode_Act,CatID,CompCode from CanopyPlanDtsSub CP inner join CanopyPlan  C  on CP.CPCode= C.CPCode   where CP.CPCode='" + job_CpyCheckerreq.PlanCode.ToString().Trim() + "'  group by  PCCode_Act,CatID, CompCode", "tbl_CanopyPlanDtsSub", con, tran);

                        if (dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows.Count > 0)
                        {
                            for (int m2 = 0; m2 < dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows.Count; m2++)
                            {
                                var r = dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows[m2];
                                string comp = r["CompCode"].ToString().Trim();
                                string cat = r["CatID"].ToString().Trim();
                                string plan = r["PCCode_Act"].ToString().Trim();

                                // ----- Canopy FAB -----
                                if (comp == "01" && cat == "029" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrFAB_PCCode = "01.101"; CatID = "029"; CompCode = "01"; StrFAB_OLDPCCode = "01.008";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 11, 1) IN ('4') AND Partcode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "01" && cat == "029" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrFAB_PCCode = "01.102"; CatID = "029"; CompCode = "01"; StrFAB_OLDPCCode = "01.008";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 11, 1) IN ('4') AND Partcode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "01" && cat == "029" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrFAB_PCCode = "01.103"; CatID = "029"; CompCode = "01"; StrFAB_OLDPCCode = "01.008";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 11, 1) IN ('4') AND Partcode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                // ----- Base Frame FAB -----
                                else if (comp == "03" && cat == "084" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrFAB_PCCode = "03.073"; CatID = "084"; CompCode = "03"; StrFAB_OLDPCCode = "03.002";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "084" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrFAB_PCCode = "03.074"; CatID = "084"; CompCode = "03"; StrFAB_OLDPCCode = "03.002";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "084" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrFAB_PCCode = "03.075"; CatID = "084"; CompCode = "03"; StrFAB_OLDPCCode = "03.002";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                // ----- Fuel Tank FAB -----
                                else if (comp == "03" && cat == "038" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrFAB_PCCode = "03.073"; CatID = "038"; CompCode = "03"; StrFAB_OLDPCCode = "03.002";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "038" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrFAB_PCCode = "03.074"; CatID = "038"; CompCode = "03"; StrFAB_OLDPCCode = "03.002";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "038" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrFAB_PCCode = "03.075"; CatID = "038"; CompCode = "03"; StrFAB_OLDPCCode = "03.002";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }

                                // ----- FAB Req master -----
                                StrDispCode_FABReq = _com.GetMaxNo("MaterialRequisitionWithOutPlan", "REQ", comp, con, tran);

                                StrDispCode_MaterialReq_FAB_ALL_msg = string.IsNullOrEmpty(StrDispCode_MaterialReq_FAB_ALL_msg.Trim())
                                    ? StrDispCode_FABReq.Trim()
                                    : StrDispCode_MaterialReq_FAB_ALL_msg + ", " + StrDispCode_FABReq.Trim();

                                sb.Remove(0, sb.Length);
                                sb.Append("insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt, Yr,ProfitCenterCode,ToProfitCenterCode, ProfitCenterCode_Act,ToProfitCenterCode_Act,ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) ");
                                sb.Append("values('" + StrDispCode_FABReq.Trim() + "','" + StrDispCode_FABReq.Substring(10, 8).ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                                sb.Append("'" + StrDispCode_FABReq.Substring(4, 5).Trim() + "','" + StrFAB_OLDPCCode.Trim() + "','23.001','" + StrFAB_PCCode.Trim() + "','23.001','" + job_CpyCheckerreq.Partcode.ToString().Trim() + "','" + comp + "','" + job_CpyCheckerreq.BatchQty.ToString().Trim() + "','P','WIP',");
                                sb.Append("'Auto Req For : " + job_CpyCheckerreq.Kva.ToString().Trim() + " Kva " + job_CpyCheckerreq.Model.ToString().Trim() + " ','1','1','1','" + job_CpyCheckerreq.PlanCode.Trim() + "','" + RequisitionForPartCode.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync();
                                await cmd.DisposeAsync();

                                // ----- FAB Req details -----
                                DataSet dsReqDts_FABReq = _com.procTranDS("exec InternalReqLogisticsKit_ERPNEW '" + job_CpyCheckerreq.Partcode.ToString().Trim() + "',1 ,'" + cat + "'", "tbl_ReqDts_FABReq", con, tran);
                                if (dsReqDts_FABReq != null && dsReqDts_FABReq.Tables["tbl_ReqDts_FABReq"].Rows.Count > 0)
                                {
                                    int SrNoReq_FABReq = 0;
                                    for (int cnt_FAB = 0; cnt_FAB < dsReqDts_FABReq.Tables["tbl_ReqDts_FABReq"].Rows.Count; cnt_FAB++)
                                    {
                                        SrNoReq_FABReq += 1;
                                        string part = dsReqDts_FABReq.Tables["tbl_ReqDts_FABReq"].Rows[cnt_FAB]["Partcode"].ToString().Trim();
                                        cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails_ERPNEW", con);
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.Parameters.AddWithValue("@REQCode", StrDispCode_FABReq.Trim());
                                        cmd.Parameters.AddWithValue("@SrNo", SrNoReq_FABReq);
                                        cmd.Parameters.AddWithValue("@PartCode", part);
                                        cmd.Parameters.AddWithValue("@Qty", double.Parse(dsReqDts_FABReq.Tables["tbl_ReqDts_FABReq"].Rows[cnt_FAB]["RaiseReqQty"].ToString().Trim()) * double.Parse(job_CpyCheckerreq.BatchQty.ToString().Trim()));
                                        cmd.Parameters.AddWithValue("@REQStatus", "P");
                                        cmd.Transaction = tran;
                                        await cmd.ExecuteNonQueryAsync();
                                        await cmd.DisposeAsync();

                                        await GetReqDetailsSubAsync(con, tran, StrDispCode_FABReq.Trim(), part, 1, double.Parse(job_CpyCheckerreq.BatchQty.ToString().Trim()));
                                    }
                                }
                            }
                        }

                        // ================= Auto Powder-Coating Req =================
                        string StrPC_PCCode = "0";
                        string StrPC_OldPCCode = "0";
                        dsCanopyPlanDtsSub = _com.procTranDS("select  PCCode_Act,CatID,CompCode from CanopyPlanDtsSub CP inner join CanopyPlan  C  on CP.CPCode= C.CPCode   where CP.CPCode='" + job_CpyCheckerreq.PlanCode.ToString().Trim() + "' and CompCode='03'  group by  PCCode_Act,CatID, CompCode", "tbl_CanopyPlanDtsSub", con, tran);

                        if (dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows.Count > 0)
                        {
                            for (int m3 = 0; m3 < dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows.Count; m3++)
                            {
                                var r = dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows[m3];
                                string comp = r["CompCode"].ToString().Trim();
                                string cat = r["CatID"].ToString().Trim();
                                string plan = r["PCCode_Act"].ToString().Trim();

                                // NOTE: faithful to original — the three Base-Frame blocks use plain
                                // 'if' (not else-if), exactly as in your source.
                                if (comp == "03" && cat == "084" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrPC_PCCode = "01.116"; CatID = "084"; StrPC_OldPCCode = "01.007";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                if (comp == "03" && cat == "084" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrPC_PCCode = "01.116"; CatID = "084"; StrPC_OldPCCode = "01.007";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                if (comp == "03" && cat == "084" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrPC_PCCode = "01.116"; CatID = "084"; StrPC_OldPCCode = "01.007";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('2') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }

                                else if (comp == "03" && cat == "038" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrPC_PCCode = "01.116"; CatID = "038"; StrPC_OldPCCode = "01.007";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "038" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrPC_PCCode = "01.116"; CatID = "038"; StrPC_OldPCCode = "01.007";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "03" && cat == "038" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrPC_PCCode = "01.116"; CatID = "038"; StrPC_OldPCCode = "01.007";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'", "tblBP", "Partcode", con, tran);
                                }

                                string StrDispCode_PCReq = _com.GetMaxNo("MaterialRequisitionWithOutPlan", "REQ", "01", con, tran);

                                StrDispCode_MaterialReq_POC_ALL_msg = string.IsNullOrEmpty(StrDispCode_MaterialReq_POC_ALL_msg.Trim())
                                    ? StrDispCode_PCReq.Trim()
                                    : StrDispCode_MaterialReq_POC_ALL_msg + ", " + StrDispCode_PCReq.Trim();

                                sb.Remove(0, sb.Length);
                                sb.Append("insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt, Yr,ProfitCenterCode,ToProfitCenterCode, ProfitCenterCode_Act,ToProfitCenterCode_Act,ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) ");
                                sb.Append("values('" + StrDispCode_PCReq.Trim() + "','" + StrDispCode_PCReq.Substring(10, 8).ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                                sb.Append("'" + StrDispCode_PCReq.Substring(4, 5).Trim() + "','" + StrPC_OldPCCode.Trim() + "','" + StrFAB_OLDPCCode.Trim() + "','" + StrPC_PCCode.Trim() + "','" + StrFAB_PCCode.Trim() + "','" + job_CpyCheckerreq.Partcode.ToString().Trim() + "','01','" + job_CpyCheckerreq.BatchQty.ToString().Trim() + "','P','WIP',");
                                sb.Append("'Auto Req For : " + job_CpyCheckerreq.Kva.ToString().Trim() + " Kva " + job_CpyCheckerreq.Model.ToString().Trim() + "','1','1','1','" + job_CpyCheckerreq.PlanCode.Trim() + "','" + RequisitionForPartCode.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync();
                                await cmd.DisposeAsync();

                                // The 6 detail branches all run the SAME query + insert loop, differing
                                // only by the comp/cat/plan guard. Collapsed to one guard for clarity.
                                bool runDetails =
                                    (comp == "03" && cat == "084" && (plan == "01.134" || plan == "03.084")) ||
                                    (comp == "03" && cat == "084" && (plan == "01.135" || plan == "03.085")) ||
                                    (comp == "03" && cat == "084" && (plan == "01.136" || plan == "03.086")) ||
                                    (comp == "03" && cat == "038" && (plan == "01.134" || plan == "03.084")) ||
                                    (comp == "03" && cat == "038" && (plan == "01.135" || plan == "03.085")) ||
                                    (comp == "03" && cat == "038" && (plan == "01.136" || plan == "03.086"));

                                if (runDetails)
                                {
                                    DataSet dsReqDts_PCReq = _com.procTranDS(
                                        "Select CPCode,CpyPartCode,SrNo,PartCode,CPQty as RaiseReqQty,Rate  from CanopyPlanDtsSub  where CPCode='" + job_CpyCheckerreq.PlanCode.Trim() + "' and CpyPartCode ='" + job_CpyCheckerreq.Partcode.ToString().Trim() + "' and CatID='" + cat + "' " +
                                        " Union ALL Select CPCode,CpyPartCode,SrNo,PartCode,CPQty as RaiseReqQty ,Rate from CanopyPlanDtsSubBelowStdRate  where CPCode='" + job_CpyCheckerreq.PlanCode.Trim() + "' and CpyPartCode ='" + job_CpyCheckerreq.Partcode.ToString().Trim() + "' and CatID='" + cat + "' Order by Srno ",
                                        "tbl_ReqDts_PCReq", con, tran);

                                    if (dsReqDts_PCReq != null && dsReqDts_PCReq.Tables["tbl_ReqDts_PCReq"].Rows.Count > 0)
                                    {
                                        int SrNoReq_PCReq = 0;
                                        for (int cnt_PC = 0; cnt_PC < dsReqDts_PCReq.Tables["tbl_ReqDts_PCReq"].Rows.Count; cnt_PC++)
                                        {
                                            SrNoReq_PCReq += 1;
                                            cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails_ERPNEW", con);
                                            cmd.CommandType = CommandType.StoredProcedure;
                                            cmd.Parameters.AddWithValue("@REQCode", StrDispCode_PCReq.Trim());
                                            cmd.Parameters.AddWithValue("@SrNo", SrNoReq_PCReq);
                                            cmd.Parameters.AddWithValue("@PartCode", dsReqDts_PCReq.Tables["tbl_ReqDts_PCReq"].Rows[cnt_PC]["Partcode"].ToString().Trim());
                                            cmd.Parameters.AddWithValue("@Qty", double.Parse(dsReqDts_PCReq.Tables["tbl_ReqDts_PCReq"].Rows[cnt_PC]["RaiseReqQty"].ToString().Trim()));
                                            cmd.Parameters.AddWithValue("@REQStatus", "P");
                                            cmd.Transaction = tran;
                                            await cmd.ExecuteNonQueryAsync();
                                            await cmd.DisposeAsync();
                                        }
                                    }
                                }
                            }
                        }

                        // ================= Auto Flat Pack Kit Req =================
                        string StrDispCode_FlatPackReq = "";
                        string StrFlatPack_PCCode = "0";
                        string StrFlatPack_OLDPCCode = "0";

                        dsCanopyPlanDtsSub = _com.procTranDS("select  PCCode_Act,CatID,CompCode from CanopyPlanDtsSub CP inner join CanopyPlan  C  on CP.CPCode= C.CPCode   where CP.CPCode='" + job_CpyCheckerreq.PlanCode.ToString().Trim() + "' and CompCode='01'  group by  PCCode_Act,CatID, CompCode", "tbl_CanopyPlanDtsSub", con, tran);

                        if (dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows.Count > 0)
                        {
                            for (int m2 = 0; m2 < dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows.Count; m2++)
                            {
                                var r = dsCanopyPlanDtsSub.Tables["tbl_CanopyPlanDtsSub"].Rows[m2];
                                string comp = r["CompCode"].ToString().Trim();
                                string cat = r["CatID"].ToString().Trim();
                                string plan = r["PCCode_Act"].ToString().Trim();

                                // ----- Canopy Flat Pack -----
                                if (comp == "01" && cat == "029" && (plan == "01.134" || plan == "03.084"))
                                {
                                    StrFlatPack_PCCode = "01.124"; CatID = "029"; CompCode = "01"; StrFlatPack_OLDPCCode = "01.093";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 13, 1) IN ('6') AND Partcode LIKE '012%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "01" && cat == "029" && (plan == "01.135" || plan == "03.085"))
                                {
                                    StrFlatPack_PCCode = "01.125"; CatID = "029"; CompCode = "01"; StrFlatPack_OLDPCCode = "01.093";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 13, 1) IN ('6') AND Partcode LIKE '012%'", "tblBP", "Partcode", con, tran);
                                }
                                else if (comp == "01" && cat == "029" && (plan == "01.136" || plan == "03.086"))
                                {
                                    StrFlatPack_PCCode = "01.126"; CatID = "029"; CompCode = "01"; StrFlatPack_OLDPCCode = "01.093";
                                    RequisitionForPartCode = _com.getTranName("SELECT Partcode FROM BOMdetails WHERE BOMCode='" + job_CpyCheckerreq.bomCode.ToString().Trim() + "' AND SUBSTRING(Partcode, 13, 1) IN ('6') AND Partcode LIKE '012%'", "tblBP", "Partcode", con, tran);
                                }


                                // ----- Flat Pack Req master -----
                                StrDispCode_FlatPackReq = _com.GetMaxNo("MaterialRequisitionWithOutPlan", "REQ", comp, con, tran);

                                StrDispCode_MaterialReq_FAB_ALL_msg = string.IsNullOrEmpty(StrDispCode_MaterialReq_FAB_ALL_msg.Trim())
                                    ? StrDispCode_FlatPackReq.Trim()
                                    : StrDispCode_MaterialReq_FAB_ALL_msg + ", " + StrDispCode_FlatPackReq.Trim();

                                sb.Remove(0, sb.Length);
                                sb.Append("insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt, Yr,ProfitCenterCode,ToProfitCenterCode, ProfitCenterCode_Act,ToProfitCenterCode_Act,ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) ");
                                sb.Append("values('" + StrDispCode_FlatPackReq.Trim() + "','" + StrDispCode_FlatPackReq.Substring(10, 8).ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                                sb.Append("'" + StrDispCode_FlatPackReq.Substring(4, 5).Trim() + "','" + StrFlatPack_OLDPCCode.Trim() + "','23.001','" + StrFlatPack_PCCode.Trim() + "','23.001','" + job_CpyCheckerreq.Partcode.ToString().Trim() + "','" + comp + "','" + job_CpyCheckerreq.BatchQty.ToString().Trim() + "','P','WIP',");
                                sb.Append("'Auto Req For : " + job_CpyCheckerreq.Kva.ToString().Trim() + " Kva " + job_CpyCheckerreq.Model.ToString().Trim() + " ','1','1','1','" + job_CpyCheckerreq.PlanCode.Trim() + "','" + RequisitionForPartCode.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                                await cmd.ExecuteNonQueryAsync();
                                await cmd.DisposeAsync();

                                // ----- FAB Req details -----
                                DataSet dsReqDts_FlatPackReq = _com.procTranDS("exec InternalReqLogisticsKit_ERPNEW '" + job_CpyCheckerreq.Partcode.ToString().Trim() + "',1 ,'" + cat + "'", "tbl_ReqDts_FlatPackReq", con, tran);
                                if (dsReqDts_FlatPackReq != null && dsReqDts_FlatPackReq.Tables["tbl_ReqDts_FlatPackReq"].Rows.Count > 0)
                                {
                                    int SrNoReq_FlatPackReq = 0;
                                    for (int cnt_FAB = 0; cnt_FAB < dsReqDts_FlatPackReq.Tables["tbl_ReqDts_FlatPackReq"].Rows.Count; cnt_FAB++)
                                    {
                                        SrNoReq_FlatPackReq += 1;
                                        string part = dsReqDts_FlatPackReq.Tables["tbl_ReqDts_FlatPackReq"].Rows[cnt_FAB]["Partcode"].ToString().Trim();
                                        cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails_ERPNEW", con);
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.Parameters.AddWithValue("@REQCode", StrDispCode_FlatPackReq.Trim());
                                        cmd.Parameters.AddWithValue("@SrNo", SrNoReq_FlatPackReq);
                                        cmd.Parameters.AddWithValue("@PartCode", part);
                                        cmd.Parameters.AddWithValue("@Qty", double.Parse(dsReqDts_FlatPackReq.Tables["tbl_ReqDts_FlatPackReq"].Rows[cnt_FAB]["RaiseReqQty"].ToString().Trim()) * double.Parse(job_CpyCheckerreq.BatchQty.ToString().Trim()));
                                        cmd.Parameters.AddWithValue("@REQStatus", "P");
                                        cmd.Transaction = tran;
                                        await cmd.ExecuteNonQueryAsync();
                                        await cmd.DisposeAsync();

                                        await GetReqDetailsSubAsync(con, tran, StrDispCode_FlatPackReq.Trim(), part, 1, double.Parse(job_CpyCheckerreq.BatchQty.ToString().Trim()));
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
                        StrDispCode = _com.GetMaxNo("CorporateRequisition", "COR", job_CpyCheckerreq.CompCode.Trim(), con, tran);

                        cmd = new SqlCommand("InsertSheetMetal6MChecker_Detail", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PlanCode", job_CpyCheckerreq.PlanCode.Trim());
                        cmd.Parameters.AddWithValue("@SixMName", DtsPlan[1].Trim());
                        cmd.Parameters.AddWithValue("@Description", DtsPlan[2].Trim());
                        cmd.Parameters.AddWithValue("@AssignTo", DtsPlan[3].Trim());
                        if (DtsPlan[3] != null && DtsPlan[3].ToString().Trim() != "0")
                            cmd.Parameters.AddWithValue("@CorReqNo", StrDispCode.Trim());
                        else
                            cmd.Parameters.AddWithValue("@CorReqNo", '0');
                        cmd.Parameters.AddWithValue("@Status", job_CpyCheckerreq.Status.Trim());
                        cmd.Transaction = tran;
                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();

                        if (DtsPlan[3] != null && DtsPlan[3].ToString().Trim() != "0")
                        {
                            string ReqMsg = string.Format(
                                " Sheet Metal Checker  JobCard  PlanCode: {0}, KVA: {1}, Model: {2}, 6MType: {3}, Description: {4}",
                                job_CpyCheckerreq.PlanCode.Trim(), job_CpyCheckerreq.Kva, job_CpyCheckerreq.Model.Trim(), DtsPlan[1].Trim(), DtsPlan[2].Trim());

                            sb.Remove(0, sb.Length);
                            sb.Append("INSERT INTO CorporateRequisition ");
                            sb.Append("(ReqCode,Dt,Yr,MaxSrNo,EmpCode,FromPCCode,ToEmpCode,ToPCCode,Priority,ReqMsg,CompanyCode,AssignStatus,Active,Discard)");
                            sb.Append(" VALUES('" + StrDispCode.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                            sb.Append("'" + (StrDispCode.Substring(4, 5)) + "','" + (StrDispCode.Substring(10, 8)) + "',");
                            sb.Append("'" + job_CpyCheckerreq.EmpCode.Trim() + "' ,'" + job_CpyCheckerreq.PCCode.Trim() + "','" + DtsPlan[3].Trim() + "',");
                            sb.Append("'" + DtsPlan[4].Trim() + "' ,'High Priority','" + ReqMsg.Trim() + "',");
                            sb.Append("'" + job_CpyCheckerreq.CompCode + "','P','1','1')");
                            cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                            await cmd.ExecuteNonQueryAsync();
                            await cmd.DisposeAsync();

                            sb.Remove(0, sb.Length);
                            sb.Append("INSERT INTO CorporateRequisitionActionTaken");
                            sb.Append("(Dt,ReqCode,AssignByCode,AssignToCode,ActionTaken,Priority,ActionStatus,AssOrAction,Active,Discard)");
                            sb.Append(" VALUES('" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "',");
                            sb.Append("'" + StrDispCode.Trim() + "',");
                            sb.Append("'" + job_CpyCheckerreq.EmpCode.Trim() + "',");
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
                cmd = new SqlCommand("insertLoginTransactionDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                cmd.Parameters.AddWithValue("@EmpID", job_CpyCheckerreq.EmpCode.Trim());
                cmd.Parameters.AddWithValue("@TransactionType", "S");
                cmd.Parameters.AddWithValue("@TransactionFrom", "Sheet Metal Checker (Primary Plan)");
                cmd.Parameters.AddWithValue("@TransactionNo", job_CpyCheckerreq.PlanCode.Trim());
                cmd.Parameters.AddWithValue("@CompanyCode", job_CpyCheckerreq.CompCode.Trim());
                cmd.Transaction = tran;
                await cmd.ExecuteNonQueryAsync();
                await cmd.DisposeAsync();

                await tran.CommitAsync();
                //await tran.RollbackAsync();

                if (!string.IsNullOrEmpty(StrDispCode_MaterialReq_CNC_ALL_msg.Trim()))
                    StrDisplayMsg += " & CNC Req: " + StrDispCode_MaterialReq_CNC_ALL_msg.Trim() + "";
                if (!string.IsNullOrEmpty(StrDispCode_MaterialReq_FAB_ALL_msg.Trim()))
                    StrDisplayMsg += " & Fab Req: " + StrDispCode_MaterialReq_FAB_ALL_msg.Trim() + "";
                if (!string.IsNullOrEmpty(StrDispCode_MaterialReq_POC_ALL_msg.Trim()))
                    StrDisplayMsg += " & PC_Unit_1 to Fab_U4 : " + StrDispCode_MaterialReq_POC_ALL_msg.Trim() + "";
                if (!string.IsNullOrEmpty(StrDispCode_MaterialReq_POC_ALL_msg.Trim()))
                    StrDisplayMsg += " & Unit 1 Line  Flat Packing : " + StrDispCode_MaterialReq_POC_ALL_msg.Trim() + "";

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

        /// <summary>
        /// // Get Canopy Hold Data
        /// </summary>
        /// <param name="compCode"></param>
        /// <returns></returns>

        public async Task<List<Dictionary<string, object>>> GetConopyHoldAsync(string compCode)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetconopyHold_NewERP";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@CompCode",
                        string.IsNullOrEmpty(compCode) ? (object)DBNull.Value : compCode));

                    if (conn.State == ConnectionState.Closed)
                        await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                                row[reader.GetName(i)] =
                                    reader.IsDBNull(i) ? null : reader.GetValue(i);

                            data.Add(row);
                        }
                    }
                }
            }

            return data;
        }

        public async Task<string> JobCardConopyReqInActiveHoldAsync(Canopy_JobCardHoldRequest job_CpyHoldreq)
        {
            string StrDisplayMsg = "";
            int HoldCount = 0;

            // ── LEGACY: chkCount == Rows.Count → "Please Select least One check box !"
            //    (nothing checked ⇒ Angular sends an empty details array)
            if (job_CpyHoldreq.Details == null || job_CpyHoldreq.Details.Count == 0)
            {
                return "Please Select At Least One Check Box !";
            }

            await using var con = new SqlConnection(_connStr);
            SqlTransaction tran = null;
            var sb = new StringBuilder();
            SqlCommand cmd;

            try
            {
                await con.OpenAsync();

                // ═════ PRE-VALIDATION PASS (legacy: 2nd loop, before transaction) ═════
                foreach (var dts in job_CpyHoldreq.Details)
                {
                    string CPCode = (dts.CPCode ?? "").Trim();
                    string Remark = (dts.InActiveRemark ?? "").Trim();

                    // LEGACY: if (txtAuthRemark.Text.Trim() == "") → alert(...)
                    if (Remark == "")
                    {
                        return "Please Fill InActiveRemark For CPCode " + CPCode + " !";
                    }

                    // LEGACY: getName("SELECT CPTStatus ... ") == "D" → alert(...)
                    string CPTStatus = _com.getName(
                        "SELECT CPTStatus FROM CanopyPlandtsSub WHERE CPCode='" + CPCode + "'",
                        "Req", "CPTStatus");

                    if (CPTStatus != null && CPTStatus.Trim() == "D")
                    {
                        return "You Cannot Hold CPCode " + CPCode
                             + " , Because Its Done , Please Uncheck !";
                    }
                }

                // ═════ UPDATE PASS (legacy: con.Open + BeginTransaction + 3rd loop) ═════
                tran = (SqlTransaction)await con.BeginTransactionAsync();

                foreach (var dts in job_CpyHoldreq.Details)
                {
                    string CPCode = (dts.CPCode ?? "").Trim();
                    string Partcode = (dts.Partcode ?? "").Trim();
                    string Remark = (dts.InActiveRemark ?? "").Trim();

                    // LEGACY: Update CanopyPlan SET Active='0', Discard='0'
                    sb.Remove(0, sb.Length);
                    sb.Append("UPDATE CanopyPlan ");
                    sb.Append("SET Active='0', Discard='0' ,Checker1='0' ");
                    sb.Append("WHERE CPCode=@CPCode");
                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                    cmd.Parameters.AddWithValue("@CPCode", CPCode);
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();

                    // LEGACY: UPDATE MaterialRequisitionWithOutPlan SET InActiveRemark...
                    //         (parameterized — legacy concatenated the remark, which
                    //          crashed on any remark containing an apostrophe)
                    sb.Remove(0, sb.Length);
                    sb.Append("UPDATE MaterialRequisitionWithOutPlan ");
                    sb.Append("SET InActiveRemark=@Remark, Active='0', Discard='0' ");
                    sb.Append("WHERE SourceCode=@CPCode AND ClassCode=@Partcode");
                    cmd = new SqlCommand(sb.ToString(), con) { Transaction = tran };
                    cmd.Parameters.AddWithValue("@Remark", Remark);
                    cmd.Parameters.AddWithValue("@CPCode", CPCode);
                    cmd.Parameters.AddWithValue("@Partcode", Partcode);
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();

                    //****************User Activity****************
                    // LEGACY: Session["UserID"] / Session["CompID"] → payload values
                    cmd = new SqlCommand("InsertLoginTransactionDetails", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EmpID", job_CpyHoldreq.EmpCode.Trim());
                    cmd.Parameters.AddWithValue("@TransactionType", "S");
                    cmd.Parameters.AddWithValue("@TransactionFrom", "JobCardConopyReq_InActive");
                    cmd.Parameters.AddWithValue("@TransactionNo", CPCode);
                    cmd.Parameters.AddWithValue("@CompanyCode", job_CpyHoldreq.CompCode.Trim());
                    cmd.Transaction = tran;
                    await cmd.ExecuteNonQueryAsync();
                    await cmd.DisposeAsync();

                    HoldCount += 1;
                }

                // ⚠ RESTORED — the draft had CommitAsync commented out and called
                //   RollbackAsync on the happy path, so nothing was persisting.
                await tran.CommitAsync();
                // await tran.RollbackAsync();

                // LEGACY: alert('Record Hold Successfully')
                StrDisplayMsg = "Record Hold Successfully (" + HoldCount + " Record)";
                return StrDisplayMsg;
            }
            catch (Exception ex)
            {
                // LEGACY: Response.Write(ex) + tran.Rollback()
                if (tran != null)
                    await tran.RollbackAsync();
                return ("StackTrace " + ex.StackTrace + ", Message " + ex.Message);
            }
            // LEGACY finally { con.Close(); } → 'await using var con' handles it.
            // LEGACY getData() refresh      → Angular calls onClickSearch() after success.
        }

    }
}










