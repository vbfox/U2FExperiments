using System;
using System.Linq;
using BlackFox.U2F.Client;

namespace U2FExperiments
{
    internal class SimpleOriginVerifier : IOriginVerifier
    {
        private readonly string[] validOrigins;

        public SimpleOriginVerifier(string[] validOrigins)
        {
            this.validOrigins = validOrigins;
        }

        public void ValidateOrigin(string appId, string origin)
        {
            if (!validOrigins.Contains(origin))
            {
                throw new Exception("Unknown origin: " + origin);
            }
        }
    }
}