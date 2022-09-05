using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Monitor.NET
{
    internal class cPartsOfIpAddress : ListViewItem
    {
        public string sIP{ get; set; }
        public string sHostName { get; set; }
        public string sState { get; set; }
    }
}
