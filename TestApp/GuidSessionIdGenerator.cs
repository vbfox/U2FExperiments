using System;
using BlackFox.U2F.Server;

namespace U2FExperiments
{
    class GuidSessionIdGenerator : ISessionIdGenerator
    {
        public string GenerateSessionId(string accountName)
        {
            return $"{Guid.NewGuid()} - {accountName}";
        }
    }
}
