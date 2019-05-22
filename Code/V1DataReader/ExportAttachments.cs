using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataReader
{
    public class ExportAttachments : IExportAssets
    {
        private V1Connector _imageConnector;

        public ExportAttachments(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, V1Connector ImageConnector, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) 
        {
            _imageConnector = ImageConnector;
        }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Attachment");
            Query query = new Query(assetType);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition contentAttribute = assetType.GetAttributeDefinition("Content");
            query.Selection.Add(contentAttribute);

            IAttributeDefinition contentTypeAttribute = assetType.GetAttributeDefinition("ContentType");
            query.Selection.Add(contentTypeAttribute);

            IAttributeDefinition fileNameAttribute = assetType.GetAttributeDefinition("Filename");
            query.Selection.Add(fileNameAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
            query.Selection.Add(categoryAttribute);

            IAttributeDefinition assetAttribute = assetType.GetAttributeDefinition("Asset");
            query.Selection.Add(assetAttribute);

            string SQL = BuildAttachmentInsertStatement();

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
                        //NAME NPI MASK:
                        object name = GetScalerValue(asset.GetAttribute(nameAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && name != DBNull.Value)
                        {
                            name = ExportUtils.RemoveNPI(name.ToString());
                        }

                        //DESCRIPTION NPI MASK:
                        object description = GetScalerValue(asset.GetAttribute(descriptionAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && description != DBNull.Value)
                        {
                            description = ExportUtils.RemoveNPI(description.ToString());
                        }

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Content", GetAttachmentValue(asset.Oid));
                        cmd.Parameters.AddWithValue("@ContentType", GetScalerValue(asset.GetAttribute(contentTypeAttribute)));
                        cmd.Parameters.AddWithValue("@FileName", GetScalerValue(asset.GetAttribute(fileNameAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Category", GetSingleRelationValue(asset.GetAttribute(categoryAttribute)));
                        cmd.Parameters.AddWithValue("@Asset", GetSingleRelationValue(asset.GetAttribute(assetAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private byte[] GetAttachmentValue(Oid attachmentID)
        {
            MemoryStream memoryStream = new MemoryStream();
            if (_config.V1Configurations.MigrateAttachmentBinaries == true)
            {
                //Attachments attachment = new Attachments(_imageConnector);
                
                //using (Stream blob = attachment.GetReadStream(AttachmentID))
                Services attachmentService = new Services(_imageConnector);
                //Oid attachmentID = new Oid(attachmentType, Convert.ToInt64(AttachmentID));
                using (Stream blob = attachmentService.GetAttachment(attachmentID))
                {
                    blob.CopyTo(memoryStream);
                }
            }
            return memoryStream.ToArray();
        }

        private string BuildAttachmentInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO ATTACHMENTS (");
            sb.Append("AssetOID,");
            sb.Append("Name,");
            sb.Append("Content,");
            sb.Append("ContentType,");
            sb.Append("FileName,");
            sb.Append("Description,");
            sb.Append("Category,");
            sb.Append("Asset) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@Name,");
            sb.Append("@Content,");
            sb.Append("@ContentType,");
            sb.Append("@FileName,");
            sb.Append("@Description,");
            sb.Append("@Category,");
            sb.Append("@Asset);");
            return sb.ToString();
        }

    }
}
