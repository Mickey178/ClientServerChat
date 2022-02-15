using ProtoBuf;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MyStream
{
    public class MyStreamReaderAndWriter
    {
        private readonly NetworkStream networkStream;
        private const int DefaultBufferSize = 128;

        public MyStreamReaderAndWriter(TcpClient tcpClientStream)
        {
            networkStream = tcpClientStream.GetStream();
        }

        public ChatMessageDto Read()
        {
            List<byte> bufferList = new List<byte>();
            int returnBufferSize;

            do
            {
                byte[] arrayToRead = new byte[DefaultBufferSize];
                returnBufferSize = networkStream.Read(arrayToRead, 0, arrayToRead.Length);

                for (int i = 0; i < returnBufferSize; i++)
                {
                    bufferList.Add(arrayToRead[i]);
                }

            } while (returnBufferSize == DefaultBufferSize);            

            return ProtoDeserialize<ChatMessageDto>(bufferList.ToArray());
        }

        public Task WriteAsync(ChatMessageDto connectedClientDto)
        {
            byte[] myWriteBuffer = ProtoSerialize(connectedClientDto);
            return networkStream.WriteAsync(myWriteBuffer, 0, myWriteBuffer.Length);
        }

        public void Write(ChatMessageDto connectedClientDto)
        {
            byte[] myWriteBuffer = ProtoSerialize(connectedClientDto);
            networkStream.Write(myWriteBuffer, 0, myWriteBuffer.Length);
        }

        private byte[] ProtoSerialize<T>(T record) where T : class
        {
            if (null == record) return null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, record);
                    return stream.ToArray();
                }
            }
            catch 
            {
                // здесь можно логи написать 
                throw;
            }
        }

        private T ProtoDeserialize<T>(byte[] data) where T : class
        {
            if (null == data) return null;

            try
            {
                using (var stream = new MemoryStream(data))
                {
                    return Serializer.Deserialize<T>(stream);
                }
            }
            catch
            {
                // здесь можно логи написать
                throw;
            }
        }
    }
}
