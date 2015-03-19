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
    //NOTE: Class used for Tripwire Rally export to import tests with no parent as regression tests to a single project.
    public class ImportOrphanedTests : IImportAssets
    {
        public ImportOrphanedTests(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string SQL = "SELECT * FROM TESTS WITH (NOLOCK) WHERE Parent IS NULL";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //CHECK DATA: RegressionTest must have a name.
                    if (String.IsNullOrEmpty(sdr["Name"].ToString()))
                    {
                        UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "RegressionTest name attribute is required.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("RegressionTest");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, _config.RallySourceConnection.OrphanedTestProject);

                    if (String.IsNullOrEmpty(sdr["Owners"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Members", "Owners", sdr["Owners"].ToString());
                    }

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, "RallyID: " + sdr["AssetNumber"].ToString());

                    IAttributeDefinition stepsAttribute = assetType.GetAttributeDefinition("Steps");
                    asset.SetAttributeValue(stepsAttribute, sdr["Steps"].ToString());

                    IAttributeDefinition inputAttribute = assetType.GetAttributeDefinition("Inputs");
                    asset.SetAttributeValue(inputAttribute, sdr["Inputs"].ToString());

                    IAttributeDefinition setupAttribute = assetType.GetAttributeDefinition("Setup");
                    asset.SetAttributeValue(setupAttribute, sdr["Setup"].ToString());

                    IAttributeDefinition expectedResultsAttribute = assetType.GetAttributeDefinition("ExpectedResults");
                    asset.SetAttributeValue(expectedResultsAttribute, sdr["ExpectedResults"].ToString());

                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("RegressionTestStatus", sdr["Status"].ToString()));

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB("TestCategory", sdr["Category"].ToString()));

                    _dataAPI.Save(asset);

                    string newAssetNumber = GetAssetNumberV1("RegressionTest", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("Tests", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Test imported as a RegressionTest.");
                    importCount++;
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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

    }
}
