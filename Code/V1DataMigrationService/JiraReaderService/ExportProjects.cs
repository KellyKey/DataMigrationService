using System;
using System.Data.SqlClient;
using System.Text;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportProjects : IExportAssets
    {
        public ExportProjects(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            assetCounter = ProcessProject(_config.JiraConfiguration.ProjectName,_config.JiraConfiguration.ProjectDescription);
            return assetCounter;
        }

        private int ProcessProject(string ProjectName, string ProjectDescription)
        {
            string SQL = BuildProjectInsertStatement();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;

                cmd.Parameters.AddWithValue("@AssetOID", "Scope-1");
                cmd.Parameters.AddWithValue("@AssetState", "Active");
                cmd.Parameters.AddWithValue("@Schedule", "Schedule:1000");
                cmd.Parameters.AddWithValue("@Parent", _config.V1TargetConnection.Project);
                cmd.Parameters.AddWithValue("@IsRelease", "FALSE");
                cmd.Parameters.AddWithValue("@Owner", DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", ProjectDescription);
                cmd.Parameters.AddWithValue("@Name", ProjectName);
                cmd.Parameters.AddWithValue("@BeginDate", "2014-02-14");
                cmd.Parameters.AddWithValue("@Members", DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            return 1;
        }

        private string BuildProjectInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO PROJECTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Schedule,");
            sb.Append("Parent,");
            sb.Append("IsRelease,");
            sb.Append("Owner,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("BeginDate,");
            sb.Append("Members) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Schedule,");
            sb.Append("@Parent,");
            sb.Append("@IsRelease,");
            sb.Append("@Owner,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@BeginDate,");
            sb.Append("@Members);");
            return sb.ToString();
        }
    }

}
