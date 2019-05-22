using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportTasks : IExportAssets
    {
        public ExportTasks(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            assetCounter += ProcessExportFile(_config.JiraConfiguration.XmlFileName);
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildTaskInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.XPathSelectElements("rss/channel/item") select asset;

            foreach (var asset in assets)
            {
                //Check to see if this is an task or technical tasks, if not continue to the next
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

                if (type != "sub-task" && type != "technical task") continue;

                string comments = GetComments(asset.Element("comments"));

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", "Task-" + asset.Element("key").Value);
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("key").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("summary").Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetTaskState(asset.Element("status").Value));
                    cmd.Parameters.AddWithValue("@Description", asset.Element("description").Value + comments);
                    cmd.Parameters.AddWithValue("@Status", asset.Element("status").Value);
                    
                    // - Determine Parent Type
                    //cmd.Parameters.AddWithValue("@ParentType", "Story");
                    string parentType = string.Empty;

                    if (GetAssetFromDB("Story-" + asset.Element("parent").Value,"Stories") != null)
                    {
                        parentType = "Story";
                    } else
                    {
                        parentType = "Defect";
                    }
                    cmd.Parameters.AddWithValue("@Parent", parentType + "-" + asset.Element("parent").Value);
                    cmd.Parameters.AddWithValue("@ParentType", parentType);
                    cmd.Parameters.AddWithValue("@Owners", (asset.Element("assignee").Attribute("username").Value));

                    var xDetailEstimate = asset.Element("timeoriginalestimate");
                    string detailEstimate = string.Empty;
                    if (xDetailEstimate != null)
                    {
                        float seconds = Convert.ToSingle(xDetailEstimate.Attribute("seconds").Value);
                        int hours = Convert.ToInt32(seconds/3600);
                        detailEstimate = hours.ToString();
                    }
                    cmd.Parameters.AddWithValue("@DetailEstimate", detailEstimate);

                    var xStillToDo = asset.Element("timeoriginalestimate");
                    string stillToDo = string.Empty;
                    if (xStillToDo != null)
                    {
                        float seconds = Convert.ToSingle(xStillToDo.Attribute("seconds").Value);
                        int hours = Convert.ToInt32(seconds / 3600);
                        stillToDo = hours.ToString();
                    }
                    if (asset.Element("status").Value == "Closed")
                        stillToDo = "0";
                    cmd.Parameters.AddWithValue("@ToDo", stillToDo);

                    cmd.ExecuteNonQuery();
                }

                int linkCnt = SaveAttachmentLinks(asset.Element("key").Value, "Task", asset.Element("attachments"));
                linkCnt++;

                StringBuilder linkAssetOid = new StringBuilder();
                linkAssetOid.Append(asset.Element("key").Value);
                linkAssetOid.Append("-");
                linkAssetOid.Append(linkCnt.ToString());

                SaveLink(linkAssetOid.ToString(), asset.Element("key").Value, "Task", "true", asset.Element("link").Value, "Jira");

                assetCounter++;
            }
            return assetCounter;
        }

        private object GetTaskState(string State)
        {
            switch (State)
            {
                case "Closed":
                    return "Closed";
                default:
                    return "Active";
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
