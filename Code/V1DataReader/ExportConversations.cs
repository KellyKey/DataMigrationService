using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataReader
{
    public class ExportConversations : IExportAssets
    {
        public ExportConversations(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Expression");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition authoredAtAttribute = assetType.GetAttributeDefinition("AuthoredAt");
            query.Selection.Add(authoredAtAttribute);

            IAttributeDefinition contentAttribute = assetType.GetAttributeDefinition("Content");
            query.Selection.Add(contentAttribute);

            IAttributeDefinition mentionsAttribute = assetType.GetAttributeDefinition("Mentions");
            query.Selection.Add(mentionsAttribute);

            //SPECIAL CASE: BaseAssets attribute only exists in V1 11.3 and earlier.
            IAttributeDefinition baseAssetsAttribute = null;
            if (_metaAPI.Version.Major < 12)
            {
                baseAssetsAttribute = assetType.GetAttributeDefinition("BaseAssets.ID");
                query.Selection.Add(baseAssetsAttribute);
            }

            IAttributeDefinition authorAttribute = assetType.GetAttributeDefinition("Author");
            query.Selection.Add(authorAttribute);

            IAttributeDefinition conversationAttribute = assetType.GetAttributeDefinition("Conversation");
            query.Selection.Add(conversationAttribute);

            IAttributeDefinition inReplyToAttribute = assetType.GetAttributeDefinition("InReplyTo");
            query.Selection.Add(inReplyToAttribute);

            string SQL = BuildConversationInsertStatement();

            if (_config.V1Configurations.PageSize != 0)
            {
                query.Paging.Start = 0;
                query.Paging.PageSize = _config.V1Configurations.PageSize;
            }

            int assetCounter = 0;
            int assetTotal = 0;

            do
            {
                QueryResult result = _dataAPI.Retrieve(query);
                assetTotal = result.TotalAvaliable;

                foreach (Asset asset in result.Assets)
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        //CONTENT NPI MASK:
                        object content = GetScalerValue(asset.GetAttribute(contentAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && content != DBNull.Value)
                        {
                            content = ExportUtils.RemoveNPI(content.ToString());
                        }

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@AuthoredAt", GetScalerValue(asset.GetAttribute(authoredAtAttribute)));
                        cmd.Parameters.AddWithValue("@Content", content);
                        cmd.Parameters.AddWithValue("@Mentions", GetMultiRelationValues(asset.GetAttribute(mentionsAttribute)));

                        if (_metaAPI.Version.Major < 12)
                            cmd.Parameters.AddWithValue("@BaseAssets", GetMultiRelationValues(asset.GetAttribute(baseAssetsAttribute)));

                        cmd.Parameters.AddWithValue("@Author", GetSingleRelationValue(asset.GetAttribute(authorAttribute)));
                        cmd.Parameters.AddWithValue("@Conversation", GetSingleRelationValue(asset.GetAttribute(conversationAttribute)));
                        cmd.Parameters.AddWithValue("@InReplyTo", GetSingleRelationValue(asset.GetAttribute(inReplyToAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
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

            if (_metaAPI.Version.Major < 12)
                sb.Append("BaseAssets,");

            sb.Append("Author,");
            sb.Append("Conversation,");
            sb.Append("InReplyTo) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AuthoredAt,");
            sb.Append("@Content,");
            sb.Append("@Mentions,");

            if (_metaAPI.Version.Major < 12)
                sb.Append("@BaseAssets,");

            sb.Append("@Author,");
            sb.Append("@Conversation,");
            sb.Append("@InReplyTo);");
            return sb.ToString();
        }

    }
}
