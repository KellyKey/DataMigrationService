using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportIssues : IExportAssets
    {
        public ExportIssues(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            assetCounter += ProcessExportFile(_config.JiraConfiguration.XmlFileName);
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildIssueInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.XPathSelectElements("rss/channel/item") select asset;

            foreach (var asset in assets)
            {
                //Check to see if this is an Epic, if not continue to the next
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

                if (type != "impediment") continue;

                string comments = GetComments(asset.Element("comments"));

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", "Issue-" + asset.Element("key").Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetIssueState(asset.Element("status").Value));
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("key").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("summary").Value);
                    cmd.Parameters.AddWithValue("@Scope", "Scope-1");
                    cmd.Parameters.AddWithValue("@Description", asset.Element("description").Value + comments);
                    cmd.Parameters.AddWithValue("@Status", asset.Element("status").Value);
                    cmd.Parameters.AddWithValue("@Owner", asset.Element("assignee").Attribute("username").Value);
                    cmd.Parameters.AddWithValue("@IdentifiedBy", asset.Element("reporter").Value);
                    cmd.Parameters.AddWithValue("@Order", GetCustomFieldValue(asset.Element("customfields"), "Rank"));
                    cmd.Parameters.AddWithValue("@Priority", asset.Element("priority").Value);

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

                    cmd.ExecuteNonQuery();
                }

                int linkCnt = SaveAttachmentLinks(asset.Element("key").Value, "Issue", asset.Element("attachments"));
                linkCnt++;

                StringBuilder linkAssetOid = new StringBuilder();
                linkAssetOid.Append(asset.Element("key").Value);
                linkAssetOid.Append("-");
                linkAssetOid.Append(linkCnt.ToString());

                SaveLink(linkAssetOid.ToString(), asset.Element("key").Value, "Issue", "true", asset.Element("link").Value, "Jira");

                assetCounter++;
            }
            return assetCounter;
        }

        //NOTE: Rally data contains no "state" field, so asset state is derived from "ScheduleState" field.
        private string GetIssueState(string State)
        {
            switch (State)
            {
                case "Closed":
                    return "Closed";
                default:
                    return "Active";
            }
        }


        private string BuildIssueInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO ISSUES (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Team,");
            sb.Append("Scope,");
            sb.Append("Owner,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("IdentifiedBy,");
            sb.Append("[Order],");
            sb.Append("Priority)");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Team,");
            sb.Append("@Scope,");
            sb.Append("@Owner,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@IdentifiedBy,");
            sb.Append("@Order,");
            sb.Append("@Priority);");
            return sb.ToString();
        }


    }
}
