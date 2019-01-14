self.addEventListener('message', function (e) {
    self.postMessage(_wrw_(e.data));
}, false);

function _wrw_(wkr_data) {
    var syn_path = wkr_data.syn_path;
    var requestUrl = wkr_data.requestUrl;

    var result;
    var oReq = new XMLHttpRequest();

    oReq.open("POST"
			  , syn_path + "/IBADI.asmx/gtWb"
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
    data.append("requestUrl", requestUrl);

    oReq.send(data);
    return result;
}