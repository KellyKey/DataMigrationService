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
    public class ImportRequests : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportRequests(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            //string customV1IDFieldName = GetV1IDCustomFieldName("Request");
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("Requests");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: Orphaned request that has no assigned scope, fail to import.
                    //if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    //{
                    //    UpdateImportStatus("Requests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Request has no scope.");
                    //    continue;
                    //}

                IAssetType assetType = _metaAPI.GetAssetType("Request");
                //Asset asset = _dataAPI.New(assetType, null);

                string newAssetOid = GetNewAssetOIDFromDB(sdr["AssetOID"].ToString(), "Request");
                Asset asset = GetAssetFromV1(newAssetOid);

                //if (String.IsNullOrEmpty(customV1IDFieldName) == false)
                //{
                //    IAttributeDefinition customV1IDAttribute = assetType.GetAttributeDefinition(customV1IDFieldName);
                //    asset.SetAttributeValue(customV1IDAttribute, sdr["AssetNumber"].ToString());
                //}

                //IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                //    asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    //asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());
                    string targetURL = _config.V1TargetConnection.Url;
                    string newDescription = SetEmbeddedImageContent(sdr["Description"].ToString(), targetURL);
                    asset.SetAttributeValue(descAttribute, newDescription);

                    //IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
                    //asset.SetAttributeValue(ownerAttribute, GetNewAssetOIDFromDB(sdr["Owner"].ToString()));

                    //IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    //asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString()));

                    //IAttributeDefinition resolutionAttribute = assetType.GetAttributeDefinition("Resolution");
                    //asset.SetAttributeValue(resolutionAttribute, sdr["Resolution"].ToString());

                    //IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    //asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    //IAttributeDefinition requestedByAttribute = assetType.GetAttributeDefinition("RequestedBy");
                    //asset.SetAttributeValue(requestedByAttribute, sdr["RequestedBy"].ToString());

                    //IAttributeDefinition resolutionReasonAttribute = assetType.GetAttributeDefinition("ResolutionReason");
                    //asset.SetAttributeValue(resolutionReasonAttribute, GetNewListTypeAssetOIDFromDB(sdr["ResolutionReason"].ToString()));

                    //IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
                    //asset.SetAttributeValue(sourceAttribute, GetNewListTypeAssetOIDFromDB(sdr["Source"].ToString()));

                    //IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
                    //asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB(sdr["Priority"].ToString()));

                    //IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));

                    //if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    //{
                    //    IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");

                    //    AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());

                    //}

                    //IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    //asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));

                    _dataAPI.Save(asset);

                    string newAssetNumber = GetAssetNumberV1("Request", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("Requests", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("Requests", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Request imported.");
                    importCount++;

                    //Get Any EmbeddedImages for the Description Field
                    //int[] imgIDLocation = new int[10];
                    //int imageCounter = 0;

                    //SqlDataReader sdrEmbeddedImages = GetDataFromDB("EmbeddedImages", sdr["AssetOID"].ToString());
                    //while (sdrEmbeddedImages.Read())
                    //{
                    //    //UploadEmbeddedImageContent(ref asset, asset.Oid,(byte[])sdrEmbeddedImages["Content"], _imageConnector);

                    //    ////Create an embedded image.
                    //    string filePath = @"C:\Windows\Temp\TempImage.png";
                    //    System.IO.File.WriteAllBytes(filePath, (byte[])sdrEmbeddedImages["Content"]);
                    //    //string file = @"C:\Temp\versionone.jpg";
                    //    Oid embeddedImageOid = _dataAPI.SaveEmbeddedImage(filePath, asset);

                    //    string currentDescription = sdr["Description"].ToString();
                    //    imgIDLocation[imageCounter] = currentDescription.IndexOf(".img/", 0, currentDescription.Length);
                    //    string firstDescriptionPart = currentDescription.Substring(0, imgIDLocation[imageCounter] + 5);
                    //    string secondDescriptionPart = currentDescription.Substring(imgIDLocation[imageCounter] + 5);
                    //    imgIDLocation[imageCounter] = secondDescriptionPart.IndexOf(">", 0, secondDescriptionPart.Length);
                    //    string newSecondDescriptionPart = secondDescriptionPart.Substring(imgIDLocation[imageCounter]);

                    //    string embeddedImageTag = string.Format(" {0} alt=\"\" data-oid=\"{1}\" {2}", firstDescriptionPart + embeddedImageOid.Key + "\"", embeddedImageOid.Momentless, newSecondDescriptionPart);
                    //    //var embeddedImageTag = string.Format("<p></p></br><img src=\"{0}\" alt=\"\" data-oid=\"{1}\" />", "embedded.img/" + embeddedImageOid.Key, embeddedImageOid.Momentless);
                    //    asset.SetAttributeValue(descAttribute, embeddedImageTag);
                    //    _dataAPI.Save(asset);

                    //    UpdateNewAssetOIDAndStatus("EmbeddedImages", sdrEmbeddedImages["AssetOID"].ToString(), embeddedImageOid.Momentless.ToString(), ImportStatuses.IMPORTED, "EmbeddedImage imported.");

                    //    imageCounter++;
                    //}

                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Requests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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

        public int CloseRequests()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Requests");
            int assetCount = 0;
            int exceptionCount = 0;
            while (sdr.Read())
            {
                Asset asset = null;

                try
                {
                    if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    {
                        asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                    }
                    else
                    {
                        continue;
                    }

                    ExecuteOperationInV1("Request.Inactivate", asset.Oid);
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

        public int OpenRequests()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Requests");
            int assetCount = 0;
            int exceptionCount = 0;
            while (sdr.Read())
            {
                Asset asset = null;

                try
                {
                    if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    {
                        asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                    }
                    else
                    {
                        continue;
                    }

                    ExecuteOperationInV1("Request.Reactivate", asset.Oid);
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
