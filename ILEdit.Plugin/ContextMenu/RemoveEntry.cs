using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using System.Windows;
using Mono.Cecil;

namespace ILEdit.ContextMenu
{
    [ExportContextMenuEntry(Icon = "Images/Delete.png", Header = "Remove ...", Category = "Injection", Order = 3)]
    public class RemoveEntry : IContextMenuEntry
    {
        public bool IsVisible(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            var node = selectedNodes[0];
            return
                (
                    node is ModuleTreeNode ||
                    node is AssemblyReferenceTreeNode ||
                    (node is IMemberTreeNode && ((IMemberTreeNode)node).Member is Mono.Cecil.IMemberDefinition)
                ) && !(node is ICSharpCode.ILSpy.TreeNodes.Analyzer.AnalyzerTreeNode);
        }

        public bool IsEnabled(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            return true;
        }

        public void Execute(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            //Node
            var node = selectedNodes[0];

            //Confirmation message
            if (MessageBox.Show("Are you sure you want to remove " + node.Text + "?" + Environment.NewLine + "Warning: this action may break some references.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            //Checks if the node is an assembly reference
            if (node is AssemblyReferenceTreeNode)
            {
                //Walks the tree until it finds the module
                ModuleTreeNode module = null;
                ICSharpCode.TreeView.SharpTreeNode currentNode = node;
                while (module == null)
                {
                    currentNode = currentNode.Parent;
                    module = currentNode as ModuleTreeNode;
                }

                //Removes the reference from the module
                module.Module.AssemblyReferences.Remove(((AssemblyReferenceTreeNode)node).Reference);
            }
            else if (node is ModuleTreeNode) //Checks if the node is a module
            {
                //Module
                var m = ((ModuleTreeNode)node).Module;

                //Checks that this isn't the only module in the assembly
                if (m.Assembly.Modules.Count == 1)
                {
                    MessageBox.Show("Cannot remove the only module of an assembly", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //Removes the module
                m.Assembly.Modules.Remove(m);
            }
            else
            {
                //Gets the member
                var m = ((IMemberTreeNode)node).Member;

                //Switches on the type of the member
                switch (m.MetadataToken.TokenType)
                {
                    //Type
                    case TokenType.TypeDef:
                        var type = (TypeDefinition)m;
                        if (type.IsNested)
                            type.DeclaringType.NestedTypes.Remove(type);
                        else
                            type.Module.Types.Remove(type);
                        break;
                    //Field
                    case TokenType.Field:
                        var field = (FieldDefinition)m;
                        field.DeclaringType.Fields.Remove(field);
                        break;
                    //Method
                    case TokenType.Method:
                        var method = (MethodDefinition)m;
                        method.DeclaringType.Methods.Remove(method);
                        break;
                    //Event
                    case TokenType.Event:
                        var evt = (EventDefinition)m;
                        evt.DeclaringType.Events.Remove(evt);
                        break;
                    //Property
                    case TokenType.Property:
                        var property = (PropertyDefinition)m;
                        property.DeclaringType.Properties.Remove(property);
                        break;
                    //Other
                    default:
                        throw new ArgumentException("Cannot remove a " + m.MetadataToken.TokenType.ToString());
                }
            }

            //Removes the node
            var parent = node.Parent;
            parent.Children.Remove(node);

            //Collapses the parent if it has no more children
            if (parent.Children.Count == 0)
                parent.IsExpanded = false;
        }
    }
}
