using MemoryPack;
using System.Net.Sockets;
using SharedProject;
using System.Net;
using System.Collections.Generic;

namespace Server
{
    internal class Server
    {
        const string serverSenderName = "server";
        static Dictionary<Guid, Client> clients = new Dictionary<Guid, Client>();

        static void Main(string[] args)
        {
            Console.Title = "server";

            TcpListener listener;
            int port = 8080;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");


            listener = new TcpListener(localAddr, port);
            listener.Start();
            Console.WriteLine("Chat server started, waiting for clients...");
            Console.WriteLine("Server ip: " + localAddr.ToString());
            while (true)
            {
                //TcpClient tcpClient = listener.AcceptTcpClient();
                Socket socket = listener.AcceptSocket();
                Console.WriteLine("\nClient connected.");
                Client client = new Client()
                {
                    Guid = Guid.NewGuid(),
                    Connection = socket,
                };

                //IPEndPoint remoteIpEndPoint = client.Connection.Client.RemoteEndPoint as IPEndPoint;
                //tcpClient.ReceiveTimeout = int.MaxValue;
                //tcpClient.SendTimeout = int.MaxValue;
                //tcpClient.SendBufferSize = int.MaxValue;
                //tcpClient.ReceiveBufferSize = int.MaxValue;
                //tcpClient.NoDelay = true;
                //tcpClient.Client.NoDelay = true;

                clients.Add(client.Guid, client);
                Console.Beep();
                new Thread(() => ListenMessages(client)).Start();
            }
        }
        static void ListenMessages(Client client)
        {
            //using (NetworkStream stream = client.Connection.GetStream())
            //{
            while (true)
            {
                if (!client.Connection.Connected)
                {
                    Console.WriteLine("tcpClient disconnected");
                    clients.Remove(client.Guid);
                    return;
                }

                byte[] header = new byte[5];
                try
                {
                    //int bytesRead = stream.Read(header, 0, header.Length);
                    int bytesRead = client.Connection.Receive(header);
                    if (bytesRead == 0)
                    {
                        onBytesReceivedZeroLength(client);
                        return;
                    }

                    bool isLittleEndian = BitConverter.ToBoolean(header, 0);
                    int bufferLength;
                    if (BitConverter.IsLittleEndian != isLittleEndian)
                        bufferLength = BitConverter.ToInt32(header.Skip(1).Reverse().ToArray());
                    else
                        bufferLength = BitConverter.ToInt32(header, 1);

                    if (bufferLength < 0)
                    {
                        Console.WriteLine("negative buffer length error. the buffer is: " + bufferLength);
                        Console.WriteLine("client id: " + client.Guid);
                        Console.WriteLine("client name: " + client.Name);
                        continue;
                    }

                    byte[] buffer = new byte[0];
                    //stream.Read(buffer, 0, bufferLength);
                    IEnumerable<byte> IeBytes = buffer;
                    bytesRead = 0;
                    while (bytesRead < bufferLength)
                    {
                        byte[] temp = new byte[1024];
                        var currentBytesRead = client.Connection.Receive(temp, SocketFlags.Partial);

                        if (currentBytesRead == 0)
                        {
                            onBytesReceivedZeroLength(client);
                            return;
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
                            Console.WriteLine("An error occured on deserializing receiving message. Message is null.");

                            Console.WriteLine("client id: " + client.Guid);
                            Console.WriteLine("client name: " + client.Name);
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("An error occured on deserializing receiving message.");

                        Console.WriteLine("client id: " + client.Guid);
                        Console.WriteLine("client name: " + client.Name);
                        continue;
                    }
                    Console.WriteLine("---------");
                    Console.WriteLine(message.MessageType);
                    Console.WriteLine(message.SenderName);
                    Console.WriteLine(message.SenderGuid);
                    Console.WriteLine(message.SenderIP);
                    switch (message.MessageType)
                    {
                        case MessageTypes.audio:
                            if (message.Audio == null)
                            {
                                Console.WriteLine("clients audio message is null");
                                continue;
                            }
                            Console.WriteLine(message.SenderName + " sent an audio message that is " + message.Audio.Length + " bytes");
                            break;
                        case MessageTypes.text:
                            Console.WriteLine(message.SenderName + ": " + message.MessageBody);
                            Console.WriteLine("Message size: " + bufferLength);
                            if (message.IsBroadCast)
                            {
                                BroadCast(header, buffer);
                            }
                            else
                            {
                                Console.WriteLine("Message receiver name: " + message.ReceiverName);
                                Console.WriteLine("Message receiver guid: " + message.ReceiverGuid);
                                SendOne(message);
                            }
                            //sender:
                            OnClientGotMessage(client, message);
                            //receiver:
                            OnClientGotMessage(clients[message.ReceiverGuid], message);
                            continue;
                        case MessageTypes.initialize:
                            client.IP = message.SenderIP;
                            client.Name = message.SenderName;

                            //sign a guid to the client:
                            Message SendGuidMessage = new Message();
                            SendGuidMessage.MessageType = MessageTypes.initialize;
                            SendGuidMessage.SenderGuid = Guid.Empty; //server is empty guid
                            SendGuidMessage.ReceiverGuid = client.Guid; //client will sign this guid to himself
                            SendGuidMessage.SenderName = serverSenderName;
                            SendOne(SendGuidMessage);
                            continue;
                        case MessageTypes.newMemberSigned:
                            Console.WriteLine("Client initialized:");
                            Console.WriteLine(client.Name);
                            Console.WriteLine(client.Guid);
                            Console.WriteLine(client.IP);

                            //tell everyone that a new member came
                            Message NewMemberMessage = new Message();
                            NewMemberMessage.SenderIP = message.SenderIP;
                            NewMemberMessage.SenderGuid = message.SenderGuid;
                            NewMemberMessage.SenderName = message.SenderName;
                            NewMemberMessage.MessageType = MessageTypes.newMemberSigned;
                            BroadCast(NewMemberMessage, client);
                            continue;
                        default:
                            Console.WriteLine("unexpected message type");
                            break;
                    }
                }
                catch (IOException)
                {
                    onClientDisconnectedWithException(client);
                    return;
                }
                catch (SocketException)
                {
                    onClientDisconnectedWithException(client);
                    return;
                }
            }
            //}
        }

        static void BroadCast(byte[] header, byte[] data, Client exception = default)
        {
            foreach (var client in clients.Values)
            {
                if (client.Equals(exception))
                    continue;
                SendOne(header, data, client);
            }
        }
        static void BroadCast(Message message, Client exception = null)
        {
            message.IsBroadCast = true;
            RawMessage.MessageToByte(message, out var header, out var buffer);
            BroadCast(header, buffer, exception);
        }

        /// <summary>
        /// Message will be sent to the  <see cref="Message.ReceiverGuid"/>
        /// </summary>
        static void SendOne(Message message)
        {
            Exception e = RawMessage.MessageToByte(message, out byte[] header, out byte[] data);
            if (e != null)
            {
                //there is an error
                Console.WriteLine(e.Message);
                return;
            }
            SendOne(header, data, clients[message.ReceiverGuid]);
        }

        static void SendOne(byte[] header, byte[] data, Client receiver)
        {

            Console.WriteLine("message sending to " + receiver.Name + " " + receiver.Guid);
            //var stream = client.Connection.GetStream();
            receiver.Connection.Send(header);

            // Veriyi 1024'er parçalara böl ve gönder
            int offset = 0;
            int chunkSize = 1024;

            while (offset < data.Length)
            {
                int remainingBytes = data.Length - offset;
                int sendSize = Math.Min(chunkSize, remainingBytes);

                // Socket.Send metodu ile parçayı gönder
                int sentBytes = receiver.Connection.Send(data, offset, sendSize, SocketFlags.Partial);

                // Gönderilen byte sayısını kontrol et
                if (sentBytes <= 0)
                {
                    Console.WriteLine("Veri gönderilemedi.");
                    break;
                }

                offset += sentBytes;
            }
            Console.WriteLine("message sent to " + receiver.Name + " " + receiver.Guid);

        }

        static void onBytesReceivedZeroLength(Client client)
        {
            Console.WriteLine(client.Name + " " + client.Guid + " disconnected with 0 byte read");
            //Console.WriteLine(client.IP + " is disconnected ip.");
            OnClientLeft(client);
        }
        static void onClientDisconnectedWithException(Client client)
        {
            Console.WriteLine(client.Name + " " + client.Guid + " disconnected with an exception");
            //Console.WriteLine(client.IP + " is disconnected ip.");
            OnClientLeft(client);
        }
        static void OnClientLeft(Client client)
        {
            Console.Beep(300, 100);
            Console.Beep(300, 100);
            announceClientLeft(client);
            client.IsOnline = false;
            //clients.Remove(client.Guid);
        }

        static void OnClientGotMessage(Client client, Message message)
        {
            client.AddNewMessage(message.SenderGuid, message);
        }
        static void announceClientLeft(Client client)
        {
            Message m = new();
            m.MessageType = MessageTypes.memberLeft;
            m.ReceiverGuid = client.Guid;
            m.ReceiverIP = client.IP;
            m.ReceiverName = client.Name;
            BroadCast(m);
        }
    }
}