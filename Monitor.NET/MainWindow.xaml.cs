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
using System.Threading;

namespace Monitor.NET
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void scan2(string start, string end)
        {
            try
            {

                //Split IP string into a 4 part array
                string[] startIPString = start.Split('.');
                int[] startIP = Array.ConvertAll<string, int>(startIPString, int.Parse); //Change string array to int array
                string[] endIPString = end.Split('.');
                int[] endIP = Array.ConvertAll<string, int>(endIPString, int.Parse);
                int count = 0; //Count the number of successful pings
                Ping myPing;
                PingReply reply;
                IPAddress addr;
                IPHostEntry host;

               

                //Loops through the IP range, maxing out at 255
                for (int i = startIP[2]; i <= endIP[2]; i++)
                { //3rd octet loop
                    for (int y = startIP[3]; y <= 255; y++)
                    { //4th octet loop
                        string ipAddress = startIP[0] + "." + startIP[1] + "." + i + "." + y; //Convert IP array back into a string
                        string endIPAddress = endIP[0] + "." + endIP[1] + "." + endIP[2] + "." + (endIP[3] + 1); // +1 is so that the scanning stops at the correct range

                        //If current IP matches final IP in range, break
                        if (ipAddress == endIPAddress)
                        {
                            break;
                        }

                        myPing = new Ping();
                        try
                        {
                            reply = myPing.Send(ipAddress, 500); //Ping IP address with 500ms timeout
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
                                addr = IPAddress.Parse(ipAddress);
                                host = Dns.GetHostEntry(addr);
                                Application.Current.Dispatcher.Invoke((Action)delegate {
                                    listVAddr.Items.Add(new cPartsOfIpAddress { sIP = ipAddress, sHostName = host.HostName, sState = "Up" }); //Log successful pings
                                });
                                
                                count++;
                            }
                            catch
                            {
                                Application.Current.Dispatcher.Invoke((Action)delegate {
                                    listVAddr.Items.Add(new cPartsOfIpAddress { sIP = ipAddress, sHostName = "Could not retrieve", sState = "Up" }); //Logs pings that are successful, but are most likely not windows machines
                                });
                                
                                count++;
                            }
                        }
                        else
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate {
                            listVAddr.Items.Add(new cPartsOfIpAddress { sIP = ipAddress, sHostName = "n/a", sState = "Down" }); //Log unsuccessful pings
                                
                            });
                        }
                    }

                    startIP[3] = 1; //If 4th octet reaches 255, reset back to 1
                }

                MessageBox.Show("Scanning done!\nFound " + count + " hosts.", "Done", MessageBoxButton.OK);
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
            //Create new thread for pinging
            //myThread = new Thread(() => scan(txtIP.Text));
            listVAddr.Items.Clear();
            int iPosition = txtNetworkAddress.Text.IndexOf("-");
            string sStart = txtNetworkAddress.Text.Substring(0, iPosition);
            string sEnd = txtNetworkAddress.Text.Substring(iPosition+1);
            Thread myThread = new Thread(() => scan2(sStart, sEnd));
                    myThread.Start();
        }
    }
}
