using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportReleases : IExportAssets
    {
        public ExportReleases(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.ReleaseExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildReleaseInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("Release") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetReleaseState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@Schedule", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Parent", GetParentOID(asset.Element("Project")));
                    cmd.Parameters.AddWithValue("@IsRelease", "TRUE");
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Notes").Value, asset.Element("Theme").Value, "Theme"));
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@BeginDate", ConvertRallyDate(asset.Element("ReleaseStartDate").Value));
                    cmd.Parameters.AddWithValue("@EndDate", ConvertRallyDate(asset.Element("ReleaseDate").Value));
                    cmd.Parameters.AddWithValue("@Members", DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private object GetReleaseState(string State)
        {
            switch (State)
            {
                case "Active":
                    return "Active";
                case "Planning":
                    return "Active";
                case "Accepted":
                    return "Closed";
                default:
                    return DBNull.Value;
            }
        }

        private string GetParentOID(XElement Parent)
        {
            return Parent != null ? GetRefValue(Parent.Attribute("ref").Value) : _config.V1TargetConnection.Project;
        }

        private string BuildReleaseInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO PROJECTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Schedule,");
            sb.Append("Parent,");
            sb.Append("IsRelease,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("BeginDate,");
            sb.Append("EndDate,");
            sb.Append("Members) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Schedule,");
            sb.Append("@Parent,");
            sb.Append("@IsRelease,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@BeginDate,");
            sb.Append("@EndDate,");
            sb.Append("@Members);");
            return sb.ToString();
        }
    }
}
