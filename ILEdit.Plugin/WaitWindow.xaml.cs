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
using System.Threading;

namespace ILEdit
{
    /// <summary>
    /// Interaction logic for WaitWindow.xaml
    /// </summary>
    public partial class WaitWindow : Window
    {
        CancellationTokenSource cts;

        #region Singleton implementation

        private WaitWindow(string title, object content, CancellationTokenSource cts)
        {
            //Checks the instance
            if (Instance != null)
                throw new InvalidOperationException("Cannot show more than one WaitWindow concurrently");
            Instance = this;

            //Component initialization
            InitializeComponent();

            //Applies the parameters
            this.cts = cts;
            this.Title = title;
            this.Content.Content = content;
        }

        private static WaitWindow Instance;
        
        #endregion

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (cts != null)
                cts.Cancel();
            Instance = null;
        }

        /// <summary>
        /// Shows a new wait window with the given title and content, along with a CancellationTokenSource to cancel if the user cancels the action
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="ct"></param>
        public static void ShowDialog(string title, object content, CancellationTokenSource cts)
        {
            new WaitWindow(title, content, cts).ShowDialog();
        }

        /// <summary>
        /// Hides the current wait window if shown
        /// </summary>
        public static void Hide()
        {
            if (Instance != null)
                Instance.Close();
        }
    }
}
