using System;
using System.Data.SqlClient;
using System.Text;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportSchedules : IExportAssets
    {
        public ExportSchedules(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        private static int scheduleCount = 0;

        public override int Export()
        {
            //TO DO: Support merging projects.
            //string parent = "Scope:0";
            string parent = string.Empty;

            SetProjectSchedules(parent);
            return scheduleCount;
        }

        private void SetProjectSchedules(string ParentOID)
        {
            SqlDataReader sdr = GetProjectsFromDB(ParentOID);
            while (sdr.Read())
            {
                string projectOID = sdr["AssetOID"].ToString();

                if (CheckIsRelease(projectOID) == false)
                {
                    scheduleCount++;
                    CreateProjectSchedule(_config.JiraConfiguration.DefaultSchedule, scheduleCount);
                    UpdateProjectSchedule(projectOID, scheduleCount);
                    SetProjectSchedules(projectOID);
                }
                else if (CheckIsRelease(projectOID) == true)
                {
                    UpdateReleaseSchedule(projectOID, sdr["Parent"].ToString());
                }
            }
        }

        private bool CheckIsRelease(string ProjectOID)
        {
            string SQL = "SELECT IsRelease FROM Projects WHERE AssetOID = '" + ProjectOID + "';";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            return System.Convert.ToBoolean(cmd.ExecuteScalar());
        }

        //TO DO: Only create a schedule if project has iterations?
        //private bool CheckHasIterations(string ProjectOID)
        //{
        //    return true;
        //}

        private void CreateProjectSchedule(string ScheduleName, int ScheduleOID)
        {
            string SQL = BuildScheduleInsertStatement();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Parameters.AddWithValue("@AssetOID", ScheduleOID.ToString());
                cmd.Parameters.AddWithValue("@AssetState", "Active");
                cmd.Parameters.AddWithValue("@Description", "Imported from Jira on " + DateTime.Now.ToString() + ".");
                cmd.Parameters.AddWithValue("@Name", ScheduleName + " Sprint");
                cmd.Parameters.AddWithValue("@TimeboxGap", "0");
                cmd.Parameters.AddWithValue("@TimeboxLength", "2 Weeks");
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateProjectSchedule(string ProjectOID, int ScheduleOID)
        {
            string SQL = "UPDATE Projects SET Schedule = '" + ScheduleOID.ToString() + "' WHERE AssetOID = '" + ProjectOID + "';";
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateReleaseSchedule(string ProjectOID, string ParentOID)
        {
            string parentSchedule = GetParentSchedule(ParentOID);
            string SQL = "UPDATE Projects SET Schedule = '" + parentSchedule + "' WHERE AssetOID = '" + ProjectOID + "';";
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        private string GetParentSchedule(string ParentOID)
        {
            string SQL = "SELECT Schedule FROM Projects WHERE AssetOID = '" + ParentOID + "';";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            return (string)cmd.ExecuteScalar();
        }

        private string BuildScheduleInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO SCHEDULES (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("TimeboxGap,");
            sb.Append("TimeboxLength) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@TimeboxGap,");
            sb.Append("@TimeboxLength);");
            return sb.ToString();
        }

    }
}
