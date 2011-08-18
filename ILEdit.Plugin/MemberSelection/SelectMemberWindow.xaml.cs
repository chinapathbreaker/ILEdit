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

namespace ILEdit
{
    /// <summary>
    /// Interaction logic for SelectMemberWindow.xaml
    /// </summary>
    public partial class SelectMemberWindow : Window
    {
        public SelectMemberWindow(Predicate<IMetadataTokenProvider> filter, TokenType token)
        {
            //Initializes the components
            InitializeComponent();

            //Sets the filter
            tree.MemberFilter = filter;
            tree.SelectableMembers = token;

            //Handler to enable or disable the Ok button
            tree.SelectionChanged += (_, e) => {
                BtnOk.IsEnabled = tree.SelectedMember != null;
            };
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedMember = tree.SelectedMember;
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Returns the member selected by the user
        /// </summary>
        public IMetadataTokenProvider SelectedMember { get; private set; }

    }
}
