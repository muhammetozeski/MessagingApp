using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SharedProject
{
    class Client
    {
        public Socket Connection { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
        public Guid Guid { get; set; }

        /// <summary>
        /// guid is for "the user guid" who this client sent message to or receive message from
        /// </summary>
        Dictionary<Guid, List<Message>> Messages = new Dictionary<Guid, List<Message>>();
    }
}
