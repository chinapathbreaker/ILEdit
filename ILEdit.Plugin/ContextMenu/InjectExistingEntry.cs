using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using ICSharpCode.TreeView;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using System.Windows;

namespace ILEdit.ContextMenu
{
    [ExportContextMenuEntry(Icon = "Images/InjectExisting.png", Header = "Inject existing ...", Category = "Injection", Order = 1)]
    public class InjectExistingEntry : IContextMenuEntry
    {
        private static Type[] AllowedNodeTypes = new Type[] { 
            typeof(AssemblyTreeNode),
            typeof(ModuleTreeNode)
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
            new Injection.InjectWindow(node, 1, true).ShowDialog();
        }
    }
}
