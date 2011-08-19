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
using Mono.Cecil;
using ICSharpCode.TreeView;

namespace ILEdit
{
    /// <summary>
    /// Interaction logic for SelectMemberWindow.xaml
    /// </summary>
    public partial class SelectMemberWindow : Window
    {
        private static Tuple<string, string>[] _commonMembers = new Tuple<string, string>[] { 
            Tuple.Create("System", "Int32"),
            Tuple.Create("System", "Int64"),
            Tuple.Create("System", "Boolean"),
            Tuple.Create("System", "String"),
            Tuple.Create("System", "Double"),
            Tuple.Create("System", "Object"),
            Tuple.Create("System", "Void"),
            Tuple.Create("System", "EventHandler"),
            Tuple.Create("System", "Action"),
            Tuple.Create("System", "Action`1"),
            Tuple.Create("System", "Func`1"),
            Tuple.Create("System", "Func`2")
        };

        public SelectMemberWindow(Predicate<IMetadataTokenProvider> filter, TokenType token, ModuleDefinition destinationModule)
        {
            //Initializes the components
            InitializeComponent();

            //Sets the filter
            tree.MemberFilter = filter;
            tree.SelectableMembers = token;
            
            //Prepares the common types
            if (destinationModule != null)
            {
                //Fills the list
                LstCommonTypes.ItemsSource =
                    _commonMembers
                    .Select(x => new ILEditTreeNode(new TypeReference(x.Item1, x.Item2, destinationModule, destinationModule.TypeSystem.Corlib).Resolve(), true))
                    .Where(x => filter(x.TokenProvider) && x.TokenProvider.MetadataToken.TokenType == token);

                //Registers the selection handler
                LstCommonTypes.SelectionChanged += (_, e) => {
                    if (e.AddedItems.Count == 1)
                    {
                        var node = ((ILEditTreeNode)e.AddedItems[0]);
                        this.SelectedMember = node.TokenProvider;
                        BtnOk.IsEnabled = true;
                        tree.SelectedIndex = -1;
                        ImgSelectedMember.Source = (ImageSource)node.Icon;
                        ContentSelectedMember.Content = node.Text;
                    }
                };
            }

            //Handler to enable or disable the Ok button
            tree.SelectionChanged += (_, e) => {
                if (tree.SelectedMember != null)
                {
                    this.SelectedMember = tree.SelectedMember;
                    BtnOk.IsEnabled = true;
                    LstCommonTypes.SelectedIndex = -1;
                    var node = (SharpTreeNode)tree.SelectedItem;
                    ImgSelectedMember.Source = (ImageSource)node.Icon;
                    ContentSelectedMember.Content = node.Text;
                }
            };
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Returns the member selected by the user
        /// </summary>
        public IMetadataTokenProvider SelectedMember { get; private set; }

    }
}
