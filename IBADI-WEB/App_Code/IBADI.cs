using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.ServiceModel.Web;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using Newtonsoft.Json;

using System.Net.Http;
using System.IO;
using System.Text;

/// <summary>
/// Summary description for IBADI
/// </summary>
[WebService(Namespace = "http://IBADI.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[System.ComponentModel.ToolboxItem(false)]
[System.Web.Script.Services.ScriptService]
public class IBADI : System.Web.Services.WebService {
    [WebMethod] 
    [ScriptMethod(UseHttpGet = true)]
    public string HelloWorld() {
        return "{\"message\":\"Hi world!\"}";
    }


    [WebMethod]
    //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    [ScriptMethod(UseHttpGet = true)]
    public string gspc(string procName, string paramsWithValues)
    {
        //Generic stored procedure call 
        //paramsWithValues expected to be in the form {"param1":"value1","param2":"value2","param3":"value3"...}

        var sqlcmd = new SqlCommand();

        //Convert incomming parameter to object of parameters...
        var deserializer = new JavaScriptSerializer();
        try
        {
            Dictionary<string, string> objParamsWithValues = deserializer.Deserialize<Dictionary<string, string>>(paramsWithValues);
            foreach (string k in objParamsWithValues.Keys)
            {
                procName = procName + " @" + k.ToString() + ",";
                sqlcmd.Parameters.AddWithValue("@" + k.ToString(), objParamsWithValues[k].ToString());
            }
            if (procName.EndsWith(",")) { procName = procName.Remove(procName.Length - 1); }//i.e. remove dangling comma, if any.

            sqlcmd.CommandText = procName;
            string result = RunSQLVariableParams(sqlcmd);
            if (!result.StartsWith("{"))
            {
                result = "{\"result\":\"" + result + "\"}";
            }
            else
            {
                result = "{\"result\":" + result + "}";
            }

            return result;
        }
        catch (Exception c)
        {
            string err_msg = "{\"error\":\"Data not updated : $$Message$$\"}".Replace("$$Message$$", c.Message).Replace(System.Environment.NewLine, " ");
            //Remove JSON-unfriendly characters from the message
            return err_msg;
        }
    }


    [WebMethod]
    [ScriptMethod(UseHttpGet = true)]
    public string gspc_tbl(string procName, string paramsWithValues)
    {
        //Generic stored procedure call 
        //paramsWithValues expected to be in the form {"param1":"value1","param2":"value2","param3":"value3"...}
        try
        {
            //Convert incomming parameter to object of parameters...
            var deserializer = new JavaScriptSerializer();
            Dictionary<string, string> objParamsWithValues = deserializer.Deserialize<Dictionary<string, string>>(paramsWithValues);
            List<KeyValuePair<string, object>> parameters = new List<KeyValuePair<string, object>>();
            foreach (string k in objParamsWithValues.Keys)
                parameters.Add(new KeyValuePair<string, object>(k.ToString(), objParamsWithValues[k].ToString()));

            DataTable dtResult = SqlDatabase.ExecuteProcedure(SqlDatabase.WbConnectionString, procName, parameters);
            return ToJsonString(dtResult);
        }
        catch (Exception c)
        {
            string err_msg = "{\"error\":\"Data not updated : $$Message$$\"}".Replace("$$Message$$", c.Message).Replace(System.Environment.NewLine, " ");
            return err_msg;
        }
    }

    [WebMethod]
    //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
    public  string gspc_tbls_large_params()
    {
        try
        {
            string procName = HttpContext.Current.Request["procName"];
            string paramsWithValues = HttpContext.Current.Request["paramsWithValues"];

            var deserializer = new JavaScriptSerializer();
            Dictionary<string, string> objParamsWithValues = deserializer.Deserialize<Dictionary<string, string>>(paramsWithValues);
            List<KeyValuePair<string, object>> parameters = new List<KeyValuePair<string, object>>();
            foreach (string k in objParamsWithValues.Keys)
                parameters.Add(new KeyValuePair<string, object>(k.ToString(), objParamsWithValues[k].ToString()));


            DataTableCollection dtResult = SqlDatabase.ExecuteProcedureReturnTables(SqlDatabase.WbConnectionString, procName, parameters);

            return ToJsonString(dtResult);
        }
        catch (Exception c)
        {
            string err_msg = "{\"error\":\"Data not updated : $$Message$$\"}".Replace("$$Message$$", c.Message).Replace(System.Environment.NewLine, " ");
            return err_msg;
        }
    }

    string RunSQLVariableParams(SqlCommand cmd)
    {
        try
        {
            SqlDatabase.ExecuteNonQuery(SqlDatabase.WbConnectionString, cmd);
            return "OK";
        }
        catch (Exception c)
        {
            string err_msg = "{\"error\":\"Data not saved : $$Message$$\"}".Replace("$$Message$$", c.Message).Replace(System.Environment.NewLine, " ");
            //Remove JSON-unfriendly characters from the message
            return err_msg;
        }
    }

    public static string ToJsonString(DataTable table)
    {
        foreach (DataColumn col in table.Columns)
            col.ColumnName = col.ColumnName.Replace(" ", "");
        string result = JsonConvert.SerializeObject(table);
        return result;
    }


    public static string ToJsonString(DataTableCollection tables)
    {
        string result = "[";
        foreach (DataTable table in tables)
        {
            result = result + ToJsonString(table) + ",";
        }
        result = result + "]";
        return result;
    }

    public static Stream ToJson(String text)
    {
        string result = JsonConvert.SerializeObject(text);
        byte[] resultBytes = Encoding.UTF8.GetBytes(result);
        //WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
        return new MemoryStream(resultBytes);
    }

    public static Stream ToJson(DataTableCollection tables)
    {
        foreach (DataTable table in tables)
        {
            foreach (DataColumn col in table.Columns)
                col.ColumnName = col.ColumnName.Replace(" ", "");
        }

        string result = JsonConvert.SerializeObject(tables);

        byte[] resultBytes = Encoding.UTF8.GetBytes(result);
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
        return new MemoryStream(resultBytes);
    }

}
public class SqlDatabase
{
    public const string WbConnectionString = "WbConnectionString";
    protected static int commandTimeout = 300;
    protected static string connStr = ConfigurationManager.ConnectionStrings[WbConnectionString].ConnectionString;

    protected static SqlConnection GetConnection(string connName)
    {
        return new SqlConnection(ConfigurationManager.ConnectionStrings[connName].ConnectionString);
    }

    public static int ExecuteNonQuery(string connName, SqlCommand command)
    {
        using (SqlConnection conn = SqlDatabase.GetConnection(connName))
        {
            conn.Open();
            command.Connection = conn;
            command.CommandTimeout = commandTimeout;
            return command.ExecuteNonQuery();
        }
    }

    public static object ExecuteScalar(string connName, SqlCommand command)
    {
        using (SqlConnection conn = SqlDatabase.GetConnection(connName))
        {
            conn.Open();
            command.Connection = conn;
            command.CommandTimeout = commandTimeout;
            return command.ExecuteScalar();
        }
    }

    public static string ExecuteScalarForString(string connName, string sql)
    {
        string result = null;
        using (SqlConnection conn = SqlDatabase.GetConnection(connName))
        {
            conn.Open();
            SqlCommand command = new SqlCommand(sql, conn);
            command.CommandTimeout = commandTimeout;
            SqlDataReader reader = command.ExecuteReader();
            if (reader.Read())
                result = reader.GetString(0);
            return result;
        }
    }

    public static DataSet ExecuteQuery(string connName, SqlCommand command)
    {
        using (SqlConnection conn = SqlDatabase.GetConnection(connName))
        {
            SqlDataAdapter dbAdapter = new SqlDataAdapter();
            DataSet resultsDataSet = new DataSet();
            dbAdapter.SelectCommand = command;
            command.Connection = conn;
            command.CommandTimeout = commandTimeout;
            conn.Open();
            dbAdapter.Fill(resultsDataSet);
            return resultsDataSet;
        }
    }

    public static DataTable ExecuteSql(string connName, string sql)
    {
        SqlCommand cmd = new SqlCommand(sql);
        DataSet ds = SqlDatabase.ExecuteQuery(connName, cmd);
        if (ds.Tables.Count > 0)
            return ds.Tables[0];
        else
            return null;
    }

    public static DataTable ExecuteProcedure(string connName, string procedureName)
    {
        SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = procedureName;
        DataSet ds = SqlDatabase.ExecuteQuery(connName, cmd);
        if (ds.Tables.Count > 0)
            return ds.Tables[0];
        else
            return null;
    }

    public static DataTable ExecuteProcedure(string connName, string procedureName, List<KeyValuePair<string, object>> parameters)
    {
        SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = procedureName;
        if (parameters != null)
            foreach (KeyValuePair<string, object> param in parameters)
                cmd.Parameters.AddWithValue(param.Key, param.Value);
        DataSet ds = SqlDatabase.ExecuteQuery(connName, cmd);
        if (ds.Tables.Count > 0)
            return ds.Tables[0];
        else
            return null;
    }

    public static DataTableCollection ExecuteProcedureReturnTables(string connName, string procedureName, List<KeyValuePair<string, object>> parameters)
    {
        SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = procedureName;
        if (parameters != null)
            foreach (KeyValuePair<string, object> param in parameters)
                if (param.Value.ToString().Length > 1000)
                {
                    cmd.Parameters.Add("@" + param.Key, SqlDbType.VarChar, -1).Value = param.Value.ToString();
                }
                else
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
        DataSet ds = SqlDatabase.ExecuteQuery(connName, cmd);
        return ds.Tables;
    }

    public static string ExecuteProcedureForScalarString(string connName, string procedureName, List<KeyValuePair<string, object>> parameters)
    {
        SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = procedureName;
        if (parameters != null)
            foreach (KeyValuePair<string, object> param in parameters)
                cmd.Parameters.AddWithValue(param.Key, param.Value);
        DataSet ds = SqlDatabase.ExecuteQuery(connName, cmd);
        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            return ds.Tables[0].Rows[0][0].ToString();
        else
            return null;
    }
}