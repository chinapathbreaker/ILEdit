using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.TreeView;
using Mono.Cecil;
using System.Windows.Media;

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
                foreach (var asm in ICSharpCode.ILSpy.MainWindow.Instance.CurrentAssemblyList.GetAssemblies())
                    Root.Children.Add(new ILEditTreeNode(asm.AssemblyDefinition, false) { ChildrenFilter = MemberFilter, ForegroundColor = Colors.Black });
            };
        }

        /// <summary>
        /// Filter used to choose which member to show
        /// </summary>
        public Predicate<IMetadataTokenProvider> MemberFilter { get; set; }

        /// <summary>
        /// Gets or sets the TokenType representing which members the user can select
        /// </summary>
        public TokenType SelectableMembers { get; set; }

        /// <summary>
        /// Returns the selected member
        /// </summary>
        public IMetadataTokenProvider SelectedMember 
        {
            get 
            {
                //Selected item
                var selected = this.SelectedItem == null ? null : ((ILEditTreeNode)this.SelectedItem).TokenProvider; 

                //Checks the token
                if (selected == null || selected.MetadataToken.TokenType != SelectableMembers)
                {
                    return null;
                }
                else
                {
                    return selected;
                }
            } 
        }
    }
}
