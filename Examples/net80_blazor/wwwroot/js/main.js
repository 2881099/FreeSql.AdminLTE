
function isArray(obj) { return Object.prototype.toString.call(obj) === '[object Array]'; }
function request(name, defaultValue) { return qs_parse()[name] || defaultValue; }
function cint(str, defaultValue) { str = parseInt(str, 10); return isNaN(str) ? defaultValue || 0 : str; }
String.prototype.trim = function () { return this.ltrim().rtrim(); }
String.prototype.ltrim = function () { return this.replace(/^\s+(.*)/g, '$1'); }
String.prototype.rtrim = function () { return this.replace(/([^ \r\n]*)\s+$/g, '$1'); }
//中文按2位算
String.prototype.getLength = function () { return this.replace(/([\u0391-\uFFE5])/ig, '11').length; }
String.prototype.left = function (len, endstr) {
    if (len > this.getLength()) return this;
    var ret = this.replace(/([\u0391-\uFFE5])/ig, '$1\0')
        .substr(0, len).replace(/([\u0391-\uFFE5])\0/ig, '$1');
    if (endstr) ret = ret.concat(endstr);
    return ret;
}
String.prototype.format = function () {
    var val = this.toString();
    for (var a = 0; a < arguments.length; a++) val = val.replace(new RegExp("\\{" + a + "\\}", "g"), arguments[a]);
    return val;
}
var __padleftright = function (str, len, padstr, isleft) {
    str = str || ' ';
    padstr = padstr || '';
    var ret = [];
    for (var a = 0; a < len - str.length; a++) ret.push(padstr);
    if (isleft) ret.unshift(this)
    else ret.push(this);
    return ret.join('');
}
// 'a'.padleft(3, '0') => '00a'
String.prototype.padleft = function (len, padstr) {
    return __padleftright(this, len, padstr, 1);
};
// 'a'.padright(3, '0') => 'a00'
String.prototype.padright = function (len, padstr) {
    return __padleftright(this, len, padstr, 0);
};
Function.prototype.toString2 = function () {
    var str = this.toString();
    str = str.substr(str.indexOf('/*') + 2, str.length);
    str = str.substr(0, str.lastIndexOf('*/'));
    return str;
};
Number.prototype.round = function (r) {
    r = typeof (r) == 'undefined' ? 1 : r;
    var rv = String(this);
    var io = rv.indexOf('.');
    var ri = io == -1 ? '' : rv.substr(io + 1, r);
    var le = io == -1 ? (rv + '.') : rv.substr(0, io + 1 + r);
    for (var a = ri.length; a < r; a++) le += '0';
    return le;
};

function getObjectURL(file) {
    var url = null;
    if (window.createObjectURL != undefined) { // basic
        url = window.createObjectURL(file);
    } else if (window.URL != undefined) { // mozilla(firefox)
        url = window.URL.createObjectURL(file);
    } else if (window.webkitURL != undefined) { // webkit or chrome
        url = window.webkitURL.createObjectURL(file);
    }
    return url;
}

var qs_parse_cache = {};
function qs_parse(str) {
    str = str || location.search;
    if (str.charAt(0) === '?') str = str.substr(1, str.length);
    if (qs_parse_cache[str]) return qs_parse_cache[str];
    var qs = {};
    var y = str.split('&');
    for (var a = 0; a < y.length; a++) {
        var x = y[a].split('=', 2);
        if (x[0] === '') continue;
        var x0 = url_decode(x[0]);
        if (!qs[x0]) qs[x0] = '';
        qs[x0] += url_decode(x[1] || '') + '\r\n';
    }
    //转换数组，去重
    for (var a in qs) {
        qs[a] = qs[a].substr(0, qs[a].length - 2);
        if (qs[a].indexOf('\r\n') === -1) continue;
        var t1 = qs[a].split('\r\n');
        var t2 = {};
        qs[a] = [];
        for (var b = 0; b < t1.length; b++) {
            if (t2[t1[b]]) continue;
            t2[t1[b]] = true;
            qs[a].push(t1[b]);
        }
    }
    return qs;
}
function qs_parseByUrl(url) {
    url = url || location.href;
    url = url.split('?', 2);
    if (url.length === 1) url.push('');
    return qs_parse(url[1]);
}
function qs_stringify(query) {
    var ret = [];
    for (var a in query) {
        var z = url_encode(a); if (z === '') continue;
        if (isArray(query[a]) == false) {
            ret.push(z + '=' + url_encode(query[a]));
            continue;
        }
        for (var b = 0; b < query[a].length; b++)
            ret.push(z + '=' + url_encode(query[a][b]));
    }
    return ret.join('&');
}
function join_url(url1, url2) {
    var ds = [];
    if (url2 === '')
        ds = url1.split('/');
    else if (url2.substr(0, 1) === '?') {
        ds = url1.split('?', 2)[0].split('/');
        ds[ds.length - 1] += url2;
    }
    else {
        ds = url1.split('?', 2)[0].split('/');
        ds.pop();
        ds = ds.concat(url2.split('/'));
    }
    var ret = [];
    var nd = 0;
    while (true) {
        var d = ds.pop();
        if (d == null) break;
        if (d === '.') continue;
        if (d === '..') {
            nd++;
            continue;
        }
        if (nd > 0) {
            nd--;
            continue;
        }
        ret.unshift(d);
    }
    return ret.join('/').replace(/\/\//g, '/');
}

var mapSeries = function (items, fn, callback) {
    var rs = [];
    var func = function () {
        var item = items.pop();
        if (item) return fn(item, function (err, r) {
            if (err) return callback(err, rs);
            rs.push(r);
            return func.call(null);
        });
        else return callback(null, rs);
    };
    func.call(null);
};

var url_encode = function (str) {
    return encodeURIComponent(str)
        .replace(/ /gi, '+')
        .replace(/~/gi, '%7e')
        .replace(/'/gi, '%26%2339%3b');
};
var url_decode = function (str) {
    str = String(str).replace(/%26%2339%3b/gi, '\'');
    return decodeURIComponent(str)
        .replace(/\+/gi, ' ')
        .replace(/%7e/gi, '~');
};
String.prototype.htmlencode = function () {
    return this
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/ /g, '&nbsp;')
        .replace(/"/g, '&quot;');
};
String.prototype.htmldecode = function () {
    return this
        .replace(/&quot;/gi, '"')
        .replace(/&lt;/gi, '<')
        .replace(/&gt;/gi, '>')
        .replace(/&nbsp;/gi, ' ')
        .replace(/&/g, '&amp;');
};
function extractobj(obj) {
    var parms = Array.prototype.slice.call(arguments, 1);
    parms.push('span');
    for (var a = 0; a < parms.length; a++) {
        var ts = obj.getElementsByTagName(parms[a]);
        for (var b = 0; b < ts.length; b++) if (ts[b].getAttribute('extract')) obj[ts[b].getAttribute('extract')] = ts[b];
    }
}

function ajax(config) {
    if (!config) {
        config = {
            timeout: 60 * 1000, // Timeout
            //baseURL: location.host == '127.0.0.1:8088' ? 'https://localhost:65034' : '',
            responseType: "json",
            headers: {
                'token': sessionStorage.getItem('token') || "",
            },
            transformRequest: [function (data, headers) {
                if (data) console.log(data)
                return qs_stringify(data)
            }],
            paramsSerializer: params => {
                return qs_stringify(params)
            },
        }
    }
    const _axios = axios.create(config);
    _axios.defaults.headers.post['Content-Type'] = 'application/x-www-form-urlencoded;charset=utf-8';

    _axios.interceptors.request.use(
        function (config) {
            return config;
        },
        function (error) {
            return Promise.reject(error);
        });

    _axios.interceptors.response.use(
        function (r) {
            if (/"code":5001,"message":"access denied"/.test(r.data)) top.location.href = '/Login'
            return r.data;
        },
        function (error) {
            return Promise.reject(error);
        });
    return _axios;
}
