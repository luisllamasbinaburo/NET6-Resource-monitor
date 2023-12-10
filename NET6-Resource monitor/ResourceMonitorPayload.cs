using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NET6_Resource_monitor
{
    internal class ResourceMonitorPacket
    {
        public double Cpu { get; set; }
        public double Ram { get; set; }
        public double Ssd { get; set; }
        public double Gpu { get; set; }
        public double Network { get; set; }
        public double Temperature { get; set; }
    }
}
