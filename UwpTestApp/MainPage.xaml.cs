﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Text;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.GnubbyApi;
using BlackFox.U2F.Server;
using BlackFox.U2F.Server.impl;
using BlackFox.U2FHid;
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
        }

        static readonly Guid guidDevinterfaceHid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        readonly string usbHid =
            @"System.Devices.InterfaceClassGuid:=""{4D1E55B2-F16F-11CF-88CB-001111000030}"" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

        async void RunClicked(object sender, RoutedEventArgs e)
        {
            textBox.Text = "";
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
            textBox.Text = sb.ToString();
        }

        async void U2fClicked(object sender, RoutedEventArgs e)
        {
            textBox.Text = "";
            var hidFactory = UwpFactory.Instance;
            var u2FFactory = new U2FHidKeyFactory(hidFactory);
            var u2FDevices = await u2FFactory.FindAllAsync();
            
            var sb = new StringBuilder();
            foreach (var device in u2FDevices)
            {
                sb.AppendLine($"{device.Product} (By {device.Manufacturer})");
            }
            textBox.Text = sb.ToString();
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

        async void EnrollClicked(object sender, RoutedEventArgs e)
        {
            var keyDict = new ConcurrentDictionary<IKeyId, bool>();
            textBox.Text = "";
            var hidFactory = UwpFactory.Instance;
            var keyFactory = new U2FHidKeyFactory(hidFactory);

            var dataStore = new InMemoryServerDataStore(new GuidSessionIdGenerator());
            var server = new U2FServerReferenceImpl(
                new ChallengeGenerator(),
                dataStore,
                new BouncyCastleServerCrypto(),
                new[] { "http://example.com", "https://example.com" });

            var myClient = new GnubbyApiClient(
                new DummySender("http://example.com", new JObject()),
                keyFactory);

            var signRequests = server.GetSignRequests("vbfox", "http://example.com");
            var regRequest = server.GetRegistrationRequest("vbfox", "http://example.com");
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            textBox.Text += "Register...\r\n";
            var x = await myClient.Register(new[] { regRequest }, signRequests, cts.Token);
            textBox.Text += "Register done, sending to server\r\n";

            var serverResp = server.ProcessRegistrationResponse(x, ToUnixTimeMilliseconds(SystemClock.Instance.Now));
            textBox.Text += "Server OK\r\n";
            textBox.Text += $"{serverResp}\r\n";
        }

        public static long ToUnixTimeMilliseconds(Instant instant)
        {
            return instant.Ticks / NodaConstants.TicksPerMillisecond;
        }
    }
}
