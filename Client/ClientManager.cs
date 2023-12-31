﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MemoryPack;
using SharedProject;
using static SharedProject.FastDebug;

namespace ClientSpace
{
    internal class ClientManager
    {
        /// <summary>
        /// singleton pattern
        /// </summary>
        public static ClientManager Instance = new ClientManager(); 
        private ClientManager() { }

        private HashSet<Action<Message>> OnMessageReceived = new();

        private HashSet<Action<Client>> OnNewMemberSignedUp = new();
        private HashSet<Action<Client>> OnMemberLeft = new();

        public string UserName = "";
        public string UserIP = "0";
        public Guid ClientGuid;

        /// <summary>
        /// guid is for the clients guid who is talking with us
        /// </summary>
        public Dictionary<Guid, List<Message>> Messages = new Dictionary<Guid, List<Message>>();
        public Dictionary<Guid, Client> Clients = new Dictionary<Guid, Client>();

        public TcpClient server = new TcpClient();
        string serverAddress;
        int port;

        SoundPlayer soundPlayer = new SoundPlayer();
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);

        public void AddToOnMessageReceived(Action<Message> action)
        {
            OnMessageReceived.Add(action);
        }
        public void RemoveFromOnMessageReceived(Action<Message> action)
        {
            OnMessageReceived.Remove(action);
        }

        public void AddToOnNewMemberSignedUp(Action<Client> action)
        {
            OnNewMemberSignedUp.Add(action);
        }
        public void RemoveFromOnNewMemberSignedUp(Action<Client> action)
        {
            OnNewMemberSignedUp.Remove(action);
        }

        public void AddToOnMemberLeft(Action<Client> action)
        {
            OnMemberLeft.Add(action);
        }
        public void RemoveFromOnMemberLeft(Action<Client> action)
        {
            OnMemberLeft.Remove(action);
        }

        public bool TryConnecting(string serverAddress, int port, string userName)
        {
            this.UserName = userName;
            try
            {
                server.Connect(serverAddress, port);
                Start();
                return true;
            }
            catch (Exception e)
            {
                p(e.Message);
                return false;
            }
        }
        public string? GetIpInfo()
        {
            try
            {
                using (var cli = new HttpClient())
                    UserIP = cli.GetStringAsync(@"https://l2.io/ip").Result;
                return UserIP;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public bool AddNewClient(Client client)
        {
            if (!Clients.TryAdd(client.Guid, client))
            {
                p("client duplicate signing\nClient guid: " + client.Guid + "\nclient name: " + client.Name);
                return false;
            }
            foreach (var action in OnNewMemberSignedUp)
            {
                //send the function to the thread that works on wpf ui
                Application.Current.Dispatcher.Invoke(() =>
                {
                    action(client);
                });
            }
            return true;
        }

        public void AddNewMessage(Guid opponent, Message message)
        {

            if (Messages.TryGetValue(opponent, out var list))
            {
                list.Add(message);
            }
            else
            {
                if (!Messages.TryAdd(opponent, new List<Message>() { message }))
                {
                    Messages[opponent] = new List<Message>() { message };
                }
            }
        }
        public bool SendMessage(Message message)
        {
            #region Default settings for every message
            if(message.SenderGuid == Guid.Empty)
                message.SenderGuid = ClientGuid;
            message.SenderIP = message.SenderIP ?? UserIP;
            message.SenderName = message.SenderName ?? UserName;
            #endregion
            /*
            if (message.MessageBody == "/rec")
            {
                Console.WriteLine("Sound recording... Press enter to send.");
                message.Audio = recordSound();
                Console.WriteLine("sound recorded... Enter a message if you want or press enter to send: ");
                message.MessageBody = Console.ReadLine() ?? "";
                message.MessageType = MessageTypes.audio;
            }
            */
            if (message.MessageBody != null && message.MessageBody.StartsWith("/sendChunk"))
            {
                int startIndex = message.MessageBody.IndexOf(' ');
                if (startIndex != -1 && startIndex < message.MessageBody.Length - 1 && int.TryParse(message.MessageBody.Substring(startIndex + 1), out int chunkStringSize))
                {
                    message.MessageBody = GenerateRandomString(chunkStringSize);
                }
                else
                {
                    Console.WriteLine("Error while converting integer from string");
                }
            }
            byte[] messageBuffer = MemoryPackSerializer.Serialize(message);

            //sending data should be like this:
            //first byte = BitConverter.IsLittleEndian
            //second [4] byte = the header length
            //rest of the bytes = the header itself

            byte[] length = MemoryPackSerializer.Serialize(messageBuffer.Length);
            byte[] header = MemoryPackSerializer.Serialize(BitConverter.IsLittleEndian).Concat(length).ToArray();
            p("data size: " + messageBuffer.Length);
            server.Client.Send(header);

            // Veriyi 1024'er parçalara böl ve gönder
            int offset = 0;
            int chunkSize = 1024;
            try
            {
                while (offset < messageBuffer.Length)
                {
                    int remainingBytes = messageBuffer.Length - offset;
                    int sendSize = Math.Min(chunkSize, remainingBytes);

                    // Socket.Send metodu ile parçayı gönder
                    int sentBytes = server.Client.Send(messageBuffer, offset, sendSize, SocketFlags.Partial);

                    // Gönderilen byte sayısını kontrol et
                    if (sentBytes <= 0)
                    {
                        return false;
                    }

                    offset += sentBytes;
                }
                if (message.MessageType == MessageTypes.text)
                    AddNewMessage(message.ReceiverGuid, message);
                return true;
            }
            catch (Exception e)
            {
                p(e.Message);
                return false;
            }
        }
        void Start()
        {
            UserIP = GetIpInfo();
            sendInitializeMessage(server);
            new Thread(() => ListenMessagesFromServer(server)).Start();
        }
        void sendInitializeMessage(TcpClient server)
        {
            Message message = new Message()
            {
                SenderName = UserName,
                MessageType = MessageTypes.initialize,
                SenderIP = UserIP,
                ReceiverType = UserType.server,
            };
            while(!SendMessage(message))
            {
                w("An error occured on initializing. Retrying.");
            }
        }

        void ListenMessagesFromServer(TcpClient server)
        {
            //NetworkStream stream;
            while (true)
            {
                if (!server.Connected)
                {
                    w("server disconnected");
                    ReconnectToServer();
                    break;
                }
                //stream = server.GetStream();

                byte[] header = new byte[5];
                try
                {
                    int bytesRead = server.Client.Receive(header);
                    if (bytesRead == 0)
                    {
                        w("server disconnected with 0 byte read");
                        ReconnectToServer();
                        break;
                    }

                    bool isLittleEndian = BitConverter.ToBoolean(header, 0);
                    int bufferLength;
                    if (BitConverter.IsLittleEndian != isLittleEndian)
                        bufferLength = BitConverter.ToInt32(header.Skip(1).Reverse().ToArray());
                    else
                        bufferLength = BitConverter.ToInt32(header, 1);

                    //byte[] buffer = new byte[bufferLength];
                    //while (bytesRead < bufferLength && stream.DataAvailable)
                    //    bytesRead += stream.Read(buffer, bytesRead, Math.Min(1024, bufferLength - bytesRead));

                    byte[] buffer = new byte[0];
                    //stream.Read(buffer, 0, bufferLength);
                    IEnumerable<byte> IeBytes = buffer;
                    bytesRead = 0;
                    while (bytesRead < bufferLength)
                    {
                        byte[] temp = new byte[1024];
                        var currentBytesRead = server.Client.Receive(temp, SocketFlags.Partial);

                        if (currentBytesRead == 0)
                        {
                            w("server disconnected with 0 byte read");
                            //ReconnectToServer();
                            break;
                        }
                        IeBytes = IeBytes.Concat(temp);
                        bytesRead += currentBytesRead;
                    }
                    buffer = IeBytes.ToArray();

                    Message? message = default;
                    try
                    {
                        message = MemoryPackSerializer.Deserialize<Message>(buffer);
                        if (message == null)
                        {
                            p("An error occured on deserializing receiving message. Message is null.");
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        p("An error occured on deserializing receiving message.");
                        continue;
                    }
                    switch (message.MessageType)
                    {
                        #region non-usable feature (commented)
                        //case MessageTypes.audio:
                        //    if (message.Audio == null)
                        //    {
                        //        p("clients audio message is null");
                        //        return;
                        //    }

                        //    Console.WriteLine(message.SenderName + ": " + message.MessageBody);
                        //    new Thread(() =>
                        //    {
                        //        Console.WriteLine(message.SenderName + " sent an audio message.\nAudio is playing:");
                        //        using (MemoryStream memoryStream = new MemoryStream(message.Audio))
                        //        {
                        //            soundPlayer.Stream = memoryStream;
                        //            soundPlayer.PlaySync();
                        //        }
                        //        Console.WriteLine("Audio finished");
                        //    }).Start();
                        //    break;
                        #endregion

                        case MessageTypes.initialize:
                            ClientGuid = message.ReceiverGuid;

                            foreach (var item in message.OnlineList)
                            {
                                Client OnlineClient = new();
                                OnlineClient.IP = item.Value.Item2;
                                OnlineClient.Name = item.Value.Item1;
                                OnlineClient.Guid = item.Key;
                                if (!Clients.TryAdd(OnlineClient.Guid, OnlineClient))
                                {
                                    p("client duplicate signing\nClient guid: " + OnlineClient.Guid + "\nclient name: " + OnlineClient.Name);
                                    continue;
                                }
                                foreach (var action in OnNewMemberSignedUp)
                                {
                                    //send the function to the thread that works on wpf ui
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        action(OnlineClient);
                                    });
                                }
                            }

                            Message NewMemberSignedUpMessage = new Message();
                            NewMemberSignedUpMessage.MessageType = MessageTypes.newMemberSigned;
                            SendMessage(NewMemberSignedUpMessage);
                            continue;
                        case MessageTypes.newMemberSigned:
                            Client client = new();
                            client.IP = message.SenderIP;
                            client.Name = message.SenderName;
                            client.Guid = message.SenderGuid;
                            AddNewClient(client);
                            continue;
                        case MessageTypes.memberLeft:
                            Client clientWhoLeft = Clients[message.SenderGuid];
                            clientWhoLeft.ChangeOnlineStatus(false);
                            foreach (var action in OnMemberLeft)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    action(clientWhoLeft);
                                });
                            }
                            continue;
                    }
                    foreach (var action in OnMessageReceived)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            action(message);
                        });
                    }
                    AddNewMessage(message.SenderGuid, message);
                }
                catch (IOException)
                {
                    w("server disconnected with an exception");
                    //ReconnectToServer();
                    break;
                }
                catch (SocketException)
                {
                    w("server disconnected with an exception");
                    //ReconnectToServer();
                    break;
                }
            }
        }
        void ReconnectToServer()
        {
            //server.Dispose();
            //server = new TcpClient();
            //bool isConnected = false;
            //while (!isConnected)
            //{
            //    w("Trying to reconnect to the server...");
            //    try
            //    {
            //        server.Connect(serverAddress, port);
            //        w("Server connection successfull");
            //        isConnected = true;
            //    }
            //    catch (Exception e)
            //    {
            //        w("Couldn't connect to server. Error message: \"" + e.Message + "\"");
            //        w("Waiting for 1 second to reconnect");
            //        isConnected = false;
            //        Thread.Sleep(1000);
            //    }
            //}
        }

        [Obsolete]
        /// <summary>
        /// Records sounds untill user press enter.
        /// </summary>
        /// <returns>The recorded audio as wav format</returns>
        static byte[] recordSound()
        {
            string path = "sound" + ".wav";
            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
            mciSendString("record recsound", "", 0, 0);
            Console.ReadLine();

            mciSendString("save recsound " + path, "", 0, 0);
            mciSendString("close recsound ", "", 0, 0);
            byte[] sound = File.ReadAllBytes(path);
            new Thread(() => File.Delete(path)).Start();
            return sound;
        }



        public static string GenerateRandomString(int size)
        {
            Random random = new Random();
            StringBuilder stringBuilder = new StringBuilder(size);

            for (int i = 0; i < size; i++)
            {
                char randomChar = (char)random.Next('a', 'z' + 1); // chooses a random character between 'a' and 'z'
                stringBuilder.Append(randomChar);
            }

            return stringBuilder.ToString();
        }
    }
}
