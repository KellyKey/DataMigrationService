using System;
using System.Data.SqlClient;
using System.Text;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportIterations : IExportAssets
    {
        public ExportIterations(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        private int IterationCount = 0;

        public override int Export()
        {
            //InsertIterationValues("R3_Sprint 15", "601", "3/4/2014", "3/17/2014");
            //InsertIterationValues("R2_Sprint 14", "601", "2/18/2014", "3/3/2014");
            InsertIterationValues("R2_Sprint 13", "601", "2/4/2014", "2/17/2014");
            InsertIterationValues("R2_Sprint 12", "582", "1/21/2014", "2/3/2014");
            InsertIterationValues("R2_Sprint 11", "561", "1/7/2014", "1/20/2014");
            InsertIterationValues("R1_Sprint 10", "541", "12/24/2013", "1/6/2014");
            InsertIterationValues("R1_Sprint 9", "521", "12/10/2013", "12/23/2013");
            InsertIterationValues("R1_Sprint 8", "481", "11/26/2013", "12/9/2013");
            InsertIterationValues("R1_Sprint 7", "441", "11/12/2013", "11/25/2013");
            InsertIterationValues("R1_Sprint 6", "401", "10/29/2013", "11/11/2013");
            InsertIterationValues("R1_Sprint 5", "361", "10/15/2013", "10/28/2013");
            InsertIterationValues("R1_Sprint 4", "341", "10/1/2013", "10/14/2013");
            InsertIterationValues("R1_Sprint 3", "302", "9/17/2013", "9/30/2013");
            InsertIterationValues("R1_Sprint 2", "283", "9/3/2013", "9/16/2013");
            InsertIterationValues("R1_Sprint 1", "241", "8/20/2013", "9/2/2013");
            InsertIterationValues("R1_Sprint 0", "181", "8/6/2013", "8/19/2013");

            return IterationCount;
        }

        private bool InsertIterationValues(string IterationName, string AssetOID, string BeginDate, string EndDate)
        {
            string SQL = BuildIterationInsertStatement();
            
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;

                cmd.Parameters.AddWithValue("@AssetOID", AssetOID);
                cmd.Parameters.AddWithValue("@AssetState", "Future");
                cmd.Parameters.AddWithValue("@State", "Future");
                cmd.Parameters.AddWithValue("@Owner", DBNull.Value);
                cmd.Parameters.AddWithValue("@Parent", "1");
                cmd.Parameters.AddWithValue("@Schedule", "Schedule:1000");
                cmd.Parameters.AddWithValue("@Description", DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", IterationName);
                cmd.Parameters.AddWithValue("@BeginDate", BeginDate);
                cmd.Parameters.AddWithValue("@EndDate", EndDate);

                cmd.ExecuteNonQuery();
            }
            IterationCount++;
            return true;
        }

        private string BuildIterationInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO ITERATIONS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Owner,");
            sb.Append("Parent,");
            sb.Append("Schedule,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("EndDate,");
            sb.Append("BeginDate,");
            sb.Append("State) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Owner,");
            sb.Append("@Parent,");
            sb.Append("@Schedule,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@EndDate,");
            sb.Append("@BeginDate,");
            sb.Append("@State);");
            return sb.ToString();
        }

        private void SetIterationSchedules()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE Iterations ");
            sb.Append("SET Iterations.Schedule = Projects.Schedule ");
            sb.Append("FROM Iterations, Projects ");
            sb.Append("WHERE Iterations.Parent = Projects.AssetOID;");

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = sb.ToString();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

    }
}
