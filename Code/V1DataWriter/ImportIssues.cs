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
    public class ImportIssues : IImportAssets
    {
        public ImportIssues(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("Issue");
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("Issues");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: Orphaned issue that has no assigned scope, fail to import.
                    if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    {
                        UpdateImportStatus("Issues", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Issue has no scope.");
                        continue;
                    }

                    //SPECIAL CASE: Issue already has been Imported.
                    if (sdr["ImportStatus"].ToString().Equals("IMPORTED"))
                    {
                        //UpdateImportStatus("Issues", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Issue has no scope.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Issue");
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

                    //TO DO: Enable restrospective.
                    //IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
                    //asset.SetAttributeValue(ownerAttribute, GetNewAssetOIDFromDB("Members", sdr["Owner"].ToString()));

                    IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
                    asset.SetAttributeValue(teamAttribute, GetNewAssetOIDFromDB(sdr["Team"].ToString()));

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString()));

                    IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
                    asset.SetAttributeValue(ownerAttribute, GetNewAssetOIDFromDB(sdr["Owner"].ToString()));

                    IAttributeDefinition identifiedByAttribute = assetType.GetAttributeDefinition("IdentifiedBy");
                    asset.SetAttributeValue(identifiedByAttribute, sdr["IdentifiedBy"].ToString());

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    IAttributeDefinition targetDateAttribute = assetType.GetAttributeDefinition("TargetDate");
                    asset.SetAttributeValue(targetDateAttribute, sdr["TargetDate"].ToString());

                    IAttributeDefinition resolutionAttribute = assetType.GetAttributeDefinition("Resolution");
                    asset.SetAttributeValue(resolutionAttribute, sdr["Resolution"].ToString());

                    IAttributeDefinition resolutionReasonAttribute = assetType.GetAttributeDefinition("ResolutionReason");
                    asset.SetAttributeValue(resolutionReasonAttribute, GetNewListTypeAssetOIDFromDB(sdr["ResolutionReason"].ToString()));

                    IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
                    asset.SetAttributeValue(sourceAttribute, GetNewListTypeAssetOIDFromDB(sdr["Source"].ToString()));

                    IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
                    asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB(sdr["Priority"].ToString()));

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));

                    if (String.IsNullOrEmpty(sdr["Requests"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Requests", sdr["Requests"].ToString());
                    }

                    _dataAPI.Save(asset);
                    string newAssetNumber = GetAssetNumberV1("Issue", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("Issues", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("Issues", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Issue imported.");
                    importCount++;
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Issues", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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

        public int CloseIssues()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Issues");
            int assetCount = 0;
            while (sdr.Read())
            {
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                ExecuteOperationInV1("Issue.Inactivate", asset.Oid);
                assetCount++;
            }
            sdr.Close();
            return assetCount;
        }

    }
}
