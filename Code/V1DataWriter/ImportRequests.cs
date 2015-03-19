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
    public class ImportRequests : IImportAssets
    {
        public ImportRequests(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("Request");
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("Requests");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: Orphaned request that has no assigned scope, fail to import.
                    if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    {
                        UpdateImportStatus("Requests", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Request has no scope.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Request");
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

                    IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
                    asset.SetAttributeValue(ownerAttribute, GetNewAssetOIDFromDB(sdr["Owner"].ToString()));

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString()));

                    IAttributeDefinition resolutionAttribute = assetType.GetAttributeDefinition("Resolution");
                    asset.SetAttributeValue(resolutionAttribute, sdr["Resolution"].ToString());

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    IAttributeDefinition requestedByAttribute = assetType.GetAttributeDefinition("RequestedBy");
                    asset.SetAttributeValue(requestedByAttribute, sdr["RequestedBy"].ToString());

                    IAttributeDefinition resolutionReasonAttribute = assetType.GetAttributeDefinition("ResolutionReason");
                    asset.SetAttributeValue(resolutionReasonAttribute, GetNewListTypeAssetOIDFromDB(sdr["ResolutionReason"].ToString()));

                    IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
                    asset.SetAttributeValue(sourceAttribute, GetNewListTypeAssetOIDFromDB(sdr["Source"].ToString()));

                    IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
                    asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB(sdr["Priority"].ToString()));

                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));

                    _dataAPI.Save(asset);
                    string newAssetNumber = GetAssetNumberV1("Request", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("Requests", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("Requests", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Request imported.");
                    importCount++;
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
            while (sdr.Read())
            {
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                ExecuteOperationInV1("Request.Inactivate", asset.Oid);
                assetCount++;
            }
            sdr.Close();
            return assetCount;
        }

    }
}
