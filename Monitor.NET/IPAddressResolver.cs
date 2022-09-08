using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Monitor.NET
{
    internal class IPAddressResolver
    {
        Regex singleIPRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
        Regex IPWithSubnetRegex = new Regex(@"(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b)\s?\/(\b\d{1,3})");
        Regex IPrangeRegex = new Regex(@"(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b\s?-\s?(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");

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

        public List<string> Resolve(string IPArea)
        {
            List<String> IPs = new List<String>();

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
    }
}
