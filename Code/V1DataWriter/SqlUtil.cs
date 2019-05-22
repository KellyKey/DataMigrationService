using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace V1DataWriter
{
    class SqlUtil
    {
        private SqlConnection _cnn;

        internal SqlUtil(string ConnectionString)
        {
            _cnn = new SqlConnection(ConnectionString);
            _cnn.Open();
        }

        internal SqlUtil(SqlConnection SqlCnn)
        {
            _cnn = SqlCnn;
        }

        internal void CloseConnection()
        {
            _cnn.Close();
        }

        internal SqlDataReader GetImportDataFromTable(string TableName)
        {
            string SQL = "SELECT * FROM " + TableName + ";";
            SqlCommand cmd = new SqlCommand(SQL, _cnn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        internal SqlDataReader GetImportDataFromSQL(string SQL)
        {
            SqlCommand cmd = new SqlCommand(SQL, _cnn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        internal SqlDataReader GetImportDataFromStoredProcedure(string StoredProcedureName)
        {
            SqlCommand cmd = new SqlCommand(StoredProcedureName, _cnn);
            cmd.CommandType = CommandType.StoredProcedure;
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        internal string GetScalerFromSQL(string SQL)
        {
            SqlCommand cmd = new SqlCommand(SQL, _cnn);
            object result = cmd.ExecuteScalar();
            _cnn.Close();
            if (result == null)
                return String.Empty;
            else
                return result.ToString();
        }

    }
}
