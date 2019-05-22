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
    public class ImportEmbeddedImages : IImportAssets
    {
        private V1Connector _imageConnector;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportEmbeddedImages(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, V1Connector ImageConnector, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations)
        {
            _imageConnector = ImageConnector;
        }

        public override int Import()
        {
            SqlDataReader sdr = null;
            sdr = GetImportDataFromSproc("spGetEmbeddedImagesForImport");
            int importCount = 0;
            int failedCount = 0;
            _logger.Info("-> Starting Embedded Images Import with DB");

            while (sdr.Read())
            {
                try
                {                    
                    string newAssetOID = GetNewAssetOIDFromDB(sdr["Asset"].ToString());                    
                    //IAssetType newAssetType = _metaAPI.GetAssetType("Story");
                    //Oid newAssetOID = new Oid(newAssetType, );
                    //Asset newAsset = new Asset(newAssetType);
                    Asset assetContainer = GetWorkitem(_imageConnector, newAssetOID);

                    if (String.IsNullOrEmpty(sdr["Asset"].ToString()) ||
                    String.IsNullOrEmpty(sdr["Content"].ToString()) ||
                    String.IsNullOrEmpty(sdr["ContentType"].ToString()) ||
                    String.IsNullOrEmpty(newAssetOID))
                    {
                        UpdateImportStatus("EmbeddedImages", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Embedded image missing required field.");
                        continue;
                    }

                   // IAssetType assetType = _metaAPI.GetAssetType("EmbeddedImage");
                   // Asset asset = _dataAPI.New(assetType, null);

                   // IAttributeDefinition assetAttribute = assetType.GetAttributeDefinition("Asset");
                   // asset.SetAttributeValue(assetAttribute, sdr["Asset"].ToString());

                   // IAttributeDefinition contentAttribute = assetType.GetAttributeDefinition("Content");
                   // asset.SetAttributeValue(contentAttribute, (byte[])sdr["Content"]);

                   // IAttributeDefinition contentTypeAttribute = assetType.GetAttributeDefinition("ContentType");
                   //// string mimeType = MimeType.Resolve(sdr["ContentType"].ToString());
                    //asset.SetAttributeValue(contentTypeAttribute, sdr["ContentType"].ToString());

                    //Now save the binary content of the embedded image.
                    UploadEmbeddedImageContent(assetContainer, (byte[])sdr["Content"]);                 
                    UpdateNewAssetOIDAndStatus("EmbeddedImages", sdr["AssetOID"].ToString(), assetContainer.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Embedded image imported.");
                    importCount++;
                    _logger.Info("-> Imported {0} Embedded image", importCount);
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        string error = ex.Message.Replace("'", ":");
                        UpdateImportStatus("Embedded image", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, error);
                        failedCount++;
                        _logger.Info("-> Failed {0} Embedded image.", failedCount);
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

        private void UploadEmbeddedImageContent(Asset newAsset, byte[] FileContent)
        {
            //Create a new story.
            //var storyType = services.Meta.GetAssetType("Story");
            //var newStory = services.New(storyType, services.GetOid("Scope:0"));
            //var nameAttribute = storyType.GetAttributeDefinition("Name");
            //var descriptionAttribute = storyType.GetAttributeDefinition("Description");
            //var name = string.Format("Story with an embedded image");
            //newStory.SetAttributeValue(nameAttribute, name);
            //services.Save(newStory);
            //Oid storyOID = newStory.Oid;

            ////Create an embedded image.
            //string file = @"C:\Temp\versionone.jpg";
            //Oid embeddedImageOid = services.SaveEmbeddedImage(file, newStory);
            //var embeddedImageTag = string.Format("<p>Here's an embedded image:</p></br><img src=\"{0}\" alt=\"\" data-oid=\"{1}\" />", "embedded.img/" + embeddedImageOid.Key, embeddedImageOid.Momentless);
            //newStory.SetAttributeValue(descriptionAttribute, embeddedImageTag);
            //services.Save(newStory);

            string filePath = @"C:\Windows\Temp\TempImage.jpg";

            try
            {
                File.WriteAllBytes(filePath, FileContent);
                Services services = new Services(_imageConnector);
                var storyType = services.Meta.GetAssetType("Story");
                //var newStory = services.New(storyType, services.GetOid("Scope:7795"));
                var nameAttribute = storyType.GetAttributeDefinition("Name");
                var descriptionAttribute = storyType.GetAttributeDefinition("Description");
                var name = string.Format("Story: Another embedded image demo");
                //newAsset.SetAttributeValue(descriptionAttribute, string.Format("<p>Image<p>"));
                services.Save(newAsset);
                //Console.Write(newAsset.GetAttribute(nameAttribute));
                //Console.Write(newAsset.GetAttribute(descriptionAttribute));
                //var oidTypeID = newAsset.Oid.ToString();
                Oid storyOID = newAsset.Oid;

                Oid embeddedImageOID = services.SaveEmbeddedImage(filePath, newAsset);
                var embeddedImageTag = string.Format("<p>Here's a migrated embedded image:</p></br><img src=\"{0}\" alt=\"\" data-oid=\"{1}\" />", "embedded.img/" + embeddedImageOID.Key, embeddedImageOID.Momentless);
                //var storyType = services.Meta.GetAssetType("Story");
                // IAttributeDefinition descFieldAttribute = storyType.GetAttributeDefinition("Description");
                newAsset.SetAttributeValue(descriptionAttribute, embeddedImageTag);
                services.Save(newAsset);
                _logger.Info("-> Imported {0} embedded image", embeddedImageOID.Token);       
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                File.Delete(filePath);
            } 
        }
        public static Asset GetWorkitem(V1Connector connector, string referenceNum)
        {

            IServices services;

            services = new Services(connector);

            //IAssetType workitemType = services.Meta.GetAssetType("Workitem");
            Oid storyId = services.GetOid(referenceNum);

            Query query = new Query(storyId);

            IAssetType storyType = services.Meta.GetAssetType("Story");
            IAttributeDefinition nameAttribute = storyType.GetAttributeDefinition("Name");
            IAttributeDefinition descFieldAttribute = storyType.GetAttributeDefinition("Description");

            //workitemType.GetAttributeDefinition(_configuration.V1Connection.IdField);
            //IAttributeDefinition idFieldAttribute   = workitemType.GetAttributeDefinition("ID");
            //IAttributeDefinition nameFieldAttribute = workitemType.GetAttributeDefinition("Name");
            //IAttributeDefinition descFieldAttribute = workitemType.GetAttributeDefinition("Description");

            query.Selection.Add(nameAttribute);
            query.Selection.Add(descFieldAttribute);

            //FilterTerm term = new FilterTerm(idFieldAttribute);

            //term.Equal(referenceNum);

            //query.Filter = term;

            QueryResult result = services.Retrieve(query);



            if (result.Assets.Any())
            {

                return result.Assets[0];

            }

            return null;

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
