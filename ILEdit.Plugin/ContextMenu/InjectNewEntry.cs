using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using System.Windows;

namespace ILEdit.ContextMenu
{
    [ExportContextMenuEntry(Icon = "Images/InjectNew.png", Header = "Inject new ...", Category = "Injection", Order = 0)]
    public class InjectNewEntry : IContextMenuEntry
    {
        private static Type[] AllowedNodeTypes = new Type[] { 
            typeof(AssemblyTreeNode),
            typeof(ModuleTreeNode),
            typeof(ReferenceFolderTreeNode)
        };

        public bool IsVisible(SharpTreeNode[] selectedNodes)
        {
            var memberNode = selectedNodes[0] as IMemberTreeNode;
            var nodeType = selectedNodes[0].GetType();
            return 
                (
                    AllowedNodeTypes.Any(x => x.IsAssignableFrom(nodeType)) ||
                    (memberNode != null && memberNode.Member.MetadataToken.TokenType == Mono.Cecil.TokenType.TypeDef)
                )
                &&
                !(selectedNodes[0] is ICSharpCode.ILSpy.TreeNodes.Analyzer.AnalyzerTreeNode);
        }

        public bool IsEnabled(SharpTreeNode[] selectedNodes)
        {
            return selectedNodes.Length == 1;
        }

        public void Execute(SharpTreeNode[] selectedNodes)
        {
            //Cast
            var node = (ILSpyTreeNode)selectedNodes[0];

            //Checks if the selected node is an assembly
            if (node is AssemblyTreeNode)
            {
                //Forces lazy-loading
                node.EnsureLazyChildren();

                //Checks if it's a multi-module assembly
                if (node.Children.Count > 1)
                {
                    MessageBox.Show("In a multi-module assembly select a specific module", "Select module", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //Takes the first (and only) module
                node = (ILSpyTreeNode)node.Children[0];
            }

            //Shows the injection window
            new Injection.InjectWindow(node, 0, new InjectExistingEntry().IsVisible(selectedNodes)).ShowDialog();
        }
    }
}
