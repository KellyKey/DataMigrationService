using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataWriter
{
    public class ImportMembers : IImportAssets
    {

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportMembers(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            MigrationConfiguration.AssetInfo assetInfo = _config.AssetsToMigrate.Find(i => i.Name == "Members");
            SqlDataReader sdr = GetImportDataFromDBTable("Members");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: Admin member will not be imported.
                    if (sdr["AssetOID"].ToString() == "Member:20")
                    {
                        UpdateNewAssetOIDAndStatus("Members", sdr["AssetOID"].ToString(), sdr["AssetOID"].ToString(), ImportStatuses.SKIPPED, "Admin member (Member:20) cannot be imported.");
                        continue;
                    }

                    ////SPECIAL CASE: Member with no username will not be imported.
                    if (String.IsNullOrEmpty(sdr["Username"].ToString()) == true)
                    {
                        UpdateImportStatus("Members", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Member with no username cannot be imported.");
                        continue;
                    }

                    //DUPLICATE CHECK: Check for duplicates if enabled.
                    if (String.IsNullOrEmpty(assetInfo.DuplicateCheckField) == false)
                    {
                        //Ensure that we have a value to check, if not, will attempt to create the member.
                        if (String.IsNullOrEmpty(sdr[assetInfo.DuplicateCheckField].ToString()) == false)
                        {
                            string currentAssetOID = CheckForDuplicateInV1(assetInfo.InternalName, assetInfo.DuplicateCheckField, sdr[assetInfo.DuplicateCheckField].ToString());
                            if (string.IsNullOrEmpty(currentAssetOID) == false)
                            {
                                UpdateNewAssetOIDAndStatus("Members", sdr["AssetOID"].ToString(), currentAssetOID, ImportStatuses.SKIPPED, "Duplicate member.");
                                continue;
                            }
                        }
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Member");
                    Asset asset = _dataAPI.New(assetType, null);
                    //string newAssetOid = GetNewAssetOIDFromDB(sdr["AssetOID"].ToString(), "Member");
                    //Asset asset = GetAssetFromV1(newAssetOid);


                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, sdr["Name"].ToString());

                    IAttributeDefinition userNameAttribute = assetType.GetAttributeDefinition("Username");
                    asset.SetAttributeValue(userNameAttribute, sdr["Username"].ToString());

                    IAttributeDefinition passwordAttribute = assetType.GetAttributeDefinition("Password");
                    asset.SetAttributeValue(passwordAttribute, sdr["Password"].ToString());
                    //asset.SetAttributeValue(passwordAttribute, "password");

                    IAttributeDefinition nickNameAttribute = assetType.GetAttributeDefinition("Nickname");
                    asset.SetAttributeValue(nickNameAttribute, sdr["Nickname"].ToString());

                    IAttributeDefinition emailAttribute = assetType.GetAttributeDefinition("Email");
                    asset.SetAttributeValue(emailAttribute, sdr["Email"].ToString());

                    IAttributeDefinition phoneAttribute = assetType.GetAttributeDefinition("Phone");
                    asset.SetAttributeValue(phoneAttribute, sdr["Phone"].ToString());

                    IAttributeDefinition defaultRoleAttribute = assetType.GetAttributeDefinition("DefaultRole");
                    asset.SetAttributeValue(defaultRoleAttribute, sdr["DefaultRole"].ToString());

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    //asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());
                    string targetURL = _config.V1TargetConnection.Url;
                    string newDescription = SetEmbeddedImageContent(sdr["Description"].ToString(), targetURL);
                    asset.SetAttributeValue(descAttribute, newDescription);


                    IAttributeDefinition notifyViaEmailAttribute = assetType.GetAttributeDefinition("NotifyViaEmail");
                    asset.SetAttributeValue(notifyViaEmailAttribute, sdr["NotifyViaEmail"].ToString());

                    if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    {
                        IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");

                        AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());

                    }

                    IAttributeDefinition IsCollaboratorAttribute = assetType.GetAttributeDefinition("IsCollaborator");
                    asset.SetAttributeValue(IsCollaboratorAttribute, sdr["IsCollaborator"].ToString());
                    //asset.SetAttributeValue(IsCollaboratorAttribute, "True");

                    IAttributeDefinition sendConversationEmailsAttribute = assetType.GetAttributeDefinition("SendConversationEmails");
                    asset.SetAttributeValue(sendConversationEmailsAttribute, sdr["SendConversationEmails"].ToString());

                    _dataAPI.Save(asset);
                    //_logger.Info("Reached this point");
                    UpdateNewAssetOIDAndStatus("Members", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Member imported.");
                    importCount++;

                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);

                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Members", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
                        _logger.Error("Asset: " + sdr["AssetOID"].ToString() + " Failed to Import ");
                        continue;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            sdr.Close();
            setManager();
            return importCount;
        }

        private void setManager()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Members");
            while (sdr.Read())
            {
                try
                {
                    IAssetType assetType = _metaAPI.GetAssetType("Member");
                    Asset asset = null;

                    if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    {
                        asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                    }
                    else
                    {
                        continue;
                    }

                    IAttributeDefinition managerAttribute = assetType.GetAttributeDefinition("Manager");
                    asset.SetAttributeValue(managerAttribute, GetNewAssetOIDFromDB(sdr["Manager"].ToString(), "Member"));

                    _dataAPI.Save(asset);
                    UpdateNewAssetOIDAndStatus("Members", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Manager imported.");
                }

                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Members", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
                        _logger.Error("Asset: " + sdr["AssetOID"].ToString() + " Failed to Import ");
                        continue;
                    }
                    else
                    {
                        throw ex;
                    }
                }

            }
            sdr.Close();
        }

        public int CloseMembers()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Members");
            int assetCount = 0;
            int exceptionCount = 0;
            while (sdr.Read())
            {
                Asset asset = null;

                try
                {
                    if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    {
                        asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                    }
                    else
                    {
                        continue;
                    }

                    ExecuteOperationInV1("Member.Inactivate", asset.Oid);
                    assetCount++;
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Open - Count: " + assetCount);
                }
                catch (Exception ex)
                {
                    exceptionCount++;
                    _logger.Info("Exception: " + sdr["AssetOID"].ToString() + " Exception - Count: " + exceptionCount);
                    continue;
                }


            }
            sdr.Close();
            return assetCount;
        }

        public int OpenMembers()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Members");
            int assetCount = 0;
            int exceptionCount = 0;
            while (sdr.Read())
            {
                Asset asset = null;

                try
                {
                    if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    {
                        asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                    }
                    else
                    {
                        continue;
                    }

                    ExecuteOperationInV1("Member.Reactivate", asset.Oid);
                    assetCount++;
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Open - Count: " + assetCount);
                }
                catch (Exception ex)
                {
                    exceptionCount++;
                    _logger.Info("Exception: " + sdr["AssetOID"].ToString() + " Exception - Count: " + exceptionCount);
                    continue;
                }


            }
            sdr.Close();
            return assetCount;
        }


    }
}
