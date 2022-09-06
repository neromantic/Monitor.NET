using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.Forms.MessageBox;

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
        }

        private static string longToIP(long ip)
        {
            return ((ip >> 24) & 0xff).ToString() + '.' + ((ip >> 16) & 0xff).ToString() + '.' + ((ip >> 8) & 0xff).ToString() + '.' + (ip & 0xff).ToString();
        }

        private static long IPToLong(string ipAddress)
        {
            IPAddress ip;
            if (IPAddress.TryParse(ipAddress, out ip))
                return (((int)ip.GetAddressBytes()[0] << 24) | ((int)ip.GetAddressBytes()[1] << 16) | ((int)ip.GetAddressBytes()[2] << 8) | ip.GetAddressBytes()[3]);
            else return 0;
        }

        private List<String> getIPAddresses()
        {
            List<String> IPs = new List<String>();
            string input = "";

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                input = txtNetworkAddress.Text;
            });

            List<String> ipInputs = input.Split(',').ToList();

            foreach (String ipInput in ipInputs)
            {
                Match IPWithSubnetMatch = IPWithSubnetRegex.Match(ipInput);
                if (IPWithSubnetMatch.Success)
                {

                    string ip = IPWithSubnetMatch.Groups[1].Value;
                    int subnet = int.Parse(IPWithSubnetMatch.Groups[2].Value);

                    long IPStart = MainWindow.IPToLong(ip);
                    long IPEnd = IPStart | ((1 << (32 - subnet)) - 1);

                    while (IPStart < IPEnd)
                    {
                        IPs.Add(MainWindow.longToIP(IPStart));
                        IPStart++;
                    }
                }
                else if (IPrangeRegex.Match(ipInput).Success)
                {
                    Match mathces = IPWithSubnetRegex.Match(ipInput);
                    long IPStart = MainWindow.IPToLong(mathces.Groups[1].Value);
                    long IPEnd = MainWindow.IPToLong(mathces.Groups[2].Value);

                    while (IPStart < IPEnd)
                    {
                        IPs.Add(MainWindow.longToIP(IPStart));
                        IPStart++;
                    }
                }
                else if (singleIPRegex.Match(ipInput).Success)
                {
                    IPs.Add(ipInput);
                }
            }
            return IPs;
        }
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            try
            {


                int count = 0; //Count the number of successful pings
                Ping myPing;
                PingReply reply;
                IPAddress addr;
                IPHostEntry host;



                //Loops through the IP range, maxing out at 255
                foreach (string ip in getIPAddresses())
                {
                    myPing = new Ping();
                    try
                    {
                        reply = myPing.Send(ip, 500); //Ping IP address with 500ms timeout
                    }
                    catch (Exception)
                    {
                        break;
                    }


                    //Log pinged IP address in listview
                    //Grabs DNS information to obtain system info
                    if (reply.Status == IPStatus.Success)
                    {
                        try
                        {
                            addr = IPAddress.Parse(ip);
                            host = Dns.GetHostEntry(addr);
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                listVAddr.Items.Add(new cPartsOfIpAddress { sIP = ip, sHostName = host.HostName, sState = "Up" }); //Log successful pings
                            });

                            count++;
                        }
                        catch
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                listVAddr.Items.Add(new cPartsOfIpAddress { sIP = ip, sHostName = "Could not retrieve", sState = "Up" }); //Logs pings that are successful, but are most likely not windows machines
                            });

                            count++;
                        }
                    }
                    else
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            listVAddr.Items.Add(new cPartsOfIpAddress { sIP = ip, sHostName = "n/a", sState = "Down" }); //Log unsuccessful pings

                        });
                    }
                }

                MessageBox.Show("Scanning done!\nFound " + count + " hosts.", "Done", (MessageBoxButtons)MessageBoxButton.OK);
                //Catch exception that throws when stopping thread, caused by ping waiting to be acknowledged
            }
            catch (ThreadAbortException tex)
            {
                Console.WriteLine(tex.StackTrace);
            }
            //Catch invalid IP types
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
