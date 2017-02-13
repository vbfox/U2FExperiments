using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.GnubbyApi;
using BlackFox.U2F.Server;
using BlackFox.U2F.Server.impl;
using BlackFox.U2FHid;
using BlackFox.UsbHid.Uwp;
using Newtonsoft.Json.Linq;
using NodaTime;
using Org.BouncyCastle.Security;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UwpTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            keyFactory = new U2FHidKeyFactory(hidFactory);
            storePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "store.json");
            dataStore = new JsonFileDataStore(new GuidSessionIdGenerator(), storePath);
        }

        static readonly Guid guidDevinterfaceHid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        readonly string usbHid =
            @"System.Devices.InterfaceClassGuid:=""{4D1E55B2-F16F-11CF-88CB-001111000030}"" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

        async void RunClicked(object sender, RoutedEventArgs e)
        {
            HidtextBox.Text = "";
            var all = await DeviceInformation.FindAllAsync(usbHid,
                new [] {
                    "System.Devices.Manufacturer",
                    "System.Devices.ModelName",
                    "System.Devices.DeviceManufacturer",
                    "System.DeviceInterface.Hid.UsagePage",
                    "System.DeviceInterface.Hid.UsageId",
                    "System.DeviceInterface.Hid.VendorId",
                    "System.DeviceInterface.Hid.ProductId"
                }, DeviceInformationKind.DeviceInterface);
            var sb = new StringBuilder();
            foreach (var device in all)
            {
                sb.AppendLine($"{device.Id} Name={device.Name}");
                foreach (var p in device.Properties)
                {
                    sb.AppendLine($"\t {p.Key} = {p.Value}");
                }
            }
            HidtextBox.Text = sb.ToString();
        }

        async void U2fClicked(object sender, RoutedEventArgs e)
        {
            U2FtextBox.Text = "";
            var u2FDevices = await keyFactory.FindAllAsync();
            
            var sb = new StringBuilder();
            foreach (var device in u2FDevices)
            {
                sb.AppendLine($"{device.Product} (By {device.Manufacturer})");
                sb.AppendLine($"\tId={device.HidDeviceInformation.Id}");
                sb.AppendLine($"\tManufacturer={device.HidDeviceInformation.Manufacturer}");
                sb.AppendLine($"\tProduct={device.HidDeviceInformation.Product}");
                sb.AppendLine($"\tProductId={device.HidDeviceInformation.ProductId}");
                sb.AppendLine($"\tSerialNumber={device.HidDeviceInformation.SerialNumber}");
                sb.AppendLine($"\tUsageId={device.HidDeviceInformation.UsageId}");
                sb.AppendLine($"\tUsagePage={device.HidDeviceInformation.UsagePage}");
                sb.AppendLine($"\tVendorId={device.HidDeviceInformation.VendorId}");
                sb.AppendLine($"\tVersion={device.HidDeviceInformation.Version}");
            }
            U2FtextBox.Text = sb.ToString();
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

        internal class ChallengeGenerator : IChallengeGenerator
        {
            public byte[] GenerateChallenge(string accountName)
            {
                var randomData = new byte[64];
                new SecureRandom().NextBytes(randomData);
                return randomData.Concat(Encoding.UTF8.GetBytes(accountName)).ToArray();
            }
        }

        class GuidSessionIdGenerator : ISessionIdGenerator
        {
            public string GenerateSessionId(string accountName)
            {
                return $"{Guid.NewGuid()} - {accountName}";
            }
        }

        private readonly UwpHidDeviceFactory hidFactory = new UwpHidDeviceFactory();
        private readonly U2FHidKeyFactory keyFactory;
        private JsonFileDataStore dataStore;
        private readonly string storePath;

        async void EnrollClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                ActionstextBox.Text = "";
                var server = new U2FServerReferenceImpl(
                    new ChallengeGenerator(),
                    dataStore,
                    new BouncyCastleServerCrypto(),
                    new[] {"http://example.com", "https://example.com"});

                var myClient = new U2FClient(
                    new DummySender("http://example.com", new JObject()),
                    keyFactory);

                var signRequests = server.GetSignRequests(EnrollUserName.Text, EnrollAppId.Text);
                var regRequest = server.GetRegistrationRequest(EnrollUserName.Text, EnrollAppId.Text);
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

                ActionstextBox.Text += "Register...\r\n";
                var x = await myClient.Register(new[] {regRequest}, signRequests, cts.Token);
                ActionstextBox.Text += "Register done, sending to server\r\n";

                var serverResp = server.ProcessRegistrationResponse(x, ToUnixTimeMilliseconds(SystemClock.Instance.Now));
                ActionstextBox.Text += "Server OK\r\n";
                ActionstextBox.Text += $"{serverResp}\r\n";
            }
            catch (Exception exception)
            {
                ActionstextBox.Text += "\r\n\r\n" + exception.ToString();
            }
        }

        public static long ToUnixTimeMilliseconds(Instant instant)
        {
            return instant.Ticks / NodaConstants.TicksPerMillisecond;
        }

        private void ClearClicked(object sender, RoutedEventArgs e)
        {
            File.Delete(storePath);
            dataStore = new JsonFileDataStore(new GuidSessionIdGenerator(), storePath);
        }
    }
}
