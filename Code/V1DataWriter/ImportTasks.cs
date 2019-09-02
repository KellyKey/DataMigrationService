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
    public class ImportTasks : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();      
        
        public ImportTasks(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("Task");

            //SqlDataReader sdr = GetImportDataFromSproc("spGetTasksForImport");
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("Tasks");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //CHECK DATA: Task must have a name.
                    //if (String.IsNullOrEmpty(sdr["Name"].ToString()))
                    //{
                    //    UpdateImportStatus("Tasks", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Task name attribute is required.");
                    //    continue;
                    //}

                    ////CHECK DATA: Task must have a parent.
                    //if (String.IsNullOrEmpty(sdr["Parent"].ToString()))
                    //{
                    //    UpdateImportStatus("Tasks", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Task parent attribute is required.");
                    //    continue;
                    //}

                    IAssetType assetType = _metaAPI.GetAssetType("Task");
                    //Asset asset = _dataAPI.New(assetType, null);

                    string newAssetOid = GetNewAssetOIDFromDB(sdr["AssetOID"].ToString(), "Task");
                    Asset asset = GetAssetFromV1(newAssetOid);

                    //if (String.IsNullOrEmpty(customV1IDFieldName) == false)
                    //{
                    //    IAttributeDefinition customV1IDAttribute = assetType.GetAttributeDefinition(customV1IDFieldName);
                    //    asset.SetAttributeValue(customV1IDAttribute, sdr["AssetNumber"].ToString());
                    //}

                    //IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    //asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    //asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());
                    string targetURL = _config.V1TargetConnection.Url;
                    string newDescription = SetEmbeddedImageContent(sdr["Description"].ToString(), targetURL);
                    asset.SetAttributeValue(descAttribute, newDescription);

                    //if (String.IsNullOrEmpty(sdr["Customer"].ToString()) == false)
                    //{
                    //    IAttributeDefinition customerAttribute = assetType.GetAttributeDefinition("Customer");
                    //    asset.SetAttributeValue(customerAttribute, GetNewAssetOIDFromDB(sdr["Customer"].ToString()));
                    //}

                    //if (String.IsNullOrEmpty(sdr["Owners"].ToString()) == false)
                    //{
                    //    AddMultiValueRelation(assetType, asset, "Members", "Owners", sdr["Owners"].ToString());
                    //}

                    //if (String.IsNullOrEmpty(sdr["Goals"].ToString()) == false)
                    //{
                    //    AddMultiValueRelation(assetType, asset, "Goals", sdr["Goals"].ToString());
                    //}

                    //IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    //asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    //IAttributeDefinition detailEstimateAttribute = assetType.GetAttributeDefinition("DetailEstimate");
                    //asset.SetAttributeValue(detailEstimateAttribute, sdr["DetailEstimate"].ToString());

                    //IAttributeDefinition toDoAttribute = assetType.GetAttributeDefinition("ToDo");
                    //asset.SetAttributeValue(toDoAttribute, sdr["ToDo"].ToString());

                    //IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
                    //asset.SetAttributeValue(estimateAttribute, sdr["Estimate"].ToString());

                    //IAttributeDefinition lastVersionAttribute = assetType.GetAttributeDefinition("LastVersion");
                    //asset.SetAttributeValue(lastVersionAttribute, sdr["LastVersion"].ToString());

                    //if (String.IsNullOrEmpty(sdr["Category"].ToString()) == false)
                    //{
                    //    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    //    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));
                    //}

                    //if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    //{
                    //    IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");

                    //    AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());

                    //}

                    //IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
                    //asset.SetAttributeValue(sourceAttribute, GetNewListTypeAssetOIDFromDB(sdr["Source"].ToString()));

                    //if (String.IsNullOrEmpty(sdr["Status"].ToString()) == false)
                    //{
                    //    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    //    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    //    //HACK: For Rally import, needs to be refactored.
                    //    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("TaskStatus", sdr["Status"].ToString()));
                    //}

                    ////HACK: For Rally import, needs to be refactored.
                    //IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
                    //if (String.IsNullOrEmpty(sdr["ParentType"].ToString()) == true)
                    //    asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(sdr["Parent"].ToString()));
                    //else
                    //{
                    //    string newAssetOID = null;
                    //    if (sdr["ParentType"].ToString() == "Story")
                    //    {
                    //        newAssetOID = GetNewAssetOIDFromDB(sdr["Parent"].ToString(), "Stories");
                    //        if (String.IsNullOrEmpty(newAssetOID) == false)
                    //            asset.SetAttributeValue(parentAttribute, newAssetOID);
                    //        else
                    //        {
                    //            newAssetOID = GetNewAssetOIDFromDB(sdr["Parent"].ToString(), "Epics");
                    //            if (String.IsNullOrEmpty(newAssetOID) == false)
                    //                asset.SetAttributeValue(parentAttribute, newAssetOID);
                    //            else
                    //                throw new Exception("Import failed. Parent could not be found.");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        newAssetOID = GetNewAssetOIDFromDB(sdr["Parent"].ToString(), "Defects");
                    //        if (String.IsNullOrEmpty(newAssetOID) == false)
                    //            asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(sdr["Parent"].ToString(), "Defects"));
                    //        else
                    //            throw new Exception("Import failed. Parent defect could not be found.");
                    //    }
                    //}

                    _dataAPI.Save(asset);
                    string newAssetNumber = GetAssetNumberV1("Task", asset.Oid.Momentless.ToString());
                    UpdateAssetRecordWithNumber("Tasks", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber, ImportStatuses.IMPORTED, "Task imported.");
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

                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);

                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        string error = ex.Message.Replace("'", ":");
                        UpdateImportStatus("Tasks", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, error);
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

        private string GetStatusAssetOID(string storyStatus)
        {
            if (storyStatus == "In-Progress" && storyStatus == "In Development" && storyStatus == "Development" && storyStatus == "In Code Review" && storyStatus == "To Be Approved"
                && storyStatus == "Ready for Test" && storyStatus == "In Test" && storyStatus == "In Testing")
            {
                //StoryStatus:InProgress StoryStatus:134

                return "StoryStatus:134";
            }
            else if (storyStatus == "Deferred" && storyStatus == "Backlog" && storyStatus == "To Do" && storyStatus == "Open" && storyStatus == "Blocked")
            {
                //StoryStatus:Reopened StoryStatus:9192
                //StoryStatus:Open StoryStatus:9191
                return "StoryStatus:9191";
            }
            else if (storyStatus == "Completed" && storyStatus == "Accepted" && storyStatus == "READY TO DEPLOY")
            {
                //StoryStatus:Completed StoryStatus:9193
                return "StoryStatus:9193";
            }
            else if (storyStatus == "Accepted" && storyStatus == "READY TO DEPLOY")
            {
                //StoryStatus:Resolved StoryStatus:9194
                return "StoryStatus:9194";
            }
            else if (storyStatus == "Deployed")
            {
                //StoryStatus:Closed StoryStatus:9195
                return "StoryStatus:9195";
            }
            else
            {
                return "StoryStatus:9191";
            }
        }

        public int CloseTasks()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Tasks");
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

                    ExecuteOperationInV1("Task.Inactivate", asset.Oid);
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

        public int OpenTasks()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Tasks");
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

                    ExecuteOperationInV1("Task.Reactivate", asset.Oid);
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
