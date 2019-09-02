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
    public class ExportRegressionTests : IExportAssets
    {
        public ExportRegressionTests(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("RegressionTest");
            Query query = new Query(assetType);

//Team : Relation to Team — reciprocal of RegressionTests

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition ownersAttribute = assetType.GetAttributeDefinition("Owners");
            query.Selection.Add(ownersAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
            query.Selection.Add(categoryAttribute);

            IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
            query.Selection.Add(referenceAttribute);

            IAttributeDefinition stepsAttribute = assetType.GetAttributeDefinition("Steps");
            query.Selection.Add(stepsAttribute);

            IAttributeDefinition inputsAttribute = assetType.GetAttributeDefinition("Inputs");
            query.Selection.Add(inputsAttribute);

            IAttributeDefinition setupAttribute = assetType.GetAttributeDefinition("Setup");
            query.Selection.Add(setupAttribute);

            IAttributeDefinition expectedResultsAttribute = assetType.GetAttributeDefinition("ExpectedResults");
            query.Selection.Add(expectedResultsAttribute);

            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
            query.Selection.Add(statusAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
            query.Selection.Add(teamAttribute);

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildRegressionTestInsertStatement();

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
                        cmd.Parameters.AddWithValue("@Category", GetSingleRelationValue(asset.GetAttribute(categoryAttribute)));
                        cmd.Parameters.AddWithValue("@Reference", GetSingleRelationValue(asset.GetAttribute(referenceAttribute)));
                        cmd.Parameters.AddWithValue("@Steps", GetSingleRelationValue(asset.GetAttribute(stepsAttribute)));
                        cmd.Parameters.AddWithValue("@Inputs", GetSingleRelationValue(asset.GetAttribute(inputsAttribute)));
                        cmd.Parameters.AddWithValue("@Setup", GetSingleRelationValue(asset.GetAttribute(setupAttribute)));
                        cmd.Parameters.AddWithValue("@ExpectedResults", GetSingleRelationValue(asset.GetAttribute(expectedResultsAttribute)));
                        cmd.Parameters.AddWithValue("@Status", GetSingleRelationValue(asset.GetAttribute(statusAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@Team", GetSingleRelationValue(asset.GetAttribute(teamAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private string BuildRegressionTestInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO REGRESSIONTESTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Owners,");
            sb.Append("Scope,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Category,");
            sb.Append("Reference,");
            sb.Append("Steps,");
            sb.Append("Inputs,");
            sb.Append("Setup,");
            sb.Append("ExpectedResults,");
            sb.Append("Status,");
            sb.Append("TaggedWith,");
            sb.Append("Team) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Owners,");
            sb.Append("@Scope,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Category,");
            sb.Append("@Reference,");
            sb.Append("@Steps,");
            sb.Append("@Inputs,");
            sb.Append("@Setup,");
            sb.Append("@ExpectedResults,");
            sb.Append("@Status,");
            sb.Append("@TaggedWith,");
            sb.Append("@Team);");
            return sb.ToString();
        }

    }
}
