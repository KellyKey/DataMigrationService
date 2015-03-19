using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataWriter
{
    public class ImportAttachments : IImportAssets
    {
        private V1APIConnector _imageConnector;

        public ImportAttachments(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, V1APIConnector ImageConnector, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) 
        {
            _imageConnector = ImageConnector;
        }

        public override int Import()
        {
            //HACK: For Rally import.
            //SqlDataReader sdr = GetImportDataFromSproc("spGetAttachmentsForImport");
            SqlDataReader sdr = GetImportDataFromSproc("spGetAttachmentsForRallyImport");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //HACK: For Rally import.
                    //string newAssetOID = GetNewAssetOIDFromDB(sdr["Asset"].ToString());

                    if (String.IsNullOrEmpty(sdr["Asset"].ToString()) ||
                    String.IsNullOrEmpty(sdr["Content"].ToString()) ||
                    String.IsNullOrEmpty(sdr["ContentType"].ToString()) ||
                    String.IsNullOrEmpty(sdr["Filename"].ToString()) ||
                    String.IsNullOrEmpty(sdr["Name"].ToString()) ||
                    String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()))
                    {
                        UpdateImportStatus("Attachments", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Attachment missing required field.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Attachment");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, sdr["Name"].ToString());

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                    IAttributeDefinition contentTypeAttribute = assetType.GetAttributeDefinition("ContentType");
                    asset.SetAttributeValue(contentTypeAttribute, sdr["ContentType"].ToString());

                    IAttributeDefinition filenameAttribute = assetType.GetAttributeDefinition("Filename");
                    asset.SetAttributeValue(filenameAttribute, sdr["Filename"].ToString());

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));

                    IAttributeDefinition assetAttribute = assetType.GetAttributeDefinition("Asset");
                    asset.SetAttributeValue(assetAttribute, sdr["NewAssetOID"].ToString());

                    IAttributeDefinition contentAttribute = assetType.GetAttributeDefinition("Content");
                    asset.SetAttributeValue(contentAttribute, String.Empty);

                    //Save the attachment to get the new AssetOID.
                    _dataAPI.Save(asset);

                    //Now save the binary content of the attachment.
                    UploadAttachmentContent(asset.Oid.Key.ToString(), (byte[])sdr["Content"], sdr["ContentType"].ToString());
                    
                    UpdateNewAssetOIDAndStatus("Attachments", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Attachment imported.");
                    importCount++;
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        string error = ex.Message.Replace("'", ":");
                        UpdateImportStatus("Attachments", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, error);
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

        private void UploadAttachmentContent(string AssetOID, byte[] FileContent, string FileType)
        {
            Attachments attachment = new Attachments(_imageConnector);

            //Writes the blob in one shot.
            using (Stream output = attachment.GetWriteStream(AssetOID))
            {
                output.Write(FileContent, 0, FileContent.Length);
            }
            attachment.SetWriteStream(AssetOID, FileType);

            //Writes the blob in chunks.
            //int buffersize = 4096;
            //using (MemoryStream input = new MemoryStream(FileContent))
            //{
            //    using (Stream output = attachment.GetWriteStream(AssetOID))
            //    {
            //        byte[] buffer = new byte[input.Length + 1];
            //        for (;;)
            //        {
            //            int read = input.Read(buffer, 0, buffersize);
            //            if (read <= 0)
            //                break;
            //            output.Write(buffer, 0, read);
            //        }
            //    }
            //}
            //attachment.SetWriteStream(AssetOID, FileType);
        }

        public int ImportAttachmentsAsLinks()
        {
            SqlDataReader sdr = GetImportDataFromSproc("spGetAttachmentsForImport");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    string assetOID = GetNewAssetOIDFromDB(sdr["Asset"].ToString());

                    if (String.IsNullOrEmpty(assetOID) == false)
                    {
                        IAssetType assetType = _metaAPI.GetAssetType("Link");
                        Asset asset = _dataAPI.New(assetType, null);

                        //Build the URL for the link.
                        string[] attachmentOID = sdr["AssetOID"].ToString().Split(':');
                        string attachmentURL = _config.V1Configurations.ImportAttachmentsAsLinksURL + "/attachment.img/" + attachmentOID[1];

                        IAttributeDefinition urlAttribute = assetType.GetAttributeDefinition("URL");
                        asset.SetAttributeValue(urlAttribute, attachmentURL);

                        IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
                        asset.SetAttributeValue(nameAttribute, sdr["Name"].ToString());

                        IAttributeDefinition assetAttribute = assetType.GetAttributeDefinition("Asset");
                        asset.SetAttributeValue(assetAttribute, assetOID);

                        _dataAPI.Save(asset);
                        UpdateNewAssetOIDAndStatus("Attachments", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Attachment imported as link.");
                        importCount++;
                    }
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Attachments", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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
