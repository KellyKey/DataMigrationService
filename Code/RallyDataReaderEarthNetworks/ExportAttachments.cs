using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;
using System.Runtime.Serialization.Json;
using Rally.RestApi;
using System.Collections.Generic;
using Rally.RestApi.Response;
using Rally.RestApi.Json;
using NLog;

namespace RallyDataReader
{
    public class ExportAttachments : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ExportAttachments(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;


            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.AttachmentExportFilePrefix + "*.json");

            foreach (string file in files)
            {
                string jsonFile;

                StreamReader reader = new StreamReader(file);
                try
                {
                    do
                    {
                        jsonFile = reader.ReadToEnd();
                    }
                    while (reader.Peek() != -1);
                }

                catch
                {
                    jsonFile = "File is Empty";
                }

                finally
                {
                    reader.Close();
                }



                assetCounter += ProcessExportFile(jsonFile);
            }
            return assetCounter;
        }
        
        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildAttachmentUpdateStatement();
            int assetCounter = 0;

            //XDocument xmlDoc = XDocument.Load(FileName);
            var xmlDoc = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(
                Encoding.ASCII.GetBytes(FileName), new XmlDictionaryReaderQuotas()));

            var assets = from asset in xmlDoc.Root.Elements("Attachment") select asset;

            foreach (var asset in assets)
            {

                //RallyRestApi restApi = new RallyRestApi(_config.RallySourceConnection.Username, _config.RallySourceConnection.Password, _config.RallySourceConnection.Url, "1.43");
                //RallyRestApi restApi = new RallyRestApi(webServiceVersion: "v2.0");
                //restApi.Authenticate(_config.RallySourceConnection.Username, _config.RallySourceConnection.Password, _config.RallySourceConnection.Url, allowSSO: false);

                try
                {
                    //DynamicJsonObject attachmentMeta = restApi.GetByReference("attachment", Convert.ToInt64(sdr["AssetOID"]), "Name", "Description", "Artifact", "Content", "ContentType");
                    //DynamicJsonObject attachmentContent = restApi.GetByReference(attachmentMeta["Content"]["_ref"]);
                    //byte[] content = System.Convert.FromBase64String(attachmentContent["Content"]);

                    //DynamicJsonObject attachmentMeta = restApi.GetByReference("attachment", Convert.ToInt64(asset.Element("ObjectID").Value), asset.Element("_refObjectName").Value, "Rally Attachment", GetRefValue(asset.Element("Artifact").Element("_ref").Value), GetRefValue(asset.Element("Content").Element("_ref").Value), asset.Element("ContentType").Value);
                    //DynamicJsonObject attachmentMeta = restApi.GetByReference("attachment", Convert.ToInt64(asset.Element("ObjectID").Value), "Name", "Description", "Artifact", "Content", "ContentType");
                    //DynamicJsonObject attachmentContent = restApi.GetByReference(attachmentMeta["Content"]["_ref"]);
                    //byte[] content = System.Convert.FromBase64String(attachmentContent["Content"]);

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                        cmd.Parameters.AddWithValue("@Asset", GetRefValue(asset.Element("Artifact").Element("_ref").Value));
                        cmd.Parameters.AddWithValue("@AssetType", GetAssetType(asset.Element("Artifact").Element("_type").Value));
                        //cmd.Parameters.AddWithValue("@Name", attachmentMeta["Name"]);
                        //cmd.Parameters.AddWithValue("@FileName", attachmentMeta["Name"]);
                        //cmd.Parameters.AddWithValue("@Content", content);
                        //cmd.Parameters.AddWithValue("@ContentType", attachmentMeta["ContentType"]);
                        //cmd.Parameters.AddWithValue("@Description", String.IsNullOrEmpty(attachmentMeta["Description"]) ? DBNull.Value : attachmentMeta["Description"]);
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("-> Exported {0} Attachments with Object ID: " + asset.Element("ObjectID").Value, assetCounter);

                }
                catch(Exception ex)
                {
                    _logger.Error("Error: " + ex.Message + " with Object ID: " + asset.Element("ObjectID").Value);
                    continue;
                }
            }
            return assetCounter;
        }

        private string GetAssetType(string assetType)
        {
            if(assetType == "HierarchicalRequirement")
                return "Story";
            else
                return "Defect";
        }

        private string BuildAttachmentUpdateStatement()
        {
            //Insert for Rally Json Files
            //StringBuilder sb = new StringBuilder();
            ////sb.Append("UPDATE ATTACHMENTS SET ");
            //sb.Append("INSERT INTO ATTACHMENTS (");
            //sb.Append("AssetOID,");
            //sb.Append("Name,");
            //sb.Append("FileName,");
            //sb.Append("Content,");
            //sb.Append("ContentType,");
            //sb.Append("Description) ");
            ////sb.Append("WHERE AssetOID = @AssetOID;");
            //sb.Append("VALUES (");
            //sb.Append("@AssetOID,");
            //sb.Append("@Name,");
            //sb.Append("@FileName,");
            //sb.Append("@Content,");
            //sb.Append("@ContentType,");
            //sb.Append("@Description)");
            //return sb.ToString();

            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE ATTACHMENTS SET ");
            sb.Append("Asset = @Asset,");
            sb.Append("AssetType = @AssetType ");
            sb.Append("WHERE AssetOID = @AssetOID;");
            return sb.ToString();

        }

    }
}
