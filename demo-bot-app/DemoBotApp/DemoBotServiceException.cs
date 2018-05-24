namespace DemoBotApp
{
    using System;

    public class DemoBotServiceException : Exception
    {
        public DemoBotServiceException()
        {
        }

        public DemoBotServiceException(string message)
            : base(message)
        {
        }

        public DemoBotServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}