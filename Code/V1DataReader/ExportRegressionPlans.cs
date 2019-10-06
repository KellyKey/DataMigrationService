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
    public class ExportRegressionPlans : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportRegressionPlans(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("RegressionPlan");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition ownersAttribute = assetType.GetAttributeDefinition("Owner");
            query.Selection.Add(ownersAttribute);

            IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
            query.Selection.Add(referenceAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition regressionSuitesAttribute = assetType.GetAttributeDefinition("RegressionSuites");
            query.Selection.Add(regressionSuitesAttribute);

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildRegressionPlanInsertStatement();

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
                        cmd.Parameters.AddWithValue("@Scope", GetSingleRelationValue(asset.GetAttribute(scopeAttribute)));
                        cmd.Parameters.AddWithValue("@Description", GetScalerValue(asset.GetAttribute(descriptionAttribute)));
                        cmd.Parameters.AddWithValue("@Name", GetScalerValue(asset.GetAttribute(nameAttribute)));
                        cmd.Parameters.AddWithValue("@Reference", GetSingleRelationValue(asset.GetAttribute(referenceAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@RegressionSuites", GetMultiRelationValues(asset.GetAttribute(regressionSuitesAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Regression Plan: added - Count = {0}", assetCounter);
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private string BuildRegressionPlanInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO REGRESSIONPLANS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Owners,");
            sb.Append("Scope,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Reference,");
            sb.Append("TaggedWith,");
            sb.Append("RegressionSuites) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Owners,");
            sb.Append("@Scope,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Reference,");
            sb.Append("@TaggedWith,");
            sb.Append("@RegressionSuites);");
            return sb.ToString();
        }

    }
}
