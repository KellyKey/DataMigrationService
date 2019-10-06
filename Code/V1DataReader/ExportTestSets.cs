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
    public class ExportTestSets : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportTestSets(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("TestSet");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition timeboxAttribute = assetType.GetAttributeDefinition("Timebox");
            query.Selection.Add(timeboxAttribute);

            IAttributeDefinition regressionSuiteAttribute = assetType.GetAttributeDefinition("RegressionSuite.ID");
            query.Selection.Add(regressionSuiteAttribute);

            IAttributeDefinition ownersAttribute = assetType.GetAttributeDefinition("Owners.ID");
            query.Selection.Add(ownersAttribute);

            IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
            query.Selection.Add(teamAttribute);

            IAttributeDefinition goalsAttribute = assetType.GetAttributeDefinition("Goals.ID");
            query.Selection.Add(goalsAttribute);

            IAttributeDefinition affectedByDefectsAttribute = assetType.GetAttributeDefinition("AffectedByDefects.ID");
            query.Selection.Add(affectedByDefectsAttribute);

            IAttributeDefinition superAttribute = assetType.GetAttributeDefinition("Super");
            query.Selection.Add(superAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
            query.Selection.Add(referenceAttribute);

            IAttributeDefinition toDoAttribute = assetType.GetAttributeDefinition("ToDo");
            query.Selection.Add(toDoAttribute);

            IAttributeDefinition detailEstimateAttribute = assetType.GetAttributeDefinition("DetailEstimate");
            query.Selection.Add(detailEstimateAttribute);

            IAttributeDefinition orderAttribute = assetType.GetAttributeDefinition("Order");
            query.Selection.Add(orderAttribute);

            IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
            query.Selection.Add(estimateAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
            query.Selection.Add(statusAttribute);

            IAttributeDefinition environmentAttribute = assetType.GetAttributeDefinition("Environment.ID");
            query.Selection.Add(environmentAttribute);

            IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
            query.Selection.Add(sourceAttribute);

            IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
            query.Selection.Add(priorityAttribute);

            IAttributeDefinition dependenciesAttribute = assetType.GetAttributeDefinition("Dependencies.ID");
            query.Selection.Add(dependenciesAttribute);

            IAttributeDefinition dependantsAttribute = assetType.GetAttributeDefinition("Dependants.ID");
            query.Selection.Add(dependantsAttribute);

            IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
            query.Selection.Add(parentAttribute);

            IAttributeDefinition requestsAttribute = assetType.GetAttributeDefinition("Requests.ID");
            query.Selection.Add(requestsAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition blockingIssuesAttribute = assetType.GetAttributeDefinition("BlockingIssues.ID");
            query.Selection.Add(blockingIssuesAttribute);

            IAttributeDefinition issuesAttribute = assetType.GetAttributeDefinition("Issues.ID");
            query.Selection.Add(issuesAttribute);

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildTestSetInsertStatement();

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
                        
                        //NAME NPI MASK:
                        object name = GetScalerValue(asset.GetAttribute(nameAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && name != DBNull.Value)
                        {
                            name = ExportUtils.RemoveNPI(name.ToString());
                        }

                        //DESCRIPTION NPI MASK:
                        object description = GetScalerValue(asset.GetAttribute(descriptionAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && description != DBNull.Value)
                        {
                            description = ExportUtils.RemoveNPI(description.ToString());
                        }

                        //REFERENCE NPI MASK:
                        object reference = GetScalerValue(asset.GetAttribute(referenceAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && reference != DBNull.Value)
                        {
                            reference = ExportUtils.RemoveNPI(reference.ToString());
                        }

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@AssetNumber", GetScalerValue(asset.GetAttribute(assetNumberAttribute)));
                        cmd.Parameters.AddWithValue("@Timebox", GetSingleRelationValue(asset.GetAttribute(timeboxAttribute)));
                        cmd.Parameters.AddWithValue("@RegressionSuite", GetSingleRelationValue(asset.GetAttribute(regressionSuiteAttribute)));
                        cmd.Parameters.AddWithValue("@Owners", GetMultiRelationValues(asset.GetAttribute(ownersAttribute)));
                        cmd.Parameters.AddWithValue("@Team", GetSingleRelationValue(asset.GetAttribute(teamAttribute)));
                        cmd.Parameters.AddWithValue("@Goals", GetMultiRelationValues(asset.GetAttribute(goalsAttribute)));
                        cmd.Parameters.AddWithValue("@AffectedByDefects", GetMultiRelationValues(asset.GetAttribute(affectedByDefectsAttribute)));
                        cmd.Parameters.AddWithValue("@Super", GetSingleRelationValue(asset.GetAttribute(superAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Reference", reference);
                        cmd.Parameters.AddWithValue("@ToDo", GetScalerValue(asset.GetAttribute(toDoAttribute)));
                        cmd.Parameters.AddWithValue("@DetailEstimate", GetScalerValue(asset.GetAttribute(detailEstimateAttribute)));
                        cmd.Parameters.AddWithValue("@Order", GetScalerValue(asset.GetAttribute(orderAttribute)));
                        cmd.Parameters.AddWithValue("@Estimate", GetScalerValue(asset.GetAttribute(estimateAttribute)));
                        cmd.Parameters.AddWithValue("@Scope", GetSingleRelationValue(asset.GetAttribute(scopeAttribute)));
                        cmd.Parameters.AddWithValue("@Status", GetSingleRelationValue(asset.GetAttribute(statusAttribute)));
                        cmd.Parameters.AddWithValue("@Environment", GetSingleRelationValue(asset.GetAttribute(environmentAttribute)));
                        cmd.Parameters.AddWithValue("@Source", GetSingleRelationValue(asset.GetAttribute(sourceAttribute)));
                        cmd.Parameters.AddWithValue("@Priority", GetSingleRelationValue(asset.GetAttribute(priorityAttribute)));
                        cmd.Parameters.AddWithValue("@Dependencies", GetMultiRelationValues(asset.GetAttribute(dependenciesAttribute)));
                        cmd.Parameters.AddWithValue("@Dependants", GetMultiRelationValues(asset.GetAttribute(dependantsAttribute)));
                        cmd.Parameters.AddWithValue("@Parent", GetSingleRelationValue(asset.GetAttribute(parentAttribute)));
                        cmd.Parameters.AddWithValue("@Requests", GetMultiRelationValues(asset.GetAttribute(requestsAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@BlockingIssues", GetMultiRelationValues(asset.GetAttribute(blockingIssuesAttribute)));
                        cmd.Parameters.AddWithValue("@Issues", GetMultiRelationValues(asset.GetAttribute(issuesAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("TestSet: added - Count = {0}", assetCounter);
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);

            return assetCounter;
        }

        private string BuildTestSetInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO TestSets (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Timebox,");
            sb.Append("RegressionSuite,");
            sb.Append("Owners,");
            sb.Append("Team,");
            sb.Append("Goals,");
            sb.Append("AffectedByDefects,");
            sb.Append("Super,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Reference,");
            sb.Append("ToDo,");
            sb.Append("DetailEstimate,");
            sb.Append("[Order],");
            sb.Append("Estimate,");
            sb.Append("Scope,");
            sb.Append("Status,");
            sb.Append("Environment,");
            sb.Append("Source,");
            sb.Append("Priority,");
            sb.Append("Dependencies,");
            sb.Append("Dependants,");
            sb.Append("Parent,");
            sb.Append("Requests,");
            sb.Append("TaggedWith,");
            sb.Append("BlockingIssues,");
            sb.Append("Issues) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Timebox,");
            sb.Append("@RegressionSuite,");
            sb.Append("@Owners,");
            sb.Append("@Team,");
            sb.Append("@Goals,");
            sb.Append("@AffectedByDefects,");
            sb.Append("@Super,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Reference,");
            sb.Append("@ToDo,");
            sb.Append("@DetailEstimate,");
            sb.Append("@Order,");
            sb.Append("@Estimate,");
            sb.Append("@Scope,");
            sb.Append("@Status,");
            sb.Append("@Environment,");
            sb.Append("@Source,");
            sb.Append("@Priority,");
            sb.Append("@Dependencies,");
            sb.Append("@Dependants,");
            sb.Append("@Parent,");
            sb.Append("@Requests,");
            sb.Append("@TaggedWith,");
            sb.Append("@BlockingIssues,");
            sb.Append("@Issues);");
            return sb.ToString();
        }
    }
}
