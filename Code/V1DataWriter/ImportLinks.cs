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
    public class ImportLinks : IImportAssets
    {
        public ImportLinks(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromSproc("spGetLinksForImport");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: Link must have an associated asset.
                    if (String.IsNullOrEmpty(sdr["Asset"].ToString()))
                    {
                        UpdateImportStatus("Links", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Link has no associated asset.");
                        continue;
                    }

                    //SPECIAL CASE: Link assocated asset must exist in target system.
                    string newAssetOID = GetNewAssetOIDFromDB(sdr["Asset"].ToString());
                    if (String.IsNullOrEmpty(newAssetOID))
                    {
                        UpdateImportStatus("Links", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Link associated asset does not exist in target.");
                        continue;
                    }
                    
                    IAssetType assetType = _metaAPI.GetAssetType("Link");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition onMenuAttribute = assetType.GetAttributeDefinition("OnMenu");
                    asset.SetAttributeValue(onMenuAttribute, sdr["OnMenu"].ToString());

                    IAttributeDefinition urlAttribute = assetType.GetAttributeDefinition("URL");
                    asset.SetAttributeValue(urlAttribute, sdr["URL"].ToString());

                    IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(nameAttribute, sdr["Name"].ToString());

                    IAttributeDefinition assetAttribute = assetType.GetAttributeDefinition("Asset");
                    asset.SetAttributeValue(assetAttribute, newAssetOID);

                    _dataAPI.Save(asset);
                    UpdateNewAssetOIDAndStatus("Links", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Link imported.");
                    importCount++;
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Links", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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
