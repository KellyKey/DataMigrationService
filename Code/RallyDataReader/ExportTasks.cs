using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportTasks : IExportAssets
    {
        public ExportTasks(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.TaskExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildTaskInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("Task") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("FormattedID").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetTaskState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Description").Value, asset.Element("Notes").Value, "Notes"));
                    cmd.Parameters.AddWithValue("@Status", GetStatus(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@Parent", GetRefValue(asset.Element("WorkProduct").Attribute("ref").Value));

                    if (asset.Element("WorkProduct").Attribute("type").Value == "HierarchicalRequirement")
                        cmd.Parameters.AddWithValue("@ParentType", "Story");
                    else if (asset.Element("WorkProduct").Attribute("type").Value == "Defect")
                        cmd.Parameters.AddWithValue("@ParentType", "Defect");
                    else
                        cmd.Parameters.AddWithValue("@ParentType", asset.Element("WorkProduct").Attribute("type").Value);

                    if (asset.Descendants("Owner").Any())
                        cmd.Parameters.AddWithValue("@Owners", GetMemberOIDFromDB(GetRefValue(asset.Element("Owner").Attribute("ref").Value)));
                    else
                        cmd.Parameters.AddWithValue("@Owners", DBNull.Value);

                    if (asset.Descendants("Estimate").Any())
                        cmd.Parameters.AddWithValue("@DetailEstimate", asset.Element("Estimate").Value);
                    else
                        cmd.Parameters.AddWithValue("@DetailEstimate", DBNull.Value);

                    if (asset.Descendants("ToDo").Any())
                        cmd.Parameters.AddWithValue("@ToDo", asset.Element("ToDo").Value);
                    else
                        cmd.Parameters.AddWithValue("@ToDo", DBNull.Value);

                    // CUSTOM FIELDS:
                    if (asset.Descendants("c_PDXTeam").Any())
                    {
                        if (string.IsNullOrEmpty(asset.Element("c_PDXTeam").Value) == false)
                            CreateCustomField(asset.Element("ObjectID").Value, "c_PDXTeam", "Text", asset.Element("c_PDXTeam").Value);
                    }

                    //ATTACHMENTS: Hack for Tripwire Chould be refactored into its own class.
                    if (System.Convert.ToInt32(asset.Element("Attachments").Element("Count").Value) > 0)
                        SaveAttachmentRecords(asset.Element("ObjectID").Value, "Task", asset.Element("Attachments"));

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private object GetTaskState(string State)
        {
            switch (State)
            {
                case "Defined":
                    return "Active";
                case "In-Progress":
                    return "Active";
                case "Completed":
                    return "Closed";
                default:
                    return DBNull.Value;
            }
        }

        private string BuildTaskInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO TASKS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Owners,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("DetailEstimate,");
            sb.Append("ToDo,");
            sb.Append("Status,");
            sb.Append("Parent,");
            sb.Append("ParentType) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Owners,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@DetailEstimate,");
            sb.Append("@ToDo,");
            sb.Append("@Status,");
            sb.Append("@Parent,");
            sb.Append("@ParentType);");
            return sb.ToString();
        }

    }
}
