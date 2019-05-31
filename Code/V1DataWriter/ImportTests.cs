﻿using System;
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
                    //if (String.IsNullOrEmpty(sdr["Parent"].ToString()))
                    //{
                    //    UpdateImportStatus("Tests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Test parent attribute is required.");
                    //    continue;
                    //}

                    //SPECIAL CASE: Do not import if ImportStatus is IMPORTED
                    if (sdr["ImportStatus"].ToString() == "IMPORTED")
                    {
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
                    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("TestStatus", sdr["Status"].ToString()));

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    //asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB("TestCategory", sdr["Category"].ToString()));

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

                    //Get Any EmbeddedImages for the Description Field
                    int[] imgIDLocation = new int[10];
                    int imageCounter = 0;

                    SqlDataReader sdrEmbeddedImages = GetDataFromDB("EmbeddedImages", sdr["AssetOID"].ToString());
                    while (sdrEmbeddedImages.Read())
                    {
                        //UploadEmbeddedImageContent(ref asset, asset.Oid,(byte[])sdrEmbeddedImages["Content"], _imageConnector);

                        ////Create an embedded image.
                        string filePath = @"C:\Windows\Temp\TempImage.png";
                        System.IO.File.WriteAllBytes(filePath, (byte[])sdrEmbeddedImages["Content"]);
                        //string file = @"C:\Temp\versionone.jpg";
                        Oid embeddedImageOid = _dataAPI.SaveEmbeddedImage(filePath, asset);

                        string currentDescription = sdr["Description"].ToString();
                        imgIDLocation[imageCounter] = currentDescription.IndexOf(".img/", 0, currentDescription.Length);
                        string firstDescriptionPart = currentDescription.Substring(0, imgIDLocation[imageCounter] + 5);
                        string secondDescriptionPart = currentDescription.Substring(imgIDLocation[imageCounter] + 5);
                        imgIDLocation[imageCounter] = secondDescriptionPart.IndexOf(">", 0, secondDescriptionPart.Length);
                        string newSecondDescriptionPart = secondDescriptionPart.Substring(imgIDLocation[imageCounter]);

                        string embeddedImageTag = string.Format(" {0} alt=\"\" data-oid=\"{1}\" {2}", firstDescriptionPart + embeddedImageOid.Key + "\"", embeddedImageOid.Momentless, newSecondDescriptionPart);
                        //var embeddedImageTag = string.Format("<p></p></br><img src=\"{0}\" alt=\"\" data-oid=\"{1}\" />", "embedded.img/" + embeddedImageOid.Key, embeddedImageOid.Momentless);
                        asset.SetAttributeValue(descAttribute, embeddedImageTag);
                        _dataAPI.Save(asset);

                        UpdateNewAssetOIDAndStatus("EmbeddedImages", sdrEmbeddedImages["AssetOID"].ToString(), embeddedImageOid.Momentless.ToString(), ImportStatuses.IMPORTED, "EmbeddedImage imported.");

                        imageCounter++;
                    }

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
