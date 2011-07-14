using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SimpleVoc
{
    public class SimpleVOC
    {
        private string _version;
        private JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public Uri Uri { get; set; }

        public string Version
        {
            get
            {
                if (_version == null)
                {
                    GetVersion();
                }

                return _version;
            }
        }

        public SimpleVOC(string host, int port)
        {
            Uri = new Uri("http://" + host + ":" + port);
        }

        public SimpleVOC(int port) : this("localhost", port) { }
        public SimpleVOC() : this("localhost", 8008) { }

        public async Task<string[]> GetKeysAsync(string prefix)
        {
            var req = CreateRequest("keys/" + prefix);

            using (var res = (HttpWebResponse)await req.GetResponseAsync())
            {
                var result = _serializer.Deserialize<string[]>(GetResponseData(res));
                return result;
            }
        }

        public string[] GetKeys(string prefix)
        {
            var req = CreateRequest("keys/" + prefix);

            using (var res = (HttpWebResponse)req.GetResponse())
            {
                var result = _serializer.Deserialize<string[]>(GetResponseData(res));
                return result;
            }
        }

        public async Task<SimpleVocValue> GetAsync(string key)
        {
            var req = CreateRequest("value/" + key);

            using (var res = (HttpWebResponse)await req.GetResponseAsync())
            {
                return new SimpleVocValue
                {
                    Key = key,
                    Data = GetResponseData(res),
                    Created = DateTime.Parse(res.GetResponseHeader("x-voc-created")),
                    Expires = !string.IsNullOrWhiteSpace(res.GetResponseHeader("x-voc-expires")) ? DateTime.Parse(res.GetResponseHeader("x-voc-expires")) : DateTime.MinValue,
                    Flags = res.GetResponseHeader("x-voc-flags")
                };
            }
        }

        public SimpleVocValue Get(string key)
        {
            var req = CreateRequest("value/" + key);

            using (var res = (HttpWebResponse)req.GetResponse())
            {
                return new SimpleVocValue
                {
                    Key = key,
                    Data = GetResponseData(res),
                    Created = DateTime.Parse(res.GetResponseHeader("x-voc-created")),
                    Expires = !string.IsNullOrWhiteSpace(res.GetResponseHeader("x-voc-expires")) ? DateTime.Parse(res.GetResponseHeader("x-voc-expires")) : DateTime.MinValue,
                    Flags = res.GetResponseHeader("x-voc-flags")
                };
            }
        }

        public void Flush()
        {
            var req = CreateRequest("flush");
            req.Method = "POST";

            using (var res = (HttpWebResponse)req.GetResponse())
            {
                var result = GetResponseData(res);
            }
        }

        private void GetResponseComplete(IAsyncResult result)
        {
            var req = (HttpWebRequest)result.AsyncState;
            var res = (HttpWebResponse)req.EndGetResponse(result);
        }

        private HttpWebRequest CreateRequest(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(Uri + path);
            request.ContentType = "application/json; charset=utf-8";
            request.UserAgent = "Alex-SimpleVoc/0.1";

            return request;
        }

        private string GetResponseData(HttpWebResponse res)
        {
            using (var stream = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
            {
                return stream.ReadToEnd();
            }
        }

        private void GetVersion()
        {
            var req = CreateRequest("version");

            using (var res = (HttpWebResponse)req.GetResponse())
            {
                var result = (Dictionary<string, object>)_serializer.DeserializeObject(GetResponseData(res));
                _version = result["version"].ToString();
            }
        }
    }
}
