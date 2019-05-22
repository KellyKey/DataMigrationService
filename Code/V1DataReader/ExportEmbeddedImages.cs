﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataReader
{
    public class ExportEmbeddedImages : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private V1Connector _imageConnector;

        public ExportEmbeddedImages(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, V1Connector ImageConnector, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations)
        {
            _imageConnector = ImageConnector;
        }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("EmbeddedImage");
            Query query = new Query(assetType);

            IAttributeDefinition assetAttribute = assetType.GetAttributeDefinition("Asset");
            query.Selection.Add(assetAttribute);

            IAttributeDefinition contentAttribute = assetType.GetAttributeDefinition("Content");
            query.Selection.Add(contentAttribute);

            IAttributeDefinition contentTypeAttribute = assetType.GetAttributeDefinition("ContentType");
            query.Selection.Add(contentTypeAttribute);

            string SQL = BuildEmbeddedImageInsertStatement();

            if (_config.V1Configurations.PageSize != 0)
            {
                query.Paging.Start = 0;
                query.Paging.PageSize = _config.V1Configurations.PageSize;
            }

            int assetCounter = 0;
            int assetTotal = 0;

            do
            {
                QueryResult result = _dataAPI.Retrieve(query);
                assetTotal = result.TotalAvaliable;

                foreach (Asset asset in result.Assets)
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@Asset", GetSingleRelationValue(asset.GetAttribute(assetAttribute)));
                        cmd.Parameters.AddWithValue("@Content", GetEmbeddedImageContent(asset.Oid));
                        cmd.Parameters.AddWithValue("@ContentType", GetScalerValue(asset.GetAttribute(contentTypeAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Asset: " + asset.Oid.ToString() + " Added - Count: " + assetCounter);
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private byte[] GetEmbeddedImageContent(Oid embeddedImageID)
        {
            MemoryStream memoryStream = new MemoryStream();            
            Services embeddedImage = new Services(_imageConnector);
            using (Stream blob = embeddedImage.GetEmbeddedImage(embeddedImageID))
            {
                blob.CopyTo(memoryStream);
            }
            return memoryStream.ToArray();
        }

        private string BuildEmbeddedImageInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO EMBEDDEDIMAGES(");
            sb.Append("AssetOID,");
            sb.Append("Asset,");
            sb.Append("Content,");
            sb.Append("ContentType)");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@Asset,");
            sb.Append("@Content,");
            sb.Append("@ContentType);");
            return sb.ToString();
        }

    }
}
