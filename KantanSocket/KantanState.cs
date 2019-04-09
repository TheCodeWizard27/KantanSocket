using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KantanNetworking
{
    public enum KantanBufferSize
    {
        Tiny = 32,
        Small = 64,
        Default = 1024,
        Big = 8192
    }

    public class KantanState
    {

        #region Constructors

        public KantanState(ISocket socket, KantanBufferSize bufferSize)
        {
            Socket = socket;
            Buffer = new byte[(int)bufferSize];
            BufferSize = bufferSize;
        }

        public KantanState(ISocket socket) : this(socket, KantanBufferSize.Default)
        {
        }

        #endregion


        #region Properties

        public ISocket Socket { get; private set; }

        public byte[] Buffer { get; private set; }

        public StringBuilder StringBuffer { get; private set; }

        public KantanBufferSize BufferSize { get; private set; }

        #endregion


        #region Public Methods

        public void ClearBuffer()
        {
            for (var i = 0; i < (int) BufferSize; i++)
                Buffer[i] = 0;

            StringBuffer.Clear();
        }

        #endregion

    }
}
