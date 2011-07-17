using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SimpleVoc
{
    public class SimpleVocConnection
    {
        private string _version;
        private JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public Uri Uri { get; set; }

        /// <summary>
        /// Version of the SimpleVoc Server
        /// </summary>
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

        public SimpleVocConnection(string host, int port)
        {
            Uri = new Uri("http://" + host + ":" + port);
        }

        public SimpleVocConnection(int port) : this("localhost", port) { }
        public SimpleVocConnection() : this("localhost", 8008) { }

        public async Task<string[]> GetKeysAsync(string prefix)
        {
            var req = CreateRequest("keys/" + prefix);

            using (var res = (HttpWebResponse)await req.GetResponseAsync())
            {
                var result = _serializer.Deserialize<string[]>(GetResponseData(res));
                return result;
            }
        }

        /// <summary>
        /// Return a list of Keys for the given prefix
        /// </summary>
        /// <param name="prefix">A prefix the Keys start with</param>
        /// <returns>A List of keys</returns>
        public string[] GetKeys(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Prefix can't be empty!");
            }

            var req = CreateRequest("keys/" + prefix);

            try
            {
                using (var res = (HttpWebResponse)req.GetResponse())
                {
                    return _serializer.Deserialize<string[]>(GetResponseData(res));
                }
            }
            catch (WebException ex)
            {
                var svEx = ConvertToSimpleVocException(ex);

                if (svEx.Message == "prefix not found") return new string[] { };

                throw svEx;
            }
        }

        /// <summary>
        /// Get a value from SimpleVoc server asynchonously for given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<SimpleVocValue> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key can't be empty!");
            }

            var req = CreateRequest("value/" + key);

            try
            {
                using (var res = (HttpWebResponse)await req.GetResponseAsync())
                {
                    return GetValueFromResponse(res, key);
                }
            }
            catch (WebException ex)
            {
                throw ConvertToSimpleVocException(ex);
            }
        }

        /// <summary>
        /// Get a value from SimpleVoc server for given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SimpleVocValue Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key can't be empty!");
            }

            var req = CreateRequest("value/" + key);

            try
            {
                using (var res = (HttpWebResponse)req.GetResponse())
                {
                    return GetValueFromResponse(res, key);
                }
            }
            catch (WebException ex)
            {
                throw ConvertToSimpleVocException(ex);
            }
        }

        /// <summary>
        /// Save a SimpleVoc value on a SimpleVoc server.
        /// </summary>
        /// <param name="value">The SimpleVoc value to save.</param>
        /// <returns>true on success</returns>
        public bool Set(SimpleVocValue value)
        {
            if (string.IsNullOrWhiteSpace(value.Key))
            {
                throw new ArgumentException("Key can't be empty!");
            }

            var req = CreateRequest("value/" + value.Key);
                req.Method = "POST"; // Set uses "POST"
                req.Headers.Add("x-voc-flags", value.Flags.ToString());

            // Set expires header if given
            if (value.Expires != DateTime.MinValue)
            {
                // Convert to ISO 8601
                req.Headers.Add("x-voc-expires", value.Expires.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }

            try
            {
                using (var reqStream = req.GetRequestStream())
                {
                    // Convert string to bytes
                    if (value.Data != null)
                    {
                        byte[] dataToWrite = Encoding.UTF8.GetBytes(value.Data);
                        reqStream.Write(dataToWrite, 0, dataToWrite.Length);
                    }

                    // If an error occurs a WebException is thrown
                    var res = (HttpWebResponse)req.GetResponse();
                        res.Close();
                }
            }
            catch (WebException ex)
            {
                throw ConvertToSimpleVocException(ex);
            }

            return true;
        }

        /// <summary>
        /// Flush all data on server
        /// </summary>
        public void Flush()
        {
            var req = CreateRequest("flush");
                req.Method = "POST";

            using (var res = (HttpWebResponse)req.GetResponse())
            {
                var result = GetResponseData(res);
            }
        }

        /// <summary>
        /// Create a SimpleVoc value from a WebResponse for a given key
        /// </summary>
        /// <param name="res"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private SimpleVocValue GetValueFromResponse(HttpWebResponse res, string key)
        {
            return new SimpleVocValue
            {
                Key = key,
                Data = GetResponseData(res),
                Created = DateTime.Parse(res.GetResponseHeader("x-voc-created")),
                Expires = !string.IsNullOrWhiteSpace(res.GetResponseHeader("x-voc-expires")) ? DateTime.Parse(res.GetResponseHeader("x-voc-expires")) : DateTime.MinValue,
                Flags = int.Parse(res.GetResponseHeader("x-voc-flags"))
            };
        }

        /// <summary>
        /// Create a HttpWebRequest for given path and set some common headers.
        /// </summary>
        /// <param name="path">The path to the resource</param>
        /// <returns></returns>
        private HttpWebRequest CreateRequest(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(Uri + path);
                request.ContentType = "application/json; charset=utf-8";
                request.UserAgent = "Alex-SimpleVoc/0.1";

            return request;
        }

        /// <summary>
        /// Read data from response stream
        /// </summary>
        /// <param name="res">The HttpWebResponse</param>
        /// <returns>Response as a string</returns>
        private string GetResponseData(HttpWebResponse res)
        {
            using (var stream = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
            {
                return stream.ReadToEnd();
            }
        }

        /// <summary>
        /// Get Version from server and save it
        /// </summary>
        private void GetVersion()
        {
            var req = CreateRequest("version");

            using (var res = (HttpWebResponse)req.GetResponse())
            {
                var result = (Dictionary<string, object>)_serializer.DeserializeObject(GetResponseData(res));
                _version = result["version"].ToString();
            }
        }

        /// <summary>
        /// Create a SimpleVocExcpetion from a WebException
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private SimpleVocException ConvertToSimpleVocException(WebException ex)
        {
            var res = (HttpWebResponse)ex.Response;
            var result = (Dictionary<string, object>)_serializer.DeserializeObject(GetResponseData(res));

            res.Close();

            return new SimpleVocException(result["message"].ToString(), ex);
        }
    }
}
