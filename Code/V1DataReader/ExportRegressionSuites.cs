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
    public class ExportRegressionSuites : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportRegressionSuites(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("RegressionSuite");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
            query.Selection.Add(estimateAttribute);

            IAttributeDefinition ownersAttribute = assetType.GetAttributeDefinition("Owner");
            query.Selection.Add(ownersAttribute);

            IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
            query.Selection.Add(referenceAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition regressionPlanAttribute = assetType.GetAttributeDefinition("RegressionPlan");
            query.Selection.Add(regressionPlanAttribute);

            IAttributeDefinition regressionTestsAttribute = assetType.GetAttributeDefinition("RegressionTests");
            query.Selection.Add(regressionTestsAttribute);

            IAttributeDefinition testSetsAttribute = assetType.GetAttributeDefinition("TestSets");
            query.Selection.Add(testSetsAttribute);

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("RegressionPlan.Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildRegressionSuiteInsertStatement();

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
                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@AssetNumber", GetScalerValue(asset.GetAttribute(assetNumberAttribute)));
                        cmd.Parameters.AddWithValue("@Owners", GetMultiRelationValues(asset.GetAttribute(ownersAttribute)));
                        cmd.Parameters.AddWithValue("@Description", GetScalerValue(asset.GetAttribute(descriptionAttribute)));
                        cmd.Parameters.AddWithValue("@Estimate", GetScalerValue(asset.GetAttribute(estimateAttribute)));
                        cmd.Parameters.AddWithValue("@Name", GetScalerValue(asset.GetAttribute(nameAttribute)));
                        cmd.Parameters.AddWithValue("@Reference", GetSingleRelationValue(asset.GetAttribute(referenceAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@RegressionPlan", GetMultiRelationValues(asset.GetAttribute(regressionPlanAttribute)));
                        cmd.Parameters.AddWithValue("@RegressionTests", GetMultiRelationValues(asset.GetAttribute(regressionTestsAttribute)));
                        cmd.Parameters.AddWithValue("@TestSets", GetMultiRelationValues(asset.GetAttribute(testSetsAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Regression Plan: added - Count = {0}", assetCounter);
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private string BuildRegressionSuiteInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO REGRESSIONSUITES (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Owners,");
            sb.Append("Description,");
            sb.Append("Estimate,");
            sb.Append("Name,");
            sb.Append("Reference,");
            sb.Append("TaggedWith,");
            sb.Append("RegressionPlan,");
            sb.Append("RegressionTests,");
            sb.Append("TestSets) ");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Owners,");
            sb.Append("@Description,");
            sb.Append("@Estimate,");
            sb.Append("@Name,");
            sb.Append("@Reference,");
            sb.Append("@TaggedWith,");
            sb.Append("@RegressionPlan,");
            sb.Append("@RegressionTests,");
            sb.Append("@TestSets);");
            return sb.ToString();
        }

    }
}
