using System;
using System.Diagnostics;
using System.Threading;
using BlackFox.U2F;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;
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

        public KeyRegisterResponse Register(KeyRegisterRequest keyRegisterRequest)
        {
            while (true)
            {
                var result = key.RegisterAsync(keyRegisterRequest).Result;

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

        public KeySignResponse Authenticate(KeySignRequest keySignRequest)
        {
            while (true)
            {
                var result = key.SignAsync(keySignRequest).Result;

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
