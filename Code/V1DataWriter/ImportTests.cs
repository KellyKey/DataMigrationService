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
    public class ImportTests : IImportAssets
    {
        public ImportTests(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("Test");
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("Tests");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //CHECK DATA: Test must have a name.
                    if (String.IsNullOrEmpty(sdr["Name"].ToString()))
                    {
                        UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Test name attribute is required.");
                        continue;
                    }

                    //CHECK DATA: Test must have a parent.
                    if (String.IsNullOrEmpty(sdr["Parent"].ToString()))
                    {
                        UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Test parent attribute is required.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Test");
                    Asset asset = _dataAPI.New(assetType, null);

                    if (String.IsNullOrEmpty(customV1IDFieldName) == false)
                    {
                        IAttributeDefinition customV1IDAttribute = assetType.GetAttributeDefinition(customV1IDFieldName);
                        asset.SetAttributeValue(customV1IDAttribute, sdr["AssetNumber"].ToString());
                    }

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                    if (String.IsNullOrEmpty(sdr["Owners"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Members", "Owners", sdr["Owners"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["Goals"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Goals", sdr["Goals"].ToString());
                    }

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    IAttributeDefinition detailEstimateAttribute = assetType.GetAttributeDefinition("DetailEstimate");
                    asset.SetAttributeValue(detailEstimateAttribute, sdr["DetailEstimate"].ToString());

                    IAttributeDefinition toDoAttribute = assetType.GetAttributeDefinition("ToDo");
                    asset.SetAttributeValue(toDoAttribute, sdr["ToDo"].ToString());

                    IAttributeDefinition stepsAttribute = assetType.GetAttributeDefinition("Steps");
                    asset.SetAttributeValue(stepsAttribute, sdr["Steps"].ToString());

                    IAttributeDefinition inputAttribute = assetType.GetAttributeDefinition("Inputs");
                    asset.SetAttributeValue(inputAttribute, sdr["Inputs"].ToString());

                    IAttributeDefinition setupAttribute = assetType.GetAttributeDefinition("Setup");
                    asset.SetAttributeValue(setupAttribute, sdr["Setup"].ToString());

                    IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
                    asset.SetAttributeValue(estimateAttribute, sdr["Estimate"].ToString());

                    IAttributeDefinition versionTestedAttribute = assetType.GetAttributeDefinition("VersionTested");
                    asset.SetAttributeValue(versionTestedAttribute, sdr["VersionTested"].ToString());

                    IAttributeDefinition actualResultsAttribute = assetType.GetAttributeDefinition("ActualResults");
                    asset.SetAttributeValue(actualResultsAttribute, sdr["ActualResults"].ToString());

                    IAttributeDefinition expectedResultsAttribute = assetType.GetAttributeDefinition("ExpectedResults");
                    asset.SetAttributeValue(expectedResultsAttribute, sdr["ExpectedResults"].ToString());

                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("TestStatus", sdr["Status"].ToString()));

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB("TestCategory", sdr["Category"].ToString()));

                    //HACK: For Rally import, needs to be refactored.
                    IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
                    if (String.IsNullOrEmpty(sdr["ParentType"].ToString()) == true)
                        asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(sdr["Parent"].ToString()));
                    else
                    {
                        string newAssetOID = null;
                        if (sdr["ParentType"].ToString() == "Story")
                        {
                            newAssetOID = GetNewAssetOIDFromDB(sdr["Parent"].ToString(), "Stories");
                            if (String.IsNullOrEmpty(newAssetOID) == false)
                                asset.SetAttributeValue(parentAttribute, newAssetOID);
                            else
                            {
                                newAssetOID = GetNewAssetOIDFromDB(sdr["Parent"].ToString(), "Epics");
                                if (String.IsNullOrEmpty(newAssetOID) == false)
                                    asset.SetAttributeValue(parentAttribute, newAssetOID);
                                else
                                    throw new Exception("Import failed. Parent could not be found.");
                            }
                        }
                        else
                        {
                            newAssetOID = GetNewAssetOIDFromDB(sdr["Parent"].ToString(), "Defects");
                            if (String.IsNullOrEmpty(newAssetOID) == false)
                                asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(sdr["Parent"].ToString(), "Defects"));
                            else
                                throw new Exception("Import failed. Parent defect could not be found.");
                        }
                    }

                    _dataAPI.Save(asset);

                    string newAssetNumber = GetAssetNumberV1("Test", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("Tests", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Test imported.");
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

        public int CloseTests()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Tests");
            int assetCount = 0;
            while (sdr.Read())
            {
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                ExecuteOperationInV1("Test.Inactivate", asset.Oid);
                assetCount++;
            }
            sdr.Close();
            return assetCount;
        }

    }
}
