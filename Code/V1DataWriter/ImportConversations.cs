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
    public class ImportConversations : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportConversations(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            //SqlDataReader sdr = GetImportDataFromSproc("spGetConversationsForImport");
            SqlDataReader sdr = GetImportDataFromDBTable("Conversations");

            int importCount = 0;

            while (sdr.Read())
            {
                try
                {
                    //CHECK DATA: Conversation must have an author.
                    if (_config.V1Configurations.MigrateUnauthoredConversationsAsAdmin == false && String.IsNullOrEmpty(sdr["Author"].ToString()))
                    {
                        UpdateImportStatus("Conversations", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Conversation author attribute is required.");
                        continue;
                    }

                    //CHECK DATA:  Conversation must be authored by a Member in the Staging DB             
                    if (_config.V1Configurations.MigrateUnauthoredConversationsAsAdmin == false && String.IsNullOrEmpty(sdr["Author"].ToString()) == false)
                    {
                        string memberNewOid = string.Empty;
                        memberNewOid = GetNewAssetOIDFromDB(sdr["Author"].ToString(), "Members");
                    }




                    IAssetType assetType = _metaAPI.GetAssetType("Expression");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition authoredAtAttribute = assetType.GetAttributeDefinition("AuthoredAt");
                    asset.SetAttributeValue(authoredAtAttribute, sdr["AuthoredAt"].ToString());

                    IAttributeDefinition contentAttribute = assetType.GetAttributeDefinition("Content");
                    asset.SetAttributeValue(contentAttribute, sdr["Content"].ToString());

                    string memberOid = string.Empty;
                    if (_config.V1Configurations.MigrateUnauthoredConversationsAsAdmin == true && String.IsNullOrEmpty(sdr["Author"].ToString()))
                    {
                        memberOid = "Member:20";
                    }
                    else
                    {
                        memberOid = GetNewAssetOIDFromDB(sdr["Author"].ToString(), "Members");
                        if (String.IsNullOrEmpty(memberOid) == true)
                            continue;
                    }

                    IAttributeDefinition authorAttribute = assetType.GetAttributeDefinition("Author");
                    asset.SetAttributeValue(authorAttribute, memberOid);

                    //NOTE: Only works with 13.2 or greater of VersionOne, should be able to remove for earlier versions

                    //BelongsTo gets set for the 1st Conversation/Expression, After that, InReplyTo gets set and BelongsTo is "Related" into it automagicly
                    string belongsTo = string.Empty;
                    //asset.SetAttributeValue(belongsToAttribute, GetConversationBelongsTo(memberOid, sdr["Conversation"].ToString(), sdr["NewConversatiionOID"].ToString()));
                    if (String.IsNullOrEmpty(sdr["InReplyTo"].ToString()) == true)
                    {
                        IAttributeDefinition belongsToAttribute = assetType.GetAttributeDefinition("BelongsTo");
                        belongsTo = GetConversationBelongsTo(memberOid, sdr);
                        asset.SetAttributeValue(belongsToAttribute, belongsTo);
                    }
                    else
                    {
                        string inReplyTo = GetNewAssetOIDFromDB(sdr["InReplyTo"].ToString(), "Expression");
                        IAttributeDefinition inReplyToAttribute = assetType.GetAttributeDefinition("InReplyTo");
                        asset.SetAttributeValue(inReplyToAttribute, inReplyTo);

                    }

                    //NOTE: Need to switch on V1 version. Right now handles 11.3 to 11.4+. Used to be BaseAssets, is now Mentions.
                    //HACK: Modified to support Rally migration, needs refactoring.
                    if (String.IsNullOrEmpty(sdr["Mentions"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Mentions", sdr["Mentions"].ToString());
                        //AddRallyMentionsValue(assetType, asset, sdr["BaseAssetType"].ToString(), sdr["Mentions"].ToString());
                    }
                    else if (String.IsNullOrEmpty(sdr["BaseAssets"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Mentions", sdr["BaseAssets"].ToString());
                    }

                    _dataAPI.Save(asset);

                    UpdateNewAssetOIDInDB("Conversations", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString());
                    UpdateImportStatus("Conversations", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Conversation imported.");
                    importCount++;

                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Conversations", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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
            //SetConversationDependencies();Not Needed since InReplyTo gets set automagically through DB Relation
            return importCount;
        }

        //NOTE: Conversations sproc currently uses BaseAsset, needs refactoring to use Mentions for later versions of V1.
        private void SetConversationDependencies()
        {
            //SqlDataReader sdr = GetImportDataFromSproc("spGetConversationsForImport");
            SqlDataReader sdr = GetImportDataFromDBTable("Conversations");

            while (sdr.Read())
            {
                if (sdr["ImportStatus"].ToString() == ImportStatuses.FAILED.ToString()) continue;

                if (String.IsNullOrEmpty(sdr["InReplyTo"].ToString()) == true) continue;

                bool canSave = false;

                IAssetType assetType = _metaAPI.GetAssetType("Expression");
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());

                //string conversation = GetNewConversationOIDFromDB(sdr["Conversation"].ToString(), "Conversations");
                //if (String.IsNullOrEmpty(conversation) == false)
                //{
                //    IAttributeDefinition conversationAttribute = assetType.GetAttributeDefinition("Conversation");
                //    asset.SetAttributeValue(conversationAttribute, conversation);
                //    canSave = true;
                //}

                string inReplyTo = GetNewAssetOIDFromDB(sdr["InReplyTo"].ToString(), "Conversations");
                if (String.IsNullOrEmpty(inReplyTo) == false)
                {
                    IAttributeDefinition inReplyToAttribute = assetType.GetAttributeDefinition("InReplyTo");
                    asset.SetAttributeValue(inReplyToAttribute, inReplyTo);
                    canSave = true;
                }

                try
                {
                    if (canSave == true) _dataAPI.Save(asset);
                }
                catch (Exception ex)
                {

                }
            }
            sdr.Close();
        }

        //private string GetConversationBelongsTo(string MemberOid, string conversation, string newConversation)
        private String GetConversationBelongsTo(string memberOID, SqlDataReader sdr)
        {

            string newConversationOID = GetNewConversationOIDFromDB(sdr["Conversation"].ToString(), "Conversations");

            if (String.IsNullOrEmpty(newConversationOID) == true)
            {
                IAssetType assetType = _metaAPI.GetAssetType("Conversation");
                Asset newAsset = _dataAPI.New(assetType, null);

                IAttributeDefinition authoredAtAttribute = assetType.GetAttributeDefinition("Participants");

                newAsset.AddAttributeValue(authoredAtAttribute, memberOID);

                _dataAPI.Save(newAsset);

                UpdateNewConversationOIDInDB("Conversations", sdr["AssetOid"].ToString(), newAsset.Oid.Momentless.ToString());

                return newAsset.Oid.Momentless.ToString();

            }
            else
            {
                UpdateNewConversationOIDInDB("Conversations", sdr["AssetOid"].ToString(), newConversationOID);
                return newConversationOID;
                //return String.Empty;
            }

        }
    }
}