namespace HighAvailabilityModule.Client.SQL
{
    using System;
    using System.Threading.Tasks;
    using System.Data;
    using System.Data.SqlClient;

    using HighAvailabilityModule.Interface;

    public class SQLMembershipClient: IMembershipClient
    {
        public string Uuid { get; }

        public string Utype { get; }

        public string Uname { get; }

        public TimeSpan OperationTimeout { get; set; }

        private string timeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        public SQLMembershipClient(string utype, string uname)
        {
            this.Uuid = Guid.NewGuid().ToString();
            this.Utype = utype;
            this.Uname = uname;
        }

        public async Task HeartBeatAsync(HeartBeatEntryDTO entryDTO)
        {
            string ConStr = "server=.;database=HighAvailabilityModule;Trusted_Connection=SSPI";
            SqlConnection con = new SqlConnection(ConStr);
            string StoredProcedure = "dbo.HeartBeatAsync";
            SqlCommand comStr = new SqlCommand(StoredProcedure, con);
            comStr.CommandType = CommandType.StoredProcedure;

            comStr.Parameters.Add("@uuid", SqlDbType.NVarChar).Value = entryDTO.Uuid;
            comStr.Parameters.Add("@utype", SqlDbType.NVarChar).Value = entryDTO.Utype;
            comStr.Parameters.Add("@uname", SqlDbType.NVarChar).Value = entryDTO.Uname;
            comStr.Parameters.Add("@lastSeenUuid", SqlDbType.NVarChar).Value = entryDTO.LastSeenEntry.Uuid;
            comStr.Parameters.Add("@lastSeenUtype", SqlDbType.NVarChar).Value = entryDTO.LastSeenEntry.Utype;
            comStr.Parameters.Add("@lastSeenTimeStamp", SqlDbType.DateTime).Value = entryDTO.LastSeenEntry.TimeStamp.ToString(this.timeFormat);

            con.Open();
            if (con.State == ConnectionState.Open)
            {
                comStr.ExecuteNonQuery();
                con.Close();
            }
            else
            {
                throw new InvalidOperationException($"[{this.Uuid}] Can't connect to the SQL Server.");
            }
        }

        public async Task<HeartBeatEntry> GetHeartBeatEntryAsync(string utype)
        {
            HeartBeatEntry heartBeatEntry = null;
            string ConStr = "server=.;database=HighAvailabilityModule;Trusted_Connection=SSPI";
            SqlConnection con = new SqlConnection(ConStr);
            string StoredProcedure = "dbo.GetHeartBeatAsync";
            SqlCommand comStr = new SqlCommand(StoredProcedure, con);
            comStr.CommandType = CommandType.StoredProcedure;

            comStr.Parameters.Add("@utype", SqlDbType.NVarChar).Value = utype;

            con.Open();
            if (con.State == ConnectionState.Open)
            {
                SqlDataReader ReturnedEntry = comStr.ExecuteReader();
                if (ReturnedEntry.HasRows)
                {
                    if (ReturnedEntry.Read())
                    {
                        heartBeatEntry = new HeartBeatEntry(ReturnedEntry[0].ToString(), ReturnedEntry[1].ToString(),
                            ReturnedEntry[2].ToString(), Convert.ToDateTime(Convert.ToDateTime(ReturnedEntry[3]).ToString(this.timeFormat)));
                    }
                }
                else
                {
                    heartBeatEntry = HeartBeatEntry.Empty;
                }
                con.Close();
                return heartBeatEntry;
            }
            else
            {
                throw new InvalidOperationException($"[{this.Uuid}] Can't connect to the SQL Server.");
            }
        }

        public string GenerateUuid() => this.Uuid;
    }
}

