using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.ResponseDTO.CNC;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace KalaGenset.ERP.Core.Services
{
    public class CNCServices : ICNC
    {
        private readonly KalaDbContext _db;

        private readonly string _connStr;
        private readonly CommonCon ComCon;

        public CNCServices(KalaDbContext context, ICommonService common, ILogger<CNCServices> logger, IConfiguration config, CommonCon com)
        {
            _db = context;
            ComCon = com;
            _connStr = config.GetConnectionString("KalaDbContext")
                       ?? throw new InvalidOperationException("Connection string 'KalaDbContext' not found.");
        }

        public async Task<List<Dictionary<string, object>>> LoadMachineAsync(string pcCode)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "LoadMachine_NewERP";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));

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

        public async Task<List<Dictionary<string, object>>> LoadOSSupplierAsync(string pcCode)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "LoadOSSupplier_NEWERP";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    // no parameters for this proc

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


        public async Task<List<Dictionary<string, object>>> GetCpyPrcddlAsync( string pcCode, string machineCode, string kva, string model, string planCode, string catId)
        {
            var parts = Regex.Split(machineCode?.Trim() ?? "", "-->");
            var machine = parts.Length > 0 ? parts[0].Trim() : "";
            var serialNo = parts.Length > 1 ? parts[1].Trim() : "";

            const string feedbackDate = "2020-07-10 00:00:00";

            // ---- Branch 1: KVA list ----
            if (kva == "0" && model == "0" && planCode == "0" && catId == "0")
            {
                string kvaFilter = pcCode == "01.190" ? " and P.kva < '82.5' "
                                 : pcCode == "01.105" ? " and P.kva >= '82.5' "
                                 : "";

                var sql = $@"select P.KVA, P.KVA as KVA1
                     from processfeedback pf
                     inner join Part P on Pf.ProductCode = P.partcode
                     where PCCode_Act = @PCCode {kvaFilter}
                       and MachineCode = @Machine and SerialNo = @SerialNo
                       and Edt is null and Pf.Active = '1'
                       and ProductCode like '401%' and Pf.Dt >= @FbDate
                     group by P.KVA";

                var rows = await QueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                    cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                });

                return rows.Count > 0 ? rows : await GetddlCpyPrcAsync(pcCode, kva, model, planCode, catId);
            }

            // ---- Branch 2: Model list ----
            if (kva != "0" && model == "0" && planCode == "0" && catId == "0")
            {
                string kvaFilter = pcCode == "01.190" ? " and P.kva < '82.5' "
                                 : pcCode == "01.105" ? " and P.kva >= '82.5' "
                                 : "";

                var sql = $@"select P.Model, P.Model as Model1
                     from processfeedback pf
                     inner join Part P on Pf.ProductCode = P.partcode
                     where PCCode_Act = @PCCode {kvaFilter}
                       and MachineCode = @Machine and SerialNo = @SerialNo
                       and Edt is null and P.KVA = @KVA and Pf.Active = '1'
                       and Pf.Dt >= @FbDate
                     group by P.Model";

                var rows = await QueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                    cmd.Parameters.Add(new SqlParameter("@KVA", kva));
                    cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                });

                return rows.Count > 0 ? rows : await GetddlCpyPrcAsync(pcCode, kva, model, planCode, catId);
            }

            // ---- Branch 3: KVA+Model selected, fetch plan row (Top 1) ----
            if (kva != "0" && model != "0" && planCode == "0" && catId == "0")
            {
                bool isCanopyLine = pcCode.Trim() is "01.190" or "01.105" or "03.069";

                string sql = isCanopyLine
                    ? @"select Top 1 Convert(varchar(10),P.KVA)+'-->'+P.Model as KVAMod, KVA, Model,
                       CanopyPlanCode as CPCode, PF.Dt, PF.ProductCode as Partcode,
                       Partdesc+'-->'+PF.Partcode as Part,
                       ProcessQty as CPQty,
                       (ProcessQty-(select Count(PFBCode) from ProcessFeedbackDetailsSub where PFbCode=pf.PFBCode and EdtD is not null)) as PlanQtyBal,
                       (ProcessQty-(select Count(PFBCode) from ProcessFeedbackDetailsSub where PFbCode=pf.PFBCode and Edt is not null)) as PrcQty,
                       isnull(PFBCode,0) as PFBCode, EDt, TurretKitCode as BOMCode, SupplierCode as SCode
                from processfeedback pf
                inner join Part P on Pf.ProductCode = P.partcode
                where PCCode_Act = @PCCode and MachineCode = @Machine and SerialNo = @SerialNo
                  and Edt is null and P.KVA = @KVA and P.Model = @Model
                  and Pf.Active = '1' and Pf.Dt >= @FbDate
                order by Dt desc"
                    : @"select Top 1 Convert(varchar(10),P.KVA)+'-->'+P.Model as KVAMod, KVA, Model,
                       CanopyPlanCode as CPCode, PF.Dt, PF.ProductCode as Partcode,
                       Partdesc+'-->'+PF.Partcode as Part, ProcessQty as CPQty,
                       isnull(PFBCode,0) as PFBCode, EDt, TurretKitCode as Code, SupplierCode as SCode
                from processfeedback pf
                inner join Part P on Pf.ProductCode = P.partcode
                where PCCode_Act = @PCCode and MachineCode = @Machine and SerialNo = @SerialNo
                  and Edt is null and P.KVA = @KVA and P.Model = @Model
                  and Pf.Active = '1' and Pf.Dt >= @FbDate
                order by Dt desc";

                var rows = await QueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                    cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                    cmd.Parameters.Add(new SqlParameter("@KVA", kva));
                    cmd.Parameters.Add(new SqlParameter("@Model", model));
                    cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                });

                return rows.Count > 0 ? rows : await GetddlCpyPrcAsync(pcCode, kva, model, planCode, catId);
            }

            // ---- Branch 4: Category list ----
            if (kva != "0" && model != "0" && planCode != "0" && catId == "0")
            {
                bool isCanopyLine = pcCode.Trim() is "01.190" or "01.105" or "03.069";

                // For canopy lines the original query was commented out -> goes straight to fallback.
                if (!isCanopyLine)
                {
                    var sql = @"select Pf.CatID, ct.CatagoryName
                        from processfeedback pf
                        inner join Catagory ct on pf.CatID = ct.CatagoryCode
                        inner join Part P on Pf.ProductCode = P.partcode
                        where PCCode_Act = @PCCode and MachineCode = @Machine and SerialNo = @SerialNo
                          and Edt is null and P.KVA = @KVA and Pf.Active = '1' and Pf.Dt >= @FbDate
                        group by Pf.CatID, ct.CatagoryName";

                    var rows = await QueryAsync(sql, cmd =>
                    {
                        cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                        cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                        cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                        cmd.Parameters.Add(new SqlParameter("@KVA", kva));
                        cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
                    });

                    if (rows.Count > 0)
                        return rows;
                }

                // Note: original passed CatID[0] (first char) to the fallback in this branch.
                return await GetddlCpyPrcAsync(pcCode, kva, model, planCode, catId.Length > 0 ? catId[0].ToString() : catId);
            }

            // No branch matched -> empty result
            return new List<Dictionary<string, object>>();
        }

        // Runs an inline parameterized query and returns rows as dictionaries.
        private async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Action<DbCommand> addParameters)
        {
            var data = new List<Dictionary<string, object>>();
            var conn = _db.Database.GetDbConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;
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

        // The shared stored-proc fallback (GetddlCpyPrc_NewERP).
        private async Task<List<Dictionary<string, object>>> GetddlCpyPrcAsync(string pcCode, string kva, string model, string planCode, string catId)
        {
            var data = new List<Dictionary<string, object>>();
            var conn = _db.Database.GetDbConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "GetddlCpyPrc_NewERP";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;
                cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                cmd.Parameters.Add(new SqlParameter("@KVA", kva));
                cmd.Parameters.Add(new SqlParameter("@Model", model));
                cmd.Parameters.Add(new SqlParameter("@PlanCode", planCode));
                cmd.Parameters.Add(new SqlParameter("@CatID", catId));

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

        public async Task<List<Dictionary<string, object>>> LoadCatIDAsync(string pcCode, string planCode)
        {
            var data = new List<Dictionary<string, object>>();
            var conn = _db.Database.GetDbConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "LoadCatagory";              // actual proc name
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;
                cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                cmd.Parameters.Add(new SqlParameter("@PlanCode", planCode));

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

        public async Task<List<Dictionary<string, object>>> LoadProductAsync(string pcCode)
        {
            var data = new List<Dictionary<string, object>>();
            var conn = _db.Database.GetDbConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "LoadProduct_NewERP";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;
                cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));

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


        public async Task<List<Dictionary<string, object>>> GetSheetPartDtsAsync(string pcCode, int sheetSrNo, string machineCode, string sheetPartcode, string planCode, string partcode, string catId)
        {
            var parts = Regex.Split(machineCode?.Trim() ?? "", "-->");
            var machine = parts.Length > 0 ? parts[0].Trim() : "";
            var serialNo = parts.Length > 1 ? parts[1].Trim() : "";

            const string feedbackDate = "2020-07-10 00:00:00";

            // Common WHERE parameters reused by all three inline queries
            void AddCommon(DbCommand cmd)
            {
                cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                cmd.Parameters.Add(new SqlParameter("@Machine", machine));
                cmd.Parameters.Add(new SqlParameter("@SerialNo", serialNo));
                cmd.Parameters.Add(new SqlParameter("@CatID", catId));
                cmd.Parameters.Add(new SqlParameter("@PlanCode", planCode));
                cmd.Parameters.Add(new SqlParameter("@Partcode", partcode));
                cmd.Parameters.Add(new SqlParameter("@FbDate", feedbackDate));
            }

            // ---- Branch 1: load sheet list ----
            if (sheetPartcode == "0" && sheetSrNo == 0)
            {
                var sql = @"select PartDesc as Sheet, Pf.Partcode as SheetCode
                    from processfeedback pf
                    inner join Part P on Pf.Partcode = P.partcode
                    where PCCode_Act = @PCCode
                      and MachineCode = @Machine and SerialNo = @SerialNo
                      and CatID = @CatID and Edt is null and Pf.Active = '1'
                      and Pf.Dt >= @FbDate
                      and CanopyPlanCode = @PlanCode and Productcode = @Partcode";

                var rows = await QueryAsync(sql, AddCommon);
                return rows.Count > 0
                    ? rows
                    : await SheetPartDtsProcAsync("SheetPartDts_NewERP", sheetSrNo, sheetPartcode, planCode, partcode, pcCode, catId);
            }

            // ---- Branch 2: sheet selected, load version/serial list ----
            if (sheetPartcode != "0" && sheetSrNo == 0)
            {
                var sql = @"select VersionCode as SerialNo, VersionCode as SerialNo1
                    from processfeedback pf
                    inner join Part P on Pf.Partcode = P.partcode
                    where PCCode_Act = @PCCode
                      and MachineCode = @Machine and SerialNo = @SerialNo
                      and CatID = @CatID and Edt is null and Pf.Active = '1'
                      and Pf.Dt >= @FbDate
                      and CanopyPlanCode = @PlanCode and Productcode = @Partcode
                      and PF.partcode = @SheetPartcode";

                var rows = await QueryAsync(sql, cmd =>
                {
                    AddCommon(cmd);
                    cmd.Parameters.Add(new SqlParameter("@SheetPartcode", sheetPartcode));
                });

                return rows.Count > 0
                    ? rows
                    : await SheetPartDtsProcAsync("SheetPartDts_NewERP", sheetSrNo, sheetPartcode, planCode, partcode, pcCode, catId);
            }

            // ---- Branch 3: sheet + version selected, load qty/weight details ----
            if (sheetPartcode != "0" && sheetSrNo != 0)
            {
                var sql = @"select PKitQty as QtyPerSet, WtPerUt as WtPerUts,
                           Round(WtPerUt * PKitQty, 2) as WtPerSet, PFBCode as TKITID
                    from processfeedback pf
                    inner join Part P on Pf.Partcode = P.partcode
                    where PCCode_Act = @PCCode
                      and MachineCode = @Machine and SerialNo = @SerialNo
                      and CatID = @CatID and Edt is null and Pf.Active = '1'
                      and Pf.Dt >= @FbDate
                      and CanopyPlanCode = @PlanCode and Productcode = @Partcode
                      and PF.partcode = @SheetPartcode and VersionCode = @SheetSrNo";

                var rows = await QueryAsync(sql, cmd =>
                {
                    AddCommon(cmd);
                    cmd.Parameters.Add(new SqlParameter("@SheetPartcode", sheetPartcode));
                    cmd.Parameters.Add(new SqlParameter("@SheetSrNo", sheetSrNo));
                });

                // ⚠️ Original fallback proc name was blank ("   ") — replace with the real one.
                return rows.Count > 0
                    ? rows
                    : await SheetPartDtsProcAsync("SheetPartDts_NewERP" /* TODO: confirm proc name */, sheetSrNo, sheetPartcode, planCode, partcode, pcCode, catId);
            }

            return new List<Dictionary<string, object>>();
        }


        private async Task<List<Dictionary<string, object>>> SheetPartDtsProcAsync(string procName, int sheetSrNo, string sheetPartcode, string planCode, string partcode, string pcCode, string catId)
        {
            var data = new List<Dictionary<string, object>>();
            var conn = _db.Database.GetDbConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = procName;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;
                cmd.Parameters.Add(new SqlParameter("@SheetSrno", sheetSrNo.ToString()));
                cmd.Parameters.Add(new SqlParameter("@SheetPartcode", sheetPartcode));
                cmd.Parameters.Add(new SqlParameter("@PlanCode", planCode));
                cmd.Parameters.Add(new SqlParameter("@Partcode", partcode));
                cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                cmd.Parameters.Add(new SqlParameter("@CatID", catId));

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


        public async Task<List<Dictionary<string, object>>> GetTKitDtsAsync(string pcCode, string tKitId, int batchQty, string trnsType, string planCode, string prodCode)
        {
            var data = new List<Dictionary<string, object>>();
            var conn = _db.Database.GetDbConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "GetTKitDts_ERPNEW";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;
                cmd.Parameters.Add(new SqlParameter("@PCCode", pcCode));
                cmd.Parameters.Add(new SqlParameter("@TKitID", tKitId));
                cmd.Parameters.Add(new SqlParameter("@BatchQty", batchQty));
                cmd.Parameters.Add(new SqlParameter("@TrnsType", trnsType));
                cmd.Parameters.Add(new SqlParameter("@PlanCode", planCode));
                cmd.Parameters.Add(new SqlParameter("@ProdCode", prodCode));

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

        public async Task<string> SubmitCNCAsync(CpyPrcCNCRequest req, CancellationToken cancellationToken = default)
        {
            string prcNo = "";
            string strTKitCode = "";
            string shRate = "0";
            string strBOMCode = "0";

            var sb = new StringBuilder();

            await using var con = new SqlConnection(_connStr);
            await con.OpenAsync(cancellationToken);
            SqlTransaction tran = null;

            try
            {
                // Count existing sheet qty (uses helper against this con - no tran yet)
                string cntSheetQty = ComCon.getTranName(
                    "SELECT isnull(Count(PFBCode),0) as PFBCode FROM processfeedback WHERE canopyplancode = '" + req.PlanCode +
                    "' AND partcode = '" + req.SheetPartcode + "' AND versioncode = '" + req.SerialNo +
                    "' AND CatID = '" + req.CatID + "' and Active ='1' ",
                    "tbl_PFCNCCode", "PFBCode", con, tran);

                if (cntSheetQty != "0")
                {
                    prcNo = "Process is already saved.";

                    tran = (SqlTransaction)await con.BeginTransactionAsync(cancellationToken);

                    await ExecAsync(con, tran,
                        "UPDATE TurretKitForPrc SET PrcStatus='D' " +
                        "WHERE TKitId = '" + req.TkitId + "' " +
                        "AND CPCode = '" + req.PlanCode + "' " +
                        "AND CanopyPartcode = '" + req.ProductCode.Trim() + "' " +
                        "AND CatID = '" + req.CatID.Trim() + "'",
                        cancellationToken);

                    await tran.CommitAsync(cancellationToken);
                    return prcNo;
                }

                // ---- TK/ branch (new process) ----
                if (req.TkitId.Substring(0, 3) == "TK/")
                {
                    string nstWtsqft;
                    string[] strMachineNo = Regex.Split(req.MachineCodeSrNo, "-->");

                    // Begin transaction for the whole save
                    tran = (SqlTransaction)await con.BeginTransactionAsync(cancellationToken);

                    bool chkforStartCPY = await GetChkforStartCpyAsync(
                        con, tran, req.PCCode_Act.Trim(), req.PlanCode, req.ProductCode, req.CatID, cancellationToken);
                    bool chkforStart = await GetChkforStartAsync(
                        con, tran, req.PCCode_Act.Trim(), req.PlanCode, req.ProductCode, strMachineNo[1], req.CatID, cancellationToken);

                    nstWtsqft = ComCon.getTranName(
                        "Select convert(varchar(10),Pwt)+'-->'+convert(varchar(10),PSqft ) as PwtSqft from ProfitcenterPlDetails where ProfitcenterCode='01.005' and Partcode='" + req.ProductCode + "'",
                        "TblPwtSqft", "PwtSqft", con, tran);
                    string[] strNstWtsqft = Regex.Split(nstWtsqft.Trim(), "-->");

                    prcNo = await GetMaxPrcAsync(
                        con, tran, "ProcessFeedback", "PFbCode", ComCon.yearEnd(con, tran),
                        req.PCCode_Act.Trim().Substring(0, 2), cancellationToken);

                    strTKitCode = ComCon.getTranName(
                        "select TurretKitPartcode+'-->'+convert(nvarchar(10),TLength)+'-->'+convert(nvarchar(10),TWidth)+'-->'+convert(nvarchar(10),TThickness) as TurretKitPartcode From TurretKitForPrc where TKitId='" + req.TkitId + "'",
                        "tblTKitCode", "TurretKitPartcode", con, tran);
                    string[] strTKitCodeDts = Regex.Split(strTKitCode, "-->");

                    if (double.Parse(strTKitCodeDts[3].Trim()) <= 1.5)
                        shRate = ComCon.getTranName("select convert(nvarchar(10),PRate) as PRate From Process where PCode='01.032' ", "tblProcess", "PRate", con, tran);
                    else if (double.Parse(strTKitCodeDts[3].Trim()) > 1.5)
                        shRate = ComCon.getTranName("select convert(nvarchar(10),PRate) as PRate From Process where PCode='01.003' ", "tblProcess", "PRate", con, tran);

                    strBOMCode = ComCon.getTranName(
                        "select Max(Bd.BOMCode) as BOMCode From BOM B Inner Join BOMDetails BD on B.BomCode=Bd.BomCode where B.Active='1' and Bd.KitCode='" + strTKitCodeDts[0].Trim() + "'",
                        "tblBOM", "BOMCode", con, tran);

                    // --- main processfeedback insert ---
                    sb.Clear();
                    sb.Append("insert into processfeedback(GroupPFBCode,PFBCode,MaxSrNo,Dt,EDt,Yr,MachineCode,SerialNo,SupplierCode,ProfitCenterCode,ProductCode,CanopyPlanCode,TurretKitCode,");
                    sb.Append("NestingforCode,NestingforQty,nstWtPerUt,nstSqftPerUt,WtperUt,");
                    sb.Append("PartCode,VersionCode,ProcessQty,PKitQty,PLength,PWidth,PThickness, CompanyCode,PFBRate,PPWCode,Remark,CatID,PCCode_Act)");
                    sb.Append(" values('" + prcNo.Trim() + "','" + prcNo.Trim() + "','" + prcNo.Substring(10, 8) + "', ");
                    //Local
                    if (chkforStart == true)
                    {
                        sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',Null,");
                    }
                    else if (chkforStart == false)
                    {
                     //local
                      //  sb.Append("'" + ComCon.dateinyyyymmdd(await GetPrevPrcTimeAsync(con, tran, req.PCCode_Act, req.PlanCode, req.ProductCode, strMachineNo[1].ToString())) + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                      //server 
                        sb.Append("'" + (await GetPrevPrcTimeAsync(con, tran, req.PCCode_Act, req.PlanCode, req.ProductCode, strMachineNo[1].ToString())) + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");

                    }
                    //End Local




                    sb.Append("'" + ComCon.yearEnd(con, tran) + "','" + strMachineNo[0] + "','" + strMachineNo[1] + "','" + req.OSSupplierCode.Trim() + "','" + req.PCCode.Trim() + "',");
                    sb.Append("'" + req.ProductCode.Trim() + "','" + req.PlanCode.Trim() + "','" + strTKitCodeDts[0].Trim() + "',");
                    sb.Append("'" + req.ProductCode + "','" + req.BatchQty + "','" + double.Parse(strNstWtsqft[0].Trim()) + "','" + double.Parse(strNstWtsqft[1].Trim()) + "','" + req.ShWtperUts + "',");
                    sb.Append("'" + req.SheetPartcode + "','" + req.SerialNo + "','" + req.BatchQty + "','" + req.ShQtyPerset + "','" + strTKitCodeDts[1].Trim() + "', ");
                    sb.Append("'" + strTKitCodeDts[2].Trim() + "', '" + strTKitCodeDts[3].Trim() + "', '" + req.PCCode_Act.Substring(0, 2).Trim() + "', ");
                    sb.Append("'" + shRate.Trim() + "','" + req.EmpCode + "','Nil','" + req.CatID + "', '" + req.PCCode_Act.Trim() + "')");
                    await ExecAsync(con, tran, sb.ToString(), cancellationToken);

                    // --- file attachments ---
                    if (!string.IsNullOrEmpty(req.AttachFileDts?.Trim()))
                    {
                        var strPlanDts = Regex.Split(req.AttachFileDts, "@#@");
                        int srNoA = 0;
                        foreach (var strSub in strPlanDts)
                        {
                            srNoA += 1;
                            var dtsA = Regex.Split(strSub.Trim(), "-->");
                            string fileName = prcNo.Trim().Substring(4, 5).Trim() + prcNo.Trim().Substring(10, 8).Trim() + "-" + srNoA + Path.GetExtension(dtsA[1].Trim());
                            string strMpath = ComCon.getMainFilePath("PrcCNC") + "/" + fileName.Trim();
                            string strTpath = "C:/TempERPFile/TempPrcCNC/" + req.EmpCode.Trim() + "/" + dtsA[1].Trim();
                            string strTempPath = "C:/TempERPFile/TempPrcCNC/" + req.EmpCode.Trim();
                            if (Directory.Exists(strTempPath) && File.Exists(strTpath))
                            {
                                File.Copy(strTpath, strMpath, overwrite: true);
                            }
                            await ExecAsync(con, tran,
                                "INSERT INTO ProcessFeedbackFiles(GroupPFBCode,SrNo,FileName) VALUES('" + prcNo.Trim() + "' ,'" + srNoA + "','" + fileName.Trim() + "')",
                                cancellationToken);
                        }
                    }

                    // --- StockWIP issue for the sheet ---
                    await ExecAsync(con, tran,
                        "INSERT INTO StockWIP(FromProfitCenterCode,PartCode,IssueCode,issueDate,issueQty,ToProfitCenterCode,StockType,StageName,FromProfitCenterCode_Act,ToProfitCenterCode_Act)" +
                        " values('" + req.PCCode.Trim() + "','" + req.SheetPartcode.Trim() + "','" + prcNo.Trim() + "',GetDate(),'" + req.ShWtperBatch + "','" + req.PCCode.Trim() + "',0,'0','" + req.PCCode_Act.Trim() + "','" + req.PCCode_Act.Trim() + "')",
                        cancellationToken);

                    if (chkforStartCPY && req.CatID == "029")
                    {
                        await ExecAsync(con, tran,
                            "INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)" +
                            " values('" + req.ProductCode.Trim() + "','" + req.PCCode.Trim() + "','" + req.PCCode.Trim() + "','" + prcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + req.BatchQty + "',0,'" + req.PCCode_Act.Trim() + "','" + req.PCCode_Act.Trim() + "')",
                            cancellationToken);
                    }

                    await ExecAsync(con, tran,
                        "Update TurretKitForPrc set PrcStatus='D' where TKitId='" + req.TkitId + "' and CPCode='" + req.PlanCode + "' and CanopyPartcode='" + req.ProductCode.Trim() + "' and CatID='" + req.CatID.Trim() + "' ",
                        cancellationToken);

                    // --- process feedback details loop ---
                    int recCount = ComCon.CountChars(req.PrcDts, ",");
                    string[] strPrcDts = Regex.Split(req.PrcDts, ",");
                    int srNo = 0;
                    for (int cSub = 0; cSub <= recCount; cSub++)
                    {
                        srNo += 1;
                        string[] dts = Regex.Split(strPrcDts[cSub].Trim(), "-->");
                        sb.Clear();
                        sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,PFBRate,PLength,PWidth,PThickness,PLossWt,PHeight,PLength1,PLength2,PWidth1,PWidth2,PLossSqft,PCatagoryCode)");
                        sb.Append("values('" + prcNo.Trim() + "','" + srNo + "','" + dts[0].Trim() + "','" +
                                  Convert.ToDouble(dts[1].Trim()) + "','" + Convert.ToDouble(dts[2].Trim()) + "','" +
                                  Convert.ToDouble(dts[3].Trim()) + "','" + Convert.ToDouble(dts[4].Trim()) + "','" +
                                  Convert.ToDouble(dts[5].Trim()) + "','" + Convert.ToDouble(dts[6].Trim()) + "','" +
                                  Convert.ToDouble(dts[7].Trim()) + "','0','0','0','0','0','0','" + dts[8].Trim() + "')");
                        await ExecAsync(con, tran, sb.ToString(), cancellationToken);
                    }

                    // --- if no more pending sheets, close plan stage + consumables ---
                    string cntSheet = ComCon.getTranName(
                        "select isnull(Count(PrcStatus),0) as PrcStatus from TurretKitForPrc where PrcStatus='P' and CPCode='" + req.PlanCode.Trim() + "' and CanopyPartcode='" + req.ProductCode.Trim() + "' and CatId='" + req.CatID.Trim() + "' ",
                        "TurretKitForPrc", "PrcStatus", con, tran);

                    if (cntSheet == "0")
                    {
                        await ExecAsync(con, tran,
                            "Update CanopyPlanDtsSub set CPTQty='" + req.BatchQty + "' ,CPTStatus='D' where CPCode='" + req.PlanCode.Trim() + "' and CpyPartcode='" + req.ProductCode.Trim() + "' and CatId='" + req.CatID.Trim() + "' ",
                            cancellationToken);

                        // NOTE: original had a missing brace here - this UPDATE always runs (preserved).
                        await ExecAsync(con, tran,
                            "Update TurretKitForPrc set PartCutStatus='D' where CPCode='" + req.PlanCode.Trim() + "' and CanopyPartcode='" + req.ProductCode.Trim() + "' and CatId='" + req.CatID.Trim() + "' ",
                            cancellationToken);

                        await ExecAsync(con, tran,
                            "Update CanopyPlanDtsSub set CPPartCutQty='" + req.BatchQty + "' ,CPPartCutStatus='D' where CPCode='" + req.PlanCode.Trim() + "' and CpyPartcode='" + req.ProductCode.Trim() + "' and CatId='" + req.CatID.Trim() + "' ",
                            cancellationToken);

                        string strBendingPCCode = MapBendingPCCode(req.PCCode_Act.Trim());
                        string strOldBendingPCCode = MapBendingOldPCCode(req.PCCode.Trim());

                        if (req.CatID == "029")
                        {
                            await ExecAsync(con, tran,
                                "INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode,IssueCode,IssueDate, IssueQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)" +
                                " values('" + req.ProductCode.Trim() + "','" + req.PCCode.Trim() + "','" + strOldBendingPCCode.Trim() + "','" + prcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + req.BatchQty + "',0,'" + req.PCCode_Act.Trim() + "','" + strBendingPCCode.Trim() + "')",
                                cancellationToken);

                            await ExecAsync(con, tran,
                                "INSERT INTO ProductWip(ProductCode, FromPCCode, ToPCCode, ReceivedCode, ReceivedDate, ReceivedQty, StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)" +
                                " values('" + req.ProductCode.Trim() + "','" + req.PCCode.Trim() + "','" + strOldBendingPCCode.Trim() + "','" + prcNo.Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + req.BatchQty + "',0,'" + req.PCCode_Act.Trim() + "','" + strBendingPCCode.Trim() + "')",
                                cancellationToken);

                            await ExecAsync(con, tran,
                                "Update CanopyPlanSerialNo set CPTSerialStatus='D' where CPCode='" + req.PlanCode.Trim() + "' and Partcode='" + req.ProductCode.Trim() + "' ",
                                cancellationToken);
                        }

                        // --- consumables ---
                        string consumableRate = ComCon.getTranName(
                            "Select Isnull(Rate,0) as Rate from ProfitcenterPlDetails where ProfitcenterCode='03.059' and Partcode='" + strTKitCodeDts[0].Trim() + "'",
                            "TblPCPL", "Rate", con, tran);

                        string prcConsumable = await GetMaxPrcAsync(
                            con, tran, "ProcessFeedback", "PFbCode", ComCon.yearEnd(con, tran),
                            req.PCCode_Act.Trim().Substring(0, 2), cancellationToken);

                        sb.Clear();
                        sb.Append("insert into processfeedback(GroupPFBCode,PFBCode,MaxSrNo,Dt,EDt,Yr,MachineCode,SerialNo,SupplierCode,ProfitCenterCode,ProductCode,CanopyPlanCode,TurretKitCode,");
                        sb.Append("NestingforCode,NestingforQty,nstWtPerUt,nstSqftPerUt,WtperUt,");
                        sb.Append("PartCode,VersionCode,ProcessQty,PKitQty,PLength,PWidth,PThickness, CompanyCode,PFBRate,PPWCode,Remark,CatID,PCCode_Act)");
                        sb.Append(" values('" + prcNo.Trim() + "','" + prcConsumable.Trim() + "','" + prcConsumable.Substring(10, 8) + "', ");
                        sb.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                        sb.Append("'" + ComCon.yearEnd(con, tran) + "','" + strMachineNo[0] + "','" + strMachineNo[1] + "','" + req.OSSupplierCode.Trim() + "','" + req.PCCode.Trim() + "',");
                        sb.Append("'" + req.ProductCode.Trim() + "','" + req.PlanCode.Trim() + "','" + strTKitCodeDts[0].Trim() + "',");
                        sb.Append("'" + req.ProductCode + "','" + req.BatchQty + "','" + double.Parse(strNstWtsqft[0].Trim()) + "','" + double.Parse(strNstWtsqft[1].Trim()) + "','0',");
                        sb.Append("'" + strTKitCodeDts[0].Trim() + "','0','" + req.BatchQty + "','0', ");
                        sb.Append("'0','0', '0','" + req.PCCode_Act.Substring(0, 2).Trim() + "', ");
                        sb.Append("'" + consumableRate.Trim() + "','" + req.EmpCode + "','Nil','" + req.CatID + "' , '" + req.PCCode_Act.Trim() + "')");
                        await ExecAsync(con, tran, sb.ToString(), cancellationToken);

                        // consumable BOM stock check
                        var dsCNCCons = ComCon.procTranDS(
                            "select Partdesc,Bd.Partcode,Qty as KitQty," + req.BatchQty + " * Qty as TotQty , (select Round(Isnull(Sum(Recqty) - sum(IssueQty), 0), 00) as Stk From (select Sum(ReceivedQty) as Recqty," +
                            "0.00 as IssueQty from stockwip where ToProfitcenterCode_Act = '" + req.PCCode_Act.Trim() + "' and StockType = '0' and Partcode = Bd.Partcode and ReceivedQty > 0 Union all " +
                            "select 0.00 as Recqty, sum(IssueQty) as IssueQty from stockwip where FromProfitcenterCode_Act = '" + req.PCCode_Act.Trim() + "' and StockType = '0' and Partcode = Bd.Partcode and IssueQty > 0) as stk) as StockQty,Bd.SuppRate,Bd.categoryID " +
                            "from BOMDetails Bd Inner Join Part P On Bd.PartCode = P.Partcode where BOMCode ='" + strBOMCode + "' and kITCode = '" + strTKitCodeDts[0].Trim() + "' and Bd.Partcode Not like '006%' ",
                            "tbl_dsCNCCons", con, tran);

                        if (dsCNCCons?.Tables["tbl_dsCNCCons"]?.Rows.Count > 0)
                        {
                            int chkStk = 0;
                            srNo = 0;
                            var rows = dsCNCCons.Tables["tbl_dsCNCCons"].Rows;
                            for (int brd = 0; brd < rows.Count; brd++)
                            {
                                double totQty = Convert.ToDouble(rows[brd]["TotQty"].ToString().Trim());
                                double stockQty = Convert.ToDouble(rows[brd]["StockQty"].ToString().Trim());

                                if (totQty > stockQty)
                                {
                                    string partDesc = rows[brd]["Partdesc"].ToString().Trim();
                                    prcNo = chkStk == 0 ? partDesc : prcNo + "," + partDesc;
                                    chkStk = 1;
                                }
                                else if (chkStk == 0)
                                {
                                    srNo += 1;
                                    string partCode = rows[brd]["Partcode"].ToString().Trim();
                                    string suppRate = Convert.ToDouble(rows[brd]["SuppRate"].ToString().Trim()).ToString();

                                    sb.Clear();
                                    sb.Append("insert into processfeedbackdetails(PFBCode,SrNo,PartCode,KITQty,TotQty,PFBRate,SaleRate,WtPerUt,SqftPerUt,PLength,PWidth,PThickness,PLossWt,PCatagoryCode)");
                                    sb.Append("values('" + prcConsumable.Trim() + "','" + srNo + "','" + partCode + "','" +
                                              Convert.ToDouble(rows[brd]["KitQty"].ToString().Trim()) + "','" + totQty + "','" +
                                              suppRate + "','" + suppRate + "','0','0','0','0','0','0','" +
                                              rows[brd]["categoryID"].ToString().Trim() + "')");
                                    await ExecAsync(con, tran, sb.ToString(), cancellationToken);

                                    await ExecAsync(con, tran,
                                        "INSERT INTO StockWIP(FromProfitCenterCode,PartCode,IssueCode,IssueDate,IssueQty,ToProfitCenterCode,StockType,FromProfitCenterCode_Act,ToProfitCenterCode_Act)" +
                                        " values('" + req.PCCode.Trim() + "','" + partCode + "','" + prcConsumable.Trim() + "',GetDate(),'" + totQty + "','" + req.PCCode.Trim() + "',0,'" + req.PCCode_Act.Trim() + "','" + req.PCCode_Act.Trim() + "')",
                                        cancellationToken);
                                }
                            }

                            if (chkStk > 0)
                            {
                                prcNo = "Insufficient Stock For Consumable: " + prcNo;
                                await tran.RollbackAsync(cancellationToken);  // nothing should commit on shortage
                                return prcNo;
                            }
                        }
                    }

                    // user activity
                    await using (var actCmd = new SqlCommand("InsertLoginTransactionDetails", con, tran))
                    {
                        actCmd.CommandType = CommandType.StoredProcedure;
                        actCmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                        actCmd.Parameters.AddWithValue("@EmpID", req.EmpCode.Trim());
                        actCmd.Parameters.AddWithValue("@TransactionType", "S");
                        actCmd.Parameters.AddWithValue("@TransactionFrom", "CNC Process");
                        actCmd.Parameters.AddWithValue("@TransactionNo", prcNo.Trim());
                        actCmd.Parameters.AddWithValue("@CompanyCode", req.PCCode_Act.Substring(0, 2).Trim());
                        await actCmd.ExecuteNonQueryAsync(cancellationToken);
                    }

                    await tran.CommitAsync(cancellationToken);
                    //await tran.RollbackAsync(cancellationToken);
                }
                // ---- PSH branch (end process) ----
                else if (req.TkitId.Substring(0, 3) == "PSH")
                {
                    tran = (SqlTransaction)await con.BeginTransactionAsync(cancellationToken);

                    await ExecAsync(con, tran,
                        "Update ProcessFeedBack set EDt='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where PFBCode='" + req.TkitId.Trim() + "' ",
                        cancellationToken);

                    string cntDatecQty = ComCon.getTranName(
                        "select count(Dt) as DT from ProcessFeedback where PFBCode='" + req.TkitId.Trim() + "' and Dt='1900-01-01 00:00:00.000' ",
                        "Tbl_Dt", "Dt", con, tran);
                    if (cntDatecQty != "0")
                    {
                        await ExecAsync(con, tran,
                            "Update ProcessFeedBack set Dt='" + DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss") + "' where PFBCode='" + req.TkitId.Trim() + "' ",
                            cancellationToken);
                    }

                    string cntSheet = ComCon.getTranName(
                        "select isnull(Count(PrcStatus),0) as PrcStatus from TurretKitForPrc where PrcStatus='P' and CPCode='" + req.PlanCode.Trim() + "' and CanopyPartcode='" + req.ProductCode.Trim() + "' ",
                        "TurretKitForPrc", "PrcStatus", con, tran);
                    if (cntSheet == "0")
                    {
                        await ExecAsync(con, tran,
                            "Update TurretKitForPrc set PartCutStatus='D' where CPCode='" + req.PlanCode.Trim() + "' and CanopyPartcode='" + req.ProductCode.Trim() + "' ",
                            cancellationToken);

                        await ExecAsync(con, tran,
                            "Update CanopyPlanDtsSub set CPPartCutQty='" + req.BatchQty + "' ,CPPartCutStatus='D' where CPCode='" + req.PlanCode.Trim() + "' and CpyPartcode='" + req.ProductCode.Trim() + "' ",
                            cancellationToken);
                    }

                    await tran.CommitAsync(cancellationToken);
                    // await tran.RollbackAsync(cancellationToken);
                    prcNo = "ProcessCode=" + req.TkitId.Trim() + " For CNC End SuccessFully ";
                    return prcNo;
                }
            }
            catch (Exception ex)
            {
                if (tran != null)
                    await tran.RollbackAsync(cancellationToken);
                return "StackTrace " + ex.StackTrace + " Message " + ex.Message;
            }
            // no finally/con.Close needed - `await using` disposes the connection

            return prcNo;
        }

        private static async Task ExecAsync(SqlConnection con, SqlTransaction tran, string sql, CancellationToken ct)
        {
            await using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static string MapBendingPCCode(string pcCode) => pcCode switch
        {
            "01.095" => "01.098",  // Unit 1 A
            "01.096" => "01.099",  // Unit 1 B
            "01.097" => "01.100",  // Unit 1 C
            "03.066" => "03.070",  // Unit 4 A
            "03.067" => "03.071",  // Unit 4 B
            "03.068" => "03.072",  // Unit 4 C
            _ => "0"
        };

        private static string MapBendingOldPCCode(string pcCode) => pcCode switch
        {
            "01.009" => "01.002",  // Unit 1 old 
            " 03.061" => "03.004",  // Unit 4 old
            _ => "0"
        };

        // 1. Max process-code generator (single ExecuteScalar, padding simplified)
        private async Task<string> GetMaxPrcAsync(SqlConnection con, SqlTransaction tran, string tableName, string fieldName, string yr, string compCode, CancellationToken cancellationToken = default)
        {
            var sql = "select max(substring(" + fieldName + ",13,7)) as MX from " + tableName.Trim() +
                      " where yr='" + yr.Trim() + "' and CompanyCode='" + compCode.Trim() + "'";

            await using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
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

            await using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
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

            await using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
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


        public async Task<List<Dictionary<string, object>>> GetCheckerCPPlanLoadAsync(string pcCode)
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    // NOTE: the stored procedure GetCheckerCPYPlan must be altered
                    //       to accept only @PcCode (the @ShiftType param is removed).
                    cmd.CommandText = "GetCheckerCPYPlan_NewERP";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // single parameter: @PcCode
                    var pPcCode = cmd.CreateParameter();
                    pPcCode.ParameterName = "@PcCode";
                    pPcCode.Value = (object)pcCode ?? DBNull.Value;
                    cmd.Parameters.Add(pPcCode);

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

        public async Task<List<Dictionary<string, object>>> GetCNC_chekerDetailsAsync(string compId, string planCode, string pcCode)
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _db.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "getPendingProcforChecker_ERPNEW";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var pComp = cmd.CreateParameter();
                    pComp.ParameterName = "@CompCode";
                    pComp.Value = (object)compId ?? DBNull.Value;
                    cmd.Parameters.Add(pComp);

                    var pPlan = cmd.CreateParameter();
                    pPlan.ParameterName = "@PlanCode";
                    pPlan.Value = (object)planCode ?? DBNull.Value;
                    cmd.Parameters.Add(pPlan);

                    var pPc = cmd.CreateParameter();
                    pPc.ParameterName = "@PCCode";
                    pPc.Value = (object)pcCode ?? DBNull.Value;
                    cmd.Parameters.Add(pPc);

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


        ////// SubmitCNCChecker

        public async Task<string> SubmitCncCheckerAsync(CpyPrcCNCCheckerRequest req, CancellationToken ct = default)
        {
            string PrcNo = "";
            string strBOMCode = "0";
            string strReqCode = "";
            string strReqCodeCPYAssly = "";   // referenced in original success message; kept as-is
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
                        "WHERE CanopyPlanCode = @PlanCode and GroupPFBCode = @PFBCode",
                        ("@Dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                        ("@PlanCode", req.PlanCode.Trim()),
                        ("@PFBCode", req.PFBCode.Trim()));

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

                    var strTKitCode = await ComCon.GetScalarAsync(
                        "select TurretKitPartcode+'-->'+convert(nvarchar(10),TLength)+'-->'+convert(nvarchar(10),TWidth)+'-->'+convert(nvarchar(10),TThickness) as TurretKitPartcode " +
                        "From TurretKitForPrc where SheetPartCode=@SheetPartCode and CPCode=@CPCode",
                        new Dictionary<string, object?> { ["@SheetPartCode"] = req.Sheetpartcode, ["@CPCode"] = req.PlanCode },
                        con, tran);

                    var strTKitCodeDts = Regex.Split(strTKitCode ?? "", "-->");

                    strBOMCode = await ComCon.GetScalarAsync(
                        "select Max(Bd.BOMCode) as BOMCode From BOM B Inner Join BOMDetails BD on B.BomCode=Bd.BomCode " +
                        "where B.Active='1' and Bd.KitCode=@KitCode",
                        new Dictionary<string, object?> { ["@KitCode"] = strTKitCodeDts[0].Trim() }, con, tran);

                    // CNC Line A/B/C -> Bending PC mapping (kept from original)
                    string StrBendingPCCode = "0";
                    if (req.PCCode_Act.Trim() == "01.095") StrBendingPCCode = "01.098";       // A Unit 1
                    else if (req.PCCode_Act.Trim() == "01.096") StrBendingPCCode = "01.099";  // B Unit 1
                    else if (req.PCCode_Act.Trim() == "01.097") StrBendingPCCode = "01.100";  // C Unit 1
                    else if (req.PCCode_Act.Trim() == "03.066") StrBendingPCCode = "03.070";  // A Unit 4
                    else if (req.PCCode_Act.Trim() == "03.067") StrBendingPCCode = "03.071";  // B Unit 4
                    else if (req.PCCode_Act.Trim() == "03.068") StrBendingPCCode = "03.072";  // C Unit 4

                    var cntchecker = await ComCon.GetScalarAsync(
                        "select isnull(Count(Checker1),0) as Checker1 from ProcessFeedBack " +
                        "where Checker1='0' and CanopyPlanCode=@PlanCode and ProductCode=@ProductCode " +
                        "and CatId=@CatId and PCCode_Act=@PCCode and active='1' ",
                        new Dictionary<string, object?>
                        {
                            ["@PlanCode"] = req.PlanCode.Trim(),
                            ["@ProductCode"] = req.ProductCode.Trim(),
                            ["@CatId"] = req.CatID.Trim(),
                            ["@PCCode"] = req.PCCode_Act.Trim()
                        }, con, tran);

                    if (cntchecker == "0")
                    {
                        string GetMaxValue;
                        string RequisitionForPartCode;

                        // ===== Unit 01 CatID 029 =====
                        if (req.PCCode_Act.Substring(0, 2) == "01" && req.CatID == "029")
                        {
                            RequisitionForPartCode = await ComCon.GetScalarAsync(
                                "SELECT Partcode FROM BOMdetails WHERE BOMCode=@BOMCode " +
                                "AND SUBSTRING(Partcode, 11, 1) IN ('4') AND Partcode LIKE '004%'",
                                new Dictionary<string, object?> { ["@BOMCode"] = strBOMCode }, con, tran);

                            GetMaxValue = await ComCon.GetMaxNoAsync("MaterialRequisitionWithOutPlan", "REQ", req.PCCode_Act.Substring(0, 2), con, tran);
                            strReqCode = GetMaxValue;

                            await ExecNonQueryAsync(con, tran, ct,
                                "insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt,Yr,ProfitCenterCode,ToProfitCenterCode,ProfitCenterCode_Act,ToProfitCenterCode_Act," +
                                "ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) " +
                                "values(@REQCode,@MaxSrNo,@Dt,@Yr,'01.007','23.001' ,'01.116','23.001',@ClassCode,'01',@ActNo,'P','WIP',@Remark,'1','1','1',@SourceCode,@RequisitionFor)",
                                ("@REQCode", strReqCode.Trim()),
                                ("@MaxSrNo", GetMaxValue.Substring(10, 8)),
                                ("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")),
                                ("@Yr", await ComCon.YearEndAsync(con, tran)),
                                ("@ClassCode", req.ProductCode),
                                ("@ProfitCenterCode", req.PCCode),
                                ("@ProfitCenterCode_Act", req.PCCode_Act.Trim()),
                                ("@ActNo", req.BatchQty.Trim()),
                                ("@Remark", "Auto Req For Plan No: " + req.ProductCode + " and Prc No: " + req.PFBCode),
                                ("@SourceCode", req.PlanCode.Trim()),
                                ("@RequisitionFor", RequisitionForPartCode.Trim()));

                            await InsertReqDetailsAsync(req, strReqCode, con, tran, ct);

                            await ExecProcAsync(con, tran, ct, "insertLoginTransactionDetails",
                                ("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                                ("@EmpID", req.EmpCode),
                                ("@TransactionType", "S"),
                                ("@TransactionFrom", "MaterialRequisitionWithoutPlan"),
                                ("@TransactionNo", strReqCode),
                                ("@CompanyCode", "01"));
                        }
                        // ===== Unit 03 CatID 038 =====
                        else if (req.PCCode_Act.Substring(0, 2) == "03" && req.CatID == "038")
                        {
                            RequisitionForPartCode = await ComCon.GetScalarAsync(
                                "SELECT Partcode FROM BOMdetails WHERE BOMCode=@BOMCode " +
                                "AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'",
                                new Dictionary<string, object?> { ["@BOMCode"] = strBOMCode }, con, tran);

                            GetMaxValue = await ComCon.GetMaxNoAsync("MaterialRequisitionWithOutPlan", "REQ", "01", con, tran);
                            strReqCode = GetMaxValue;

                            await ExecNonQueryAsync(con, tran, ct,
                                "insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt,Yr,ProfitCenterCode,ToProfitCenterCode,ProfitCenterCode_Act,ToProfitCenterCode_Act," +
                                "ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) " +
                                "values(@REQCode,@MaxSrNo,@Dt,@Yr,'01.007','23.001' ,'01.116','23.001',@ClassCode,'01',@ActNo,'P','WIP',@Remark,'1','1','1',@SourceCode,@RequisitionFor)",
                                ("@REQCode", strReqCode.Trim()),
                                ("@MaxSrNo", GetMaxValue.Substring(10, 8)),
                                ("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")),
                                ("@Yr", await ComCon.YearEndAsync(con, tran)),
                                ("@ClassCode", req.ProductCode),
                                 ("@ProfitCenterCode", req.PCCode),
                                   ("@ProfitCenterCode_Act", req.PCCode_Act.Trim()),
                                ("@ActNo", req.BatchQty.Trim()),
                                ("@Remark", "Auto Req For Plan No: " + req.ProductCode + " and Prc No: " + req.PFBCode),
                                ("@SourceCode", req.PlanCode.Trim()),
                                ("@RequisitionFor", RequisitionForPartCode.Trim()));

                            await InsertReqDetailsAsync(req, strReqCode, con, tran, ct);

                            await ExecProcAsync(con, tran, ct, "insertLoginTransactionDetails",
                                ("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                                ("@EmpID", req.EmpCode),
                                ("@TransactionType", "S"),
                                ("@TransactionFrom", "MaterialRequisitionWithoutPlan"),
                                ("@TransactionNo", strReqCode),
                                ("@CompanyCode", "01"));
                        }
                        // ===== Unit 03 CatID 084 =====
                        else if (req.PCCode_Act.Substring(0, 2) == "03" && req.CatID == "084")
                        {
                            RequisitionForPartCode = await ComCon.GetScalarAsync(
                                "SELECT Partcode FROM BOMdetails WHERE BOMCode=@BOMCode " +
                                "AND SUBSTRING(Partcode, 12, 1) IN ('3') AND SUBSTRING(kitcode, 11, 1) IN ('5') AND KITCode LIKE '004%'",
                                new Dictionary<string, object?> { ["@BOMCode"] = strBOMCode }, con, tran);

                            GetMaxValue = await ComCon.GetMaxNoAsync("MaterialRequisitionWithOutPlan", "REQ", "01", con, tran);
                            strReqCode = GetMaxValue;

                            await ExecNonQueryAsync(con, tran, ct,
                                "insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt,Yr,ProfitCenterCode,ToProfitCenterCode,ProfitCenterCode_Act,ToProfitCenterCode_Act," +
                                "ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode,RequisitionFor) " +
                                "values(@REQCode,@MaxSrNo,@Dt,@Yr,'01.007','23.001','01.116','23.001',@ClassCode,'01',@ActNo,'P','WIP',@Remark,'1','1','1',@SourceCode,@RequisitionFor)",
                                ("@REQCode", strReqCode.Trim()),
                                ("@MaxSrNo", GetMaxValue.Substring(10, 8)),
                                ("@Dt", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")),
                                ("@Yr", await ComCon.YearEndAsync(con, tran)),
                                ("@ClassCode", req.ProductCode),
                                ("@ProfitCenterCode", req.PCCode),
                                ("@ProfitCenterCode_Act", req.PCCode_Act.Trim()),
                                ("@ActNo", req.BatchQty.Trim()),
                                ("@Remark", "Auto Req For Plan No: " + req.ProductCode + " and Prc No: " + req.PFBCode),
                                ("@SourceCode", req.PlanCode.Trim()),
                                ("@RequisitionFor", RequisitionForPartCode.Trim()));

                            await InsertReqDetailsAsync(req, strReqCode, con, tran, ct);

                            await ExecProcAsync(con, tran, ct, "insertLoginTransactionDetails",
                                ("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                                ("@EmpID", req.EmpCode),
                                ("@TransactionType", "S"),
                                ("@TransactionFrom", "MaterialRequisitionWithoutPlan"),
                                ("@TransactionNo", strReqCode),
                                ("@CompanyCode", "01"));
                        }

                        // ===== KanBan Processing =====
                        strKanBan = "";
                        var dsKanBan = await ComCon.ExecuteToDataSetAsync(
                            "exec InternalTOCReq_NewERP @PCCode",
                            new Dictionary<string, object?> { ["@PCCode"] = req.PCCode_Act.Trim() },
                            "tbl_RaiseReqDtsKanBan", con, tran);

                        if (dsKanBan?.Tables["tbl_RaiseReqDtsKanBan"] is { Rows.Count: > 0 } kanTable)
                        {
                            GetMaxValue = await ComCon.GetMaxNoAsync("MaterialRequisitionWithOutPlan", "REQ", req.PCCode_Act.Substring(0, 2), con, tran);
                            strKanBan = GetMaxValue;
                            var toPCCode = kanTable.Rows[0]["ToPCCode"].ToString()!.Trim();

                            await ExecNonQueryAsync(con, tran, ct,
                                "insert into MaterialRequisitionWithOutPlan(REQCode,MaxSrNo,Dt,Yr,ProfitCenterCode,ToProfitCenterCode,ProfitCenterCode_Act,ToProfitCenterCode_Act," +
                                "ClassCode,CompanyCode,ActNo,REQStatus,ReqType,Remark,Discard,Active,Auth,SourceCode) " +
                                "values(@REQCode,@MaxSrNo,@Dt,@Yr,@ProfitCenterCode,@ToProfitCenterCode ,@ProfitCenterCode_Act,@ToProfitCenterCode_Act,@ClassCode,@CompanyCode,@ActNo,'P','WIP',@Remark,'1','1','1','KanBan')",
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

                        // KanBan activity log (runs regardless, matching original)
                        await ExecProcAsync(con, tran, ct, "insertLoginTransactionDetails",
                            ("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                            ("@EmpID", req.EmpCode),
                            ("@TransactionType", "S"),
                            ("@TransactionFrom", "MaterialRequisitionWithoutPlan"),
                            ("@TransactionNo", strKanBan),
                            ("@CompanyCode", req.PCCode_Act.Substring(0, 2).Trim()));

                        await tran.CommitAsync(ct);
                        PrcNo = "ProcessCode=" + req.PFBCode + " For CNC  and Req No " + strReqCode + "," + strReqCodeCPYAssly + " For Powder Coting Saved SuccessFully ";
                        return PrcNo;
                    }

                    await tran.CommitAsync(ct);
                    PrcNo = "ProcessCode=" + req.PFBCode + "   For CNC Checker Saved SuccessFully ";
                    return PrcNo;
                }
                else
                {
                    // ---- Status is NOT "AUTH" ----
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
                                " CNC Checker    PlanCode: {0}, PFBCode: {1}, 6MType: {2}, Description: {3}",
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

                    // Original committed inside the loop, which throws on a 2nd assigned item under
                    // one transaction. Commit once here to keep the intended behaviour atomic.

                    await tran.CommitAsync(ct);
                    //await tran.RollbackAsync(ct);
                }
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync(ct);
                return "StackTrace " + ex.StackTrace + " Message " + ex.Message;
            }

            return PrcNo;
        }

        // ---- shared MaterialRequisitionWithOutPlan details insert (InternalReqLogisticsKit) ----
        private async Task InsertReqDetailsAsync(CpyPrcCNCCheckerRequest req, string strReqCode, SqlConnection con, SqlTransaction tran, CancellationToken ct)
        {
            var dsReqDts = await ComCon.ExecuteToDataSetAsync(
                "exec InternalReqLogisticsKit_ERPNEW @ProductCode,2,@CatId",
                new Dictionary<string, object?> { ["@ProductCode"] = req.ProductCode.Trim(), ["@CatId"] = req.CatID },
                "tbl_RaiseReqDts", con, tran);

            if (dsReqDts?.Tables["tbl_RaiseReqDts"] is not { Rows.Count: > 0 } table)
                return;

            var batchQty = double.Parse(req.BatchQty.Trim());
            int SrNoReq = 0;
            foreach (DataRow row in table.Rows)
            {
                SrNoReq++;
                var part = row["Partcode"].ToString()!.Trim();
                var raiseQty = double.Parse(row["RaiseReqQty"].ToString()!.Trim());

                await ExecProcAsync(con, tran, ct, "insertMaterialRequisitionWithOutPlanDetails_ERPNEW",
                    ("@REQCode", strReqCode),
                    ("@SrNo", SrNoReq),
                    ("@PartCode", part),
                    ("@Qty", raiseQty * batchQty),
                    ("@REQStatus", "P"));

                await GetReqDetailsSubAsync(strReqCode.Trim(), part, 2, raiseQty, con, tran, ct);
            }
        }

        // ---- GetReqDetailsSub: inserts MaterialReqDetailsSub rows for a kit ----
        // (was a protected method on the original class; lives in the service, same logic)
        private async Task GetReqDetailsSubAsync(string reqCode, string kitCode, int pcWise, double kitQty,SqlConnection con, SqlTransaction tran, CancellationToken ct)
        {
            var dsReqDtsSub = await ComCon.ExecuteToDataSetAsync(
                "exec InternalReqLogisticsdetails_NewERP @KitCode, @PCwise",
                new Dictionary<string, object?> { ["@KitCode"] = kitCode, ["@PCwise"] = pcWise },
                "tbl_RaiseReqDtsSub", con, tran);

            if (dsReqDtsSub?.Tables["tbl_RaiseReqDtsSub"] is not { Rows.Count: > 0 } table)
                return;

            int SrNok = 0;
            foreach (DataRow row in table.Rows)
            {
                var raiseReqQty = double.Parse(row["RaiseReqQty"].ToString()!.Trim());

                // SrNo only advances on positive qty, but every row is still inserted (original quirk).
                if (raiseReqQty > 0)
                    SrNok += 1;

                await ExecNonQueryAsync(con, tran, ct,
                    "insert into MaterialReqDetailsSub(REQCode,SrNo,RKitCode,PartCode,Qty,REQStatus) " +
                    "values(@REQCode,@SrNo,@RKitCode,@PartCode,@Qty,'P')",
                    ("@REQCode", reqCode.Trim()),
                    ("@SrNo", SrNok),
                    ("@RKitCode", kitCode),
                    ("@PartCode", row["PartCode"].ToString()!.Trim()),
                    ("@Qty", raiseReqQty * kitQty));
            }
        }

        // ---- ADO.NET helpers ----
        private static async Task ExecNonQueryAsync(SqlConnection con, SqlTransaction tran, CancellationToken ct,string sql, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(sql, con, tran);
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static async Task ExecProcAsync(SqlConnection con, SqlTransaction tran, CancellationToken ct,string procName, params (string Name, object? Value)[] parameters)
        {
            await using var cmd = new SqlCommand(procName, con, tran) { CommandType = CommandType.StoredProcedure };
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }


    }
}