using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILEdit.Injection;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;

namespace ILEdit.Injection.Injectors
{
    /// <summary>
    /// Class injector
    /// </summary>
    public class ClassInjector : IInjector
    {
        #region Properties

        public string Name
        {
            get { return "Class"; }
        }

        public string Description
        {
            get { return "Injects a new class"; }
        }

        public System.Windows.Media.ImageSource Icon
        {
            get { return new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Class.png")); }
        }

        public bool NeedsMember
        {
            get { return false; }
        }

        public Predicate<Mono.Cecil.IMetadataTokenProvider> MemberFilter
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        public bool CanInjectInNode(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node)
        {
            //Try-cast
            var memberNode = node as IMemberTreeNode;
            var type = memberNode == null ? null : (memberNode.Member as TypeDefinition);

            //Can inject only in modules and other existing types (except enums and interfaces)
            return
                node is ModuleTreeNode ||
                (memberNode != null && 
                    type != null &&
                    !type.IsEnum &&
                    !type.IsInterface
                );
        }

        public void Inject(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node, string name)
        {
            //Name and namespace
            var typeName = node is ModuleTreeNode ? (name.Substring(name.Contains(".") ? name.LastIndexOf('.') + 1 : 0)) : name;
            var typeNamespace = node is ModuleTreeNode ? (name.Substring(0, name.Contains(".") ? name.LastIndexOf('.') : 0)) : string.Empty;

            //Creates a new class definition
            var c = new TypeDefinition (
                typeNamespace,
                typeName,
                TypeAttributes.Class | TypeAttributes.Public
            ) {
                IsClass = true,
                IsPublic = true
            };

            //Checks if the node is a module or not
            if (node is ModuleTreeNode)
            {
                //Module node
                var moduleNode = (ModuleTreeNode)node;

                //Injects in the module
                var module = moduleNode.Module;
                module.Types.Add(c);

                //Checks for the namespace
                var namespaceNode =
                    moduleNode.Children
                    .OfType<NamespaceTreeNode>()
                    .FirstOrDefault(x => x.Text.ToString().ToLower() == (string.IsNullOrEmpty(typeNamespace) ? "-" : typeNamespace.ToLower()));
                if (namespaceNode != null)
                {
                    //Adds the node to the namespace
                    namespaceNode.Children.Add(new ILEditTreeNode(c, false));
                    TreeHelper.SortChildren(namespaceNode);
                }
                else
                {
                    //Creates a new namespace containing the new type and adds it to the module node
                    namespaceNode = new NamespaceTreeNode(typeNamespace);
                    namespaceNode.Children.Add(new ILEditTreeNode(c, false));
                    moduleNode.Children.Add(namespaceNode);
                    TreeHelper.SortChildren(moduleNode);
                }
            }
            else
            {
                //Marks the class as nested public
                c.Attributes |= TypeAttributes.NestedPublic;
                c.IsNestedPublic = true;

                //Injects in the type
                var type = (TypeDefinition)((IMemberTreeNode)node).Member;
                type.NestedTypes.Add(c);

                //Adds a node to the tree
                node.Children.Add(new ILEditTreeNode(c, false));
                TreeHelper.SortChildren((TypeTreeNode)node);
            }
        }
    }
}
