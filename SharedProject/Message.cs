using System;
using System.Collections.Generic;
using System.Text;
using MemoryPack;

namespace SharedProject
{
    [MemoryPackable]
    public partial class Message
    {
        public string SenderIP { get; set; }
        public string ReceiverIP { get; set; }
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }

        public Guid SenderGuid { get; set; }
        public Guid ReceiverGuid { get; set; }
        public Guid MessageGuid = Guid.NewGuid();

        public MessageTypes MessageType = MessageTypes.text;
        public UserType SenderType = UserType.user;
        public UserType ReceiverType = UserType.user;

        public string MessageBody { get; set; }

        public bool IsBroadCast = false;

        public byte[] Audio { get; set; }
        public byte[] Image { get; set; }
        public byte[] File { get; set; }

        /// <summary>
        /// online members list. in order; his guid, name, ip
        /// </summary>
        public Dictionary<Guid, (string, string)> OnlineList { get; set; }
    }
}
