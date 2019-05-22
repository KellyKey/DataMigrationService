using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;
using Rally.RestApi;
using System.Collections.Generic;
using Rally.RestApi.Response;

namespace RallyDataReader
{
    public class ExportAttachments : IExportAssets
    {
        public ExportAttachments(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;

        //    RallyRestApi restApi = new RallyRestApi(_config.RallySourceConnection.Username, _config.RallySourceConnection.Password, _config.RallySourceConnection.Url, "1.43");

        //    SqlDataReader sdr = GetAttachmentsFromDB();
        //    string SQL = BuildAttachmentUpdateStatement();

        //    while (sdr.Read())
        //    {
        //        try
        //        {
        //            DynamicJsonObject attachmentMeta = restApi.GetByReference("attachment", Convert.ToInt64(sdr["AssetOID"]), "Name", "Description", "Artifact", "Content", "ContentType");
        //            DynamicJsonObject attachmentContent = restApi.GetByReference(attachmentMeta["Content"]["_ref"]);
        //            byte[] content = System.Convert.FromBase64String(attachmentContent["Content"]);

        //            using (SqlCommand cmd = new SqlCommand())
        //            {
        //                cmd.Connection = _sqlConn;
        //                cmd.CommandText = SQL;
        //                cmd.CommandType = System.Data.CommandType.Text;
        //                cmd.Parameters.AddWithValue("@AssetOID", sdr["AssetOID"]);
        //                cmd.Parameters.AddWithValue("@Name", attachmentMeta["Name"]);
        //                cmd.Parameters.AddWithValue("@FileName", attachmentMeta["Name"]);
        //                cmd.Parameters.AddWithValue("@Content", content);
        //                cmd.Parameters.AddWithValue("@ContentType", attachmentMeta["ContentType"]);
        //                cmd.Parameters.AddWithValue("@Description", String.IsNullOrEmpty(attachmentMeta["Description"]) ? DBNull.Value : attachmentMeta["Description"]);
        //                cmd.ExecuteNonQuery();
        //            }
        //            assetCounter++;
        //        }
        //        catch
        //        {
        //            continue;
        //        }
        //    }
            return assetCounter;
        }

        private string BuildAttachmentUpdateStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE ATTACHMENTS SET ");
            sb.Append("Name = @Name,");
            sb.Append("FileName = @FileName,");
            sb.Append("Content = @Content,");
            sb.Append("ContentType = @ContentType,");
            sb.Append("Description = @Description ");
            sb.Append("WHERE AssetOID = @AssetOID;");
            return sb.ToString();
        }

    }
}
