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

namespace TranslationBindingTesting
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ServiceLocator.Instance.MainWindowViewModel.Dispatcher = Dispatcher;

            DataContext = ServiceLocator.Instance.MainWindowViewModel;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TestingWindow testingWindow = new TestingWindow();
            testingWindow.ShowDialog();
        }
    }
}
