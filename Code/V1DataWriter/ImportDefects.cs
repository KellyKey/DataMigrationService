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
    public class ImportDefects : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ImportDefects(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("Defect");
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("Defects");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: No assigned scope, fail to import.
                    if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    {
                        UpdateImportStatus("Defects", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Defect has no scope.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Defect");
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

                    IAttributeDefinition iterationAttribute = assetType.GetAttributeDefinition("Timebox");
                    asset.SetAttributeValue(iterationAttribute, GetNewAssetOIDFromDB(sdr["Timebox"].ToString(), "Timebox"));

                    IAttributeDefinition verifiedByAttribute = assetType.GetAttributeDefinition("VerifiedBy");
                    asset.SetAttributeValue(verifiedByAttribute, GetNewAssetOIDFromDB(sdr["VerifiedBy"].ToString(), "Members"));

                    if (String.IsNullOrEmpty(sdr["Owners"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Members", "Owners", sdr["Owners"].ToString());
                    }

                    IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
                    asset.SetAttributeValue(teamAttribute, GetNewAssetOIDFromDB(sdr["Team"].ToString()));

                    //TO DO: Versions (VersionLabel)???

                    if (String.IsNullOrEmpty(sdr["Goals"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Goals", sdr["Goals"].ToString());
                    }

                    IAttributeDefinition superAttribute = assetType.GetAttributeDefinition("Super");
                    asset.SetAttributeValue(superAttribute, GetNewAssetOIDFromDB(sdr["Super"].ToString()));

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    IAttributeDefinition detailEstimateAttribute = assetType.GetAttributeDefinition("DetailEstimate");
                    asset.SetAttributeValue(detailEstimateAttribute, sdr["DetailEstimate"].ToString());

                    IAttributeDefinition toDoAttribute = assetType.GetAttributeDefinition("ToDo");
                    asset.SetAttributeValue(toDoAttribute, sdr["ToDo"].ToString());

                    IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
                    asset.SetAttributeValue(estimateAttribute, sdr["Estimate"].ToString());

                    IAttributeDefinition environmentAttribute = assetType.GetAttributeDefinition("Environment");
                    asset.SetAttributeValue(environmentAttribute, sdr["Environment"].ToString());

                    IAttributeDefinition resolutionAttribute = assetType.GetAttributeDefinition("Resolution");
                    asset.SetAttributeValue(resolutionAttribute, sdr["Resolution"].ToString());

                    IAttributeDefinition versionAffectedAttribute = assetType.GetAttributeDefinition("VersionAffected");
                    asset.SetAttributeValue(versionAffectedAttribute, sdr["VersionAffected"].ToString());

                    IAttributeDefinition fixedInBuildAttribute = assetType.GetAttributeDefinition("FixedInBuild");
                    asset.SetAttributeValue(fixedInBuildAttribute, sdr["FixedInBuild"].ToString());

                    IAttributeDefinition foundByAttribute = assetType.GetAttributeDefinition("FoundBy");
                    asset.SetAttributeValue(foundByAttribute, sdr["FoundBy"].ToString());

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString(), "Scope"));
                    //asset.SetAttributeValue(scopeAttribute, _config.V1Configurations.p);

                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("StoryStatus", sdr["Status"].ToString()));
                    //asset.SetAttributeValue(statusAttribute, GetStatusAssetOID(sdr["Status"].ToString()));

                    IAttributeDefinition typeAttribute = assetType.GetAttributeDefinition("Type");
                    asset.SetAttributeValue(typeAttribute, GetNewListTypeAssetOIDFromDB(sdr["Type"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(typeAttribute, GetNewListTypeAssetOIDFromDB("DefectType", sdr["Type"].ToString()));

                    IAttributeDefinition resolutionReasonAttribute = assetType.GetAttributeDefinition("ResolutionReason");
                    asset.SetAttributeValue(resolutionReasonAttribute, GetNewListTypeAssetOIDFromDB(sdr["ResolutionReason"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(resolutionReasonAttribute, GetNewListTypeAssetOIDFromDB("DefectResolution", sdr["ResolutionReason"].ToString()));

                    //HACK: For Rally import, needs to be refactored.
                    IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
                    if (String.IsNullOrEmpty(_config.V1Configurations.SourceListTypeValue) == false)
                        asset.SetAttributeValue(sourceAttribute, _config.V1Configurations.SourceListTypeValue);
                    else
                        asset.SetAttributeValue(sourceAttribute, GetNewListTypeAssetOIDFromDB(sdr["Source"].ToString()));

                    IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
                    asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB(sdr["Priority"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB("WorkitemPriority", sdr["Priority"].ToString()));

                    IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
                    asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(sdr["Parent"].ToString()));

                    if (String.IsNullOrEmpty(sdr["Requests"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Requests", sdr["Requests"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["BlockingIssues"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "BlockingIssues", sdr["BlockingIssues"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["Issues"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Issues", sdr["Issues"].ToString());
                    }

                    _dataAPI.Save(asset);

                    if (sdr["AssetState"].ToString() == "Template")
                    {
                        ExecuteOperationInV1("Defect.MakeTemplate", asset.Oid);
                    }

                    string newAssetNumber = GetAssetNumberV1("Defect", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("Defects", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("Defects", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Defect imported.");
                    importCount++;

                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Defects", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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
            SetDefectDependencies();
            return importCount;
        }

        //Updated for Rally Migration
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


        private void SetDefectDependencies()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Defects");
            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Defect");
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());

                IAttributeDefinition duplicateOfAttribute = assetType.GetAttributeDefinition("DuplicateOf");
                asset.SetAttributeValue(duplicateOfAttribute, GetNewAssetOIDFromDB(sdr["DuplicateOf"].ToString()));

                if (String.IsNullOrEmpty(sdr["AffectedPrimaryWorkitems"].ToString()) == false)
                {
                    IAttributeDefinition affectedPrimaryWorkitemsAttribute = assetType.GetAttributeDefinition("AffectedPrimaryWorkitems");
                    string[] assetList = sdr["AffectedPrimaryWorkitems"].ToString().Split(';');
                    foreach (string item in assetList)
                    {
                        string assetOID = GetNewAssetOIDFromDB(item, "Stories");
                        if (String.IsNullOrEmpty(assetOID) == false)
                            asset.AddAttributeValue(affectedPrimaryWorkitemsAttribute, assetOID);
                    }
                }

                if (String.IsNullOrEmpty(sdr["AffectedByDefects"].ToString()) == false)
                {
                    AddMultiValueRelation(assetType, asset, "Defects", "AffectedByDefects", sdr["AffectedByDefects"].ToString());
                }
                _dataAPI.Save(asset);
            }
            sdr.Close();
        }

        public int CloseDefects()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Defects");
            int assetCount = 0;
            while (sdr.Read())
            {
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                ExecuteOperationInV1("Defect.Inactivate", asset.Oid);
                assetCount++;
            }
            sdr.Close();
            return assetCount;
        }

    }
}
