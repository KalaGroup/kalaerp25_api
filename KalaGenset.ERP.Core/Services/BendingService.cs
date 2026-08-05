using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.ResponseDTO.Bending;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Services
{
    public class BendingService : IBending
    {
        private readonly KalaDbContext _db;
        private readonly string _connStr;
        private readonly CommonCon ComCon;

        public BendingService(KalaDbContext context,ICommonService common,ILogger<BendingService> logger,IConfiguration config,CommonCon com)
        {
            _db = context;
            ComCon = com;
            _connStr = config.GetConnectionString("KalaDbContext")
                       ?? throw new InvalidOperationException("Connection string 'KalaDbContext' not found.");
        }
        private string strReqCodeCPYAssly = "";
        //private SqlConnection con;
        //private SqlTransaction tran;
        //private readonly StringBuilder sb = new StringBuilder();

        //private string strReqCode = "";
        //private bool ChkforStartCPY;
        //private bool ChkforStart;

        //private string[] strPlanDts;
        //private string[] DtsA;
        //private int SrNoA;

        //private DataSet dsKitbelowRate;
        //private DataSet dsKitbelowRatedts;
        //private DataSet dsChkforStart;


        public async Task<IEnumerable<BendingCpyKitDto>> GetCpyKitAsync(string pcCode, string machineCode, string planCode, string partCode, string cpyKit, CancellationToken cancellationToken = default)
        {
            // MachineCode is "Machine-->SerialNo"  (was Regex.Split in the original)
            var machineParts = (machineCode ?? string.Empty).Trim().Split("-->", StringSplitOptions.None);
            if (machineParts.Length < 2)
                throw new ArgumentException("MachineCode must be in the format 'Machine-->SerialNo'.", nameof(machineCode));

            var machine = machineParts[0].Trim();
            var serialNo = machineParts[1].Trim();

            // CpyKit == "0"  -> kit list ;  otherwise -> balance (Bal)
            string inlineSql = cpyKit == "0"
                ? @"
            SELECT  AliseName AS KitDesc,
                    Pf.Partcode + '-->' + PartDesc AS KitCode,
                    PfbCode,
                    EDt
            FROM    processfeedback pf
            INNER JOIN Part P ON Pf.partcode = P.partcode
            WHERE   PCCode_Act = @PCCode
              AND   MachineCode      = @Machine
              AND   SerialNo         = @SerialNo
              AND   Edt IS NULL
              AND   CanopyPlanCode   = @PlanCode
              AND   Productcode      = @Partcode
              AND   Pf.Active        = '1'
              AND   Pf.Dt >= '2020-07-10 00:00:00'
            ORDER BY Pf.Dt DESC"
                : @"
            SELECT  ISNULL(ProcessQty, 0) AS Bal
            FROM    processfeedback pf
            INNER JOIN Part P ON Pf.partcode = P.partcode
            WHERE   PCCode_Act = @PCCode
              AND   MachineCode      = @Machine
              AND   SerialNo         = @SerialNo
              AND   Edt IS NULL
              AND   CanopyPlanCode   = @PlanCode
              AND   Productcode      = @Partcode
              AND   Pf.Partcode      = @CpyKit
              AND   Pf.Active        = '1'
              AND   Pf.Dt >= '2020-07-10 00:00:00'
            ORDER BY Pf.Dt DESC";

            await using var connection = new SqlConnection(_connStr);
            await connection.OpenAsync(cancellationToken);

            // 1) inline query (replaces ComCon.procDS)
            List<BendingCpyKitDto> result;
            await using (var cmd = new SqlCommand(inlineSql, connection))
            {
                cmd.CommandTimeout = 0;
                cmd.Parameters.Add("@PCCode", SqlDbType.VarChar).Value = (pcCode ?? string.Empty).Trim();
                cmd.Parameters.Add("@Machine", SqlDbType.VarChar).Value = machine;
                cmd.Parameters.Add("@SerialNo", SqlDbType.VarChar).Value = serialNo;
                cmd.Parameters.Add("@PlanCode", SqlDbType.VarChar).Value = planCode ?? string.Empty;
                cmd.Parameters.Add("@Partcode", SqlDbType.VarChar).Value = partCode ?? string.Empty;
                if (cpyKit != "0")
                    cmd.Parameters.Add("@CpyKit", SqlDbType.VarChar).Value = cpyKit ?? string.Empty;

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                result = await MapAsync(reader, cancellationToken);
            }

            if (result.Count > 0)
                return result;

            // 2) fallback stored procedure GetCpyKit_NewERP
            await using (var spCmd = new SqlCommand("GetCpyKit_NewERP", connection))
            {
                spCmd.CommandType = CommandType.StoredProcedure;
                spCmd.CommandTimeout = 0;
                spCmd.Parameters.Add("@PCCode", SqlDbType.Char).Value = pcCode;
                spCmd.Parameters.Add("@PlanCode", SqlDbType.Char).Value = planCode;
                spCmd.Parameters.Add("@Partcode", SqlDbType.Char).Value = partCode;
                spCmd.Parameters.Add("@CpyKit", SqlDbType.Char).Value = cpyKit;

                await using var spReader = await spCmd.ExecuteReaderAsync(cancellationToken);
                result = await MapAsync(spReader, cancellationToken);
            }

            return result;
        }

        // Maps whichever columns are present (the two branches return different shapes)
        private static async Task<List<BendingCpyKitDto>> MapAsync(SqlDataReader reader, CancellationToken cancellationToken)
        {
            var list = new List<BendingCpyKitDto>();

            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));

            string? GetStr(string c) =>
                cols.Contains(c) && reader[c] != DBNull.Value ? reader[c].ToString() : null;

            decimal? GetDec(string c)
            {
                if (!cols.Contains(c) || reader[c] == DBNull.Value) return null;
                return decimal.TryParse(reader[c].ToString(), out var d) ? d : null;
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                // EDt may be a real datetime (inline query) OR the literal text 'Null' (SP).
                // Read it as a string and normalize 'Null' -> null so Angular gets a clean value.
                var edtRaw = GetStr("EDt");
                var edt = string.Equals(edtRaw, "Null", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : edtRaw;

                list.Add(new BendingCpyKitDto
                {
                    KitDesc = GetStr("KitDesc"),
                    KitCode = GetStr("KitCode"),
                    PfbCode = GetStr("PfbCode"),
                    EDt = edt,                 // <-- string, NEVER Convert.ToDateTime
                    Rate = GetDec("Rate"),
                    Strokes = GetDec("Strokes"),
                    CatID = GetStr("CatID"),
                    Bal = GetDec("Bal"),
                    PRate = GetDec("PRate"),
                    SRate = GetDec("SRate"),
                    Pwt = GetDec("Pwt"),
                    Psqft = GetDec("Psqft"),
                });
            }

            return list;
        }

        public async Task<IEnumerable<Dictionary<string, object?>>> GetCpyKitDtsAsync(string pcCode, int batchQty, string cpyKitCode, string bomCode, string pfbCode, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connStr);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand("CpyKitDts_NewERP", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;

            // NOTE: original passed BatchQty as SqlDbType.Char even though it's an int.
            // Sending it as Int is correct; if your SP param is actually char, switch back.
            cmd.Parameters.Add("@PCCode", SqlDbType.Char).Value = pcCode;
            cmd.Parameters.Add("@BatchQty", SqlDbType.Int).Value = batchQty;
            cmd.Parameters.Add("@CpyKitcode", SqlDbType.Char).Value = cpyKitCode;
            cmd.Parameters.Add("@BOMCode", SqlDbType.Char).Value = bomCode;
            cmd.Parameters.Add("@PFBCode", SqlDbType.Char).Value = pfbCode;

            var rows = new List<Dictionary<string, object?>>();

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                }
                rows.Add(row);
            }

            return rows;
        }

        public async Task<string> SubmitBendingAsync(CpyPrcBendRequest CpyPrcBendReq, CancellationToken cancellationToken = default)
        {
            string PrcNo = "";
            string strReqCode = "";
            strReqCodeCPYAssly = "";

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

                // Unit 1 Ben -> FEB process line A,B,C ; Unit 4 Ben -> FEB process line A,B,C
                string StrFabrication_PCCode_Act = "0";
                if (CpyPrcBendReq.PCCode_Act.Trim() == "01.098") StrFabrication_PCCode_Act = "01.101";       // Fab A
                else if (CpyPrcBendReq.PCCode_Act.Trim() == "01.099") StrFabrication_PCCode_Act = "01.102";  // Fab B
                else if (CpyPrcBendReq.PCCode_Act.Trim() == "01.100") StrFabrication_PCCode_Act = "01.103";  // Fab C
                else if (CpyPrcBendReq.PCCode_Act.Trim() == "03.070") StrFabrication_PCCode_Act = "03.073";  // Fab B
                else if (CpyPrcBendReq.PCCode_Act.Trim() == "03.071") StrFabrication_PCCode_Act = "03.074";  // Fab C
                else if (CpyPrcBendReq.PCCode_Act.Trim() == "03.072") StrFabrication_PCCode_Act = "03.075";  // Fab C


                string StrFabrication_PCCode = "0";
                if (CpyPrcBendReq.PCCode.Trim() == "01.002") StrFabrication_PCCode = " 01.008";       // unit 1
                else if (CpyPrcBendReq.PCCode.Trim() == "03.004") StrFabrication_PCCode = " 03.002";  // unit 4



                //  string  benSheetQty = ComCon.getTranName("IF EXISTS (SELECT 1 FROM processfeedback WITH (UPDLOCK, HOLDLOCK) WHERE canopyplancode = '" + CpyPrcBendReq.PlanCode + "' AND partcode = '" + CpyPrcBendReq.CpyKitcode + "' AND ProfitCenterCode = '" + CpyPrcBendReq.PCCode + "' AND Active = '1') SELECT '1' ELSE SELECT '0'", "tbl_PFBCode", "PFBCode", con, tran);

                string benSheetQty = ComCon.getTranName("IF EXISTS (SELECT 1 FROM processfeedback WITH (UPDLOCK, HOLDLOCK) WHERE canopyplancode = '" + CpyPrcBendReq.PlanCode + "' AND partcode = '" + CpyPrcBendReq.CpyKitcode + "' AND PCCode_Act = '" + CpyPrcBendReq.PCCode_Act + "' AND Active = '1') SELECT '1' as PFBCode ELSE SELECT '0' as PFBCode", "tbl_PFBCode", "PFBCode", con, tran);

                if (benSheetQty != "0" && CpyPrcBendReq.PFBCode.Substring(0, 3) == "NEW")
                {
                    PrcNo = "Process is already saved.";

                    tran = (SqlTransaction)await con.BeginTransactionAsync();

                    SqlCommand cmd;
                    sb.Remove(0, sb.Length);
                    sb.Append("UPDATE CanopyPlanDtsSub SET CPBStatus='D',CPBQty='" + CpyPrcBendReq.PrcQty + "' " +
                              "WHERE CPCode = '" + CpyPrcBendReq.PlanCode + "' " +
                              "AND partcode = '" + CpyPrcBendReq.CpyKitcode.Trim() + "' " +
                              "AND CatID = '" + CpyPrcBendReq.CatID.Trim() + "'");
                    cmd = new SqlCommand(sb.ToString(), con);
                    cmd.Transaction = tran;
                    await cmd.ExecuteNonQueryAsync();

                    await tran.CommitAsync();
                    return PrcNo;
                }
                else
                {
                    string BendReqFlag = "";

                    if (CpyPrcBendReq.PFBCode.Substring(0, 3) == "NEW" && CpyPrcBendReq.CatID.ToString() == "029")
                    {
                        var dsDetailsSub = await ComCon.procTranDSAsync(
                            "Exec Get_ProductWip_Ben_CanopyAssly_NewERP '" + CpyPrcBendReq.PCCode_Act.Trim() + "','" + CpyPrcBendReq.ProductCode.Trim() + "'",
                            "tbl_ProductWip_Ben_CanopyAssly", con, tran);

                        var table = dsDetailsSub.Tables["tbl_ProductWip_Ben_CanopyAssly"];
                        if (table.Rows.Count > 0)
                        {
                            int ClsQty = Convert.ToInt32(table.Rows[0]["ClsQty"]);
                            if (CpyPrcBendReq.PrcQty > ClsQty)
                            {
                                string partDesc = table.Rows[0]["PartDesc"].ToString().Trim();
                                BendReqFlag = string.IsNullOrEmpty(BendReqFlag)
                                    ? partDesc
                                    : BendReqFlag + ", " + partDesc;

                                BendReqFlag = "Insufficient Stock For Part(BR): " + BendReqFlag;
                                return BendReqFlag;
                            }
                        }
                    }

                    if (CpyPrcBendReq.PFBCode.Substring(0, 3) == "NEW")
                    {
                        string[] strMachineNo = Regex.Split(CpyPrcBendReq.MachineCodeSrNo, "-->");

                        // NOTE: tran is still null here (begun just below). These reads run with
                        // no active transaction on con, same as the existence check above - fine.
                        bool ChkforStartCPY = await GetChkforStartCpyAsync(con, tran, CpyPrcBendReq.PCCode_Act.Trim(), CpyPrcBendReq.PlanCode, CpyPrcBendReq.ProductCode, CpyPrcBendReq.CatID);
                        bool ChkforStart = await GetChkforStartAsync(con, tran, CpyPrcBendReq.PCCode_Act.Trim(), CpyPrcBendReq.PlanCode, CpyPrcBendReq.ProductCode, strMachineNo[1].ToString(), CpyPrcBendReq.CatID);

                        tran = (SqlTransaction)await con.BeginTransactionAsync();

                        SqlCommand cmd;

                        // ---------------- Mst Entry ----------------
                        PrcNo = await GetMaxPrcAsync(con, tran, "ProcessFeedback", "PFbCode", ComCon.yearEnd(con, tran), CpyPrcBendReq.PCCode_Act.Trim().Substring(0, 2));
                        string NstPart = "0";
                        string NstWtsqft = "0";
                        if (CpyPrcBendReq.CpyKitcode.Trim().Substring(11, 1) == "1" || CpyPrcBendReq.CpyKitcode.Trim().Substring(11, 1) == "0")
                        {
                            NstPart = ComCon.getTranName("select KitCode from Bomdetails where BOMCode='" + CpyPrcBendReq.BOMcode + "' and Kitcode Like '004%' and  substring(Kitcode,11,1) in ('4') and Partcode='" + CpyPrcBendReq.CpyKitcode.Trim() + "'", "TblNstPartCode", "KitCode", con, tran);
                        }
                        else if (CpyPrcBendReq.CpyKitcode.Trim().Substring(11, 1) == "6")
                        {
                            NstPart = ComCon.getTranName("select KitCode from Bomdetails where BOMCode='" + CpyPrcBendReq.BOMcode + "' and Kitcode Like '004%' and  substring(Kitcode,11,1) in ('4') group by KitCode ", "TblNstPartCode", "KitCode", con, tran);
                        }
                        else if (CpyPrcBendReq.CpyKitcode.Trim().Substring(11, 1) == "2" || CpyPrcBendReq.CpyKitcode.Trim().Substring(11, 1) == "3")
                        {
                            NstPart = CpyPrcBendReq.CpyKitcode.Trim();
                        }

                        NstWtsqft = ComCon.getTranName("Select convert(varchar(10),Pwt)+'-->'+convert(varchar(10),PSqft ) as PwtSqft from ProfitcenterPlDetails where ProfitcenterCode='01.007' and Partcode='" + NstPart + "'", "TblPwtSqft", "PwtSqft", con, tran);
                        string[] strNstWtsqft = Regex.Split(NstWtsqft.Trim(), "-->");

                        sb.Remove(0, sb.Length);
                        sb.Append("insert into processfeedback(GroupPFBCode,PFBCode,MaxSrNo,Dt,EDt,Yr,MachineCode,SerialNo,ProfitCenterCode,ProductCode,CanopyPlanCode,TurretKitCode,");
                        sb.Append("NestingforCode,NestingforQty,nstWtPerUt,nstSqftPerUt,WtperUt,SqftperUt,");
                        sb.Append("PartCode,ProcessQty,CompanyCode,PFBRate,PPWCode,Remark,SilCladdingRate,CatID,PCCode_Act)");
                        sb.Append(" values('" + PrcNo.Trim() + "','" + PrcNo.Trim() + "','" + (PrcNo.Substring(10, 8)) + "', ");
                        //local
                        if (ChkforStart == true)
                        {
                            sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',Null,");
                        }
                        else if (ChkforStart == false)
                        {
                            //local
                           // sb.Append("'" + ComCon.dateinyyyymmdd(await GetPrevPrcTimeAsync(con, tran, CpyPrcBendReq.PCCode_Act, CpyPrcBendReq.PlanCode, CpyPrcBendReq.ProductCode, strMachineNo[1].ToString())) + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                          //server
                            sb.Append("'" + (await GetPrevPrcTimeAsync(con, tran, CpyPrcBendReq.PCCode_Act, CpyPrcBendReq.PlanCode, CpyPrcBendReq.ProductCode, strMachineNo[1].ToString())) + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");

                        }
                        //End



                        sb.Append("'" + ComCon.yearEnd(con, tran) + "','" + strMachineNo[0].ToString() + "','" + strMachineNo[1].ToString() + "','" + CpyPrcBendReq.PCCode.Trim() + "','" + CpyPrcBendReq.ProductCode.Trim() + "',");
                        sb.Append("'" + CpyPrcBendReq.PlanCode.Trim() + "','" + CpyPrcBendReq.BOMcode.Trim() + "',");
                        sb.Append("'" + NstPart + "','" + CpyPrcBendReq.BatchQty + "','" + double.Parse(strNstWtsqft[0].Trim()) + "','" + double.Parse(strNstWtsqft[1].Trim()) + "','" + CpyPrcBendReq.PWt + "','" + CpyPrcBendReq.PSqft + "',");
                        sb.Append("'" + CpyPrcBendReq.CpyKitcode.Trim() + "','" + CpyPrcBendReq.PrcQty + "','" + CpyPrcBendReq.PCCode_Act.Trim().Substring(0, 2) + "', ");
                        sb.Append("'" + CpyPrcBendReq.PFBRate + "','" + CpyPrcBendReq.EmpCode + "','Nil','" + CpyPrcBendReq.Strokes + "', '" + CpyPrcBendReq.CatID + "','" + CpyPrcBendReq.PCCode_Act.Trim() + "')");
                        cmd = new SqlCommand(sb.ToString(), con);
                        cmd.Transaction = tran;
                        await cmd.ExecuteNonQueryAsync();

                        // ---------------- Action Taken File Attachment ----------------
                        if (!string.IsNullOrEmpty(CpyPrcBendReq.AttachFileDts.ToString().Trim()))
                        {
                            string[] strPlanDts = Regex.Split(CpyPrcBendReq.AttachFileDts, "@#@");
                            int SrNoA = 0;
                            foreach (String StrSub in strPlanDts)
                            {
                                SrNoA += 1;
                                string[] DtsA = Regex.Split(StrSub.ToString().Trim(), "-->");
                                string FileName = PrcNo.ToString().Trim().Substring(4, 5).Trim() + PrcNo.ToString().Trim().Substring(10, 8).Trim() + "-" + (SrNoA) + Path.GetExtension(DtsA[1].ToString().Trim());
                                string StrMpath = ComCon.getMainFilePath("PrcBend") + "/" + FileName.ToString().Trim();
                                string StrTpath = "C:/TempERPFile/TempPrcBend/" + CpyPrcBendReq.EmpCode.Trim() + "/" + DtsA[1].ToString().Trim();
                                string StrTempPath = "C:/TempERPFile/TempPrcBend/" + CpyPrcBendReq.EmpCode.Trim();
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
                        int recCount = ComCon.CountChars(CpyPrcBendReq.PrcDts, ",");
                        string[] strPrcDts = Regex.Split(CpyPrcBendReq.PrcDts, ",");
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
                            sb.Append(" values('" + CpyPrcBendReq.PCCode.Trim() + "','" + Dts[0].ToString().Trim() + "',");
                            sb.Append("'" + PrcNo.Trim() + "',GetDate(),'" + double.Parse(Dts[2].ToString().Trim()) + "','" + CpyPrcBendReq.PCCode.Trim() + "',1,'" + CpyPrcBendReq.PCCode_Act.Trim() + "','" + CpyPrcBendReq.PCCode_Act.Trim() + "')");
                            cmd = new SqlCommand(sb.ToString(), con);
                            cmd.Transaction = tran;
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // ---------------- Status Update ----------------
                        sb.Remove(0, sb.Length);
                        sb.Append("Update CanopyPlanDtsSub set CPBQty=CPBQty + '" + CpyPrcBendReq.PrcQty + "' where CPCode='" + CpyPrcBendReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcBendReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcBendReq.CpyKitcode + "' and CatId='" + CpyPrcBendReq.CatID.Trim() + "' ");
                        cmd = new SqlCommand(sb.ToString(), con);
                        cmd.Transaction = tran;
                        await cmd.ExecuteNonQueryAsync();

                        string cntPrcQty = "0";
                        cntPrcQty = ComCon.getTranName("select CPQty-CPBQty as BalQty from CanopyPlanDtsSub where CPCode='" + CpyPrcBendReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcBendReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcBendReq.CpyKitcode + "' and CatId='" + CpyPrcBendReq.CatID.Trim() + "'  ", "BendingPrc", "BalQty", con, tran);
                        if (cntPrcQty == "0")
                        {
                            sb.Remove(0, sb.Length);
                            sb.Append("Update CanopyPlanDtsSub set CPBStatus='D' where CPCode='" + CpyPrcBendReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcBendReq.ProductCode.Trim() + "' and Partcode='" + CpyPrcBendReq.CpyKitcode + "' and CatId='" + CpyPrcBendReq.CatID.Trim() + "' ");
                            cmd = new SqlCommand(sb.ToString(), con);
                            cmd.Transaction = tran;
                            await cmd.ExecuteNonQueryAsync();
                        }

                        string cntBndStatus = "0";
                        cntBndStatus = ComCon.getTranName("select Count(CPBStatus) as CPBStatus from CanopyPlanDtsSub where CPCode='" + CpyPrcBendReq.PlanCode.Trim() + "' and CpyPartcode='" + CpyPrcBendReq.ProductCode.Trim() + "' and CatId='" + CpyPrcBendReq.CatID.Trim() + "' and  CPBStatus='P'  ", "BendingPrc", "CPBSTatus", con, tran);
                        if (cntBndStatus == "0")
                        {
                            if (CpyPrcBendReq.CatID.ToString() == "029")
                            {
                                sb.Remove(0, sb.Length);
                                sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode,IssueCode,IssueDate, IssueQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                sb.Append(" values('" + CpyPrcBendReq.ProductCode.ToString().Trim() + "','" + CpyPrcBendReq.PCCode.Trim() + "','" + StrFabrication_PCCode.Trim() + "',");
                                sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + CpyPrcBendReq.BatchQty.ToString().Trim() + "',0,'" + CpyPrcBendReq.PCCode_Act.Trim() + "','" + StrFabrication_PCCode_Act.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con);
                                cmd.Transaction = tran;
                                await cmd.ExecuteNonQueryAsync();

                                sb.Remove(0, sb.Length);
                                sb.Append("INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)");
                                sb.Append(" values('" + CpyPrcBendReq.ProductCode.ToString().Trim() + "','" + CpyPrcBendReq.PCCode.Trim() + "','" + StrFabrication_PCCode.Trim() + "',");
                                sb.Append("'" + PrcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','" + CpyPrcBendReq.BatchQty.ToString().Trim() + "',0,'" + CpyPrcBendReq.PCCode_Act.Trim() + "','" + StrFabrication_PCCode_Act.Trim() + "')");
                                cmd = new SqlCommand(sb.ToString(), con);
                                cmd.Transaction = tran;
                                await cmd.ExecuteNonQueryAsync();

                                sb.Remove(0, sb.Length);
                                sb.Append("Update CanopyPlanSerialNo set CPBSerialStatus='D' where CPCode='" + CpyPrcBendReq.PlanCode.ToString().Trim() + "' and Partcode='" + CpyPrcBendReq.ProductCode.ToString().Trim() + "'  ");
                                cmd = new SqlCommand(sb.ToString(), con);
                                cmd.Transaction = tran;
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // ---------------- User Activity ----------------
                        cmd = new SqlCommand("InsertLoginTransactionDetails", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EmpID", CpyPrcBendReq.EmpCode.Trim());
                        cmd.Parameters.AddWithValue("@TransactionType", "S");
                        cmd.Parameters.AddWithValue("@TransactionFrom", "Bending Process");
                        cmd.Parameters.AddWithValue("@TransactionNo", PrcNo.Trim());
                        cmd.Parameters.AddWithValue("@CompanyCode", CpyPrcBendReq.PCCode_Act.Substring(0, 2).Trim());
                        cmd.Transaction = tran;
                        await cmd.ExecuteNonQueryAsync();

                        // ---------------- Prc Below 1000 ----------------
                        string GetMaxRatePartCode = "0";
                        string PrcBelowRate = "";

                        GetMaxRatePartCode = ComCon.getTranName("Select top 1 PartCode From CanopyplandtsSub where CPCode='" + CpyPrcBendReq.PlanCode + "' and CpyPartcode='" + CpyPrcBendReq.ProductCode + "' and CatID='" + CpyPrcBendReq.CatID + "' order by rate desc  ", "tbl_ChkForMaxRate", "PartCode", con, tran);

                        if (GetMaxRatePartCode.Trim() == CpyPrcBendReq.CpyKitcode.Trim())
                        {
                            DataSet dsKitbelowRate = ComCon.procTranDS("select Pf.Partcode,Pl.rate,Pl.PurRate,Pwt,Psqft From CanopyPlanDtsSubBelowStdRate Pf Inner Join ProfitcenterPldetails Pl on  Pf.Partcode=Pl.partcode where CpyPartcode='" + CpyPrcBendReq.ProductCode.ToString().Trim() + "' and CPCode='" + CpyPrcBendReq.PlanCode + "' and CatID='" + CpyPrcBendReq.CatID + "' and ProfitcenterCode='01.002' ", "tbl_KitbelowRate", con, tran);
                            if (dsKitbelowRate != null && dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows.Count > 0)
                            {
                                for (int br = 0; br < dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows.Count; br++)
                                {
                                    // ---- Mst Entry (below rate) ----
                                    PrcBelowRate = await GetMaxPrcAsync(con, tran, "ProcessFeedback", "PFbCode", ComCon.yearEnd(con, tran), CpyPrcBendReq.PCCode_Act.Trim().Substring(0, 2));

                                    sb.Remove(0, sb.Length);
                                    sb.Append("insert into processfeedback(GroupPFBCode,PFBCode,MaxSrNo,Dt,EDt,Yr,MachineCode,SerialNo,ProfitCenterCode,ProductCode,CanopyPlanCode,TurretKitCode,");
                                    sb.Append("NestingforCode,NestingforQty,nstWtPerUt,nstSqftPerUt,WtperUt,SqftperUt,");
                                    sb.Append("PartCode,ProcessQty,CompanyCode,PFBRate,PPWCode,Remark,CatID,PCCode_Act,silCladdingRate)");
                                    sb.Append(" values('" + PrcNo.Trim() + "','" + PrcBelowRate.Trim() + "','" + (PrcBelowRate.Substring(10, 8)) + "', ");
                                    sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                                    sb.Append("'" + ComCon.yearEnd(con, tran) + "','" + strMachineNo[0].ToString() + "','" + strMachineNo[1].ToString() + "','" + CpyPrcBendReq.PCCode.Trim() + "','" + CpyPrcBendReq.ProductCode.Trim() + "',");
                                    sb.Append("'" + CpyPrcBendReq.PlanCode.Trim() + "','" + CpyPrcBendReq.BOMcode.Trim() + "',");
                                    // NOTE: original indexes the raw NstWtsqft string here (char), preserved as-is.
                                    sb.Append("'" + NstPart + "','" + CpyPrcBendReq.BatchQty + "','" + NstWtsqft[0].ToString().Trim() + "','" + NstWtsqft[1].ToString().Trim() + "','" + double.Parse(dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Pwt"].ToString().Trim()) + "','" + double.Parse(dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["PSqft"].ToString().Trim()) + "',");
                                    sb.Append("'" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "','" + CpyPrcBendReq.PrcQty + "','" + CpyPrcBendReq.PCCode_Act.Trim().Substring(0, 2) + "', ");
                                    sb.Append("'" + ComCon.getTranName("Select Isnull(Max(Rate),0) as Rate From ProfitcenterPLDetails where ProfitcenterCode='" + CpyPrcBendReq.PCCode_Act + "' and Partcode='" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "' ", "tbl_PLRate", "Rate", con, tran) + "'");
                                    sb.Append(", '" + CpyPrcBendReq.EmpCode + "','Nil','" + CpyPrcBendReq.CatID + "' ,'" + CpyPrcBendReq.PCCode_Act + "', '" + ComCon.getTranName("Select Isnull(Max(Strokes),0) as Strokes From CanopyPlanDtsSubBelowStdRate where CPCode='" + CpyPrcBendReq.PlanCode + "' and CPYPartcode='" + CpyPrcBendReq.ProductCode + "' and Partcode='" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "'", "tbl_Strokes", "Strokes", con, tran) + "')");
                                    // sb.Append(", '" + CpyPrcBendReq.EmpCode + "','Nil','" + CpyPrcBendReq.CatID + "' , '" + ComCon.getTranName("Select Isnull(Max(Strokes),0) as Strokes From CanopyPlanDtsSubBelowStdRate where CPCode='" + CpyPrcBendReq.PlanCode + "' and CPYPartcode='" + CpyPrcBendReq.ProductCode + "' and Partcode='" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "' and PCCode_Act='" + CpyPrcBendReq.PCCode.Trim() + "' ", "tbl_Strokes", "Strokes", con, tran) + "')");
                                    cmd = new SqlCommand(sb.ToString(), con);
                                    cmd.Transaction = tran;
                                    await cmd.ExecuteNonQueryAsync();

                                    // ---- Dts Entry (below rate) ----
                                    int ChkStk = 0;
                                    DataSet dsKitbelowRatedts = ComCon.procTranDS("select Bd.Partcode,P.Partdesc,p.AliseName,Qty,Purrate,rate,Pwt,Psqft,Bd.Length,bd.Width,bd.Thickness,bd.LossWgt,Bd.categoryID, " +
                                       " (select Round(Isnull(Sum(Recqty) - sum(IssueQty), 0), 00) as Stk From ( select Sum(ReceivedQty) as Recqty, " +
                                       " 0.00 as IssueQty from stockwip where ToProfitcenterCode_Act = '" + CpyPrcBendReq.PCCode_Act + "' and StockType = '1' " +
                                       " and Partcode = Bd.Partcode and  ReceivedQty > 0  Union all " +
                                       " select 0.00 as Recqty, sum(IssueQty) as IssueQty from stockwip where FromProfitcenterCode_Act = '" + CpyPrcBendReq.PCCode_Act + "' and StockType = '1' " +
                                       " and Partcode = Bd.Partcode and  IssueQty > 0) as stk) as Stock " +
                                       "From BOMDetails Bd Inner Join ProfitcenterPLdetails Pl On   Bd.KitCode=Pl.Partcode  Inner Join part P On Bd.Partcode=P.partcode " +
                                       " where Bd.BOMCode='" + CpyPrcBendReq.BOMcode + "' and Bd.KitCode='" + dsKitbelowRate.Tables["tbl_KitbelowRate"].Rows[br]["Partcode"].ToString().Trim() + "' " +
                                       " and Pl.ProfitcenterCode='01.002' and Bd.MOB='M' and P.Kit='0' ", "tbl_KitbelowRatedts", con, tran);
                                    if (dsKitbelowRatedts != null && dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows.Count > 0)
                                    {
                                        SrNo = 0;
                                        for (int brd = 0; brd < dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows.Count; brd++)
                                        {
                                            if ((Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Qty"].ToString().Trim()) * CpyPrcBendReq.PrcQty) >
                                                Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Stock"].ToString().Trim()))
                                            {
                                                if (ChkStk == 0)
                                                {
                                                    PrcNo = dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Partdesc"].ToString().Trim();
                                                    ChkStk = 1;
                                                }
                                                else
                                                {
                                                    PrcNo = PrcNo + "," + dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Partdesc"].ToString().Trim();
                                                }
                                            }
                                            else if (ChkStk == 0)
                                            {
                                                SrNo += 1;
                                                sb.Remove(0, sb.Length);
                                                sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,");
                                                sb.Append("PFBRate,SaleRate,WtPerUt,SqftPerUt,PLength,PWidth,PThickness,PLossWt,PCatagoryCode)");
                                                sb.Append("values('" + PrcBelowRate.Trim() + "','" + SrNo + "',");
                                                sb.Append("'" + dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Partcode"].ToString().Trim().Trim() + "',");
                                                sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Qty"].ToString().Trim()) + "',");
                                                sb.Append("'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Qty"].ToString().Trim()) * CpyPrcBendReq.PrcQty + "',");
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
                                                sb.Append(" values('" + CpyPrcBendReq.PCCode.Trim() + "','" + dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Partcode"].ToString().Trim() + "',");
                                                sb.Append("'" + PrcBelowRate.Trim() + "',GetDate(),'" + Convert.ToDouble(dsKitbelowRatedts.Tables["tbl_KitbelowRatedts"].Rows[brd]["Qty"].ToString()) * CpyPrcBendReq.PrcQty + "','" + CpyPrcBendReq.PCCode.Trim() + "',1,'" + CpyPrcBendReq.PCCode_Act.Trim() + "','" + CpyPrcBendReq.PCCode_Act.Trim() + "')");
                                                cmd = new SqlCommand(sb.ToString(), con);
                                                cmd.Transaction = tran;
                                                await cmd.ExecuteNonQueryAsync();
                                            }
                                        }
                                        if (ChkStk > 0)
                                        {
                                            PrcNo = "Insufficient Stock For Part(BR): " + PrcNo;
                                            await tran.RollbackAsync();   // discard the open transaction before returning
                                            return PrcNo;
                                        }
                                    }
                                }
                            }
                        }

                        await tran.CommitAsync();
                        //await tran.RollbackAsync();
                        PrcNo = "ProcessCode:" + PrcNo + "    For Bending  Saved SuccessFully ";
                    }
                    else if (CpyPrcBendReq.PFBCode.Substring(0, 3) == "PSH")
                    {
                        tran = (SqlTransaction)await con.BeginTransactionAsync();

                        SqlCommand cmd;
                        sb.Remove(0, sb.Length);
                        sb.Append("Update ProcessFeedBack set EDt='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'  where PFBCode='" + CpyPrcBendReq.PFBCode.Trim() + "' ");
                        cmd = new SqlCommand(sb.ToString(), con);
                        cmd.Transaction = tran;
                        await cmd.ExecuteNonQueryAsync();

                        string cntDatecQty = "0";
                        cntDatecQty = ComCon.getTranName("select count(Dt) as DT from ProcessFeedback where PFBCode='" + CpyPrcBendReq.PFBCode.Trim() + "'and Dt='1900-01-01 00:00:00.000' ", "Tbl_Dt", "Dt", con, tran);
                        if (cntDatecQty != "0")
                        {
                            sb.Remove(0, sb.Length);
                            sb.Append("Update ProcessFeedBack set Dt='" + DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss") + "'  where PFBCode='" + CpyPrcBendReq.PFBCode.Trim() + "' ");
                            cmd = new SqlCommand(sb.ToString(), con);
                            cmd.Transaction = tran;
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await tran.CommitAsync();
                        //await tran.RollbackAsync();
                        PrcNo = "ProcessCode=" + CpyPrcBendReq.PFBCode.Trim() + " For Bending  End SuccessFully ";
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
                // The connection is owned by the DbContext; only close it if we opened it here.
                if (openedHere && con.State == ConnectionState.Open) await con.CloseAsync();
            }
        }

        // 1. Max process-code generator (single ExecuteScalar, padding simplified)
        private async Task<string> GetMaxPrcAsync(SqlConnection con, SqlTransaction tran, string tableName, string fieldName, string yr, string compCode, CancellationToken cancellationToken = default)
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
        private async Task<bool> GetChkforStartAsync(SqlConnection con, SqlTransaction tran, string pcCode, string planCode, string productCode, string machineNo, string catId, CancellationToken cancellationToken = default)
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
        private async Task<bool> GetChkforStartCpyAsync(SqlConnection con, SqlTransaction tran, string pcCode, string planCode, string productCode, string catId, CancellationToken cancellationToken = default)
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
        private async Task<string> GetPrevPrcTimeAsync(SqlConnection con, SqlTransaction tran, string pcCode, string planCode, string productCode, string machineNo, CancellationToken cancellationToken = default)
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


        public async Task<string> SubmitBendingCheckerAsync(CpyPrcBendCheckerRequest req, CancellationToken ct = default)
        {
            string PrcNo = "";
            string strReqCode = "";          // never set in the bending AUTH path; kept for message parity
            string strReqCodeCPYAssly = "";  // referenced in original success message; kept as-is
            string strKanBan = "";

            await using var con = new SqlConnection(_connStr);
            await con.OpenAsync(ct);
            await using var tran = (SqlTransaction)await con.BeginTransactionAsync(ct);

            try
            {
                var strPlanDts = Regex.Split(req.ProductionDetails ?? "", "@@#@@");

                // Bending PC -> Fabrication PC mapping (kept from original; not used downstream)
                string StrFabricationPCCode_Act = req.PCCode_Act.Trim() switch
                {
                    "01.098" => "01.101",   // Fab A Unit 1
                    "01.099" => "01.102",   // Fab B Unit 1
                    "01.100" => "01.103",   // Fab C Unit 1
                    "03.070" => "03.073",   // Fab B Unit 4
                    "03.071" => "03.074",   // Fab C Unit 4
                    "03.072" => "03.075",   // Fab C Unit 4
                    _ => "0"
                };

                string StrFabricationPCCode = req.PCCode.Trim() switch
                {
                    "01.002" => "01.008",   // Fab  Unit 1
                    "03.004" => "03.002",   // Fab Unit 4
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
                    foreach (var StrSub in strPlanDts)
                    {
                        var DtsPlan = Regex.Split(StrSub.Trim(), "@#@");
                        if (DtsPlan[3] != null && DtsPlan[3].Trim() == "0")
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

                    // ---- any bending rows still unchecked for this plan / product / cat / PC? ----
                    var cntBndStatus = await ComCon.GetScalarAsync(
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

                    if (cntBndStatus == "0")
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
                                ("@ActNo", req.BatchQty.Trim()),
                                ("@Remark", "Auto Req For Plan No: " + req.ProductCode + " and Prc No: " + PrcNo));

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

                        // ---- activity log (runs whenever fully checked, matching original) ----
                        await ExecProcAsync(con, tran, ct, "insertLoginTransactionDetails",
                            ("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                            ("@EmpID", req.EmpCode),
                            ("@TransactionType", "S"),
                            ("@TransactionFrom", "MaterialRequisitionWithoutPlan"),
                            ("@TransactionNo", strKanBan),
                            ("@CompanyCode", req.PCCode_Act.Substring(0, 2).Trim()));

                        await tran.CommitAsync(ct);
                        //  await tran.RollbackAsync(ct);
                        PrcNo = "ProcessCode:" + req.PFBCode.Trim() + " and ReqCode: " + strKanBan + " For Bending Saved SuccessFully ";
                        return PrcNo;
                    }

                    await tran.CommitAsync(ct);
                    //await tran.RollbackAsync(ct);
                    PrcNo = "ProcessCode:" + req.PFBCode.Trim() + " For Bending Saved SuccessFully ";
                    return PrcNo;
                }
                else
                {
                    // ---- Status is NOT "AUTH" (REJECT) ----
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
                                " Bending Checker PlanCode: {0}, PFBCode: {1}, 6MType: {2}, Description: {3}",
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

                    // Original committed inside the loop (throws on a 2nd assigned item under one
                    // transaction). Commit once here to keep the intended behaviour atomic.

                    await tran.CommitAsync(ct);
                    // await tran.RollbackAsync(ct);
                    PrcNo = "ProcessCode:" + req.PFBCode.Trim() + " For Bending Saved SuccessFully ";
                    return PrcNo;
                }
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync(ct);
                return "StackTrace " + ex.StackTrace + " Message " + ex.Message;
            }
        }

        // ---- ADO.NET helpers (identical to the CNC service) ----
        private static async Task ExecNonQueryAsync(SqlConnection con, SqlTransaction tran, CancellationToken ct, string sql, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(sql, con, tran);
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static async Task ExecProcAsync(SqlConnection con, SqlTransaction tran, CancellationToken ct, string procName, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(procName, con, tran) { CommandType = CommandType.StoredProcedure };
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
