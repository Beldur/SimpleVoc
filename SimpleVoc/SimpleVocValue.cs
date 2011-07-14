using System;

namespace SimpleVoc
{
    public struct SimpleVocValue
    {
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public string Flags { get; set; }
        public string Data { get; set; }
        public string Key { get; set; }
    }
}