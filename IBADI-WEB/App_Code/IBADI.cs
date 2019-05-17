using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using Newtonsoft.Json;

//using System.Net.Http;
using System.Net;
using System.IO;
//using System.Web.Script.Services;

[WebService(Namespace = "http://IBADI.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[System.ComponentModel.ToolboxItem(false)]
[System.Web.Script.Services.ScriptService]
public class IBADI : System.Web.Services.WebService {
    [WebMethod]
    public string gtWb()
    {
        try
        {
            string requestUrl = HttpContext.Current.Request["requestUrl"];
            requestUrl = requestUrl.Replace("&amp;","&");
            HttpWebRequest http = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            HttpWebResponse response = (HttpWebResponse)http.GetResponse();
            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                string responseJson = sr.ReadToEnd();
                return responseJson;
            }
        }
        catch
        {
            return String.Empty;
        }       
    }




    [WebMethod]
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
    
    [WebMethod]
    public string processUploadedFile()
    {
        string result = null;
        try
        {
            if (HttpContext.Current.Request.Files.Count > 0)
            {
                // Get the uploaded image from the Files collection
                var httpPostedFile = HttpContext.Current.Request.Files["UploadedFile"];
                if (httpPostedFile != null)
                {
                    // Get the complete file path
                    string fileName = Guid.NewGuid() + "";//httpPostedFile.FileName;
                    var fileSavePath = Path.Combine(HttpContext.Current.Server.MapPath("~/Upload"), fileName);
                    // Save the uploaded file to "UploadedFiles" folder
                    httpPostedFile.SaveAs(fileSavePath + System.IO.Path.GetExtension(httpPostedFile.FileName));
                    result = "Upload/" + fileName +  System.IO.Path.GetExtension(httpPostedFile.FileName);//fileSavePath;
                }
            }
        }
        catch (Exception ex)
        {
            //return "{\"error\":\"Data not updated : $$Message$$\"}".Replace("$$Message$$", ex.Message.Replace("'", "")).Replace(System.Environment.NewLine, " ");
            //this.Context.Response.Write(ex.Message.Replace("'", ""));
            this.Context.Response.Write("<error>" + ex.Message.Replace("'", "") + "</error>");
            return "<error>" + ex.Message.Replace("'", "") + "</error>";
        }

        //this.Context.Response.Write(result);
        return result;
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


}
