using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataReader
{
    public class ExportConversations : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportConversations(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
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
            if (false)
            {
                baseAssetsAttribute = assetType.GetAttributeDefinition("BaseAssets.ID");
                query.Selection.Add(baseAssetsAttribute);
            }

            IAttributeDefinition authorAttribute = assetType.GetAttributeDefinition("Author");
            query.Selection.Add(authorAttribute);

            IAttributeDefinition conversationAttribute = null;
            IAttributeDefinition beLongsToAttribute = null;
            //if (_metaAPI.Version.Major < 12)
             if (  false )
            {
                conversationAttribute = assetType.GetAttributeDefinition("Conversation");
                query.Selection.Add(conversationAttribute);
            }
            else
            {
                beLongsToAttribute = assetType.GetAttributeDefinition("BelongsTo");
                query.Selection.Add(beLongsToAttribute);
            }

            IAttributeDefinition inReplyToAttribute = assetType.GetAttributeDefinition("InReplyTo");
            query.Selection.Add(inReplyToAttribute);

            //Filter on parent scope.
            IAttributeDefinition workitemScopeAttribute = assetType.GetAttributeDefinition("Mentions:Workitem.Scope.ParentMeAndUp");
            FilterTerm termWorkitem = new FilterTerm(workitemScopeAttribute);
            termWorkitem.Equal(_config.V1SourceConnection.Project);

            IAttributeDefinition issueScopeAttribute = assetType.GetAttributeDefinition("Mentions:Issue.Scope.ParentMeAndUp");
            FilterTerm termIssue = new FilterTerm(issueScopeAttribute);
            termIssue.Equal(_config.V1SourceConnection.Project);

            IAttributeDefinition goalScopeAttribute = assetType.GetAttributeDefinition("Mentions:Goal.Scope.ParentMeAndUp");
            FilterTerm termGoal = new FilterTerm(goalScopeAttribute);
            termGoal.Equal(_config.V1SourceConnection.Project);

            IAttributeDefinition regressionTestScopeAttribute = assetType.GetAttributeDefinition("Mentions:RegressionTest.Scope.ParentMeAndUp");
            FilterTerm termRegressionTest = new FilterTerm(regressionTestScopeAttribute);
            termRegressionTest.Equal(_config.V1SourceConnection.Project);

            IAttributeDefinition requestScopeAttribute = assetType.GetAttributeDefinition("Mentions:Request.Scope.ParentMeAndUp");
            FilterTerm termRequest = new FilterTerm(requestScopeAttribute);
            termRequest.Equal(_config.V1SourceConnection.Project);

            query.Filter = new OrFilterTerm(termWorkitem, termIssue, termGoal, termRegressionTest, termRequest);


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

                        if (false)
                            cmd.Parameters.AddWithValue("@BaseAssets", GetMultiRelationValues(asset.GetAttribute(baseAssetsAttribute)));

                        if (false)
                        {
                            cmd.Parameters.AddWithValue("@Conversation", GetMultiRelationValues(asset.GetAttribute(conversationAttribute)));
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@BelongsTo", GetSingleRelationValue(asset.GetAttribute(beLongsToAttribute)));
                        }

                        cmd.Parameters.AddWithValue("@Author", GetSingleRelationValue(asset.GetAttribute(authorAttribute)));
                        cmd.Parameters.AddWithValue("@InReplyTo", GetSingleRelationValue(asset.GetAttribute(inReplyToAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Conversation: added - Count = {0}", assetCounter);
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

            if (false)
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

            if (false)
                sb.Append("@BaseAssets,");

            sb.Append("@Author,");

            if (false)
            {
                sb.Append("@Conversation,");
            }
            else 
            {
                sb.Append("@BelongsTo,");            
            }

            sb.Append("@InReplyTo);");
            return sb.ToString();
        }

    }
}
