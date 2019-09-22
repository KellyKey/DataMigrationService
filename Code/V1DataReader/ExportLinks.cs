using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataReader
{
    public class ExportLinks : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportLinks(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Link");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition onMenuAttribute = assetType.GetAttributeDefinition("OnMenu");
            query.Selection.Add(onMenuAttribute);

            IAttributeDefinition urlAttribute = assetType.GetAttributeDefinition("URL");
            query.Selection.Add(urlAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition assetAttribute = assetType.GetAttributeDefinition("Asset");
            query.Selection.Add(assetAttribute);

            string SQL = BuildLinkInsertStatement();

            //int assetCounter = 11820;
            //int skippedCounter = 446902;
            //int totalCounter = 458722;
            int assetCounter = 0;
            int skippedCounter = 0;
            int totalCounter = 0;
            int assetTotal = 0;

            if (_config.V1Configurations.PageSize != 0)
            {
                query.Paging.Start = totalCounter;
                query.Paging.PageSize = _config.V1Configurations.PageSize;
            }


            do
            {
                QueryResult result = _dataAPI.Retrieve(query);
                assetTotal = result.TotalAvaliable;

                foreach (Asset asset in result.Assets)
                {

                    SqlDataReader sdr = GetDataFromDB(asset.GetAttribute(assetAttribute).Value.ToString());
                    if (sdr == null)
                    {
                        skippedCounter++;
                        _logger.Info("Link: Skipped - Count = {0}", skippedCounter);
                        continue;
                    }

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        //NAME NPI MASK:
                        object name = GetScalerValue(asset.GetAttribute(nameAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && name != DBNull.Value)
                        {
                            name = ExportUtils.RemoveNPI(name.ToString());
                        }

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@OnMenu", GetScalerValue(asset.GetAttribute(onMenuAttribute)));
                        cmd.Parameters.AddWithValue("@URL", GetScalerValue(asset.GetAttribute(urlAttribute)));
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Asset", GetSingleRelationValue(asset.GetAttribute(assetAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Link: added - Count = {0}", assetCounter);
                }
                totalCounter = assetCounter + skippedCounter;
                query.Paging.Start = totalCounter;
            } while (totalCounter != assetTotal);
            return assetCounter;
        }

        private string BuildLinkInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO LINKS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("OnMenu,");
            sb.Append("URL,");
            sb.Append("Name,");
            sb.Append("Asset) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@OnMenu,");
            sb.Append("@URL,");
            sb.Append("@Name,");
            sb.Append("@Asset);");
            return sb.ToString();
        }

    }
}
