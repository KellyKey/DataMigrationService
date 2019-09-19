using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataReader
{
    abstract public class IExportAssets
    {
        protected IMetaModel _metaAPI;
        protected Services _dataAPI;
        protected SqlConnection _sqlConn;
        protected MigrationConfiguration _config;

        public IExportAssets(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
        {
            _sqlConn = sqlConn;
            _metaAPI = MetaAPI;
            _dataAPI = DataAPI;
            _config = Configurations;
        }

        /**************************************************************************************
         * Virtual method that must be implemented in derived classes.
         **************************************************************************************/
        public abstract int Export();

        protected Object GetScalerValue(VersionOne.SDK.APIClient.Attribute attribute)
        {
            if (attribute.Value != null)
                return attribute.Value.ToString();
            else
                return DBNull.Value;
        }

        protected SqlDataReader GetDataFromDB(string assetOID)
        {
            string tableName = GetTableNameForAsset(assetOID);

            string SQL = "SELECT * FROM " + tableName + " WHERE AssetOID = '" + assetOID + "'";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();

            if (sdr.HasRows == true)
            {
                return sdr;
            }
            else
            {
                return null;
            }
        }


        protected Object GetMultiRelationValues(VersionOne.SDK.APIClient.Attribute attribute)
        {
            string values = String.Empty;
            if (attribute.ValuesList.Count > 0)
            {
                for (int i = 0; i < attribute.ValuesList.Count; i++)
                {
                    if (i == 0)
                        values = attribute.ValuesList[i].ToString();
                    else
                        values += ";" + attribute.ValuesList[i].ToString();
                }
                return values;
            }
            else
            {
                return DBNull.Value;
            }
        }

        protected Object GetSingleRelationValue(VersionOne.SDK.APIClient.Attribute attribute)
        {
            if (attribute.Value != null && attribute.Value.ToString() != "NULL")
                return attribute.Value.ToString();
            else
                return DBNull.Value;
            //return "Scope:0";
        }

        protected Object GetSingleListValue(VersionOne.SDK.APIClient.Attribute attribute)
        {
            if (attribute.Value != null && attribute.Value.ToString() != "NULL")
            {
                IAssetType assetType = _metaAPI.GetAssetType("List");
                Query query = new Query(assetType);

                IAttributeDefinition assetIDAttribute = assetType.GetAttributeDefinition("ID");
                query.Selection.Add(assetIDAttribute);

                IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
                query.Selection.Add(nameAttribute);

                FilterTerm assetName = new FilterTerm(assetIDAttribute);
                assetName.Equal(attribute.Value.ToString());
                query.Filter = assetName;

                QueryResult result = _dataAPI.Retrieve(query);
                return result.Assets[0].GetAttribute(nameAttribute).Value.ToString();
            }
            else
            {
                return DBNull.Value;
            }
        }

        protected string GetTableNameForAsset(string CurrentAssetOID)
        {
            string tableName = String.Empty;
            if (CurrentAssetOID.Contains("Member"))
                tableName = "Members";
            else if (CurrentAssetOID.Contains("MemberLabel"))
                tableName = "MemberGroups";
            else if (CurrentAssetOID.Contains("Team"))
                tableName = "Teams";
            else if (CurrentAssetOID.Contains("Schedule"))
                tableName = "Schedules";
            else if (CurrentAssetOID.Contains("Scope"))
                tableName = "Projects";
            else if (CurrentAssetOID.Contains("ScopeLabel"))
                tableName = "Programs";
            else if (CurrentAssetOID.Contains("Timebox"))
                tableName = "Iterations";
            else if (CurrentAssetOID.Contains("Goal"))
                tableName = "Goals";
            else if (CurrentAssetOID.Contains("Theme"))
                tableName = "FeatureGroups";
            else if (CurrentAssetOID.Contains("Request"))
                tableName = "Requests";
            else if (CurrentAssetOID.Contains("Issue"))
                tableName = "Issues";
            else if (CurrentAssetOID.Contains("Epic"))
                tableName = "Epics";
            else if (CurrentAssetOID.Contains("Story"))
                tableName = "Stories";
            else if (CurrentAssetOID.Contains("Defect"))
                tableName = "Defects";
            else if (CurrentAssetOID.Contains("Task"))
                tableName = "Tasks";
            else if (CurrentAssetOID.Contains("Test"))
                tableName = "Tests";
            else if (CurrentAssetOID.Contains("Link"))
                tableName = "Links";
            else if (CurrentAssetOID.Contains("Expression"))
                tableName = "Conversations";
            else if (CurrentAssetOID.Contains("Actual"))
                tableName = "Actuals";
            else if (CurrentAssetOID.Contains("Attachment"))
                tableName = "Attachments";
            else if (CurrentAssetOID.Contains("EmbeddedImage"))
                tableName = "EmbeddedImages";
            else
                tableName = "ListTypes";

            return tableName;
        }


    }
}
