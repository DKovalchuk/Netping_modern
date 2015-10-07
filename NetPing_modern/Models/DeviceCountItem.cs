using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetPing_modern.ViewModels
{
    [Serializable]
    public class DeviceCountItem
    {
        public string Name { get; set; }
        public double Balance { get; set; }
        public double FreeBalance { get; set; }
        public double Reserved { get; set; }
    }
}
