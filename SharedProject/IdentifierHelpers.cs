using System;
using System.Collections.Generic;
using System.Text;

namespace SharedProject
{
    public enum UserType
    {
        user,
        server,
        publicRoom,
    }

    internal class IdentifierHelpers
    {
        public readonly Guid serverGuid = CreateOneCharacterGuid('1');

        public static Guid CreateOneCharacterGuid(char c)
        {
            byte[] byteArray = new byte[16];

            byte hexValue = (byte)c;

            for (int i = 0; i < 16; i++)
            {
                byteArray[i] = hexValue;
            }

            return new Guid(byteArray);
        }
    }
}
