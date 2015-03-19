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
    public class ImportActuals : IImportAssets
    {
        public ImportActuals(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations) 
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Actuals");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    if (String.IsNullOrEmpty(sdr["Value"].ToString()) ||
                        String.IsNullOrEmpty(sdr["Date"].ToString()) ||
                        String.IsNullOrEmpty(sdr["Scope"].ToString()) ||
                        String.IsNullOrEmpty(sdr["Member"].ToString()) ||
                        String.IsNullOrEmpty(sdr["Workitem"].ToString()) ||
                        String.IsNullOrEmpty(GetNewAssetOIDFromDB(sdr["Workitem"].ToString())))
                    {
                        UpdateImportStatus("Actuals", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Actual missing required field.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Actual");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition valueAttribute = assetType.GetAttributeDefinition("Value");
                    asset.SetAttributeValue(valueAttribute, sdr["Value"].ToString());

                    IAttributeDefinition dateAttribute = assetType.GetAttributeDefinition("Date");
                    asset.SetAttributeValue(dateAttribute, sdr["Date"].ToString());

                    IAttributeDefinition timeboxAttribute = assetType.GetAttributeDefinition("Timebox");
                    asset.SetAttributeValue(timeboxAttribute, GetNewAssetOIDFromDB(sdr["Timebox"].ToString()));

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString()));

                    IAttributeDefinition memberAttribute = assetType.GetAttributeDefinition("Member");
                    asset.SetAttributeValue(memberAttribute, GetNewAssetOIDFromDB(sdr["Member"].ToString()));

                    IAttributeDefinition workitemAttribute = assetType.GetAttributeDefinition("Workitem");
                    asset.SetAttributeValue(workitemAttribute, GetNewAssetOIDFromDB(sdr["Workitem"].ToString()));

                    IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
                    asset.SetAttributeValue(teamAttribute, GetNewAssetOIDFromDB(sdr["Team"].ToString()));

                    _dataAPI.Save(asset);
                    UpdateNewAssetOIDAndStatus("Actuals", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Actual imported.");
                    importCount++;
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Actuals", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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
