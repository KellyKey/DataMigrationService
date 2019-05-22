using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportConversations : IExportAssets
    {
        public ExportConversations(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.ConversationExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildConversationInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("ConversationPost") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetState", "Active");
                    cmd.Parameters.AddWithValue("@AuthoredAt", ConvertRallyDate(asset.Element("CreationDate").Value));
                    cmd.Parameters.AddWithValue("@Author", GetMemberOIDFromDB(GetRefValue(asset.Element("User").Attribute("ref").Value)));
                    cmd.Parameters.AddWithValue("@Mentions", GetRefValue(asset.Element("Artifact").Attribute("ref").Value));
                    cmd.Parameters.AddWithValue("@Conversation", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@InReplyTo", DBNull.Value);
                    cmd.Parameters.AddWithValue("@BaseAssetType", GetBaseAssetType(asset.Element("Artifact").Attribute("type").Value));
                    cmd.Parameters.AddWithValue("@Index", asset.Element("PostNumber").Value);

                    //Convert HTML content to plain text.
                    HtmlToText htmlParser = new HtmlToText();
                    cmd.Parameters.AddWithValue("@Content", htmlParser.Convert(asset.Element("Text").Value));

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private object GetBaseAssetType(string Type)
        {
            switch (Type)
            {
                case "TestCase":
                    return "Test";
                case "HierarchicalRequirement":
                    return "Story";
                case "Defect":
                    return "Defect";
                case "Task":
                    return "Task";
                default:
                    return DBNull.Value;
            }
        }

        private string BuildConversationInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO CONVERSATIONS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AuthoredAt,");
            sb.Append("Content,");
            sb.Append("Mentions,");
            sb.Append("Author,");
            sb.Append("BaseAssetType,");
            sb.Append("Conversation,");
            sb.Append("InReplyTo,");
            sb.Append("[Index]) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AuthoredAt,");
            sb.Append("@Content,");
            sb.Append("@Mentions,");
            sb.Append("@Author,");
            sb.Append("@BaseAssetType,");
            sb.Append("@Conversation,");
            sb.Append("@InReplyTo,");
            sb.Append("@Index);");
            return sb.ToString();
        }

    }
}
