var sp_svc = "IBADI.asmx/gspc_tbl";
var sp_svc_table = "IBADI.asmx/gspc_tbl";
var sp_svc_scalar = "IBADI.asmx/gspc";


//Generic routine for running services from our local webservice
//Example:
//If parameter is not required
//var result = getServiceDataJSON("IBADI.asmx/HelloWorld");
//
//If parameters are required - be sure that the parameter names match exactly
//var result = getServiceDataJSON("WebServices/PCWorkbenchWebService.asmx/HelloWorld", { PID: "1303787" });
function getServiceDataJSON(actionUrl, params, errorhandler) {
    $.ajax({
        type: "GET",
        async: false,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        url: actionUrl,
        data: params,
        success: function (data) {
            if ((typeof data) == "string") {
                //console.log(data);
                result = JSON.parse(data);
            } else {
                result = data;
            }
        },
        error: function (e) {
            //console.log(e.responseText);
            var fault = JSON.parse(e.responseText);
            if (fault != null) {
                var htmlMsg = "Url: " + actionUrl + "<br/>Location: " + fault.Location + "<br/>Message: " + fault.Message;
                var msg = "Url: " + actionUrl + "; Location: " + fault.Location + "; Message: " + fault.Message;
                alert( htmlMsg);
                //console.error("[getServiceDataJSON] " + msg);
            }
            else {
                var htmlMsg = "Url: " + actionUrl + "<br/>Message: Unknown Internal Error";
                var msg = "Url: " + actionUrl + "; Message: Unknown Internal Error";
                alert( htmlMsg);
                //console.error("[getServiceDataJSON] " + msg);
            }
            if (errorhandler != null)
                errorhandler(e);
        }
    });
    return result;
}

function IB_renderHTML(fields, containerToAppendTo) {
    /*   
    This routine can be used to place objects directly on the page, in a pop-up or within pannels on the page.
    e.g.
    renderFields([{ "html": "<b>Hello</b><br/>" }, { "html": "<b>Tester!</b>" }], $("#_canvas"))
    */
    containerToAppendTo.css("float:right");
    fields.forEach(
    function (item, index) {

        if (!(typeof item.html === "undefined")) {
            /*Special field to enable us add generic html*/
            var html_field_fragment = $(item.html);
            containerToAppendTo.append(html_field_fragment);
        }
    }
   )//add_new_dialog.fields.forEach
}

function IB_renderFromProc(procName, paramsWithValues,targetDiv)
{
    if (!IB_check_if_proc_exists(procName)) {
       alert("proc does not exist in database - " + procName);
        return;
    }

    var dbtest = getServiceDataJSON(sp_svc_table, { procName: JSON.stringify("webpage." + procName), paramsWithValues: JSON.stringify(paramsWithValues) });
    //console.log(JSON.parse(JSON.parse(dbtest.d)[0].html));
    IB_renderHTML(JSON.parse(JSON.parse(dbtest.d)[0].html), $("#" + targetDiv));
}

function IB_check_if_proc_exists(procName)
{
    var procName2 = "ibadi.usp_check_if_proc_exists";
    var paramsWithValues = JSON.stringify({ procName: procName });
    var result = getServiceDataJSON(sp_svc_table, { procName: JSON.stringify(procName2), paramsWithValues: JSON.stringify(paramsWithValues) });
    //console.log(result);
    if (  JSON.parse(result.d)[0].result == 1    ) {
        return true;
    } 
    return false;
}

function getParameterByName(name, url) {
    if (!url) url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
    results = regex.exec(url);
    //console.info(results);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function IB_runProc_with_paramvals(procName,paramswithvalues) {   
    var pwv =JSON.stringify(paramswithvalues); 
    var result = getServiceDataJSON("IBADI.asmx/gspc_tbl", { procName: JSON.stringify(procName), paramsWithValues:JSON.stringify(pwv)});    
    return result;
}

function IB_runProc(procName, paramswithvalues) {
    var pwv = JSON.stringify(paramswithvalues);
    var result = getServiceDataJSON("IBADI.asmx/gspc_tbl", { procName: JSON.stringify(procName), paramsWithValues: JSON.stringify(pwv) });
    var arr = JSON.parse(result.d);
    return arr;
}


function IB_runProc2(procName, paramswithvalues) {
    var outresult = null;
    var data = new FormData();
    data.append("procName", procName);
    data.append("paramswithvalues", JSON.stringify(paramswithvalues));
    var ajaxRequest = $.ajax({
        type: "POST",
        async: false,
        contentType: "application/json; charset=utf-8",
        dataType: "xml",
        contentType: false,
        url: "IBADI.asmx/gspc_tbls_large_params",
        cache: false,
        processData: false,
        data: data,
        context: "_canvas",
        success: function (result) {
            //console.log("OK-001");
            outresult = JSON.parse(result.childNodes[0].innerHTML.replace(/],]/g, "]]"));
            //console.log(outresult);
            //console.log("OK-002");
            
            return result;
        },
        error: function (e) {
            //console.log("ERROR-003");
            //console.log(e);
            //console.log("ERROR-004");
            result = e.responseText;
            result = result.replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");
            result = result.replace("<string xmlns=\"http://IBADI.org/\">", "");
            result = result.replace("</string>", "");
            outresult = JSON.parse(result.replace(/],]/g, "]]"));
            //console.log(result);
            return e;
        }
    });

    //Wont get result unles we set  async: false
    return outresult;
}


function IB_extract_first_item_from_result(result) {
    return JSON.parse(result.d)[0].result
}

//Example 1:   render from JSON string
//IB_renderHTML([{ "html": "<b>Hello!</b><br/>" }, { "html": "<b>Hello!</b>" }], $("#_canvas"))

//Example 2:  render from stored procedure
//IB_renderFromProc("test_simple_html_page", "{}", "_canvas");

//Example 3: get proc name from querystring
function IB_start_page() {
    proc_name = getParameterByName("proc_name");
    if (!proc_name) {
        proc_name = getParameterByName("page_id");
    }
    if (!proc_name) {
        proc_name = getParameterByName("page_name");
    }
    if (proc_name) {
        IB_renderFromProc(proc_name, "{}", "_canvas");
    }

}
