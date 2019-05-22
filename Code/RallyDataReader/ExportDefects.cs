using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportDefects : IExportAssets
    {
        public ExportDefects(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.DefectExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildDefectInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("Defect") select asset;

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
                    cmd.Parameters.AddWithValue("@Scope", GetRefValue(asset.Element("Project").Attribute("ref").Value));
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Description").Value, asset.Element("Notes").Value, "Notes"));
                    cmd.Parameters.AddWithValue("@AssetState", GetDefectState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@Status", GetStatus(asset.Element("ScheduleState").Value));
                    cmd.Parameters.AddWithValue("@Priority", asset.Element("Priority").Value);

                    if (asset.Descendants("Owner").Any())
                        cmd.Parameters.AddWithValue("@Owners", GetMemberOIDFromDB(GetRefValue(asset.Element("Owner").Attribute("ref").Value)));
                    else
                        cmd.Parameters.AddWithValue("@Owners", DBNull.Value);

                    if (asset.Descendants("Iteration").Any())
                        cmd.Parameters.AddWithValue("@Timebox", GetRefValue(asset.Element("Iteration").Attribute("ref").Value));
                    else
                        cmd.Parameters.AddWithValue("@Timebox", DBNull.Value);

                    if (asset.Descendants("PlanEstimate").Any())
                        cmd.Parameters.AddWithValue("@Estimate", asset.Element("PlanEstimate").Value);
                    else
                        cmd.Parameters.AddWithValue("@Estimate", DBNull.Value);

                    if (asset.Descendants("Package").Any())
                        cmd.Parameters.AddWithValue("@Parent", asset.Element("Package").Value);
                    else
                        cmd.Parameters.AddWithValue("@Parent", DBNull.Value);

                    if (asset.Descendants("Environment").Any() && asset.Element("Environment").Value != "None")
                        cmd.Parameters.AddWithValue("@Environment", asset.Element("Environment").Value);
                    else
                        cmd.Parameters.AddWithValue("@Environment", DBNull.Value);

                    if (asset.Descendants("Requirement").Any())
                       cmd.Parameters.AddWithValue("@AffectedPrimaryWorkitems", GetRefValue(asset.Element("Requirement").Attribute("ref").Value));
                    else
                       cmd.Parameters.AddWithValue("@AffectedPrimaryWorkitems", DBNull.Value);

                    if (asset.Descendants("FoundInBuild").Any())
                        cmd.Parameters.AddWithValue("@FoundInBuild", asset.Element("FoundInBuild").Value);
                    else
                        cmd.Parameters.AddWithValue("@FoundInBuild", DBNull.Value);

                    if (asset.Descendants("FixedInBuild").Any())
                        cmd.Parameters.AddWithValue("@FixedInBuild", asset.Element("FixedInBuild").Value);
                    else
                        cmd.Parameters.AddWithValue("@FixedInBuild", DBNull.Value);

                    if (asset.Descendants("Severity").Any() && asset.Element("Severity").Value != "None")
                        cmd.Parameters.AddWithValue("@Type", asset.Element("Severity").Value);
                    else
                        cmd.Parameters.AddWithValue("@Type", DBNull.Value);

                    if (asset.Descendants("Resolution").Any() && asset.Element("Resolution").Value != "None")
                        cmd.Parameters.AddWithValue("@ResolutionReason", asset.Element("Resolution").Value);
                    else
                        cmd.Parameters.AddWithValue("@ResolutionReason", DBNull.Value);

                    if (System.Convert.ToInt32(asset.Element("Duplicates").Element("Count").Value) > 0)
                        cmd.Parameters.AddWithValue("@DuplicateOf", GetRefValue(asset.Element("Duplicates").Element("_itemRefArray").Element("Defect").Attribute("ref").Value));
                    else
                        cmd.Parameters.AddWithValue("@DuplicateOf", DBNull.Value);

                    //c_defectsource

                    //CUSTOM FIELDS:
                    if (asset.Descendants("c_PDXTeam").Any())
                        cmd.Parameters.AddWithValue("@Team", asset.Element("c_PDXTeam").Value);
                    else if (asset.Descendants("c_Teamname").Any())
                        cmd.Parameters.AddWithValue("@Team", asset.Element("c_Teamname").Value);
                    else
                        cmd.Parameters.AddWithValue("@Team", DBNull.Value);

                    if (asset.Descendants("c_defectsource").Any())
                    {
                        if (string.IsNullOrEmpty(asset.Element("c_defectsource").Value) == false)
                            CreateCustomField(asset.Element("ObjectID").Value, "c_defectsource", "Text", asset.Element("c_defectsource").Value);
                    }

                    if (asset.Descendants("c_DefectSource").Any())
                    {
                        if (string.IsNullOrEmpty(asset.Element("c_DefectSource").Value) == false)
                            CreateCustomField(asset.Element("ObjectID").Value, "c_DefectSource", "Text", asset.Element("c_DefectSource").Value);
                    }

                    if (asset.Descendants("c_ModulesFunctionality").Any())
                    {
                        if (string.IsNullOrEmpty(asset.Element("c_ModulesFunctionality").Value) == false)
                            CreateCustomField(asset.Element("ObjectID").Value, "c_ModulesFunctionality", "Text", asset.Element("c_ModulesFunctionality").Value);
                    }

                    if (asset.Element("Tags").Element("Count").Value != "0")
                    {
                        string tags = GetTagsList(asset.Element("Tags"));
                        CreateCustomField(asset.Element("ObjectID").Value, "Tags", "Text", tags);
                    }

                    //ATTACHMENTS: Hack for Tripwire Chould be refactored into its own class.
                    if (System.Convert.ToInt32(asset.Element("Attachments").Element("Count").Value) > 0)
                        SaveAttachmentRecords(asset.Element("ObjectID").Value, "Defect", asset.Element("Attachments"));

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private string GetDefectState(string State)
        {
            switch (State)
            {
                case "Closed":
                    return "Closed";
                default:
                    return "Active";
            }
        }

        private string BuildDefectInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO Defects (");
            sb.Append("AssetOID,");
            sb.Append("AssetNumber,");
            sb.Append("AssetState,");
            sb.Append("Name,");
            sb.Append("Scope,");
            sb.Append("Owners,");
            sb.Append("Description,");
            sb.Append("Estimate,");
            sb.Append("Status,");
            sb.Append("Parent,");
            sb.Append("Environment,");
            sb.Append("Priority,");
            sb.Append("ResolutionReason,");
            sb.Append("Type,");
            sb.Append("Team,");
            sb.Append("FoundInBuild,");
            sb.Append("FixedInBuild,");
            sb.Append("DuplicateOf,");
            sb.Append("AffectedPrimaryWorkitems,");
            sb.Append("Timebox) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetNumber,");
            sb.Append("@AssetState,");
            sb.Append("@Name,");
            sb.Append("@Scope,");
            sb.Append("@Owners,");
            sb.Append("@Description,");
            sb.Append("@Estimate,");
            sb.Append("@Status,");
            sb.Append("@Parent,");
            sb.Append("@Environment,");
            sb.Append("@Priority,");
            sb.Append("@ResolutionReason,");
            sb.Append("@Type,");
            sb.Append("@Team,");
            sb.Append("@FoundInBuild,");
            sb.Append("@FixedInBuild,");
            sb.Append("@DuplicateOf,");
            sb.Append("@AffectedPrimaryWorkitems,");
            sb.Append("@Timebox);");
            return sb.ToString();
        }

    }
}
