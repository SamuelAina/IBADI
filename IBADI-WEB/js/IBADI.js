IB_start_page();
function IB_runProc_async(procName, paramswithvalues, callback) {
    var result;
    _syn_path = document.URL.split("/");
    _syn_path.pop();
    _syn_path = _syn_path.join("/");

    var wkr = new Worker(_syn_path + "/js/_syn.js");

    wkr.onmessage = function (e) {
        result = e.data;
        if (callback) {
            callback(result);
        }
        wkr.terminate();
    }

    wkr.postMessage({ syn_path: _syn_path, proc: procName, proc_params: paramswithvalues });
    return result
}

function IB_runSvc_async(requestUrl,  callback) {
    var result;
    _syn_path = document.URL.split("/");
    _syn_path.pop();
    _syn_path = _syn_path.join("/");

    var wkrw = new Worker(_syn_path + "/js/_syn_w.js");

    wkrw.onmessage = function (e) {
        result = e.data;
        if (callback) {
            callback(result);
        }
        wkrw.terminate();
    }
    wkrw.postMessage({ syn_path: _syn_path, requestUrl: requestUrl });
    return result
}


function IB_renderHTML(fields, containerToAppendTo) {
    containerToAppendTo.css("float:right");
    fields.forEach(
    function (item, index) {
        if (!(typeof item.html === "undefined")) {
            var html_field_fragment = $(item.html);
            containerToAppendTo.append(html_field_fragment);
        }
    }
   );
}

function IB_renderFromProc(procName, paramsWithValues,targetDiv)
{
    IB_runProc_async(
                "ibadi.usp_check_if_proc_exists"
                , { procName: procName }
                , function (result) {
                    if (result[0][0].result != "1") {
                        alert("proc does not exist in database - " + procName);
                        return;
                    } else {
                        IB_runProc_async("webpage." + procName
                                    , paramsWithValues = JSON.parse(paramsWithValues)
                                    , function (htmlresult) {
                                        h = htmlresult[0][0].html;
                                        h = h.replace(/&lt;/g, "<");
                                        h = h.replace(/&gt;/g, ">");
                                        h = JSON.parse(h);
                                        IB_renderHTML(h, $("#" + targetDiv));
                                    });
                    }
                }
    );
}


function getParameterByName(name, url) {
    if (!url) url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
    results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function IB_runProc(procName, paramswithvalues) {
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
            outresult = JSON.parse(result.childNodes[0].innerHTML.replace(/],]/g, "]]"));            
            return result;
        },
        error: function (e) {
            result = e.responseText;
            result = result.replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");
            result = result.replace("<string xmlns=\"http://IBADI.org/\">", "");
            result = result.replace("</string>", "");
            outresult = JSON.parse(result.replace(/],]/g, "]]"));
            return e;
        }
    });
    //Wont get result unles we set  async: false
    return outresult;
}

function IB_start_page() {
    proc_name = getParameterByName("proc_name");
    if (!proc_name) {
        proc_name = getParameterByName("page_id");
    }
    if (!proc_name) {
        proc_name = getParameterByName("page_name");
    }
    if (!proc_name) {
        proc_name = getParameterByName("page");
    }

    if (proc_name) {
        IB_renderFromProc(proc_name, "{}", "_canvas");
    }
}



function IB_runSvc(url) {

    var outresult = null;
    var data = new FormData();
    data.append("url", url);

    var ajaxRequest = $.ajax({
        type: "POST",
        async: false,
        contentType: "application/json; charset=utf-8",
        dataType: "xml",
        //contentType: false,
        url: "IBADI.asmx/gtWb",
        cache: false,
        processData: false,
        data: data,
        context: "_canvas",
        success: function (result) {
            console.log("====001====");
            //outresult = JSON.parse(result.childNodes[0].innerHTML.replace(/],]/g, "]]"));
            return result;
        },
        error: function (e) {
            console.log("====002====",e);
            result = e.responseText;
            result = result.replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");
            result = result.replace("<string xmlns=\"http://IBADI.org/\">", "");
            result = result.replace("</string>", "");
            console.log(result);
            //outresult = JSON.parse(result.replace(/],]/g, "]]"));
            return result;
        }
    });
    //Wont get result unles we set  async: false
    return outresult;
}