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
        protected MetaModel _metaAPI;
        protected Services _dataAPI;
        protected SqlConnection _sqlConn;
        protected MigrationConfiguration _config;

        public IExportAssets(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
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

    }
}
