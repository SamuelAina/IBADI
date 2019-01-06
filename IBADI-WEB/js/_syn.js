self.addEventListener('message', function (e) {
    self.postMessage(_wr_(e.data));
}, false);


function _wr_(wkr_data) {
    var syn_path = wkr_data.syn_path;
    var proc = wkr_data.proc;
    var proc_params = wkr_data.proc_params;

    var result;
    var oReq = new XMLHttpRequest();

    oReq.open("POST"
			  , syn_path + "/IBADI.asmx/gspc_tbls_large_params"
			  , false);

    oReq.onreadystatechange = function (aEvt) {
        if (oReq.readyState == 4) {
            if (oReq.status == 200) {
                result = oReq.responseText;
                result = result.replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");
                result = result.replace("<string xmlns=\"http://IBADI.org/\">", "");
                result = result.replace("</string>", "");
                result = JSON.parse(result.replace(/],]/g, "]]"));
            }
            else {
                result = { error: "ERROR" };
            }
        }
    };

    var data = new FormData();
    data.append("procName", proc);
    data.append("paramswithvalues", JSON.stringify(proc_params));

    oReq.send(data);
    return result;
}