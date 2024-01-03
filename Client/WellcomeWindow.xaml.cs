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
        MainWindow mainWindow = new MainWindow();
        static bool switched = false;
        public WellcomeWindow()
        {
            InitializeComponent();
            Loaded += WellcomeWindow_Loaded;
        }

        private void WellcomeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //test purpose:
            //w(IdentifierHelpers.CreateOneCharacterGuid('6'));
            IpTextBox.Focus();
            IpTextBox.SelectAll();

            mainWindow.Show();
            mainWindow.Visibility = Visibility.Hidden;

            Closed += (_, _) =>
            {
                if(!switched)
                    Environment.Exit(Environment.ExitCode);
            };
        }

        private void IP_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                PortTextBox.Focus();
                PortTextBox.SelectAll();
            }
        }

        private void Port_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UserNameTextBox.Focus();
                UserNameTextBox.SelectAll();
            }
        }

        private void UserName_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                enter();
            }
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

                if (!ClientManager.Instance.TryConnecting(IpTextBox.Text, port, UserNameTextBox.Text))
                {
                    WarningText.Visibility = Visibility.Visible;
                    WarningText.Text = "Error on connecting to the server. Please try again.";
                    EnterButton.IsEnabled = true;
                    return;
                }
                WarningText.Visibility = Visibility.Collapsed;
                InfoText.Visibility = Visibility.Visible;
                InfoText.Text = "Connected to the server";

                mainWindow.Visibility = Visibility.Visible;
                switched = true;
                Close();
            }
        }
    }
}