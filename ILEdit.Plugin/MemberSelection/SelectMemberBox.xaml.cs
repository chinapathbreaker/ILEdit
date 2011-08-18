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
    /// Interaction logic for SelectMemberBox.xaml
    /// </summary>
    public partial class SelectMemberBox : Control
    {
        public SelectMemberBox()
        {
            InitializeComponent();
        }

        #region Template

        private Image ImgIcon;
        private TextBlock LblName;

        public override void OnApplyTemplate()
        {
            //Extracts the controls from the template
            ImgIcon = (Image)GetTemplateChild("ImgIcon");
            LblName = (TextBlock)GetTemplateChild("LblName");
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
            DependencyProperty.Register("MemberFilter", typeof(Predicate<IMetadataTokenProvider>), typeof(SelectMemberBox), new PropertyMetadata(null));


        #endregion

        #region SelectableMembers property


        public TokenType SelectableMembers
        {
            get { return (TokenType)GetValue(SelectableMembersProperty); }
            set { SetValue(SelectableMembersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectableMembers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectableMembersProperty =
            DependencyProperty.Register("SelectableMembers", typeof(TokenType), typeof(SelectMemberBox), new UIPropertyMetadata(TokenType.TypeDef));


        #endregion

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            //Shows the member selection window
            var win = new SelectMemberWindow(MemberFilter, SelectableMembers);
            if (win.ShowDialog().GetValueOrDefault(false))
            {
                //Sets icon and text using an ILEditTreeNode
                var node = new ILEditTreeNode(win.SelectedMember, true);
                ImgIcon.Source = (ImageSource)node.Icon;
                LblName.Text = node.Text.ToString();

                //Updates the properties
                HasMember = true;
                SelectedMember = win.SelectedMember;
            }
        }

    }
}
