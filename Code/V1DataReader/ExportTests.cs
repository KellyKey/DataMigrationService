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
    public class ExportTests : IExportAssets
    {
        public ExportTests(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Test");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition ownersAttribute = assetType.GetAttributeDefinition("Owners.ID");
            query.Selection.Add(ownersAttribute);

            IAttributeDefinition goalsAttribute = assetType.GetAttributeDefinition("Goals.ID");
            query.Selection.Add(goalsAttribute);

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

            IAttributeDefinition stepsAttribute = assetType.GetAttributeDefinition("Steps");
            query.Selection.Add(stepsAttribute);

            IAttributeDefinition inputsAttribute = assetType.GetAttributeDefinition("Inputs");
            query.Selection.Add(inputsAttribute);

            IAttributeDefinition setupAttribute = assetType.GetAttributeDefinition("Setup");
            query.Selection.Add(setupAttribute);

            IAttributeDefinition orderAttribute = assetType.GetAttributeDefinition("Order");
            query.Selection.Add(orderAttribute);

            IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
            query.Selection.Add(estimateAttribute);

            IAttributeDefinition versionTestedAttribute = assetType.GetAttributeDefinition("VersionTested");
            query.Selection.Add(versionTestedAttribute);

            IAttributeDefinition actualResultsAttribute = assetType.GetAttributeDefinition("ActualResults");
            query.Selection.Add(actualResultsAttribute);

            IAttributeDefinition expectedResultsAttribute = assetType.GetAttributeDefinition("ExpectedResults");
            query.Selection.Add(expectedResultsAttribute);

            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
            query.Selection.Add(statusAttribute);

            IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
            query.Selection.Add(categoryAttribute);

            IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
            query.Selection.Add(parentAttribute);

            IAttributeDefinition generatedFromAttribute = assetType.GetAttributeDefinition("GeneratedFrom");
            query.Selection.Add(generatedFromAttribute);

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildTestInsertStatement();

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

                        //STEPS NPI MASK:
                        object steps = GetScalerValue(asset.GetAttribute(stepsAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && steps != DBNull.Value)
                        {
                            steps = ExportUtils.RemoveNPI(steps.ToString());
                        }

                        //INPUTS NPI MASK:
                        object inputs = GetScalerValue(asset.GetAttribute(inputsAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && inputs != DBNull.Value)
                        {
                            inputs = ExportUtils.RemoveNPI(inputs.ToString());
                        }

                        //SETUP NPI MASK:
                        object setup = GetScalerValue(asset.GetAttribute(setupAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && setup != DBNull.Value)
                        {
                            setup = ExportUtils.RemoveNPI(setup.ToString());
                        }

                        //VERSION TESTED NPI MASK:
                        object versionTested = GetScalerValue(asset.GetAttribute(versionTestedAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && versionTested != DBNull.Value)
                        {
                            versionTested = ExportUtils.RemoveNPI(versionTested.ToString());
                        }

                        //ACTUAL RESULTS NPI MASK:
                        object actualResults = GetScalerValue(asset.GetAttribute(actualResultsAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && actualResults != DBNull.Value)
                        {
                            actualResults = ExportUtils.RemoveNPI(actualResults.ToString());
                        }

                        //EXPECTED RESULTS NPI MASK:
                        object expectedResults = GetScalerValue(asset.GetAttribute(expectedResultsAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && expectedResults != DBNull.Value)
                        {
                            expectedResults = ExportUtils.RemoveNPI(expectedResults.ToString());
                        }

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@AssetNumber", GetScalerValue(asset.GetAttribute(assetNumberAttribute)));
                        cmd.Parameters.AddWithValue("@Owners", GetMultiRelationValues(asset.GetAttribute(ownersAttribute)));
                        cmd.Parameters.AddWithValue("@Goals", GetMultiRelationValues(asset.GetAttribute(goalsAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Reference", reference);
                        cmd.Parameters.AddWithValue("@ToDo", GetScalerValue(asset.GetAttribute(toDoAttribute)));
                        cmd.Parameters.AddWithValue("@DetailEstimate", GetScalerValue(asset.GetAttribute(detailEstimateAttribute)));
                        cmd.Parameters.AddWithValue("@Steps", steps);
                        cmd.Parameters.AddWithValue("@Inputs", inputs);
                        cmd.Parameters.AddWithValue("@Setup", setup);
                        cmd.Parameters.AddWithValue("@Order", GetScalerValue(asset.GetAttribute(orderAttribute)));
                        cmd.Parameters.AddWithValue("@Estimate", GetScalerValue(asset.GetAttribute(estimateAttribute)));
                        cmd.Parameters.AddWithValue("@VersionTested", versionTested);
                        cmd.Parameters.AddWithValue("@ActualResults", actualResults);
                        cmd.Parameters.AddWithValue("@ExpectedResults", expectedResults);
                        cmd.Parameters.AddWithValue("@Status", GetSingleRelationValue(asset.GetAttribute(statusAttribute)));
                        cmd.Parameters.AddWithValue("@Category", GetSingleRelationValue(asset.GetAttribute(categoryAttribute)));
                        cmd.Parameters.AddWithValue("@Parent", GetSingleRelationValue(asset.GetAttribute(parentAttribute)));
                        cmd.Parameters.AddWithValue("@GeneratedFrom", GetSingleRelationValue(asset.GetAttribute(generatedFromAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            DeleteEpicTests();
            return assetCounter;
        }

        private string BuildTestInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO TESTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Owners,");
            sb.Append("Goals,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Reference,");
            sb.Append("ToDo,");
            sb.Append("DetailEstimate,");
            sb.Append("Steps,");
            sb.Append("Inputs,");
            sb.Append("Setup,");
            sb.Append("[Order],");
            sb.Append("Estimate,");
            sb.Append("VersionTested,");
            sb.Append("ActualResults,");
            sb.Append("ExpectedResults,");
            sb.Append("Status,");
            sb.Append("Category,");
            sb.Append("Parent,");
            sb.Append("GeneratedFrom) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Owners,");
            sb.Append("@Goals,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Reference,");
            sb.Append("@ToDo,");
            sb.Append("@DetailEstimate,");
            sb.Append("@Steps,");
            sb.Append("@Inputs,");
            sb.Append("@Setup,");
            sb.Append("@Order,");
            sb.Append("@Estimate,");
            sb.Append("@VersionTested,");
            sb.Append("@ActualResults,");
            sb.Append("@ExpectedResults,");
            sb.Append("@Status,");
            sb.Append("@Category,");
            sb.Append("@Parent,");
            sb.Append("@GeneratedFrom);");
            return sb.ToString();
        }

        private void DeleteEpicTests()
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = "DELETE FROM Tests WHERE AssetState = '208';";
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

    }
}
