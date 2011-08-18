using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ILEdit.Injection.Injectors
{
    /// <summary>
    /// Assembly reference injector
    /// </summary>
    public class AssemblyReferenceInjector : IInjector
    {
        #region Properties

        public string Name
        {
            get { return "Assembly reference"; }
        }

        public string Description
        {
            get { return "Injects a new assembly reference. Full name required!"; }
        }

        public System.Windows.Media.ImageSource Icon
        {
            get { return new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Assembly.png")); }
        }

        public bool NeedsMember
        {
            get { return false; }
        }

        public Predicate<Mono.Cecil.IMetadataTokenProvider> MemberFilter
        {
            get { return null; }
        }

        public TokenType SelectableMembers
        {
            get { return TokenType.TypeDef; }
        }

        #endregion

        public bool CanInjectInNode(ILSpyTreeNode node)
        {
            //Can inject only in ReferenceFolderTreeNode
            return node is ReferenceFolderTreeNode;
        }

        public void Inject(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node, string name, IMetadataTokenProvider member)
        {
            //Gets the parent module
            var moduleNode = TreeHelper.GetModuleNode(node);

            //Injects the assembly reference
            moduleNode.Module.AssemblyReferences.Add(AssemblyNameReference.Parse(name));

            //Adds the node
            node.Children.Add(new AssemblyReferenceTreeNode(AssemblyNameReference.Parse(name), TreeHelper.GetAssemblyNode(moduleNode)) { ForegroundColor = GlobalContainer.ModifiedNodesColor });
            TreeHelper.SortChildren((ReferenceFolderTreeNode)node);
        }
    }
}
