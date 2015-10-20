using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using BlackFox.U2FHid;
using JetBrains.Annotations;
using IU2FKey = BlackFox.U2F.Key.IU2FKey;

namespace U2FExperiments
{
    class U2FDeviceKey : IU2FKey
    {
        private readonly U2FHidKey key;

        public U2FDeviceKey([NotNull] U2FHidKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            this.key = key;
        }

        public RegisterResponse Register(RegisterRequest registerRequest)
        {
            while (true)
            {
                var result = key.RegisterAsync(registerRequest).Result;

                switch (result.Status)
                {
                    case KeyResponseStatus.Success:
                        Debug.Assert(result.Data != null, "no data for success");
                        return result.Data;
                    case KeyResponseStatus.TestOfuserPresenceRequired:
                        Console.WriteLine("User presence required");
                        Thread.Sleep(100);
                        continue;
                    case KeyResponseStatus.Failure:
                        throw new U2FException("Failure");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest authenticateRequest)
        {
            while (true)
            {
                var result = key.AuthenticateAsync(authenticateRequest).Result;

                switch (result.Status)
                {
                    case KeyResponseStatus.Success:
                        Debug.Assert(result.Data != null, "no data for success");
                        return result.Data;
                    case KeyResponseStatus.TestOfuserPresenceRequired:
                        Console.WriteLine("User presence required");
                        Thread.Sleep(100);
                        continue;
                    case KeyResponseStatus.Failure:
                        throw new U2FException("Failure: " + result.Raw.Status);
                    case KeyResponseStatus.BadKeyHandle:
                        throw new U2FException("Bad key handle");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
