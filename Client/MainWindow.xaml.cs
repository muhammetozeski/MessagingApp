using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;
using MemoryPack;
using SharedProject;
using static SharedProject.FastDebug;
using System.IO;
using System.Xml;

namespace ClientSpace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Client? OpenedClient;
        enum UserElementChildObjectType
        {
            name,
            guid,
            isOnline,
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ClientSaveManager.Instance.Start();
            Closed += (_,_) =>
            {
                Environment.Exit(Environment.ExitCode);
            };

            ClientManager.Instance.AddToOnNewMemberSignedUp(AddNewClientToUI);

            ClientManager.Instance.AddToOnMessageReceived(OnNewMessageReceived);

        }

        void OnNewMessageReceived(Message message)
        {
            if(ClientManager.Instance.Clients.TryGetValue(message.SenderGuid, out Client client))
            {
                if(client == OpenedClient)
                    AddMessageToScreen(message, false);
            }
            else
            {
                Client newClient = new()
                {
                    Guid = message.SenderGuid,
                    IP = message.SenderIP,
                    Name = message.SenderName,
                };
                ClientManager.Instance.AddNewClient(newClient);
            }
        }

        public void AddNewClientToUI(Client client)
        {
            ListBox parent = UserTemplate.Parent as ListBox;
            var element = XamlHelper.CopyUIElement(UserTemplate, parent);
            element.Visibility = Visibility.Visible;
            element.Tag = client.Guid;
            var ellipse = XamlHelper.FindChildByTag<Ellipse>(element, (int)UserElementChildObjectType.isOnline);
            client.OnOnlineStatusChanged += IsOnline =>
            {
                if(IsOnline)
                    ellipse.Fill = Brushes.Green;
                else
                    ellipse.Fill = Brushes.Red;
            };

            var guidTextBlock = XamlHelper.FindChildByTag<TextBlock>(element, (int)UserElementChildObjectType.guid);
            guidTextBlock.Text = client.Guid.ToString();

            var nameTextBlock = XamlHelper.FindChildByTag<TextBlock>(element, (int)UserElementChildObjectType.name);
            nameTextBlock.Text = client.Name;
        }

        private void UsersListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Guid selectedGuid = (Guid)((Grid)UsersListbox.SelectedItem).Tag;
            if (selectedGuid == null)
                p("users list box selected guid is null");

            Client client = ClientManager.Instance.Clients[selectedGuid];

            UserScreenName.Text = client.Name;
            if (client.IsOnline)
                UserScreenIsOnlineEllipse.Fill = Brushes.Green;
            else
                UserScreenIsOnlineEllipse.Fill = Brushes.Red;
            UserScreenGuid.Text = client.Guid.ToString();

            OpenedClient = client;

            MessageContentListBox.Items.Clear();
            if(ClientManager.Instance.Messages.TryGetValue(client.Guid, out var messageList))
                foreach (var msg in messageList)
                {
                    AddMessageToScreen(msg, msg.SenderGuid == ClientManager.Instance.ClientGuid);
                }
        }

        private void UsersListbox_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            UsersListbox.SelectedItem = null;
        }

        private void Input_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                SendInput();
            }
        }

        private void OnSendButtonClicked(object sender, RoutedEventArgs e)
        {
            SendInput();
        }

        void SendInput()
        {
            if (string.IsNullOrWhiteSpace(InputTextBox.Text) || OpenedClient == null)
                return;
            Message message = new();
            message.MessageType = MessageTypes.text;
            message.MessageBody = InputTextBox.Text;
            message.SenderGuid = ClientManager.Instance.ClientGuid;
            message.SenderName = ClientManager.Instance.UserName;
            message.SenderIP = ClientManager.Instance.UserIP;
            message.ReceiverGuid = OpenedClient.Guid;
            message.ReceiverIP = OpenedClient.IP;
            message.ReceiverName = OpenedClient.Name;
            if (!ClientManager.Instance.SendMessage(message))
            {
                w("An error occured while sending the message. Please try again. Error code 0x39e4");
                return;
            }
            var element = AddMessageToScreen(message, true);
            InputTextBox.Text = "";
            MessageContentListBox.ScrollIntoView(element);
        }

        private void MessageContentListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MessageContentListBox.SelectedItem = null;
        }

        private void MessageContentListBox_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            MessageContentListBox.SelectedItem = null;
        }

        FrameworkElement? AddMessageToScreen(Message message, bool isUserMessage)
        {
            if (message.MessageType == MessageTypes.text)
            {
                Border messageTemplate;
                if (isUserMessage)
                    messageTemplate = MessageContentUser;
                else
                    messageTemplate = MessageContentContact;

                var element = XamlHelper.CopyUIElement(messageTemplate, MessageContentListBox);
                element.Visibility = Visibility.Visible;
                ((element as Border).Child as TextBlock).Text = message.MessageBody;
                element.Tag = message;
                return element;
            }

            p("no text message on AddUserMessageToScreen");
            return null;
        }

    }
}
