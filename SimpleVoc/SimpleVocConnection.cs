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

        /// <summary>
        /// Return a list of Keys for the given prefix
        /// </summary>
        /// <param name="prefix">A prefix the Keys start with</param>
        /// <param name="filter">A filter parameter see http://www.worldofvoc.com/wiki/index.php5/SVOC:Concepts_Attributes#Filtering_by_Attributes </param>
        /// <returns>A List of keys</returns>
        public async Task<string[]> GetKeysAsync(string prefix, string filter = null)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Prefix can't be empty!");
            }

            var req = CreateRequest("keys/" + prefix + ((filter != null) ? "?filter=" + filter : ""));

            try
            {
                using (var res = (HttpWebResponse)await req.GetResponseAsync())
                {
                    var result = _serializer.Deserialize<string[]>(await GetResponseDataAsync(res));
                    return result;
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
        /// Return a list of Keys for the given prefix
        /// </summary>
        /// <param name="prefix">A prefix the Keys start with</param>
        /// <param name="filter">A filter parameter see http://www.worldofvoc.com/wiki/index.php5/SVOC:Concepts_Attributes#Filtering_by_Attributes </param>
        /// <returns>A List of keys</returns>
        public string[] GetKeys(string prefix, string filter = null)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Prefix can't be empty!");
            }

            var req = CreateRequest("keys/" + prefix + ((filter != null) ? "?filter=" + filter : ""));

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
                    return await GetValueFromResponseAsync(res, key);
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
            var req = CreateRequestForSet(value);

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
        /// Asynchronously save a SimpleVoc value on a SimpleVoc server.
        /// </summary>
        /// <param name="value">The SimpleVoc value to save.</param>
        /// <returns>true on success</returns>
        public async Task<bool> SetAsync(SimpleVocValue value)
        {
            var req = CreateRequestForSet(value);

            try
            {
                using (var reqStream = await req.GetRequestStreamAsync())
                {
                    // Convert string to bytes
                    if (value.Data != null)
                    {
                        byte[] dataToWrite = Encoding.UTF8.GetBytes(value.Data);
                        await reqStream.WriteAsync(dataToWrite, 0, dataToWrite.Length);
                    }

                    // If an error occurs a WebException is thrown
                    var res = (HttpWebResponse) await req.GetResponseAsync();
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
                Flags = int.Parse(res.GetResponseHeader("x-voc-flags")),
                Extended = !string.IsNullOrWhiteSpace(res.GetResponseHeader("x-voc-extended")) ? _serializer.DeserializeObject(res.GetResponseHeader("x-voc-extended")) : null
            };
        }

        /// <summary>
        /// Create a SimpleVoc value from a WebResponse for a given key asynchronously
        /// </summary>
        /// <param name="res"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private async Task<SimpleVocValue> GetValueFromResponseAsync(HttpWebResponse res, string key)
        {
            return new SimpleVocValue
            {
                Key = key,
                Data = await GetResponseDataAsync(res),
                Created = DateTime.Parse(res.GetResponseHeader("x-voc-created")),
                Expires = !string.IsNullOrWhiteSpace(res.GetResponseHeader("x-voc-expires")) ? DateTime.Parse(res.GetResponseHeader("x-voc-expires")) : DateTime.MinValue,
                Flags = int.Parse(res.GetResponseHeader("x-voc-flags")),
                Extended = !string.IsNullOrWhiteSpace(res.GetResponseHeader("x-voc-extended")) ? _serializer.DeserializeObject(res.GetResponseHeader("x-voc-extended")) : null
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
        /// Create a HttpWebRequest for a SET (POST) operation with given value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private HttpWebRequest CreateRequestForSet(SimpleVocValue value)
        {
            if (string.IsNullOrWhiteSpace(value.Key))
            {
                throw new ArgumentException("Key can't be empty!");
            }

            var req = CreateRequest("value/" + value.Key);
                req.Method = "POST"; // Set uses "POST"
                req.Headers.Add("x-voc-flags", value.Flags.ToString());
            
            if (value.Extended != null) {
                req.Headers.Add("x-voc-extended", _serializer.Serialize(value.Extended));
            }

            // Set expires header if given
            if (value.Expires != DateTime.MinValue)
            {
                // Convert to ISO 8601
                req.Headers.Add("x-voc-expires", value.Expires.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }

            return req;
        }

        /// <summary>
        /// Read data from response stream asynchronously
        /// </summary>
        /// <param name="res">The HttpWebResponse</param>
        /// <returns>Response as a string</returns>
        private async Task<string> GetResponseDataAsync(HttpWebResponse res)
        {
            using (var stream = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
            {
                return await stream.ReadToEndAsync();
            }
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
