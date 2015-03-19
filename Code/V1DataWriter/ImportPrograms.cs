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
    public class ImportPrograms : IImportAssets
    {
        public ImportPrograms(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Programs");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    string currentAssetOID = CheckForDuplicateInV1("ScopeLabel", "Name", sdr["Name"].ToString());

                    if (string.IsNullOrEmpty(currentAssetOID) == false)
                    {
                        UpdateNewAssetOIDAndStatus("Programs", sdr["AssetOID"].ToString(), currentAssetOID, ImportStatuses.SKIPPED, "Duplicate program.");
                        continue;
                    }
                    else
                    {
                        IAssetType assetType = _metaAPI.GetAssetType("ScopeLabel");
                        Asset asset = _dataAPI.New(assetType, null);

                        IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                        asset.SetAttributeValue(fullNameAttribute, sdr["Name"].ToString());

                        IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                        asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                        if (String.IsNullOrEmpty(sdr["Scopes"].ToString()) == false)
                        {
                            AddMultiValueRelation(assetType, asset, "Scopes", sdr["Scopes"].ToString());
                        }

                        _dataAPI.Save(asset);
                        UpdateNewAssetOIDAndStatus("Programs", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Program imported.");
                        importCount++;
                    }
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Programs", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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
