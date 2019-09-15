using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using VncDotnet;

namespace Demo.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CancellationTokenSource CancelSource = new CancellationTokenSource();
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CancelSource.Cancel();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //await MyVncElement.ConnectAsync("10.128.1.104", 5900, "asdf", RfbConnection.SupportedSecurityTypes);
            //await MyVncElement.Start("10.128.1.104", 5900, "asdf", RfbConnection.SupportedSecurityTypes, new MonitorSnippet(920, 0, 1920, 540), CancelSource.Token);
            //MyVncElement.Start("192.168.178.20", 5900, "asdf", RfbConnection.SupportedSecurityTypes, CancelSource.Token);
            MyVncElement.Start("192.168.178.22", 5900, "asdf", new SecurityType[] { SecurityType.VncAuthentication }, CancelSource.Token);
        }
    }
}
