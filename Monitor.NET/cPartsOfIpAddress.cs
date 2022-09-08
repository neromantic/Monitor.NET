using System.Windows.Controls;

namespace Monitor.NET
{
    internal class cPartsOfIpAddress : ListViewItem
    {
        public string IP { get; set; }
        public string MAC { get; set; }
        public string DNS { get; set; }
    }
}
