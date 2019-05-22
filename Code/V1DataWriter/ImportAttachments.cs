using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataWriter
{
    public class ImportAttachments : IImportAssets
    {
        private V1Connector _imageConnector;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportAttachments(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, V1Connector ImageConnector, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) 
        {
            _imageConnector = ImageConnector;
        }

        public override int Import()
        {
            //Test Method
            //createAttachment();
            //return 1000;

            //HACK: For Rally import.
            //SqlDataReader sdr = GetImportDataFromSproc("spGetAttachmentsForRallyImport");
            SqlDataReader sdr = GetImportDataFromSproc("spGetAttachmentsForImport");

            int importCount = 0;
            int failedCount = 0;
            _logger.Info("-> Database Count is {0} Attachments", sdr.RecordsAffected);

            while (sdr.Read())
            {

                try
                {
                    //HACK: For Rally import.
                    string newAssetOID = GetNewAssetOIDFromDB(sdr["Asset"].ToString());

                    //if (String.IsNullOrEmpty(sdr["Asset"].ToString()) ||
                    //String.IsNullOrEmpty(sdr["Content"].ToString()) ||
                    //String.IsNullOrEmpty(sdr["ContentType"].ToString()) ||
                    //String.IsNullOrEmpty(sdr["Filename"].ToString()) ||
                    //String.IsNullOrEmpty(sdr["Name"].ToString()) ||
                    ////String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()))
                    //String.IsNullOrEmpty(newAssetOID))
                    //{
                    //    UpdateImportStatus("Attachments", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Attachment missing required field.");
                    //    continue;
                    //}

                    IAssetType assetType = _metaAPI.GetAssetType("Attachment");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, sdr["Name"].ToString());

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                    IAttributeDefinition contentTypeAttribute = assetType.GetAttributeDefinition("ContentType");
                    string mimeType = MimeType.Resolve(sdr["ContentType"].ToString());
                    asset.SetAttributeValue(contentTypeAttribute, mimeType);

                    IAttributeDefinition filenameAttribute = assetType.GetAttributeDefinition("Filename");
                    asset.SetAttributeValue(filenameAttribute, sdr["Filename"].ToString());

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));

                    IAttributeDefinition assetAttribute = assetType.GetAttributeDefinition("Asset");
                    asset.SetAttributeValue(assetAttribute, newAssetOID);

                    IAttributeDefinition contentAttribute = assetType.GetAttributeDefinition("Content");
                    asset.SetAttributeValue(contentAttribute, String.Empty);

                    Services services = new Services(_imageConnector);
                    _logger.Info("-> Meta is {0} ", ((MetaModel)services.Meta).Version);

                    //Oid assetId = services.GetOid(sdr["NewAssetOID"].ToString());

                    Asset assetContainer = GetAssetFromV1(newAssetOID);
                    //Asset assetContainer = GetWorkitem(_imageConnector, sdr["NewAssetOID"].ToString());

                    string filePath = createFile(sdr["Filename"].ToString(), (byte[])sdr["Content"]);

                    try
                    {
                        bool success = AttachFile(_imageConnector, assetContainer, filePath);
                    }
                    finally
                    {
                        File.Delete(filePath);
                    }

                    //Oid assetId = services.GetOid("Story:5814");
                    //Query query = new Query(assetId);

                    //QueryResult result = services.Retrieve(query);
                    //Asset assetContainer = result.Assets[0];

                    //Save the attachment to get the new AssetOID.
                    _dataAPI.Save(asset);

                    //Now save the binary content of the attachment.
                    //UploadAttachmentContent(asset.Oid.Key.ToString(), (byte[])sdr["Content"], sdr["ContentType"].ToString());
                    
                    
                    //UploadAttachmentContent(assetContainer, (byte[])sdr["Content"], sdr["Filename"].ToString(), sdr["Name"].ToString());
                    //UploadAttachmentContent(asset, (byte[])sdr["Content"], sdr["Filename"].ToString(), sdr["Name"].ToString());

                    UpdateNewAssetOIDAndStatus("Attachments", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Attachment imported.");
                    importCount++;
                    _logger.Info("-> Imported {0} Attachments", importCount);
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        string error = ex.Message.Replace("'", ":");
                        UpdateImportStatus("Attachments", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, error);
                        failedCount++;
                        _logger.Info("-> Failed {0} Attachment.", failedCount);
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

        //private void UploadAttachmentContent(string AssetOID, byte[] FileContent, string FileType)
        private void UploadAttachmentContent(Asset newAsset, byte[] FileContent, string fileName, string attachmentName)
        {

            string filePath = @"C:\Temp\" + fileName;
            
            try
            {
                //Process the File Bytes
                //int buffersize = 4096;
                //File.Create(filePath, buffersize);
                File.WriteAllBytes(filePath, FileContent);

                Services services = new Services(_imageConnector);
                //Create an attachment 
                Oid attachmentOID = services.SaveAttachment(filePath, newAsset, attachmentName);
                _logger.Info("-> Imported {0} attachment", attachmentOID.Token);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                File.Delete(filePath);
            }

            //Attachments attachments = new Attachments(_imageConnector);

            ////Writes the blob in one shot.
            //using (Stream output = attachments.GetWriteStream(AssetOID))
            //{
            //    output.Write(FileContent, 0, FileContent.Length);
            //}
            //attachments.SetWriteStream(AssetOID, FileType);

            //string filePath = @"C:\Temp\";
            //using (Stream input = new FileStream(filePath + fileName, FileMode.Open, FileAccess.Read))
            //{
                //using (Stream output = attachments.GetWriteStream(AssetOID))
                //{
                //    byte[] buffer = new byte[input.Length + 1];
                //    while (true)
                //    {
                //        int read = input.Read(buffer, 0, buffer.Length);
                //        if (read <= 0)
                //            break;
                //        output.Write(buffer, 0, read);
                //        }
                //    }
                //}
                //attachments.SetWriteStream(AssetOid, FileType);
            //}
            
            
            
            //Attachments attachment = new Attachments(_imageConnector);


            //Services attachmentService = new Services(_imageConnector);
            //Oid attachmentID = new Oid(attachmentType, Convert.ToInt64(AttachmentID));
            //    using (Stream blob = attachmentService.SaveAttachment(attachmentID))
            //    {
            //        blob.CopyTo(memoryStream);
            //    }
            //}
            //return memoryStream.ToArray();

            
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

        private void createAttachment()
        {
            //Create a story in Scope:0.
            Services services = new Services(_imageConnector);
            var storyType = services.Meta.GetAssetType("Story");
            var newStory = services.New(storyType, services.GetOid("Scope:0"));
            var nameAttribute = storyType.GetAttributeDefinition("Name");
            var name = string.Format("Story with an VersionOne PNG Attachment");
            newStory.SetAttributeValue(nameAttribute, name);
            services.Save(newStory);

            //Create an attachment for the story.
            string fileName = @"C:\Temp\versionone.png";
            Oid attachmentOID = services.SaveAttachment(fileName, newStory, "Attachment for " + newStory.Oid.Momentless);
        }

        public static Asset GetWorkitem(V1Connector connector, string referenceNum)
        {



            IServices services;

            services = new Services(connector);



            IAssetType workitemType = services.Meta.GetAssetType("Workitem");

            Query query = new Query(workitemType);



            IAttributeDefinition idFieldAttribute =

                //workitemType.GetAttributeDefinition(_configuration.V1Connection.IdField);
                workitemType.GetAttributeDefinition("ID");

            query.Selection.Add(idFieldAttribute);



            FilterTerm term = new FilterTerm(idFieldAttribute);

            term.Equal(referenceNum);

            query.Filter = term;

            QueryResult result = services.Retrieve(query);



            if (result.Assets.Any())
            {

                return result.Assets[0];

            }

            return null;

        }

        public static bool AttachFile(V1Connector connector, Asset workitem, string filename)
        {

            IServices services;

            services = new Services(connector);



            Oid workitemId = services.GetOid(workitem.Oid.Momentless.ToString());

            Query query = new Query(workitemId);

            QueryResult result = services.Retrieve(query);

            Asset workitemAsset = result.Assets[0];



            try
            {

                Oid attachmentOid = services.SaveAttachment(filename, workitemAsset, "Attachment: " + filename);

                if (attachmentOid != null) return true;

            }

            catch (Exception ex)
            {

                if (ex.Message.Contains("413"))
                {

                    _logger.Warn("File too large to import into VersionOne (exceeds 4MB): {0}", filename);

                }

                else
                {

                    _logger.Error("Something bad happened - {0}", ex.Message);

                }

            }

            return false;

        }

        private string createFile(string fileName, byte[] FileContent)
        {
            string filePath = @"C:\Temp\" + fileName;

            try
            {
                //Process the File Bytes
                //int buffersize = 4096;
                //File.Create(filePath, buffersize);
                File.WriteAllBytes(filePath, FileContent);

                //_logger.Info("-> Created File: {0}", filePath);
                return filePath;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

    }
}
