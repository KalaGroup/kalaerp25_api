using Microsoft.Data.SqlClient;   // .NET 8 replacement for System.Data.SqlClient
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Globalization;
using System.Text;

namespace KalaGenset.ERP.Core.Services
{
    // .NET 8 conversion of the legacy KalaERPApi CommonCon helper.
    // Changes from the framework version:
    //   - System.Data.SqlClient            -> Microsoft.Data.SqlClient
    //   - ConfigurationManager.AppSettings  -> IConfiguration (injected)
    //   - single shared 'con' field         -> a connection string; each non-transaction
    //                                          method opens its OWN connection (thread-safe)
    //   - transaction methods still use the caller's (con, tran) exactly as before
    //   - procTranDS/procDS no longer Dispose() the DataSet they return (that was a latent
    //     bug — you cannot return a disposed object); the adapter is still disposed.
    public class CommonCon
    {
        private readonly string _connStr;

        public CommonCon(IConfiguration config)
        {
            _connStr = config.GetConnectionString("KalaDbContext")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // ============================================================
        //  TRANSACTION-AWARE methods (use the caller's con + tran)
        // ============================================================
        public string GetMaxNo(string TableName, string Prefix, string CompCode, SqlConnection con, SqlTransaction tran)
        {
            string strmax = "";
            string NewTransCode = "";
            int intmax = 0;
            var sb = new StringBuilder();

            try
            {
                SqlCommand cmd = new SqlCommand("SELECT ISNULL(MaxValue,0) as MXNO FROM GetMaxCode WHERE TblName='" + TableName + "'  and CompCode='" + CompCode + "' AND Prefix='" + Prefix + "' and Yr='" + yearEnd(con, tran) + "'", con);
                cmd.CommandTimeout = 0;
                cmd.Transaction = tran;
                intmax = Convert.ToInt32(cmd.ExecuteScalar());

                if (intmax == 0) strmax = "000001";
                else if (intmax < 9) strmax = "00000" + (intmax + 1);
                else if (intmax < 99) strmax = "0000" + (intmax + 1);
                else if (intmax < 999) strmax = "000" + (intmax + 1);
                else if (intmax < 9999) strmax = "00" + (intmax + 1);
                else if (intmax < 99999) strmax = "0" + (intmax + 1);
                else strmax = Convert.ToString(intmax + 1);
                cmd.Dispose();

                NewTransCode = Prefix + "/" + yearEnd(con, tran) + "/" + CompCode + strmax;

                sb.Remove(0, sb.Length);
                sb.Append("UPDATE GetMaxCode SET MaxValue='" + strmax + "' WHERE Prefix='" + Prefix + "' and TblName='" + TableName + "' ");
                sb.Append("and CompCode='" + CompCode + "' AND Yr='" + yearEnd(con, tran) + "'");
                cmd = new SqlCommand(sb.ToString(), con);
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();
                cmd.Dispose();

                return NewTransCode;
            }
            catch
            {
                return null;
            }
        }

        public DataSet procTranDS(string strproc, string tblName, SqlConnection con, SqlTransaction tran)
        {
            using var dAd = new SqlDataAdapter(strproc, con);
            dAd.SelectCommand.CommandType = CommandType.Text;
            dAd.SelectCommand.Transaction = tran;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet, tblName);
            return dSet;   // NOTE: do NOT dispose dSet here — it is returned to the caller.
        }

        public string getTranName(string strqry, string tblName, string fieldName, SqlConnection con, SqlTransaction tran)
        {
            using var dAd = new SqlDataAdapter(strqry, con);
            dAd.SelectCommand.CommandType = CommandType.Text;
            dAd.SelectCommand.Transaction = tran;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet, tblName);

            if (dSet.Tables[tblName].Rows.Count > 0)
            {
                var val = dSet.Tables[tblName].Rows[0][fieldName].ToString();
                return string.IsNullOrEmpty(val) ? "0" : val;
            }
            return "0";
        }

        public string yearEnd(SqlConnection con, SqlTransaction tran)
        {
            DataSet ds = procTranDS("select substring(convert(Varchar(10),startdate,103),9,2)+'-'+ substring(convert(Varchar(10),enddate,103),9,2) as yr from yearend", "yearend", con, tran);
            return ds.Tables["yearend"].Rows[0]["yr"].ToString();
        }

        public string SysStartDate(SqlConnection con, SqlTransaction tran)
        {
            DataSet ds = procTranDS("select convert(varchar(10),StartDate,103) as SDate ,Convert(varchar(10),EndDate,103) as EDate from  YearEnd", "SysStart", con, tran);
            return ds.Tables["SysStart"].Rows[0]["SDate"].ToString();
        }

        public string SysEndDate(SqlConnection con, SqlTransaction tran)
        {
            DataSet ds = procTranDS("select convert(varchar(10),StartDate,103) as SDate ,Convert(varchar(10),EndDate,103) as EDate from  YearEnd", "SysEnd", con, tran);
            return ds.Tables["SysEnd"].Rows[0]["EDate"].ToString();
        }

        // ============================================================
        //  NON-TRANSACTION methods (open their own connection)
        //  Each creates a fresh SqlConnection -> thread-safe under DI.
        //  SqlDataAdapter.Fill auto-opens/closes the connection.
        // ============================================================
        public DataTable GetEmpPCINFO(string Type, string Code)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter("GetEmpPCINFO_SP_NewERP", con);
            dAd.SelectCommand.CommandType = CommandType.StoredProcedure;
            dAd.SelectCommand.Parameters.Add("@Type", SqlDbType.Char).Value = Type;
            dAd.SelectCommand.Parameters.Add("@Code", SqlDbType.Char).Value = Code;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet);
            return dSet.Tables[0];
        }

        public DataTable GetUserLoginInfo(string Id, string Password)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter("GetLoginInfo_SP_New", con);
            dAd.SelectCommand.CommandType = CommandType.StoredProcedure;
            dAd.SelectCommand.Parameters.Add("@Id", SqlDbType.Char).Value = Id;
            dAd.SelectCommand.Parameters.Add("@Password", SqlDbType.Char).Value = Password;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet);
            return dSet.Tables[0];
        }

        public DataTable GetLoginCompInfo(string Type, string Code)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter("GetLoginCompInfo_SP", con);
            dAd.SelectCommand.CommandType = CommandType.StoredProcedure;
            dAd.SelectCommand.Parameters.Add("@Type", SqlDbType.Char).Value = Type;
            dAd.SelectCommand.Parameters.Add("@Code", SqlDbType.Char).Value = Code;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet);
            return dSet.Tables[0];
        }

        public DataSet procDS(string strproc, string tblName)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter(strproc, con);
            dAd.SelectCommand.CommandType = CommandType.Text;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet, tblName);
            return dSet;
        }

        public DataTable procDT(string strproc, string tblName)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter(strproc, con);
            dAd.SelectCommand.CommandType = CommandType.Text;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet, tblName);
            return dSet.Tables[tblName];
        }

        public DataTable GetPCodeAll(string PCCode, string ReqType)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter("GetPCCodeALL", con);
            dAd.SelectCommand.CommandType = CommandType.StoredProcedure;
            dAd.SelectCommand.Parameters.Add("@PCCode", SqlDbType.Char).Value = PCCode;
            dAd.SelectCommand.Parameters.Add("@ReqType", SqlDbType.Char).Value = ReqType;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet);
            return dSet.Tables[0];
        }

        public DataTable GetPartDesc(string PartCode)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter("GetPartDesc", con);
            dAd.SelectCommand.CommandType = CommandType.StoredProcedure;
            dAd.SelectCommand.Parameters.Add("@PartCode", SqlDbType.Char).Value = PartCode;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet);
            return dSet.Tables[0];
        }

        public string getName(string strqry, string tblName, string fieldName)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter(strqry, con);
            dAd.SelectCommand.CommandType = CommandType.Text;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet, tblName);

            if (dSet.Tables[tblName].Rows.Count > 0)
            {
                var val = dSet.Tables[tblName].Rows[0][fieldName].ToString();
                return string.IsNullOrEmpty(val) ? "0" : val;
            }
            return "0";
        }

        public DataTable GetPrcStatus()
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter("GetTRPrcStatus", con);
            dAd.SelectCommand.CommandType = CommandType.StoredProcedure;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet);
            return dSet.Tables[0];
        }

        public DataTable GetPrcChkDts(string strStageNo, string PrcStatusName)
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter("GetTRPrcChkDts", con);
            dAd.SelectCommand.CommandType = CommandType.StoredProcedure;
            dAd.SelectCommand.Parameters.Add("@strStageNo", SqlDbType.Char).Value = strStageNo;
            dAd.SelectCommand.Parameters.Add("@PrcStatusName", SqlDbType.Char).Value = PrcStatusName;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet);
            return dSet.Tables[0];
        }

        public DataTable Get6M()
        {
            using var con = new SqlConnection(_connStr);
            using var dAd = new SqlDataAdapter("Get6M", con);
            dAd.SelectCommand.CommandType = CommandType.StoredProcedure;
            dAd.SelectCommand.CommandTimeout = 0;
            var dSet = new DataSet();
            dAd.Fill(dSet);
            return dSet.Tables[0];
        }

        public string getCCodeMax(string strSqlQuery, string StrCode)
        {
            try
            {
                using var con = new SqlConnection(_connStr);
                using var cmd = new SqlCommand(strSqlQuery, con) { CommandTimeout = 0 };
                con.Open();

                var scalar = cmd.ExecuteScalar();
                if (scalar == DBNull.Value || scalar == null)
                {
                    StrCode = StrCode + "0001";
                }
                else
                {
                    StrCode = Convert.ToString(scalar);
                    int codeCnt = Convert.ToInt32(StrCode.Substring(15, 4)) + 1;
                    if (codeCnt.ToString().Length == 4) StrCode = StrCode.Substring(0, 15) + codeCnt;
                    else if (codeCnt.ToString().Length == 3) StrCode = StrCode.Substring(0, 15) + ("0" + codeCnt);
                    else if (codeCnt.ToString().Length == 2) StrCode = StrCode.Substring(0, 15) + ("00" + codeCnt);
                    else if (codeCnt.ToString().Length == 1) StrCode = StrCode.Substring(0, 15) + ("000" + codeCnt);
                }
                return StrCode;
            }
            catch
            {
                return "0";
            }
        }

        // ============================================================
        //  PURE helpers (no DB)
        // ============================================================
        public int CountChars(string sText, string str)
        {
            int lCount = 0;
            char[] TextArray = sText.ToCharArray();
            for (int i = 0; i < TextArray.Length; i++)
                if (TextArray[i] == Convert.ToChar(str)) lCount++;
            return lCount;
        }

        public string dateinyyyymmdd(string dt)
        {
            if (dt == "") return "1900-01-01";
            return dt.Substring(6, 4) + "-" + dt.Substring(3, 2) + "-" + dt.Substring(0, 2);
        }

        //public string dateinyyyymmdd(string dt)
        //{
        //    if (string.IsNullOrWhiteSpace(dt)) return "1900-01-01";

        //    if (DateTime.TryParseExact(dt.Trim(), "dd/MM/yyyy",
        //            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        //        return parsed.ToString("yyyy-MM-dd");

        //    return "1900-01-01";   // or throw with context to surface the bad caller
        //}


        // NOTE: hard-coded Windows path "F:\ERP". Keep only if the host is Windows
        // with that drive; otherwise move the base path to configuration.
        public string getMainFilePath(string MFileFolder)
        {
            string yr = getName("SELECT Year(GETDATE())AS Yr", "tblYr", "Yr");
            string Mainfolder = "F:\\ERP" + "\\" + yr.Trim();
            if (!Directory.Exists(Mainfolder)) Directory.CreateDirectory(Mainfolder);

            string Mnth = getName("SELECT CASE WHEN month(GETDATE())<10 THEN '0'+ cast(month(GETDATE())AS nvarchar(10))+' '+DATENAME(MONTH, GETDATE()) " +
                        "WHEN month(GETDATE())>=10 THEN cast(month(GETDATE())AS nvarchar(10))+' '+DATENAME(MONTH, GETDATE()) " +
                        "END AS Mnth", "tblCmnth", "Mnth");

            Mainfolder = "F:\\ERP" + "\\" + yr.Trim() + "\\" + Mnth.Trim();
            if (!Directory.Exists(Mainfolder)) Directory.CreateDirectory(Mainfolder);

            Mainfolder = "F:\\ERP" + "\\" + yr.Trim() + "\\" + Mnth.Trim() + "\\" + MFileFolder.Trim();
            if (!Directory.Exists(Mainfolder)) Directory.CreateDirectory(Mainfolder);

            return Mainfolder;
        }

        public string NumberToText(int number)
        {
            if (number == 0) return "Zero";
            if (number == -2147483648) return "Minus Two Hundred and Fourteen Crore Seventy Four Lakh Eighty Three Thousand Six Hundred and Forty Eight";
            int[] num = new int[4];
            int first = 0;
            int u, h, t;
            var sb = new StringBuilder();
            if (number < 0) { sb.Append("Minus "); number = -number; }

            string[] words0 = { "", "One ", "Two ", "Three ", "Four ", "Five ", "Six ", "Seven ", "Eight ", "Nine " };
            string[] words1 = { "Ten ", "Eleven ", "Twelve ", "Thirteen ", "Fourteen ", "Fifteen ", "Sixteen ", "Seventeen ", "Eighteen ", "Nineteen " };
            string[] words2 = { "Twenty ", "Thirty ", "Fourty ", "Fifty ", "Sixty ", "Seventy ", "Eighty ", "Ninety " };
            string[] words3 = { "Thousand ", "Lakh ", "Crore " };

            num[0] = number % 1000;
            num[1] = number / 1000;
            num[2] = number / 100000;
            num[1] = num[1] - 100 * num[2];
            num[3] = number / 10000000;
            num[2] = num[2] - 100 * num[3];

            for (int i = 3; i > 0; i--) { if (num[i] != 0) { first = i; break; } }
            for (int i = first; i >= 0; i--)
            {
                if (num[i] == 0) continue;
                u = num[i] % 10;
                t = num[i] / 10;
                h = num[i] / 100;
                t = t - 10 * h;
                if (h > 0) sb.Append(words0[h] + "Hundred ");
                if (u > 0 || t > 0)
                {
                    if (h > 0 || i == 0) sb.Append("and ");
                    if (t == 0) sb.Append(words0[u]);
                    else if (t == 1) sb.Append(words1[u]);
                    else sb.Append(words2[t - 2] + words0[u]);
                }
                if (i != 0) sb.Append(words3[i - 1]);
            }
            return sb.ToString().TrimEnd();
        }

        public string getStockTbl(string CompID)
        {
            return CompID switch
            {
                "01" => "Stock01",
                "02" => "Stock04",
                "03" => "Stock03",
                "04" => "Stock02",
                "05" => "Stock05",
                "07" => "Stock07",
                "08" => "Stock08",
                "09" => "Stock09",
                "10" => "Stock10",
                "13" => "Stock13",
                "14" => "Stock14",
                "15" => "Stock15",
                "16" => "Stock16",
                "17" => "Stock17",
                "18" => "Stock18",
                "19" => "Stock19",
                "20" => "Stock20",
                "21" => "Stock21",
                "22" => "Stock22",
                "23" => "Stock23",
                _ => "0"
            };
        }


        public async Task<string> GetScalarAsync(string sql,IDictionary<string, object?>? parameters, SqlConnection con,SqlTransaction tran)
        {
            await using var cmd = new SqlCommand(sql, con, tran);
            if (parameters is not null)
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return result is null || result == DBNull.Value ? string.Empty : result.ToString()!;
        }

        public async Task<DataSet> ExecuteToDataSetAsync(string sql,IDictionary<string, object?>? parameters,string resultTableName,SqlConnection con,SqlTransaction tran)
        {
            await using var cmd = new SqlCommand(sql, con, tran);
            if (parameters is not null)
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

            var ds = new DataSet();
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(ds, resultTableName);
            return ds;
        }

        public async Task<string> GetMaxNoAsync(string tableName, string prefix, string compCode, SqlConnection con, SqlTransaction tran)
        {
            try
            {
                var yr = await YearEndAsync(con, tran);

                // Read current max for this table/company/prefix/year.
                int intmax;
                await using (var select = new SqlCommand(
                    "SELECT ISNULL(MaxValue,0) as MXNO FROM GetMaxCode " +
                    "WHERE TblName=@TblName and CompCode=@CompCode AND Prefix=@Prefix and Yr=@Yr",
                    con, tran)
                { CommandTimeout = 0 })
                {
                    select.Parameters.AddWithValue("@TblName", tableName);
                    select.Parameters.AddWithValue("@CompCode", compCode);
                    select.Parameters.AddWithValue("@Prefix", prefix);
                    select.Parameters.AddWithValue("@Yr", yr);
                    intmax = Convert.ToInt32(await select.ExecuteScalarAsync());
                }

                // Zero-pad the next number to 6 digits (same ladder as the original).
                string strmax;
                if (intmax == 0) strmax = "000001";
                else if (intmax < 9) strmax = "00000" + (intmax + 1);
                else if (intmax < 99) strmax = "0000" + (intmax + 1);
                else if (intmax < 999) strmax = "000" + (intmax + 1);
                else if (intmax < 9999) strmax = "00" + (intmax + 1);
                else if (intmax < 99999) strmax = "0" + (intmax + 1);
                else strmax = Convert.ToString(intmax + 1);

                var newTransCode = prefix + "/" + yr + "/" + compCode + strmax;

                // Persist the new max.
                await using (var update = new SqlCommand(
                    "UPDATE GetMaxCode SET MaxValue=@MaxValue " +
                    "WHERE Prefix=@Prefix and TblName=@TblName and CompCode=@CompCode AND Yr=@Yr",
                    con, tran)
                { CommandTimeout = 0 })
                {
                    update.Parameters.AddWithValue("@MaxValue", strmax);
                    update.Parameters.AddWithValue("@Prefix", prefix);
                    update.Parameters.AddWithValue("@TblName", tableName);
                    update.Parameters.AddWithValue("@CompCode", compCode);
                    update.Parameters.AddWithValue("@Yr", yr);
                    await update.ExecuteNonQueryAsync();
                }

                return newTransCode;
            }
            catch
            {
                // Preserves the original behaviour (returns null on error).
                // Consider letting it throw instead, so the service's catch can roll back
                // with the real SQL error rather than failing later on a null reference.
                return null!;
            }
        }

        public async Task<string> YearEndAsync(SqlConnection con, SqlTransaction tran)
        {
            const string sql =
                "select substring(convert(Varchar(10),startdate,103),9,2)+'-'+" +
                "substring(convert(Varchar(10),enddate,103),9,2) as yr from yearend";

            await using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }

        public async Task<string> GetmaxPrcAsync(string tablename, string fieldname, string Yr, string CompCode,SqlConnection con, SqlTransaction tran)
        {
            // Concurrency note (legacy had the same race): two simultaneous saves can
            // read the same MAX and mint duplicate codes. To serialize them, change
            // "from <table>" to "from <table> WITH (UPDLOCK, HOLDLOCK)".
            string sql = "select max(substring(" + fieldname + ",13,7)) as MX from " + tablename.Trim()
                       + " where yr=@Yr and CompanyCode=@CompCode";

            await using var cmd = new SqlCommand(sql, con, tran) { CommandTimeout = 0 };
            cmd.Parameters.AddWithValue("@Yr", Yr.Trim());
            cmd.Parameters.AddWithValue("@CompCode", CompCode.Trim());

            // LEGACY re-ran ExecuteScalar in every branch — read once instead.
            object scalar = await cmd.ExecuteScalarAsync();

            string Max;
            if (scalar == null || scalar == DBNull.Value || scalar.ToString() == "")
            {
                Max = CompCode + "000001";
            }
            else
            {
                int intmax = Convert.ToInt32(scalar);

                // Same zero-padding ladder as the original (6 digits; 7+ unpadded).
                string strmax;
                if (intmax < 9) strmax = "00000" + (intmax + 1);
                else if (intmax < 99) strmax = "0000" + (intmax + 1);
                else if (intmax < 999) strmax = "000" + (intmax + 1);
                else if (intmax < 9999) strmax = "00" + (intmax + 1);
                else if (intmax < 99999) strmax = "0" + (intmax + 1);
                else strmax = Convert.ToString(intmax + 1);

                Max = CompCode + strmax;
            }

            // LEGACY verbatim: "PSH" prefix is hard-coded for these process codes.
            return "PSH" + "/" + Yr + "/" + Max;
        }

        public async Task<DataSet> procTranDSAsync(
    string strproc, string tblName, SqlConnection con, SqlTransaction tran)
        {
            var dSet = new DataSet();
            var dTable = new DataTable(tblName);

            using (var cmd = new SqlCommand(strproc, con, tran))
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dTable.Load(reader);   // Load() reads the reader synchronously, but the query executed async
                }
            }

            dSet.Tables.Add(dTable);
            return dSet;
        }
    }
}