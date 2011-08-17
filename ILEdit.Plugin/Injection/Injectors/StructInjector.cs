using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;
using System.Windows;

namespace ILEdit.Injection.Injectors
{
    public class StructInjector : IInjector
    {
        #region Properties

        public string Name
        {
            get { return "Struct"; }
        }

        public string Description
        {
            get { return "Injects a new structure"; }
        }

        public System.Windows.Media.ImageSource Icon
        {
            get { return new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Struct.png")); }
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

            //Checks that the typename isn't empty
            if (string.IsNullOrEmpty(typeName))
            {
                MessageBox.Show("Please, specify the name of the type", "Type name required", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Creates a new class definition
            var c = new TypeDefinition(
                typeNamespace,
                typeName,
                TypeAttributes.Class | TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.Public
            ) {
                IsClass = true,
                IsPublic = true,
                IsValueType = true,
                IsSealed = true,
                IsSequentialLayout = true
            };

            //Adds the type
            TreeHelper.AddTreeNode(node, c,
                module => { c.BaseType = module.Import(new TypeReference("System", "ValueType", module, module.TypeSystem.Corlib, true)); },
                type => { c.BaseType = type.Module.Import(new TypeReference("System", "ValueType", type.Module, type.Module.TypeSystem.Corlib, true)); }
            );
        }
    }
}
