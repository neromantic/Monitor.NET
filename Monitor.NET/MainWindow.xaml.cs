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
        string sNetAddress;
        public MainWindow()
        {
            InitializeComponent();
            lvResult = new List<cPartsOfIpAddress>();
            lvListResult.ItemsSource = lvResult;
        }

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            lvResult.Clear();
            sNetAddress = txtNetworkAddress.Text;
            Task.Factory.StartNew(new Action(() =>
            {
            Ping ping = new Ping();
                for (int i = 1; i < 255; i++)
                {
                    string ip = $"{sNetAddress}.{i}";
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
                for (int i = 2; i < 255; i++)
                {
                    string ip = $"{sNetAddress}.{i}";
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
