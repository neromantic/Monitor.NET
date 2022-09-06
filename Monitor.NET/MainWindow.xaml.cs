using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MessageBox = System.Windows.Forms.MessageBox;
using static System.Windows.Forms.AxHost;

namespace Monitor.NET
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<cPartsOfIpAddress> lvResult;
        Regex singleIPRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
        Regex IPWithSubnetRegex = new Regex(@"(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b)\s?\/(\b\d{1,3})");
        Regex IPrangeRegex = new Regex(@"(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b\s?-\s?(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");

    

        public MainWindow()
        {
            InitializeComponent();
            lvResult = new List<cPartsOfIpAddress>();
            lvListResult.ItemsSource = lvResult;
        }

        private static string longToIP(long ip)
        {
            return (ip >> 24).ToString() + '.' + ((ip >> 16) & 0xff).ToString() + '.' + ((ip >> 8) & 0xff).ToString() + '.' + (ip & 0xff).ToString();
        }

        private static long IPToLong(string ipAddress)
        {
            IPAddress ip;
            if (System.Net.IPAddress.TryParse(ipAddress, out ip))
                return (((long)ip.GetAddressBytes()[0] << 24) | ((int)ip.GetAddressBytes()[1] << 16) | ((int)ip.GetAddressBytes()[2] << 8) | ip.GetAddressBytes()[3]);
            else return 0;
        }

        private List<String> getIPAddresses()
        {
            List<String> IPs = new List<String>();
            string input = "";

            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate {
                input = txtNetworkAddress.Text;
            });
            
            List<String> ipInputs = input.Split(',').ToList();

            foreach(String ipInput in ipInputs)
            {
                
                if(IPWithSubnetRegex.Match(ipInput).Success)
                {
                    Match mathces = IPWithSubnetRegex.Match(ipInput);
                    string ip = mathces.Groups[0].Captures[0].Value;
                    int subnet = int.Parse(mathces.Groups[1].Value);

                    long IPStart = MainWindow.IPToLong(ip);
                    long IPEnd = IPStart | ((1 << (32 - subnet)) - 1);

                    foreach (int currentIp in Enumerable.Range(((int)IPStart), ((int)IPEnd)))
                    {
                        IPs.Add(MainWindow.longToIP(currentIp));
                    }
                } else if(IPrangeRegex.Match(ipInput).Success)
                {
                    Match mathces = IPWithSubnetRegex.Match(ipInput);
                    long IPStart = MainWindow.IPToLong(mathces.Groups[0].Value);
                    long IPEnd = MainWindow.IPToLong(mathces.Groups[1].Value);

                    foreach (int currentIp in Enumerable.Range(((int)IPStart), ((int)IPEnd)))
                    {
                        IPs.Add(MainWindow.longToIP(currentIp));
                    }
                } else if (singleIPRegex.Match(ipInput).Success)
                {
                    IPs.Add(ipInput);
                }
            }
            return IPs;
        }
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            lvResult.Clear();
            
            Task.Factory.StartNew(new Action(() =>
            {
            Ping ping = new Ping();
                foreach (string ip in getIPAddresses())
                {
                    PingReply reply = ping.Send(ip, 100);
                    if (reply.Status == IPStatus.Success)
                    {
                            try
                            {
                                IPHostEntry host = Dns.GetHostEntry(IPAddress.Parse(ip));
                                lvResult.Add(new cPartsOfIpAddress() { sIP = ip, sHostName = host.HostName, sState = "Up" });
                        }
                            catch
                            {
                                //System.Windows.Forms.MessageBox.Show($"Couldn't retrieve hostname from {ip}", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                    }
                    else
                    {
                            lvResult.Add(new cPartsOfIpAddress() { sIP = ip,sHostName = "",sState = "Down" });
                    }
                }
            })); Task.Factory.StartNew(new Action(() =>
            {
                foreach (string ip in getIPAddresses())
                {
                    Ping ping = new Ping();
                    PingReply reply = ping.Send(ip, 100);
                    if (reply.Status == IPStatus.Success)
                    {
                            try
                            {
                                IPHostEntry host = Dns.GetHostEntry(IPAddress.Parse(ip));
                                lvResult.Add(new cPartsOfIpAddress() { sIP = ip, sHostName = host.HostName, sState = "Up" });
                            }
                            catch
                            {
                                MessageBox.Show($"Couldn't retrieve hostname from {ip}", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                    }
                    else
                    {
                            lvResult.Add(new cPartsOfIpAddress() {sIP = ip,sHostName = "",sState =  "Down" });
                    }
                }
            }));
        }
    }
}
