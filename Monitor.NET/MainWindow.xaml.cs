using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
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





        public MainWindow()
        {
            InitializeComponent();
        }



        private List<string> getIPAddresses()
        {
            List<string> IPs = new List<string>();
            string input = "";

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                input = txtNetworkAddress.Text;
            });

            List<string> ipInputs = input.Split(',').ToList();

            foreach (string ipInput in ipInputs)
            {
                IPs.AddRange(IPAddressResolver.Resolve(ipInput));
            }
            return IPs.Distinct().ToList();
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
            Thread myThread = new Thread(() => PerformPing());
            myThread.Start();
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
