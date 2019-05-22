using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportDefects : IExportAssets
    {
        public ExportDefects(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            assetCounter += ProcessExportFile(_config.JiraConfiguration.XmlFileName);
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildDefectInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.XPathSelectElements("rss/channel/item") select asset;

            foreach (var asset in assets)
            {
                //Check to see if this is an Defect, if not continue to the next
                string type;
                var xType = asset.Element("type");
                if (xType != null)
                {
                    type = xType.Value.ToString().ToLower();
                }
                else
                {
                    continue;
                }

                if (type != "bug") continue;

                string comments = GetComments(asset.Element("comments"));

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", "Defect-" + asset.Element("key").Value);
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("key").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("summary").Value);
                    cmd.Parameters.AddWithValue("@Scope", "Scope-1");
                    cmd.Parameters.AddWithValue("@Description", (asset.Element("description").Value + comments));
                    cmd.Parameters.AddWithValue("@AssetState", GetDefectState(asset.Element("status").Value));
                    cmd.Parameters.AddWithValue("@Status", asset.Element("status").Value);
                    cmd.Parameters.AddWithValue("@Priority", GetSyngentaDefectPriority(asset.Element("priority").Value));
                    cmd.Parameters.AddWithValue("@Owners", asset.Element("assignee").Value);
                    cmd.Parameters.AddWithValue("@Estimate", GetCustomFieldValue(asset.Element("customfields"), "Story Points"));
                    cmd.Parameters.AddWithValue("@Timebox", "Timebox-" + GetCustomFieldValue(asset.Element("customfields"), "Sprint"));
                    
                    string epic = GetCustomFieldValue(asset.Element("customfields"), "Epic Link");
                    if (string.IsNullOrEmpty(epic) == false)
                    {
                        cmd.Parameters.AddWithValue("@Super", "Epic-" + epic);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Super", DBNull.Value);
                    }
                    
                    
                    cmd.Parameters.AddWithValue("@Order", GetCustomFieldValue(asset.Element("customfields"), "Rank"));
                    cmd.Parameters.AddWithValue("@FoundBy", GetCustomFieldValue(asset.Element("customfields"), "Reported By"));
                    cmd.Parameters.AddWithValue("@Environment", asset.Element("environment").Value);
                    cmd.Parameters.AddWithValue("@FoundInBuild", GetCustomFieldValue(asset.Element("customfields"), "Found Build"));
                    cmd.Parameters.AddWithValue("@FixedInBuild", GetCustomFieldValue(asset.Element("customfields"), "Fix Build"));

                    HtmlToText htmlParser = new HtmlToText();
                    XElement xComponent = asset.Element("component");
                    string team = string.Empty;
                    if (xComponent != null)
                    {
                        team = xComponent.Value;
                        if (string.IsNullOrEmpty(team) == false)
                        {
                            team = htmlParser.Convert(team);
                        }
                    }
                    cmd.Parameters.AddWithValue("@Team", team);

                    string resolution = string.Empty;
                    XElement xResolution = asset.Element("resolution");
                    if (xResolution != null)
                    {
                        resolution = xResolution.Value;
                        if (string.IsNullOrEmpty(resolution) == false)
                        {
                            cmd.Parameters.AddWithValue("@ResolutionReason", htmlParser.Convert(resolution));
                        }
                    }

                    cmd.ExecuteNonQuery();
                }

                int linkCnt = SaveAttachmentLinks(asset.Element("key").Value, "Defect", asset.Element("attachments"));
                linkCnt++;

                StringBuilder linkAssetOid = new StringBuilder();
                linkAssetOid.Append(asset.Element("key").Value);
                linkAssetOid.Append("-");
                linkAssetOid.Append(linkCnt.ToString());

                SaveLink(linkAssetOid.ToString(), asset.Element("key").Value, "Defect", "true", asset.Element("link").Value, "Jira");

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

        private string GetSyngentaDefectPriority(string Priority)
        {
            switch (Priority)
            {
                case "Blocker [P1-High]":
                    return "Custom_Priority:2425";
                case "Critical [P1-Low]":
                    return "Custom_Priority:2426";
                case "Major [P2-High]":
                    return "Custom_Priority:2427";
                case "Minor [P2-Med]":
                    return "Custom_Priority:2428";
                case "Trivial [P3]":
                    return "Custom_Priority:2429";
                default:
                    return "Custom_Priority:2428";
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
            sb.Append("Environment,");
            sb.Append("Priority,");
            sb.Append("ResolutionReason,");
            sb.Append("FoundInBuild,");
            sb.Append("FixedInBuild,");
            sb.Append("Team,");
            sb.Append("FoundBy,");
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
            sb.Append("@Environment,");
            sb.Append("@Priority,");
            sb.Append("@ResolutionReason,");
            sb.Append("@FoundInBuild,");
            sb.Append("@FixedInBuild,");
            sb.Append("@Team,");
            sb.Append("@FoundBy,");
            sb.Append("@Timebox);");
            return sb.ToString();
        }

    }
}
