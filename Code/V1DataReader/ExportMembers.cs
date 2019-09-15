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
    public class ExportMembers : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ExportMembers(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Member");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition emailAttribute = assetType.GetAttributeDefinition("Email");
            query.Selection.Add(emailAttribute);

            IAttributeDefinition nicknameAttribute = assetType.GetAttributeDefinition("Nickname");
            query.Selection.Add(nicknameAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition phoneAttribute = assetType.GetAttributeDefinition("Phone");
            query.Selection.Add(phoneAttribute);

            IAttributeDefinition defaultRoleAttribute = assetType.GetAttributeDefinition("DefaultRole");
            query.Selection.Add(defaultRoleAttribute);

            IAttributeDefinition usernameAttribute = assetType.GetAttributeDefinition("Username");
            query.Selection.Add(usernameAttribute);

            IAttributeDefinition memberLabelsAttribute = assetType.GetAttributeDefinition("MemberLabels.ID");
            query.Selection.Add(memberLabelsAttribute);

            IAttributeDefinition notifyViaEmailAttribute = assetType.GetAttributeDefinition("NotifyViaEmail");
            query.Selection.Add(notifyViaEmailAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition managerAttribute = assetType.GetAttributeDefinition("Manager");
            query.Selection.Add(managerAttribute);

            //SPECIAL CASE: sendConversationEmailsAttribute attribute only exists in V1 12 and later.
            IAttributeDefinition sendConversationEmailsAttribute = null;
            //IAttributeDefinition IsCollaboratorAttribute = null;

            if (true)
            {
                sendConversationEmailsAttribute = assetType.GetAttributeDefinition("SendConversationEmails");
                query.Selection.Add(sendConversationEmailsAttribute);

                //IsCollaboratorAttribute = assetType.GetAttributeDefinition("IsCollaborator");
                //query.Selection.Add(IsCollaboratorAttribute);
            }

            string SQL = BuildMemberInsertStatement();

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
                //_logger.Info("The TotalAvailable Members is {0} ",assetTotal);


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
                        cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@Email", GetScalerValue(asset.GetAttribute(emailAttribute)));
                        cmd.Parameters.AddWithValue("@Nickname", GetScalerValue(asset.GetAttribute(nicknameAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Phone", GetScalerValue(asset.GetAttribute(phoneAttribute)));
                        cmd.Parameters.AddWithValue("@DefaultRole", GetSingleRelationValue(asset.GetAttribute(defaultRoleAttribute)));
                        cmd.Parameters.AddWithValue("@Username", GetScalerValue(asset.GetAttribute(usernameAttribute)));
                        cmd.Parameters.AddWithValue("@MemberLabels", GetMultiRelationValues(asset.GetAttribute(memberLabelsAttribute)));
                        cmd.Parameters.AddWithValue("@NotifyViaEmail", GetScalerValue(asset.GetAttribute(notifyViaEmailAttribute)));
                        cmd.Parameters.AddWithValue("@Manager", GetMultiRelationValues(asset.GetAttribute(managerAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        //cmd.Parameters.AddWithValue("@IsCollaborator", IsCollaboratorAttribute == null ? "True" : GetScalerValue(asset.GetAttribute(IsCollaboratorAttribute)));
                        cmd.Parameters.AddWithValue("@SendConversationEmails", sendConversationEmailsAttribute == null ? "True" : GetScalerValue(asset.GetAttribute(sendConversationEmailsAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Member: added - Count = {0}", assetCounter);

                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private string BuildMemberInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO MEMBERS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Email,");
            sb.Append("Nickname,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Phone,");
            sb.Append("DefaultRole,");
            sb.Append("Username,");
            sb.Append("TaggedWith,");
            sb.Append("Manager,");
            sb.Append("MemberLabels,");
            sb.Append("NotifyViaEmail,");
            //sb.Append("IsCollaborator,");
            sb.Append("SendConversationEmails) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Email,");
            sb.Append("@Nickname,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Phone,");
            sb.Append("@DefaultRole,");
            sb.Append("@Username,");
            sb.Append("@TaggedWith,");
            sb.Append("@Manager,");
            sb.Append("@MemberLabels,");
            sb.Append("@NotifyViaEmail,");
            //sb.Append("@IsCollaborator,");
            sb.Append("@SendConversationEmails);");
            return sb.ToString();
        }

    }
}
