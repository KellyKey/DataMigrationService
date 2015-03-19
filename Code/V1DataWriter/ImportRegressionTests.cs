using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataWriter
{
    public class ImportRegressionTests : IImportAssets
    {
        public ImportRegressionTests(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("RegressionTests");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //CHECK DATA: RegressionTest must have a name.
                    if (String.IsNullOrEmpty(sdr["Name"].ToString()))
                    {
                        UpdateImportStatus("RegressionTests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "RegressionTest name attribute is required.");
                        continue;
                    }

                    //CHECK DATA: RegressionTest must have a scope.
                    if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    {
                        UpdateImportStatus("RegressionTests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "RegressionTest scope attribute is required.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("RegressionTest");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString(), "Projects"));

                    if (String.IsNullOrEmpty(sdr["Owners"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Members", "Owners", sdr["Owners"].ToString());
                    }

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    IAttributeDefinition stepsAttribute = assetType.GetAttributeDefinition("Steps");
                    asset.SetAttributeValue(stepsAttribute, sdr["Steps"].ToString());

                    IAttributeDefinition inputAttribute = assetType.GetAttributeDefinition("Inputs");
                    asset.SetAttributeValue(inputAttribute, sdr["Inputs"].ToString());

                    IAttributeDefinition setupAttribute = assetType.GetAttributeDefinition("Setup");
                    asset.SetAttributeValue(setupAttribute, sdr["Setup"].ToString());

                    IAttributeDefinition expectedResultsAttribute = assetType.GetAttributeDefinition("ExpectedResults");
                    asset.SetAttributeValue(expectedResultsAttribute, sdr["ExpectedResults"].ToString());

                    //HACK: For Rally import, needs to be refactored.
                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("RegressionTestStatus", sdr["Status"].ToString()));

                    //HACK: For Rally import, needs to be refactored.
                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    //asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB("TestCategory", sdr["Category"].ToString()));

                    _dataAPI.Save(asset);

                    string newAssetNumber = GetAssetNumberV1("RegressionTest", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("RegressionTests", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("RegressionTests", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "RegressionTest imported.");
                    importCount++;
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("RegressionTests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
                        continue;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            sdr.Close();
            return importCount;
        }

        public int CloseRegressionTests()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("RegressionTests");
            int assetCount = 0;
            while (sdr.Read())
            {
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                ExecuteOperationInV1("RegressionTest.Inactivate", asset.Oid);
                assetCount++;
            }
            sdr.Close();
            return assetCount;
        }

    }
}
