using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.TreeView;
using Mono.Cecil;
using System.Windows.Media;
using System.Windows;

namespace ILEdit
{
    public class SelectMemberControl : SharpTreeView
    {
        public SelectMemberControl()
        {
            //Creates the root
            this.Root = new SharpTreeNode();
            this.ShowRoot = false;
            this.Loaded += (_, __) => {
                if (ICSharpCode.ILSpy.MainWindow.Instance != null)
                    foreach (var asm in ICSharpCode.ILSpy.MainWindow.Instance.CurrentAssemblyList.GetAssemblies().Where(x => x.AssemblyDefinition != null))
                        Root.Children.Add(new ILEditTreeNode(asm.AssemblyDefinition, false) { ChildrenFilter = MemberFilter, Foreground = new SolidColorBrush(Colors.Black) });
            };
        }

        #region MemberFilter property



        public Predicate<IMetadataTokenProvider> MemberFilter
        {
            get { return (Predicate<IMetadataTokenProvider>)GetValue(MemberFilterProperty); }
            set { SetValue(MemberFilterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MemberFilter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MemberFilterProperty =
            DependencyProperty.Register("MemberFilter", typeof(Predicate<IMetadataTokenProvider>), typeof(SelectMemberControl), new PropertyMetadata(null));


        #endregion

        #region SelectableMembers property


        public IList<TokenType> SelectableMembers
        {
            get { return (IList<TokenType>)GetValue(SelectableMembersProperty); }
            set { SetValue(SelectableMembersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectableMembers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectableMembersProperty =
            DependencyProperty.Register("SelectableMembers", typeof(IList<TokenType>), typeof(SelectMemberControl), new UIPropertyMetadata(new[] { TokenType.TypeDef }));


        #endregion

        #region SelectedMember proeprty


        /// <summary>
        /// Returns the selected member
        /// </summary>
        public IMetadataTokenProvider SelectedMember
        {
            get { return (IMetadataTokenProvider)GetValue(SelectedMemberProperty); }
            set { SetValue(SelectedMemberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedMember.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty SelectedMemberProperty =
            DependencyProperty.Register("SelectedMember", typeof(IMetadataTokenProvider), typeof(SelectMemberControl), new PropertyMetadata(null));


        #endregion

        protected override void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //Selected item
            var selected = (e.AddedItems == null || e.AddedItems.Count == 0 || e.AddedItems[0] == null || !(e.AddedItems[0] is ILEditTreeNode)) ? null : ((ILEditTreeNode)e.AddedItems[0]).TokenProvider;

            //Checks the token
            if (selected == null || !SelectableMembers.Any(x => x == selected.MetadataToken.TokenType))
            {
                SelectedMember = null;
            }
            else
            {
                SelectedMember = selected;
            }

            //Call to base method
            base.OnSelectionChanged(e);
        }

    }
}
