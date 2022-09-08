using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Monitor.NET
{
    internal class IPAddressResolver
    {
        static Regex SingleIPRegex = new Regex(@"^\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b$");
        static Regex IPWithSubnetRegex = new Regex(@"^(\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b)\s*?\/\s*?\b(\d{1,3})$");
        static Regex IPRangeRegex = new Regex(@"^\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s*?-\s*?\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})$");

        public static string LongToIP(long ip)
        {
            return ((ip >> 24) & 0xff).ToString() + '.' + ((ip >> 16) & 0xff).ToString() + '.' + ((ip >> 8) & 0xff).ToString() + '.' + (ip & 0xff).ToString();
        }

        public static long IPToLong(string ipAddress)
        {
            IPAddress ip;
            if (IPAddress.TryParse(ipAddress, out ip))
                return (((int)ip.GetAddressBytes()[0] << 24) | ((int)ip.GetAddressBytes()[1] << 16) | ((int)ip.GetAddressBytes()[2] << 8) | ip.GetAddressBytes()[3]);
            else return 0;
        }

        public static List<string> Resolve(string IPArea)
        {
            List<string> IPs = new List<string>();
            Match IPMatch;

            IPMatch = IPWithSubnetRegex.Match(IPArea);

            if (IPMatch.Success)
            {
                string ip = IPMatch.Groups[1].Value;
                int subnet = int.Parse(IPMatch.Groups[2].Value);

                long IPStart = IPToLong(ip);
                long IPEnd = IPStart | ((1 << (32 - subnet)) - 1);

                while (IPStart < IPEnd)
                {
                    IPs.Add(LongToIP(IPStart));
                    IPStart++;
                }
                return IPs;
            }

            IPMatch = IPRangeRegex.Match(IPArea);
            if (IPMatch.Success)
            {
                long IPStart = IPToLong(IPMatch.Groups[1].Value);
                long IPEnd = IPToLong(IPMatch.Groups[2].Value);

                while (IPStart <= IPEnd)
                {
                    IPs.Add(LongToIP(IPStart));
                    IPStart++;
                }
                return IPs;
            }
            IPMatch = SingleIPRegex.Match(IPArea);
            if (IPMatch.Success)
            {
                IPs.Add(IPArea);
                return IPs;
            }
            throw new Exception("IP Area invalid");
        }
    }
}
