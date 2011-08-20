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
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Options;
using System.Xml.Linq;

namespace ILEdit.OptionPages
{
    /// <summary>
    /// Interaction logic for ILEditOptionPage.xaml
    /// </summary>
    [ExportOptionPage(Title = "ILEdit", Order = 3)]
    public partial class ILEditOptionPage : UserControl, IOptionPage
    {
        public ILEditOptionPage()
        {
            InitializeComponent();
        }

        public void Load(ILSpySettings settings)
        {
            //Creates the viewmodel
            var vm = new ILEditOptionPageViewModel();

            //Initializes the viewmodel
            vm.Load();

            //Sets the data context
            this.DataContext = vm;
        }

        public void Save(XElement root)
        {
            //Saves data
            var xel = GlobalContainer.SettingsManager.Instance.Root;
            ((ILEditOptionPageViewModel)this.DataContext).Save(xel);
            root.Element("ILEdit").ReplaceWith(xel);
        }
    }
}
