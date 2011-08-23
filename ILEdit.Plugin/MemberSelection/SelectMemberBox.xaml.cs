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
    /// Interaction logic for SelectMemberBox.xaml
    /// </summary>
    public partial class SelectMemberBox : Control
    {
        public SelectMemberBox()
        {
            InitializeComponent();
            ApplyTemplate();
        }

        #region Template

        private Grid NoGenericsGrid;
        private Image ImgIcon;
        private TextBlock LblName;
        private ComboBox GenericsCombo;

        public override void OnApplyTemplate()
        {
            //Extracts the controls from the template
            NoGenericsGrid = (Grid)GetTemplateChild("NoGenericsGrid");
            ImgIcon = (Image)GetTemplateChild("ImgIcon");
            LblName = (TextBlock)GetTemplateChild("LblName");
            GenericsCombo = (ComboBox)GetTemplateChild("GenericsCombo");

            //Registers the event handlers
            GenericsCombo.SelectionChanged += GenericsCombo_SelectionChanged;
        }

        #endregion

        #region HasMember


        public bool HasMember
        {
            get { return (bool)GetValue(HasMemberProperty); }
            private set { SetValue(HasMemberPropertyKey, value); }
        }

        // Using a DependencyProperty as the backing store for HasMember.  This enables animation, styling, binding, etc...
        private static readonly DependencyPropertyKey HasMemberPropertyKey =
            DependencyProperty.RegisterReadOnly("HasMember", typeof(bool), typeof(SelectMemberBox), new PropertyMetadata(false));
        public static readonly DependencyProperty HasMemberProperty = HasMemberPropertyKey.DependencyProperty;


        #endregion

        #region SelectedMember


        public IMetadataTokenProvider SelectedMember
        {
            get { return (IMetadataTokenProvider)GetValue(SelectedMemberProperty); }
            set { SetValue(SelectedMemberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedMember.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedMemberProperty =
            DependencyProperty.Register("SelectedMember", typeof(IMetadataTokenProvider), typeof(SelectMemberBox), new PropertyMetadata(null));

        
        #endregion

        #region MemberFilter property



        public Predicate<IMetadataTokenProvider> MemberFilter
        {
            get { return (Predicate<IMetadataTokenProvider>)GetValue(MemberFilterProperty); }
            set { SetValue(MemberFilterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MemberFilter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MemberFilterProperty =
            DependencyProperty.Register("MemberFilter", typeof(Predicate<IMetadataTokenProvider>), typeof(SelectMemberBox), new PropertyMetadata(null, OnMemberFilterChanged));

        private static void OnMemberFilterChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //Sender
            var s = (SelectMemberBox)sender;

            //If the new value isn't null updates the generic combo
            if (e.NewValue != null && s.EnclosingType != null && s.EnclosingType.HasGenericParameters)
                s.GenericsCombo.ItemsSource = s.GetComboBoxItems(GetGenericParameters(s.EnclosingType));
        }


        #endregion

        #region SelectableMembers property


        public List<TokenType> SelectableMembers
        {
            get { return (List<TokenType>)GetValue(SelectableMembersProperty); }
            set { SetValue(SelectableMembersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectableMembers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectableMembersProperty =
            DependencyProperty.Register("SelectableMembers", typeof(List<TokenType>), typeof(SelectMemberBox), new PropertyMetadata(new[] { TokenType.TypeDef }.ToList()));


        #endregion

        #region DestinationModule property


        public ModuleDefinition DestinationModule
        {
            get { return (ModuleDefinition)GetValue(DestinationModuleProperty); }
            set { SetValue(DestinationModuleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DestinationModule.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DestinationModuleProperty =
            DependencyProperty.Register("DestinationModule", typeof(ModuleDefinition), typeof(SelectMemberBox), new PropertyMetadata(null));

        
        #endregion

        #region EnclosingType property


        public TypeDefinition EnclosingType
        {
            get { return (TypeDefinition)GetValue(EnclosingTypeProperty); }
            set { SetValue(EnclosingTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnclosingType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnclosingTypeProperty =
            DependencyProperty.Register("EnclosingType", typeof(TypeDefinition), typeof(SelectMemberBox), new PropertyMetadata(null, OnEnclosingTypeChanged));

        private static void OnEnclosingTypeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //Checks if the type has generic parameters or not
            var type = (TypeDefinition)e.NewValue;
            var generics = type != null && type.HasGenericParameters;

            //Sets the visibility of the elements
            var s = (SelectMemberBox)sender;
            s.NoGenericsGrid.Visibility = generics ? Visibility.Collapsed : Visibility.Visible;
            s.GenericsCombo.Visibility = generics ? Visibility.Visible : Visibility.Collapsed;

            //Fills the generic combo
            if (generics)
                s.GenericsCombo.ItemsSource = s.GetComboBoxItems(GetGenericParameters(type));
        }

        private void GenericsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Checks that ther's exactly one item selected
            if (e.AddedItems == null || e.AddedItems.Count == 0)
                return;

            //The newly selected item
            var item = (ComboBoxItem)e.AddedItems[0];

            //Checks if it has the tag set
            if (item.Tag != null)
            {
                SelectedMember = (IMetadataTokenProvider)item.Tag;
                HasMember = true;
            }
            else
            {
                //Shows the window to select the type
                var win = new SelectMemberWindow(MemberFilter, new[] { TokenType.TypeDef }, DestinationModule, EnclosingType);
                if (win.ShowDialog().GetValueOrDefault(false))
                {
                    //Creates the combo box item
                    var type = (TypeReference)win.SelectedMember;
                    var cbi = new ComboBoxItem() { Content = GetContentForComboBoxItem(type), Tag = type };

                    //Selects the new item
                    item.IsSelected = false;
                    var items = (ObservableCollection<ComboBoxItem>)GenericsCombo.ItemsSource;
                    items.Insert(items.Count - 1, cbi);
                    cbi.IsSelected = true;
                }
                //Restores the previously selected item
                else if (e.RemovedItems != null && e.RemovedItems.Count > 0)
                {
                    var exItem = (ComboBoxItem)e.RemovedItems[0];
                    item.IsSelected = false;
                    exItem.IsSelected = true;
                }
                else
                {
                    item.IsSelected = false;
                    GenericsCombo.SelectedIndex = -1;
                }
            }
        }

        private static List<GenericParameter> GetGenericParameters(TypeDefinition type)
        {
            var lst = new List<GenericParameter>();
            foreach (var p in type.GenericParameters)
                if (!lst.Any(x => x.Name == p.Name))
                    lst.Add(p);
            if (type.DeclaringType != null)
                foreach (var p in GetGenericParameters(type.DeclaringType))
                    if (!lst.Any(x => x.Name == p.Name))
                        lst.Add(p);
            return lst;
        }

        private ObservableCollection<ComboBoxItem> GetComboBoxItems(IEnumerable<GenericParameter> parameters)
        {
            //List
            var lst = new List<ComboBoxItem>();

            //Filters the parameters
            IEnumerable<GenericParameter> filteredParameters = parameters;
            if (MemberFilter != null)
                filteredParameters = parameters.Where(p => MemberFilter(p));

            //Creates the combo box items from the parameters
            foreach (var p in filteredParameters)
                lst.Add(new ComboBoxItem() { Content = GetContentForComboBoxItem(p), Tag = p });

            //Creates the select type item
            lst.Add(new ComboBoxItem() { Content = "Select type ...", FontStyle = FontStyles.Italic });

            //Returns the observable collection
            return new ObservableCollection<ComboBoxItem>(lst);
        }

        private static StackPanel GetContentForComboBoxItem(IMetadataTokenProvider obj)
        {
            //Gets image and text
            var node = new ILEditTreeNode(obj, true);
            ImageSource img;
            var text = node.Text.ToString();
            if (obj is GenericParameter)
                img = new BitmapImage(new Uri("pack://application:,,,/ILEdit.Plugin;component/Images/GenericType.png"));
            else
                img = (ImageSource)node.Icon;

            //Builds the stack panel
            var sp = new StackPanel() { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Image() { Source = img, Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(new TextBlock() { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) });

            //Returns
            return sp;
        }

        #endregion

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            //Checks that there are no generics
            if (EnclosingType != null && EnclosingType.HasGenericParameters)
                return;

            //Shows the member selection window
            var win = new SelectMemberWindow(MemberFilter, SelectableMembers, DestinationModule, EnclosingType);
            if (win.ShowDialog().GetValueOrDefault(false))
            {
                //Selected member
                var member = win.SelectedMember;

                //Sets icon and text using an ILEditTreeNode
                var node = new ILEditTreeNode(member, true);
                ImgIcon.Source = (ImageSource)node.Icon;
                LblName.Text = node.Text.ToString();

                //Updates the properties
                HasMember = true;
                SelectedMember = member;
            }
        }

    }
}
