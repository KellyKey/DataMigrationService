using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportStories : IExportAssets
    {
        public ExportStories(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            assetCounter += ProcessExportFile(_config.JiraConfiguration.XmlFileName);
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildStoryInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.XPathSelectElements("rss/channel/item") select asset;

            foreach (var asset in assets)
            {
                //Check to see if this is an Story, if not continue to the next
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

                if (type != "story" && type != "task") continue;

                string comments = GetComments(asset.Element("comments"));

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", "Story-" + asset.Element("key").Value);
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("key").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("summary").Value);
                    cmd.Parameters.AddWithValue("@Scope", "Scope-1");
                    cmd.Parameters.AddWithValue("@Description", (asset.Element("description").Value + comments));
                    cmd.Parameters.AddWithValue("@AssetState", GetStoryState(asset.Element("status").Value));
                    cmd.Parameters.AddWithValue("@Status", asset.Element("status").Value);
                    cmd.Parameters.AddWithValue("@Estimate", GetCustomFieldValue(asset.Element("customfields"), "Story Points"));
                    cmd.Parameters.AddWithValue("@Owners", asset.Element("assignee").Attribute("username").Value);
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
                    cmd.Parameters.AddWithValue("@RequestedBy", GetCustomFieldValue(asset.Element("customfields"), "Reported By"));

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

                    if (type == "task")
                    {
                        cmd.Parameters.AddWithValue("@Category", "Task");
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Category", DBNull.Value);
                    }
                    
                    cmd.Parameters.AddWithValue("@Dependencies", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Dependants", DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                int linkCnt = SaveAttachmentLinks(asset.Element("key").Value, "Story", asset.Element("attachments"));
                linkCnt++;

                StringBuilder linkAssetOid = new StringBuilder();
                linkAssetOid.Append(asset.Element("key").Value);
                linkAssetOid.Append("-");
                linkAssetOid.Append(linkCnt.ToString());

                SaveLink(linkAssetOid.ToString(), asset.Element("key").Value, "Story", "true", asset.Element("link").Value, "Jira");

                assetCounter++;
            }
            return assetCounter;
        }

        //NOTE: Rally data contains no "state" field, so asset state is derived from "ScheduleState" field.
        private string GetStoryState(string State)
        {
            switch (State)
            {
                case "Closed":
                    return "Closed";
                default:
                    return "Active";
            }
        }

        private string BuildStoryInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO STORIES (");
            sb.Append("AssetOID,");
            sb.Append("AssetNumber,");
            sb.Append("AssetState,");
            sb.Append("Name,");
            sb.Append("Scope,");
            sb.Append("Owners,");
            sb.Append("Super,");
            sb.Append("Description,");
            sb.Append("Estimate,");
            sb.Append("Status,");
            sb.Append("Dependencies,");
            sb.Append("Dependants,");
            sb.Append("[Order],");
            sb.Append("RequestedBy,");
            sb.Append("Category,");
            sb.Append("Team,");
            sb.Append("Timebox) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetNumber,");
            sb.Append("@AssetState,");
            sb.Append("@Name,");
            sb.Append("@Scope,");
            sb.Append("@Owners,");
            sb.Append("@Super,");
            sb.Append("@Description,");
            sb.Append("@Estimate,");
            sb.Append("@Status,");
            sb.Append("@Dependencies,");
            sb.Append("@Dependants,");
            sb.Append("@Order,");
            sb.Append("@RequestedBy,");
            sb.Append("@Category,");
            sb.Append("@Team,");
            sb.Append("@Timebox);");
            return sb.ToString();
        }
    }
}
