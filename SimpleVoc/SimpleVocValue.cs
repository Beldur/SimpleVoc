using System;
using System.Net;
using System.Collections.Generic;

namespace SimpleVoc
{
    public struct SimpleVocValue
    {
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public int Flags { get; set; }
        public string Data { get; set; }
        public string Key { get; set; }
        public Dictionary<string, string> Extended { get; set; }
    }
}