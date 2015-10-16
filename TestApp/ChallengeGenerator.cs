using System.Linq;
using System.Text;
using BlackFox.U2F.Server;
using Org.BouncyCastle.Security;

namespace U2FExperiments
{
    internal class ChallengeGenerator : IChallengeGenerator
    {
        public byte[] GenerateChallenge(string accountName)
        {
            var randomData = new byte[64];
            new SecureRandom().NextBytes(randomData);
            return randomData.Concat(Encoding.UTF8.GetBytes(accountName)).ToArray();
        }
    }
}