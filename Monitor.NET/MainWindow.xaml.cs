﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
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
        [DllImport("Iphlpapi.dll")]
        private static extern int SendARP(Int32 dest, Int32 host, ref Int64 mac, ref Int32 length);
        [DllImport("Ws2_32.dll")]
        private static extern Int32 inet_addr(string ip);

        bool bScanning = false;
        Thread myThread;

        Regex singleIPRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
        Regex IPWithSubnetRegex = new Regex(@"(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b)\s?\/(\b\d{1,3})");
        Regex IPrangeRegex = new Regex(@"(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b\s?-\s?(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");



        public MainWindow()
        {
            InitializeComponent();
            myThread = new Thread(() => PerformPing());
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
        private void PerformPing()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                listVAddr.Items.Clear();
            });

            try
            {


                int count = 0; //Count the number of successful pings
                Ping myPing;
                PingReply reply;
                IPAddress addr;
                IPHostEntry host;
                string sMac;


                //Loops through the IP range, maxing out at 255
                foreach (string ip in getIPAddresses())
                {
                    myPing = new Ping();
                    try
                    {
                        reply = myPing.Send(ip, 100); //Ping IP address with 500ms timeout
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
                            sMac = GetClientMAC(ip);
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                listVAddr.Items.Add(new cPartsOfIpAddress { IP = ip, MAC = sMac }); //Log successful pings
                            });

                            count++;
                        }
                        catch
                        {
                            sMac = GetClientMAC(ip);
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                listVAddr.Items.Add(new cPartsOfIpAddress { IP = ip, MAC = sMac }); //Logs pings that are successful, but are most likely not windows machines
                            });

                            count++;
                        }
                    }
                    else
                    {
                        sMac = GetClientMAC(ip);
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            listVAddr.Items.Add(new cPartsOfIpAddress { IP = ip, MAC = sMac }); //Log unsuccessful pings

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
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            if (bScanning == true)
            {
                myThread.Abort();
                btnScan.Content = "Scannen";
                bScanning = false;
            }
            else
            {
                myThread = new Thread(() => PerformPing());
                myThread.Start();
                btnScan.Content = "Abbrechen";
                bScanning = true;
            }
        }
        private static string GetClientMAC(string strClientIP)
        {
            string mac_dest = "";
            try
            {
                Int32 ldest = inet_addr(strClientIP);
                Int32 lhost = inet_addr("");
                Int64 macinfo = new Int64();
                Int32 len = 6;
                int res = SendARP(ldest, 0, ref macinfo, ref len);
                string mac_src = macinfo.ToString("X");

                while (mac_src.Length < 12)
                {
                    mac_src = mac_src.Insert(0, "0");
                }

                for (int i = 0; i < 11; i++)
                {
                    if (0 == (i % 2))
                    {
                        if (i == 10)
                        {
                            mac_dest = mac_dest.Insert(0, mac_src.Substring(i, 2));
                        }
                        else
                        {
                            mac_dest = "-" + mac_dest.Insert(0, mac_src.Substring(i, 2));
                        }
                    }
                }
            }
            catch (Exception err)
            {
                throw new Exception("L?i " + err.Message);
            }
            return mac_dest;
        }
    }
}