﻿using System;
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
using VncDotnet;

namespace Demo.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //await MyVncElement.ConnectAsync("10.128.1.104", 5900, "asdf", RfbConnection.SupportedSecurityTypes);
            await MyVncElement.ConnectAsync("10.128.1.104", 5900, "asdf", RfbConnection.SupportedSecurityTypes, new MonitorSnippet(920, 0, 1920, 540));
            //await MyVncElement.ConnectAsync("192.168.178.20", 5900, "asdf", RfbConnection.SupportedSecurityTypes);
        }
    }
}