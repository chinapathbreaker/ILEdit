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
using System.Collections.ObjectModel;

namespace ILEdit
{
    /// <summary>
    /// Interaction logic for GenericResolutionWindow.xaml
    /// </summary>
    public partial class GenericResolutionWindow : Window
    {
        public class GenericParameterRowItem 
        {
            public string Name { get; set; }

            public Predicate<IMetadataTokenProvider> MemberFilter { get; set; }

            public ModuleDefinition DestinationModule { get; set; }

            public TypeDefinition EnclosingType { get; set; }

            private IMetadataTokenProvider _selectedMember;
            public IMetadataTokenProvider SelectedMember
            {
                get { return _selectedMember; }
                set 
                { 
                    _selectedMember = value;
                    HasSelectedParameter = _selectedMember != null;
                    var evt = Selected;
                    if (evt != null)
                        evt();
                }
            }

            public bool HasSelectedParameter { get; set; }

            public event Action Selected;

        }

        /// <summary>
        /// Returns the resolved generic member
        /// </summary>
        public IGenericParameterProvider ResolvedGeneric { get; private set; }

        private IGenericParameterProvider originalGeneric;

        public GenericResolutionWindow(IGenericParameterProvider generic, TypeDefinition context)
        {
            //Component initialization
            InitializeComponent();
            originalGeneric = generic;

            //Sets icon and text
            var node = new ILEditTreeNode(generic, true);
            ImgIcon.Source = (ImageSource)node.Icon;
            LblName.Text = node.Text.ToString();

            //Sets destination type
            LblDestinationType.Text = new ILEditTreeNode(context, true).Text.ToString();

            //Populates the list
            LstParameters.ItemsSource =
                generic.GenericParameters
                .Select(p => {
                    var row = new GenericParameterRowItem() { Name = p.Name, MemberFilter = Injection.MemberFilters.Types, DestinationModule = context.Module, EnclosingType = context };
                    row.HasSelectedParameter = false;
                    row.Selected += Row_Selected;
                    return row;
                }).ToArray();
        }

        private void Row_Selected()
        {
            //Enables the OK button oly if the user has selected all the types
            BtnOk.IsEnabled = LstParameters.ItemsSource.Cast<GenericParameterRowItem>().All(x => x.HasSelectedParameter);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            //Resolves the ganerics
            IGenericInstance instance;
            if (originalGeneric is TypeDefinition)
                instance = new GenericInstanceType((TypeReference)originalGeneric);
            else 
                instance = new GenericInstanceMethod((MethodReference)originalGeneric);
            foreach (var row in LstParameters.ItemsSource.Cast<GenericParameterRowItem>())
                instance.GenericArguments.Add((TypeReference)row.SelectedMember);

            //Sets the return value
            this.ResolvedGeneric = (IGenericParameterProvider)instance;

            //Returns to the caller
            this.DialogResult = true;
            this.Close();
        }

    }
}
