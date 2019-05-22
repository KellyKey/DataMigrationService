using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.SDK.APIClient;

namespace V1DataWriter
{
    static class APIUtil
    {
        internal static string GetAssetIDFromName(string AssetType, string Name, IMetaModel MetaAPI, IServices DataAPI)
        {
            IAssetType assetType = MetaAPI.GetAssetType(AssetType);
            Query query = new Query(assetType);
            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            FilterTerm term = new FilterTerm(nameAttribute);
            term.Equal(Name);
            query.Filter = term;

            QueryResult result;
            try
            {
                result = DataAPI.Retrieve(query);
            }
            catch
            {
                return String.Empty;
            }

            if (result.TotalAvaliable > 0)
                return result.Assets[0].Oid.Token;
            else
                return String.Empty;
        }

        internal static string GetAssetIDFromCode(string AssetType, string Code, IMetaModel MetaAPI, IServices DataAPI)
        {
            IAssetType assetType = MetaAPI.GetAssetType(AssetType);
            Query query = new Query(assetType);
            IAttributeDefinition codeAttribute = assetType.GetAttributeDefinition("Code");
            FilterTerm term = new FilterTerm(codeAttribute);
            term.Equal(Code);
            query.Filter = term;
            QueryResult result = DataAPI.Retrieve(query);

            if (result.TotalAvaliable > 0)
                return result.Assets[0].Oid.Token;
            else
                return String.Empty;
        }
    }
}
