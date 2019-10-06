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
            sdr = GetImportDataFromDBTable("EmbeddedImages");  

            int importCount = 0;
            int failedCount = 0;
            Asset asset = null;
            _logger.Info("-> Starting Embedded Images Import with DB");

            while (sdr.Read())
            {
                if 
                (
                        String.IsNullOrEmpty(sdr["Content"].ToString()) ||
                        String.IsNullOrEmpty(sdr["ContentType"].ToString())
                )
                {
                    UpdateImportStatus("EmbeddedImages", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Embedded image missing required field.");
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Failed - Count: " + ++failedCount);
                    continue;
                }

                try
                {
                    string newAssetOID = null;
                    Services services = new Services(_imageConnector);

                    if (String.IsNullOrEmpty(sdr["Asset"].ToString()))
                    {
                        newAssetOID = _config.V1TargetConnection.Project;
                        asset = GetWorkitem(services, newAssetOID);
                    }
                    else
                    {
                        newAssetOID = GetNewAssetOIDFromDB(sdr["Asset"].ToString());
                        asset = GetWorkitem(services, newAssetOID);
                    }

                    int typePosition = sdr["ContentType"].ToString().IndexOf("/", 0);
                    string imageType = sdr["ContentType"].ToString().Substring(typePosition + 1);
                    string filePath = @"C:\Windows\Temp\" + "tempFile." + imageType;
                    Oid embeddedImageOID = null;

                    try
                    {
                        //Process the File Bytes
                        File.WriteAllBytes(filePath, (byte[])sdr["Content"]);

                        embeddedImageOID = services.SaveEmbeddedImage(filePath, asset);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        File.Delete(filePath);
                    }

                    UpdateNewAssetOIDAndStatus("EmbeddedImages", sdr["AssetOID"].ToString(), embeddedImageOID.Momentless.ToString(), ImportStatuses.IMPORTED, "Embedded image imported.");

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

        public static Asset GetWorkitem(IServices services, string referenceNum)
        {
            //IServices services;

            //services = new Services(connector);
            int characterPosition = referenceNum.IndexOf(":", 0);
            string assetType = referenceNum.Substring(0, characterPosition);

            IAssetType workitemType = services.Meta.GetAssetType(assetType);

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

        public int GetBlobs(IServices services, string referenceNum)
        {
            //Use Export Wizard to move the Blob Data from the Source Db into Staging created as the Query Table
            //Query to Insert Blob Data into the EmbeddedImages Table before running GetBlobs
            //Insert into EmbeddedImages
            //(AssetOID, Content, ContentType, Hash)
            //Select ID, Content, ContentType, Hash
            //From Query

            return 0;

        }

//            SqlDataReader sdr = null;
//            sdr = GetImportDataFromDBTable("Query");

//            while (sdr.Read())
//            {
//                try
//                {
//                    string newAssetOID = null;

//                    if (String.IsNullOrEmpty(sdr["Asset"].ToString()))
//                    {
//                        newAssetOID = "Scope:0";
//                    }
//                    else
//                    {
//                        newAssetOID = GetNewAssetOIDFromDB(sdr["Asset"].ToString());
//                    }



//                    if (
//                        String.IsNullOrEmpty(sdr["Content"].ToString()) ||
//                        String.IsNullOrEmpty(sdr["ContentType"].ToString())
//                        )
//                    {
//                        UpdateImportStatus("EmbeddedImages", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Embedded image missing required field.");
//                        continue;
//                    }
//`
//                    //IServices services;

//                    //services = new Services(connector);
//                    int characterPosition = referenceNum.IndexOf(":", 0);
//            string assetType = referenceNum.Substring(0, characterPosition);

//            IAssetType workitemType = services.Meta.GetAssetType(assetType);

//            Query query = new Query(workitemType);

//            IAttributeDefinition idFieldAttribute =

//            //workitemType.GetAttributeDefinition(_configuration.V1Connection.IdField);
//            workitemType.GetAttributeDefinition("ID");

//            query.Selection.Add(idFieldAttribute);

//            FilterTerm term = new FilterTerm(idFieldAttribute);

//            term.Equal(referenceNum);

//            query.Filter = term;

//            QueryResult result = services.Retrieve(query);


//            if (result.Assets.Any())
//            {

//                return result.Assets[0];

//            }

//            return null;

//        }

    }
}
