using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KantanNetworking
{
    public struct NetworkMessage
    {
        public string Channel { get; set; }
        public object Data { get; set; }

        public T GetData<T>() => (T) Data;

    }
}
