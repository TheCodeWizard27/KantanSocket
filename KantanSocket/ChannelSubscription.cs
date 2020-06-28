using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KantanNetworking
{
    public class ChannelSubscription
    {

        public string Channel { get; set; }
        public Action<NetworkMessage> Action { get; set; }

    }
}
