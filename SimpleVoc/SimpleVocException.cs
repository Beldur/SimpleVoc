using System;

namespace SimpleVoc
{
    public class SimpleVocException : Exception
    {
        public SimpleVocException(string message) : base(message)
        {
            
        }

        public SimpleVocException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}