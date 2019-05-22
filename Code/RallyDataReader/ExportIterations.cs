using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportIterations : IExportAssets
    {
        public ExportIterations(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.IterationExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            SetIterationSchedules();
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildIterationInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("Iteration") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetIterationState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@State", GetIterationState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@Owner", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Parent", GetRefValue(asset.Element("Project").Attribute("ref").Value));
                    cmd.Parameters.AddWithValue("@Schedule", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Notes").Value, asset.Element("Theme").Value, "Theme"));
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@BeginDate", ConvertRallyDate(asset.Element("StartDate").Value));
                    cmd.Parameters.AddWithValue("@EndDate", ConvertRallyDate(asset.Element("EndDate").Value));

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private object GetIterationState(string State)
        {
            switch (State)
            {
                case "Accepted":
                    return "Closed";
                case "Planning":
                    return "Future";
                case "Committed":
                    return "Active";
                default:
                    return DBNull.Value;
            }
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
