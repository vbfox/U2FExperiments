using System;
using BlackFox.U2F.Gnubby.Simulated;
using BlackFox.U2F.Key;

namespace U2FExperiments
{
    class ConsolePresenceVerifier : IUserPresenceVerifier
    {
        public byte VerifyUserPresence()
        {
            Console.WriteLine("Are you present and agreeing to an operation with this virtual key ? (y/n)");
            var answer = Console.ReadLine().Trim();
            return answer == "y" || answer == "yes" ? UserPresenceVerifierConstants.UserPresentFlag : (byte) 0;
        }
    }
}
