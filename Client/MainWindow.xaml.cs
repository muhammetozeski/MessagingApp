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


namespace ClientSpace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void AddNewClientToUI()
        {
            ListBox parent = UserTemplate.Parent as ListBox;

            TextBlock clonedTextBlock = XamlReader.Parse(XamlWriter.Save(UserTemplate)) as TextBlock;
            parent.Items.Add(clonedTextBlock);
        }
    }
}
