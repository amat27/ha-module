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

        public string ConStr { get; set; }

        private string timeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        public SQLMembershipClient(string utype, string uname)
        {
            this.Uuid = Guid.NewGuid().ToString();
            this.Utype = utype;
            this.Uname = uname;
            this.ConStr = "server=.;database=HighAvailabilityWitness;Trusted_Connection=SSPI;Connect Timeout=5";
        }

        public async Task HeartBeatAsync(HeartBeatEntryDTO entryDTO)
        {
            SqlConnection con = new SqlConnection(this.ConStr);
            string StoredProcedure = "dbo.HeartBeat";
            SqlCommand comStr = new SqlCommand(StoredProcedure, con);
            comStr.CommandType = CommandType.StoredProcedure;
            comStr.CommandTimeout = this.OperationTimeout.Seconds;

            comStr.Parameters.Add("@uuid", SqlDbType.NVarChar).Value = entryDTO.Uuid;
            comStr.Parameters.Add("@utype", SqlDbType.NVarChar).Value = entryDTO.Utype;
            comStr.Parameters.Add("@uname", SqlDbType.NVarChar).Value = entryDTO.Uname;
            comStr.Parameters.Add("@lastSeenUuid", SqlDbType.NVarChar).Value = entryDTO.LastSeenEntry.Uuid;
            comStr.Parameters.Add("@lastSeenUtype", SqlDbType.NVarChar).Value = entryDTO.LastSeenEntry.Utype;
            comStr.Parameters.Add("@lastSeenTimeStamp", SqlDbType.DateTime).Value = entryDTO.LastSeenEntry.TimeStamp.ToString(this.timeFormat);

            try
            {
                await con.OpenAsync();
                await comStr.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"[{this.Uuid}] Error occured when sending heartbeat entry: {ex.ToString()}");
            }
            finally
            {
                con.Close();
                con.Dispose();
            }
        }

        public async Task<HeartBeatEntry> GetHeartBeatEntryAsync(string utype)
        {
            HeartBeatEntry heartBeatEntry;
            SqlConnection con = new SqlConnection(this.ConStr);
            string StoredProcedure = "dbo.GetHeartBeat";
            SqlCommand comStr = new SqlCommand(StoredProcedure, con);
            comStr.CommandType = CommandType.StoredProcedure;
            comStr.CommandTimeout = this.OperationTimeout.Seconds;

            comStr.Parameters.Add("@utype", SqlDbType.NVarChar).Value = utype;

            try
            {
                await con.OpenAsync();
                SqlDataReader ReturnedEntry = await comStr.ExecuteReaderAsync();
                if (ReturnedEntry.HasRows)
                {
                    ReturnedEntry.Read();
                    heartBeatEntry = new HeartBeatEntry(ReturnedEntry[0].ToString(), ReturnedEntry[1].ToString(),
                        ReturnedEntry[2].ToString(), Convert.ToDateTime(Convert.ToDateTime(ReturnedEntry[3]).ToString(this.timeFormat)));
                    
                    ReturnedEntry.Close();
                }
                else
                {
                    heartBeatEntry = HeartBeatEntry.Empty;
                }
                return heartBeatEntry;
            }
            catch (Exception ex)
            {
                throw new Exception($"[{this.Uuid}] Error occured when getting heartbeat entry: {ex.ToString()}");
            }
            finally
            {
                con.Close();
                con.Dispose();
            }
        }

        public string GenerateUuid() => this.Uuid;
    }
}