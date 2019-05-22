using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportMembers : IExportAssets
    {
        public ExportMembers(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter;
            assetCounter = ProcessExportFile(_config.JiraConfiguration.XmlFileName);
            return assetCounter;
        }

        private int ProcessExportFile(string fileName)
        {
            string sql = BuildMemberInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(fileName);
            var assets = from asset in xmlDoc.XPathSelectElements("rss/channel/item") select asset;

            foreach (var asset in assets)
            {
                var xMemberName = asset.Element("assignee");
                if (xMemberName != null)
                {
                    string memberName = xMemberName.Value;
                    string userName = xMemberName.Attribute("username").Value;

                    if (GetMemberOIDFromDB(userName) == DBNull.Value)
                    {
                        assetCounter++;
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = _sqlConn;
                            cmd.CommandText = sql;
                            cmd.CommandType = System.Data.CommandType.Text;

                            cmd.Parameters.AddWithValue("@AssetOID", userName);
                            cmd.Parameters.AddWithValue("@Username", userName);
                            cmd.Parameters.AddWithValue("@Password", userName);
                            cmd.Parameters.AddWithValue("@AssetState", "Active");
                            cmd.Parameters.AddWithValue("@Email", DBNull.Value);
                            cmd.Parameters.AddWithValue("@Nickname", memberName);
                            cmd.Parameters.AddWithValue("@Name", memberName);
                            cmd.Parameters.AddWithValue("@Description", "Imported from Jira on " + DateTime.Now.ToShortDateString() + ".");
                            cmd.Parameters.AddWithValue("@DefaultRole", "Role:4");
                            cmd.Parameters.AddWithValue("@NotifyViaEmail", "False");
                            cmd.Parameters.AddWithValue("@SendConversationEmails", "True");

                            cmd.ExecuteNonQuery();
                        }

                    }


                }
                
            }
            return assetCounter;
        }

        private string BuildMemberInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO MEMBERS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Email,");
            sb.Append("Nickname,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("DefaultRole,");
            sb.Append("Username,");
            sb.Append("Password,");
            sb.Append("NotifyViaEmail, ");
            sb.Append("SendConversationEmails) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Email,");
            sb.Append("@Nickname,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@DefaultRole,");
            sb.Append("@Username, ");
            sb.Append("@Password,");
            sb.Append("@NotifyViaEmail, ");
            sb.Append("@SendConversationEmails);");
            return sb.ToString();
        }
    }
}
