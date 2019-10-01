using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataWriter
{
    public class ImportTests : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ImportTests(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
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

                    //SPECIAL CASE: Do not import if ImportStatus is IMPORTED
                    //if (sdr["ImportStatus"].ToString() == "IMPORTED")
                    //{
                    //    continue;
                    //}


                    IAssetType assetType = _metaAPI.GetAssetType("Test");
                    Asset asset = _dataAPI.New(assetType, null);

                    //string newAssetOid = GetNewAssetOIDFromDB(sdr["AssetOID"].ToString(), "Task");
                    //Asset asset = GetAssetFromV1(newAssetOid);

                    if (String.IsNullOrEmpty(customV1IDFieldName) == false)
                    {
                        IAttributeDefinition customV1IDAttribute = assetType.GetAttributeDefinition(customV1IDFieldName);
                        asset.SetAttributeValue(customV1IDAttribute, sdr["AssetNumber"].ToString());
                    }

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    //asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());
                    string targetURL = _config.V1TargetConnection.Url;
                    string newDescription = SetEmbeddedImageContent(sdr["Description"].ToString(), targetURL);
                    asset.SetAttributeValue(descAttribute, newDescription);

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
                    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    ////HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("TestStatus", sdr["Status"].ToString()));

                    if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    {
                        IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");

                        AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());

                    }

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));
                    ////HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB("TestCategory", sdr["Category"].ToString()));

                    //Fix the Orphan Parent due to VersionOne not allowing it.  Set it to the Default Story
                    String parentValue = null;
                    String parentType = null;

                    if (String.IsNullOrEmpty(sdr["Parent"].ToString()) == true)
                    {
                        parentValue = _config.RallySourceConnection.OrphanTasksDefaultStory;
                        parentType = "Story";
                    }
                    else
                    {
                        parentValue = sdr["Parent"].ToString();
                    }

                    IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
                    if (String.IsNullOrEmpty(parentType) == true)
                    {
                        if (GetNewAssetOIDFromDB(parentValue, "Story") != null)
                        {
                            asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(parentValue, "Story"));
                        }
                        else if (GetNewAssetOIDFromDB(parentValue, "Defect") != null)
                        {
                            asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(parentValue, "Defect"));
                        }
                        else
                        {
                            parentValue = _config.RallySourceConnection.OrphanTasksDefaultStory;
                            asset.SetAttributeValue(parentAttribute, parentValue);
                        }
                    }
                    else
                    {
                        //string newAssetOID = null;
                        //    if (sdr["ParentType"].ToString() == "Story")
                        //    {
                        //newAssetOID = GetNewAssetOIDFromDB(parentValue, "Stories");
                        //        if (String.IsNullOrEmpty(newAssetOID) == false)
                        asset.SetAttributeValue(parentAttribute, parentValue);

                    }

                    _dataAPI.Save(asset);

                    string newAssetNumber = GetAssetNumberV1("Test", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("Tests", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Test imported.");
                    importCount++;

                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);

                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
                        _logger.Error("Asset: " + sdr["AssetOID"].ToString() + " Failed to Import ");
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
            int exceptionCount = 0;
            int colonLocation = 0;

            while (sdr.Read())
            {
                Asset asset = null;

                if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                {
                    asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                }
                else
                {
                    continue;
                }

                try
                {
                    colonLocation = asset.Oid.ToString().IndexOf(":");
                    ExecuteOperationInV1(asset.Oid.ToString().Substring(0, colonLocation) + ".Inactivate", asset.Oid);
                    assetCount++;
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Close - Count: " + assetCount);
                }
                catch (Exception ex)
                {
                    exceptionCount++;
                    _logger.Info("Exception: " + sdr["AssetOID"].ToString() + " Exception - Count: " + exceptionCount);
                    continue;
                }

            }
            sdr.Close();
            return assetCount;
        }

        public int OpenTests()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Tests");
            int assetCount = 0;
            int exceptionCount = 0;
            int colonLocation = 0;

            while (sdr.Read())
            {
                Asset asset = null;

                if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                {
                    asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                }
                else
                {
                    continue;
                }

                try
                {
                    colonLocation = asset.Oid.ToString().IndexOf(":");
                    ExecuteOperationInV1(asset.Oid.ToString().Substring(0, colonLocation) + ".Reactivate", asset.Oid);
                    assetCount++;
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Open - Count: " + assetCount);
                }
                catch (Exception ex)
                {
                    exceptionCount++;
                    _logger.Info("Exception: " + sdr["AssetOID"].ToString() + " Exception - Count: " + exceptionCount);
                    continue;
                }

            }
            sdr.Close();
            return assetCount;
        }


    }
}
