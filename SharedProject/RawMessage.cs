using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedProject
{
    internal class RawMessage
    {
        public static Exception? MessageToByte(Message message, out byte[] header, out byte[] buffer)
        {
            try
            {
                buffer = MemoryPackSerializer.Serialize(message);

                //sending data must be like this:
                //first byte = BitConverter.IsLittleEndian
                //second [4] byte = the header length
                //rest of the bytes = the header itself

                byte[] length = MemoryPackSerializer.Serialize(buffer.Length);
                header = MemoryPackSerializer.Serialize(BitConverter.IsLittleEndian).Concat(length).ToArray();
                return null;
            }
            catch (Exception e)
            {
                header = buffer = null;
                return e;
            }
        }

    }
}
