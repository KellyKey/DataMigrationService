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
    public class ImportIterations : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ImportIterations(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("Timebox");
            SqlDataReader sdr = GetImportDataFromDBTable("Iterations");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: Orphaned iteration that has no schedule, fail to import.
                    if (String.IsNullOrEmpty(sdr["Schedule"].ToString()))
                    {
                        UpdateImportStatus("Iterations", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Iteration has no schedule.");
                        continue;
                    }

                    string sprintName = sdr["Name"].ToString();
                    
                    //DUPLICATE CHECK: Check for existing sprint by name (and schedule).
                    string currentAssetOID = CheckForDuplicateIterationByName(sdr["Schedule"].ToString(), sprintName);
                    if (string.IsNullOrEmpty(currentAssetOID) == false)
                    {
                        //UpdateNewAssetOIDAndStatus("Iterations", sdr["AssetOID"].ToString(), currentAssetOID, ImportStatuses.SKIPPED, "Duplicate iteration, matched on name.");
                        //ActivateIteration(sdr["AssetState"].ToString(), currentAssetOID);
                        sprintName = sdr["Schedule"].ToString() + " - " + sdr["Name"].ToString();
                        //continue;
                    }

                    //DUPLICATE CHECK: Check for existing sprint by begin date (and schedule) as the name did not match.
                    currentAssetOID = CheckForDuplicateIterationByDate(sdr["Schedule"].ToString(), sdr["BeginDate"].ToString());
                    if (string.IsNullOrEmpty(currentAssetOID) == false)
                    {
                        UpdateNewAssetOIDAndStatus("Iterations", sdr["AssetOID"].ToString(), currentAssetOID, ImportStatuses.UPDATED, "Duplicate iteration, matched on begin date, name was updated.");
                        ActivateIteration(sdr["AssetState"].ToString(), currentAssetOID);
                        UpdateIterationName(currentAssetOID, sdr["Name"].ToString().Trim());
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Timebox");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, sprintName);

                    if (String.IsNullOrEmpty(customV1IDFieldName) == false)
                    {
                        IAttributeDefinition customV1IDAttribute = assetType.GetAttributeDefinition(customV1IDFieldName);
                        asset.SetAttributeValue(customV1IDAttribute, sdr["AssetOID"].ToString());
                    }

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    asset.SetAttributeValue(descAttribute, sdr["Description"].ToString().Trim());

                    IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
                    //if the original Owner is not available, assign to the Admin Member 20
                    string result = GetNewAssetOIDFromDB(sdr["Owner"].ToString(), "Members");
                    if (string.IsNullOrEmpty(result) == false)
                    {
                        asset.SetAttributeValue(ownerAttribute, result);
                    }
                    else
                    {
                        asset.SetAttributeValue(ownerAttribute, "Member:20");

                    }

                    IAttributeDefinition scheduleAttribute = assetType.GetAttributeDefinition("Schedule");
                    asset.SetAttributeValue(scheduleAttribute, GetNewAssetOIDFromDB(sdr["Schedule"].ToString(), "Schedules"));

                    IAttributeDefinition beginDateAttribute = assetType.GetAttributeDefinition("BeginDate");
                    asset.SetAttributeValue(beginDateAttribute, sdr["BeginDate"].ToString());

                    IAttributeDefinition endDateAttribute = assetType.GetAttributeDefinition("EndDate");
                    asset.SetAttributeValue(endDateAttribute, sdr["EndDate"].ToString());

                    if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    {
                        IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");

                        AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());

                    }

                    IAttributeDefinition targetEstimateAttribute = assetType.GetAttributeDefinition("TargetEstimate");
                    asset.SetAttributeValue(targetEstimateAttribute, sdr["TargetEstimate"].ToString());

                    //NOTE: Initally import all as "Active" state, will set future state after save.
                    IAttributeDefinition stateAttribute = assetType.GetAttributeDefinition("State");
                    asset.SetAttributeValue(stateAttribute, "State:101");

                    _dataAPI.Save(asset);

                    if (sdr["AssetState"].ToString() == "Future")
                    {
                        ExecuteOperationInV1("Timebox.MakeFuture", asset.Oid);
                    }

                    UpdateNewAssetOIDAndStatus("Iterations", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Iteration imported.");
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
                        UpdateImportStatus("Iterations", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Iteration failed to import.");
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

        //Reactivate the iteration, will be closed again when CloseIterations is called.
        private void ActivateIteration(string AssetState, string NewAssetOID)
        {
            Asset asset = GetAssetFromV1(NewAssetOID);
            ExecuteOperationInV1("Timebox.Activate", asset.Oid);
        }

        private void UpdateIterationName(string AssetOID, string Name)
        {
            Asset asset = GetAssetFromV1(AssetOID);
            IAssetType assetType = _metaAPI.GetAssetType("Timebox");
            IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
            asset.SetAttributeValue(fullNameAttribute, Name);
            _dataAPI.Save(asset);
        }

        public int CloseIterations()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Iterations");
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

                    ExecuteOperationInV1("Timebox.Inactivate", asset.Oid);
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

        public int OpenIterations()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Iterations");
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

                    ExecuteOperationInV1("Timebox.Reactivate", asset.Oid);
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
