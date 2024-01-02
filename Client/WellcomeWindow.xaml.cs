using SharedProject;
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
using System.Windows.Shapes;
using static SharedProject.FastDebug;

namespace ClientSpace
{
    /// <summary>
    /// WellcomeWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class WellcomeWindow : Window
    {
        public WellcomeWindow()
        {
            InitializeComponent();
            Loaded += WellcomeWindow_Loaded;
        }

        private void WellcomeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //test purpose:
            w(IdentifierHelpers.CreateOneCharacterGuid('f'));
            IpTextBox.Focus();
            IpTextBox.SelectAll();
        }

        private void IP_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            PortTextBox.Focus();
            PortTextBox.SelectAll();
        }

        private void Port_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                enter();
        }

        private void Enter_Button_Click(object sender, RoutedEventArgs e)
        {
            enter();
        }

        void enter()
        {
            if (EnterButton.IsEnabled)
            {
                EnterButton.IsEnabled = false;
                if (!int.TryParse(PortTextBox.Text, out int port))
                {
                    WarningText.Visibility = Visibility.Visible;
                    WarningText.Text = "Error on converting the port. Please enter only port numbers.";
                    EnterButton.IsEnabled = true;
                    return;
                }

                if(!ClientManager.Instance.TryConnecting(IpTextBox.Text, port))
                {
                    WarningText.Visibility = Visibility.Visible;
                    WarningText.Text = "Error on connecting to the server. Please try again.";
                    EnterButton.IsEnabled = true;
                    return;
                }
                ClientManager.Instance.UserName = UserNameTextBox.Text;
                WarningText.Visibility = Visibility.Collapsed;
                InfoText.Visibility = Visibility.Visible;
                InfoText.Text = "Connected to the server. Getting ip info.";
                string? ip = ClientManager.Instance.GetIpInfo();
                if(ip == null)
                {
                    InfoText.Text = "An error occured while getting ip information";
                }
                else
                {
                    InfoText.Text = "Your ip is " + ip;
                }
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                Visibility = Visibility.Hidden;
                EnterButton.IsEnabled = true;
            }
        }
    }
}