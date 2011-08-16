using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ILEdit
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    internal partial class InputBox : Window
    {
        public string Value { get { return this.TxtValue.Text; } }

        public InputBox(string caption, object content)
        {
            InitializeComponent();
            this.Title = caption;
            Text.Content = content;
            this.Loaded += (_, __) => TxtValue.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TxtValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnOk_Click(null, null);
        }
    }
}
