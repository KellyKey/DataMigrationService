using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportProjects : IExportAssets
    {
        public ExportProjects(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.ProjectExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildProjectInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("Project") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetProjectState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@Schedule", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Parent", GetParentOID(asset.Element("Parent")));
                    cmd.Parameters.AddWithValue("@IsRelease", "FALSE");
                    cmd.Parameters.AddWithValue("@Owner", GetMemberOIDFromDB(GetOwner(asset.Element("Owner"))));
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Description").Value, asset.Element("Notes").Value, "Notes"));
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@BeginDate", ConvertRallyDate(asset.Element("CreationDate").Value));
                    cmd.Parameters.AddWithValue("@Members", DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private object GetProjectState(string State)
        {
            switch (State)
            {
                case "Open":
                    return "Active";
                case "Closed":
                    return "Closed";
                default:
                    return DBNull.Value;
            }
        }

        //TO DO: Add support for merging project trees.
        private string GetParentOID(XElement Parent)
        {
            return Parent != null ? GetRefValue(Parent.Attribute("ref").Value) : "Scope:0";
        }

        // MTB - Added to handle nodes with no Owner
        private string GetOwner(XElement Owner)
        {
            return Owner != null ? GetRefValue(Owner.Attribute("ref").Value) : string.Empty;
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
