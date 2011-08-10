using System;
using System.Net;

namespace SimpleVoc
{
    public struct SimpleVocValue
    {
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public int Flags { get; set; }
        public string Data { get; set; }
        public string Key { get; set; }
        public object Extended { get; set; }
    }
}