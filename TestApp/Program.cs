extern alias LoggingPcl;
extern alias LoggingNet4x;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.Binary;
using BlackFox.U2F;
using BlackFox.U2F.Client.impl;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.impl;
using BlackFox.U2F.Key.messages;
using BlackFox.U2F.Server.impl;
using BlackFox.U2F.Server.messages;
using BlackFox.U2F.Tests;
using BlackFox.U2FHid;
using BlackFox.U2FHid.Core;
using BlackFox.U2FHid.Core.RawPackets;
using BlackFox.UsbHid;
using BlackFox.UsbHid.Win32;
using BlackFox.Win32.Kernel32;
using Common.Logging.NLog;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;
using NodaTime;
using U2FExperiments.Tmp;

namespace U2FExperiments
{
    class Program
    {
        const byte TYPE_INIT = 0x80;

        const int FIDO_USAGE_DATA_IN = 0x20; // Raw IN data report
        const int FIDO_USAGE_DATA_OUT = 0x21; // Raw OUT data report
        const uint U2FHID_BROADCAST_CID = 0xffffffff;

        static void ShowDevices(ICollection<IHidDeviceInformation> deviceInfos)
        {
            foreach (var device in deviceInfos)
            {
                Console.WriteLine(" * {0}", device.Id);
                Console.WriteLine("   {0} {1} (VID=0x{2:X4}, PID=0x{3:X4}, SN={4})", device.Manufacturer, device.Product,
                    device.VendorId, device.ProductId, device.SerialNumber);
                if (device.IsFidoU2F()) Console.WriteLine("   FIDO Device !");
            }
        }

        static void ConfigureLogging()
        {
            ConfigureNLog();
            ConfigureCommonLogging();
        }

        static void ConfigureCommonLogging()
        {
            var nameValueCollection = new LoggingNet4x::Common.Logging.Configuration.NameValueCollection();
            var nlogAdapter = new NLogLoggerFactoryAdapter(nameValueCollection);
            LoggingNet4x::Common.Logging.LogManager.Adapter = nlogAdapter;
            LoggingPcl::Common.Logging.LogManager.Adapter = nlogAdapter;
        }

        static void ConfigureNLog()
        {
            const string logLayout =
                "[${date:format=HH\\:mm} ${logger}.${callsite:className=False:methodName=True}] "
                + "${message}${onexception:${newline}${exception:format=ToString}}";

            var consoleTarget = new ColoredConsoleTarget
            {
                Name = "Console",
                Layout = logLayout,
            };

            var rowHighlightingRules = new[]
            {
                new ConsoleRowHighlightingRule("level == LogLevel.Fatal", ConsoleOutputColor.White, ConsoleOutputColor.Red),
                new ConsoleRowHighlightingRule("level == LogLevel.Error", ConsoleOutputColor.Red, ConsoleOutputColor.NoChange),
                new ConsoleRowHighlightingRule("level == LogLevel.Warn", ConsoleOutputColor.Yellow, ConsoleOutputColor.NoChange),
                new ConsoleRowHighlightingRule("level == LogLevel.Info", ConsoleOutputColor.White, ConsoleOutputColor.NoChange),
                new ConsoleRowHighlightingRule("level == LogLevel.Debug", ConsoleOutputColor.Gray, ConsoleOutputColor.NoChange),
                new ConsoleRowHighlightingRule("level == LogLevel.Trace", ConsoleOutputColor.DarkGray,
                    ConsoleOutputColor.NoChange),
            };

            consoleTarget.RowHighlightingRules.Clear();
            foreach (var highlightingRule in rowHighlightingRules)
            {
                consoleTarget.RowHighlightingRules.Add(highlightingRule);
            }

            LogManager.Configuration = new LoggingConfiguration();

            LogManager.Configuration.AddTarget(consoleTarget);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, consoleTarget));
            LogManager.ReconfigExistingLoggers();
        }

        static void Main(string[] args)
        {
            ConfigureLogging();

            TestNew2().Wait();
            //TestDual().Wait();
            //TestSoftwareOnly();
            //TestHardwareOnly();
        }

        static readonly string dataStorePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dataStore.json");

        static void SaveDataStore(InMemoryServerDataStore dataStore)
        {
            using (var stream = File.OpenWrite(dataStorePath))
            {
                dataStore.SaveToStream(stream);
            }
        }

        static void LoadDataStore(InMemoryServerDataStore dataStore)
        {
            if (File.Exists(dataStorePath))
            {
                using (var stream = File.OpenRead(dataStorePath))
                {
                    dataStore.LoadFromStream(stream);
                }
            }
        }

        /// <summary>
        /// Continuously query all the keys connected for a signature, returning when either one key succeeded in signing or the request is cancelled.
        /// </summary>
        private class MultiSigner
        {
            private static readonly ILogger log = LogManager.GetCurrentClassLogger();
            private static readonly TimeSpan timeBetweenKeyScans = TimeSpan.FromSeconds(1);
            private readonly IKeyFactory keyFactory;
            private readonly bool forEnroll;
            private readonly List<AuthenticateRequest> requests;

            private struct SingleSignerInfo
            {
                public IKeyId KeyId { get; }
                public SingleSigner SingleSigner { get; }
                public Task<SignerResult> Task { get; }

                public SingleSignerInfo(IKeyId keyId, SingleSigner singleSigner, Task<SignerResult> task)
                {
                    KeyId = keyId;
                    SingleSigner = singleSigner;
                    Task = task;
                }
            }

            public MultiSigner(IKeyFactory keyFactory, bool forEnroll, List<AuthenticateRequest> requests)
            {
                this.keyFactory = keyFactory;
                this.forEnroll = forEnroll;
                this.requests = requests;
            }

            readonly ConcurrentDictionary<IKeyId, SingleSignerInfo> signers = new ConcurrentDictionary<IKeyId, SingleSignerInfo>();

            [NotNull]
            [ItemCanBeNull]
            public async Task<SignerResult> Sign(CancellationToken cancellationToken)
            {
                signers.Clear();
                var childsCancellation = new CancellationTokenSource();
                var linkedChildCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                    childsCancellation.Token);
                var tcs = new TaskCompletionSource<SignerResult>();
                var detectionLoop = NewSignersDetectionLoopAsync(tcs, linkedChildCancellation.Token);
                try
                {
                    var finishedTask = await Task.WhenAny(tcs.Task, detectionLoop);
                    if (finishedTask == tcs.Task)
                    {
                        return tcs.Task.Result;
                    }
                    else
                    {
                        return SignerResult.Failure();
                    }
                }
                finally
                {
                    childsCancellation.Cancel();
                }
            }

            private async Task NewSignersDetectionLoopAsync(TaskCompletionSource<SignerResult> tcs, CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await AddNewSignersAsync(tcs, cancellationToken);
                    await Task.Delay(timeBetweenKeyScans, cancellationToken);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            private async Task AddNewSignersAsync(TaskCompletionSource<SignerResult> tcs, CancellationToken cancellationToken)
            {
                var allKeys = await keyFactory.FindAllAsync(cancellationToken);
                var newKeys = allKeys.Where(k => !signers.ContainsKey(k)).ToList();
                if (newKeys.Count > 0)
                {
                    log.Info($"Found {newKeys.Count} new keys connected");

                    foreach (var key in newKeys)
                    {
                        signers.AddOrUpdate(key, k => StartSingleSigner(k, tcs, cancellationToken), (k, v) => v);
                    }
                }
            }

            private SingleSignerInfo StartSingleSigner(IKeyId keyId, TaskCompletionSource<SignerResult> tcs, CancellationToken cancellationToken)
            {
                var singleSigner = new SingleSigner(forEnroll, keyId, requests);
                var task = singleSigner.Sign(cancellationToken);
                task.ContinueWith(t =>
                {
                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            if (t.Result.IsSuccess)
                            {
                                tcs.TrySetResult(t.Result);
                            }

                            // If the result is null it mean that there is no way that it can sign any of the requests,
                            // so we  remember it so that we won't query it again.
                            break;
                        case TaskStatus.Faulted:
                            // Will be re-discovered next iteration
                            ForgetSigner(keyId);
                            break;
                    }
                }, cancellationToken);
                return new SingleSignerInfo(keyId, singleSigner, task);
            }

            private void ForgetSigner(IKeyId keyId)
            {
                SingleSignerInfo signerInfo;
                signers.TryRemove(keyId, out signerInfo);
            }
        }

        private interface ISender
        {
            string Origin { get; }
            JObject ChannelId { get;}
        }

        private class MyClient
        {
            public delegate Task<bool> CheckAppIdOrOrigin(string origin, ICollection<string> appIds, CancellationToken cancellationToken);

            [NotNull] private readonly ISender sender;

            /// <summary>
            /// Check that an origin should be allowed to claim all the specified AppIds.
            /// </summary>
            /// <remarks>
            /// Should check if the eTLD+1 is the same (google.com) or if there is a redirection in place with the header :
            /// FIDO-AppID-Redirect-Authorized
            /// </remarks>
            [NotNull] private readonly CheckAppIdOrOrigin originChecker;
            /// <summary>
            /// Check that an origin accept the specified appids using the 'trustedFacets' in a JSON document
            /// </summary>
            [NotNull] private readonly CheckAppIdOrOrigin appIdChecker;

            [NotNull] private readonly IKeyFactory keyFactory;

            public MyClient([NotNull] ISender sender, [CanBeNull] CheckAppIdOrOrigin originChecker,
                [CanBeNull] CheckAppIdOrOrigin appIdChecker, [NotNull] IKeyFactory keyFactory)
            {
                this.sender = sender;
                this.originChecker = originChecker ?? ((o, a, ct) => Task.FromResult(true));
                this.appIdChecker = appIdChecker ?? ((o, a, ct) => Task.FromResult(true));
                this.keyFactory = keyFactory;
            }

            private struct SignInfo
            {
                public string ClientDataBase64 { get; }
                public AuthenticateRequest AuthenticateRequest { get; }
                public SignRequest SignRequest { get; }

                public SignInfo(SignRequest signRequest, string clientDataBase64, AuthenticateRequest authenticateRequest)
                {
                    ClientDataBase64 = clientDataBase64;
                    AuthenticateRequest = authenticateRequest;
                    SignRequest = signRequest;
                }
            }

            private SignInfo GenerateSignInfo(SignRequest signRequest)
            {
                string clientDataB64;
                var authRequest = U2FClientReferenceImpl.SignRequestToAuthenticateRequest(sender.Origin,
                    signRequest, sender.ChannelId,
                    out clientDataB64, new BouncyCastleClientCrypto());
                return new SignInfo(signRequest, clientDataB64, authRequest);
            }

            public async Task<SignResponse> Sign(ICollection<SignRequest> requests, CancellationToken cancellationToken)
            {
                var appIds = requests.Select(r => r.AppId).Distinct().ToList();
                var originsOk = await originChecker(sender.Origin, appIds, cancellationToken);
                if (!originsOk)
                {
                    throw new Exception("Bad origins for appid");
                }

                var appIdsOk = await appIdChecker(sender.Origin, appIds, cancellationToken);
                if (!appIdsOk)
                {
                    throw new Exception("App IDs not allowed by this origin");
                }

                var signInfos = requests.Select(GenerateSignInfo).ToDictionary(s => s.AuthenticateRequest);
                var signer = new MultiSigner(keyFactory, false, signInfos.Keys.ToList());
                var result = await signer.Sign(cancellationToken);
                if (!result.IsSuccess)
                {
                    return null;
                }

                var signInfo = signInfos[result.Request];

                return new SignResponse(
                    WebSafeBase64Converter.ToBase64String(signInfo.ClientDataBase64),
                    WebSafeBase64Converter.ToBase64String(RawMessageCodec.EncodeAuthenticateResponse(result.Response)),
                    signInfo.SignRequest.Challenge,
                    signInfo.SignRequest.SessionId,
                    signInfo.SignRequest.AppId);
            }
        }

        private class DummySender : ISender
        {
            public string Origin { get; }
            public JObject ChannelId { get; }

            public DummySender(string origin, JObject channelId)
            {
                Origin = origin;
                ChannelId = channelId;
            }
        }

        private static async Task TestNew2()
        {
            var hidFactory = Win32HidDeviceFactory.Instance;
            var keyFactory = new U2FHidKeyFactory(hidFactory);

            var dataStore = new InMemoryServerDataStore(new GuidSessionIdGenerator());
            LoadDataStore(dataStore);
            var server = new U2FServerReferenceImpl(
                new ChallengeGenerator(),
                dataStore,
                new BouncyCastleServerCrypto(),
                new[] {"http://example.com", "https://example.com"});

            var myClient = new MyClient(
                new DummySender("http://example.com", new JObject()),
                (o, a, ct) => Task.FromResult(true),
                (o, a, ct) => Task.FromResult(true),
                keyFactory);

            var signRequests = server.GetSignRequests("vbfox", "http://example.com");

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var x = await myClient.Sign(signRequests, cts.Token);

            Console.WriteLine("Signature done {0}", x);
            if (x != null)
            {
                var serverResp = server.ProcessSignResponse(x);
                Console.WriteLine("Server ok: {0}", serverResp);
            }

            
            Console.ReadLine();
            return;
        }

        private static async Task TestNew()
        {
            var hidFactory = Win32HidDeviceFactory.Instance;
            var keyFactory = new U2FHidKeyFactory(hidFactory);
            var keyIds = await keyFactory.FindAllAsync();
            var keyId = keyIds.First();

            var dataStore = new InMemoryServerDataStore(new GuidSessionIdGenerator());
            LoadDataStore(dataStore);
            var server = new U2FServerReferenceImpl(
                new ChallengeGenerator(),
                dataStore,
                new BouncyCastleServerCrypto(),
                new[] { "http://example.com", "https://example.com" });

            var myClient = new MyClient(
                new DummySender("http://example.com", new JObject()),
                (o, a, ct) => Task.FromResult(true),
                (o, a, ct) => Task.FromResult(true),
                keyFactory);

            var signRequests = server.GetSignRequests("vbfox", "http://example.com");

            var x = await myClient.Sign(signRequests, CancellationToken.None);
            return;


            var origin = "http://example.com";
            var channelId = new JObject();

            

            var requestAndClientDatas = signRequests
                .Select(signRequest =>
                {
                    string clientDataB64;
                    var authRequest = U2FClientReferenceImpl.SignRequestToAuthenticateRequest("http://example.com", signRequest, new JObject(),
                        out clientDataB64, new BouncyCastleClientCrypto());
                    return Tuple.Create(signRequest, clientDataB64, authRequest);
                })
                .ToList();

            new MyClient(
                new DummySender("http://example.com", new JObject()),
                (o, a, ct) => Task.FromResult(true),
                (o, a, ct) => Task.FromResult(true),
                keyFactory);



            using (var u2f = await keyId.OpenAsync())
            {
                var key = new U2FDeviceKey(u2f);



                var client = new U2FClientReferenceImpl(
                    new BouncyCastleClientCrypto(),
                    new SimpleOriginVerifier(new[] { "http://example.com", "https://example.com" }),
                    new ChannelProvider(),
                    server,
                    key,
                    SystemClock.Instance);

                
                //client.Register("http://example.com", "vbfox");
                //SaveDataStore(dataStore);

                client.Authenticate("http://example.com", "vbfox");
                SaveDataStore(dataStore);
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static async Task TestDual()
        {
            var hidFactory = Win32HidDeviceFactory.Instance;
            var keyFactory = new U2FHidKeyFactory(hidFactory);
            var keyIds = await keyFactory.FindAllAsync();
            var keyId = keyIds.First();
            using (var u2f = await keyId.OpenAsync())
            {
                var key = new U2FDeviceKey(u2f);

                var dataStore = new InMemoryServerDataStore(new GuidSessionIdGenerator());
                var server = new U2FServerReferenceImpl(
                    new ChallengeGenerator(),
                    dataStore,
                    new BouncyCastleServerCrypto(),
                    new[] {"http://example.com", "https://example.com"});

                var client = new U2FClientReferenceImpl(
                    new BouncyCastleClientCrypto(),
                    new SimpleOriginVerifier(new[] {"http://example.com", "https://example.com"}),
                    new ChannelProvider(),
                    server,
                    key,
                    SystemClock.Instance);

                LoadDataStore(dataStore);
                //client.Register("http://example.com", "vbfox");
                //SaveDataStore(dataStore);

                client.Authenticate("http://example.com", "vbfox");
                SaveDataStore(dataStore);
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static void TestSoftwareOnly()
        {
            var key = new U2FKeyReferenceImpl(
                TestVectors.VENDOR_CERTIFICATE,
                TestVectors.VENDOR_CERTIFICATE_PRIVATE_KEY,
                new TestKeyPairGenerator(),
                new GuidKeyHandleGenerator(),
                new InMemoryKeyDataStore(),
                new ConsolePresenceVerifier(),
                new BouncyCastleCrypto());

            var server = new U2FServerReferenceImpl(
                new ChallengeGenerator(),
                new InMemoryServerDataStore(new GuidSessionIdGenerator()),
                new BouncyCastleServerCrypto(),
                new [] { "http://example.com", "https://example.com" });

            var client = new U2FClientReferenceImpl(
                new BouncyCastleClientCrypto(),
                new SimpleOriginVerifier(new[] {"http://example.com", "https://example.com"}),
                new ChannelProvider(),
                server,
                key,
                SystemClock.Instance);

            client.Register("http://example.com", "vbfox");
            client.Authenticate("http://example.com", "vbfox");
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static void TestHardwareOnly()
        {
            var factory = (IHidDeviceFactory) Win32HidDeviceFactory.Instance;
            var devices = factory.FindAllAsync().Result;
            var fidoInfo = devices.Where(FidoU2FIdentification.IsFidoU2F).FirstOrDefault();

            Console.WriteLine("Devices found:");
            ShowDevices(devices);
            Console.WriteLine();

            if (fidoInfo == null)
            {
                Console.WriteLine("Can't find FIDO device :-(");
                Console.ReadLine();
                return;
            }

            Console.WriteLine(fidoInfo.Id);
            Console.WriteLine(fidoInfo.Manufacturer);
            Console.WriteLine(fidoInfo.Product);
            Console.WriteLine(fidoInfo.SerialNumber);
            Console.WriteLine("VID = 0x{0:X4}", fidoInfo.VendorId);
            Console.WriteLine("PID = 0x{0:X4}", fidoInfo.ProductId);

            using (var device = (Win32HidDevice) fidoInfo.OpenDeviceAsync().Result)
            {
                device.SetNumInputBuffers(64);
                var caps = device.Information.Capabilities;
                Console.WriteLine(caps.NumberFeatureButtonCaps);

                //Test(device);

                Console.WriteLine("Using high level API");

                var u2f = new U2FHidKey(device, false);

                var init = u2f.InitAsync().Result;

                var pongShort = u2f.Ping(Encoding.UTF8.GetBytes("Pong !!").Segment()).Result;
                WriteBuffer(pongShort);

                var pong =
                    u2f.Ping(
                        Encoding.UTF8.GetBytes(
                            "abcdefgh1-abcdefgh2-abcdefgh3-abcdefgh4-abcdefgh5-abcdefgh6-abcdefgh7-abcdefgh8-").Segment())
                        .Result;

                WriteBuffer(pong);

                if (init.Capabilities.HasFlag(U2FDeviceCapabilities.Wink))
                {
                    Console.WriteLine("Winking");
                    u2f.WinkAsync().Wait();
                }
                Console.ReadLine();
            }
        }

        static unsafe void Test(Win32HidDevice device)
        {
            var init = new InitializationPacket();
            init.CommandIdentifier = (byte)U2FHidCommand.Init;
            init.ChannelIdentifier = U2FHID_BROADCAST_CID;
            init.PayloadLength = 8;
            var caps = device.Information.Capabilities;

            var buffer = new byte[caps.InputReportByteLength];

            fixed (byte* pBuffer = buffer)
            {
                Marshal.StructureToPtr(init, new IntPtr(pBuffer + 1), false);

                buffer[0] = 0x00;
                buffer[8] = 0xCA;
                buffer[9] = 0xFE;
                buffer[10] = 0xBA;
                buffer[11] = 0xBE;
                buffer[12] = 0xDE;
                buffer[13] = 0xAD;
                buffer[14] = 0xBA;
                buffer[15] = 0xBE;

                WriteBuffer(buffer);

                var task = Kernel32Dll.WriteFileAsync(device.Handle, new IntPtr(pBuffer), buffer.Length);
                var writen = task.Result;
                Console.WriteLine("Writen {0} bytes", writen);
            }

            var bufferOut = new byte[caps.OutputReportByteLength];
            fixed (byte* pBuffer = bufferOut)
            {
                var intPtr = new IntPtr(pBuffer);
                var task = Kernel32Dll.ReadFileAsync(device.Handle, intPtr, bufferOut.Length);
                var read = task.Result;
                Console.WriteLine("Read {0} bytes", read);
            }

            WriteBuffer(bufferOut);

            Wink(device, bufferOut[16], bufferOut[17], bufferOut[18], bufferOut[19]);
        }

        public static void WriteBuffer(byte[] array)
        {
            WriteBuffer(new ArraySegment<byte>(array));
        }

        public static void WriteBuffer(ArraySegment<byte> segment)
        {
            segment.WriteAsHexTo(Console.Out, true);
        }

        static unsafe void Wink(Win32HidDevice device, byte b1, byte b2, byte b3, byte b4)
        {
            var msg = new FidoU2FHidMessage(
                (uint)(unchecked (b1 << 24 | b2 << 16 | b3 << 8 | b4)),
                U2FHidCommand.Wink);
            device.WriteFidoU2FHidMessageAsync(msg, CancellationToken.None).Wait();

            var caps = device.Information.Capabilities;

            var bufferOut = new byte[caps.OutputReportByteLength];
            fixed (byte* pBuffer = bufferOut)
            {
                var intPtr = new IntPtr(pBuffer);
                var task = Kernel32Dll.ReadFileAsync(device.Handle, intPtr, bufferOut.Length);
                var read = task.Result;
                Console.WriteLine("Read {0} bytes", read);
            }

            WriteBuffer(bufferOut);
        }
    }
}
