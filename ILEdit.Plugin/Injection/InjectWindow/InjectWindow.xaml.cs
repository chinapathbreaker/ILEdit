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
using ICSharpCode.ILSpy.TreeNodes;

namespace ILEdit.Injection
{
    /// <summary>
    /// Interaction logic for InjectWindow.xaml
    /// </summary>
    public partial class InjectWindow : Window
    {
        public InjectWindow(ILSpyTreeNode node, int tabSelectedIndex, bool injectExistingEnabled)
        {
            this.DataContext = new InjectWindowViewModel(node, this) { TabSelectedIndex = tabSelectedIndex, InjectExistingEnabled = injectExistingEnabled };
            InitializeComponent();
            this.Loaded += (_, __) => TxtName.Focus();
        }
    }
}
