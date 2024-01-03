using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace SharedProject
{
    public class Client
    {
        public Socket Connection { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
        public Guid Guid { get; set; }
        public bool IsOnline = true;

        /// <summary>
        /// guid is for "the user guid" who this client sent message to or receive message from
        /// </summary>
        public Dictionary<Guid, List<Message>> Messages = new Dictionary<Guid, List<Message>>();

        public event Action<bool> OnOnlineStatusChanged;

        public void ChangeOnlineStatus(bool isOnline)
        {
            IsOnline = isOnline;
#if CLIENT
            Application.Current.Dispatcher.Invoke(() => { OnOnlineStatusChanged?.Invoke(isOnline); });
            return;
#endif
            OnOnlineStatusChanged?.Invoke(isOnline);
        }

        public void AddNewMessage(Guid guid, Message message)
        {
            if (Messages.TryGetValue(message.SenderGuid, out var list))
            {
                list.Add(message);
            }
            else
            {
                if (!Messages.TryAdd(message.SenderGuid, new List<Message>() { message }))
                {
                    Messages[message.SenderGuid] = new List<Message>() { message };
                }
            }
        }
    }
}
