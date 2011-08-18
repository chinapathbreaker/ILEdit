using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;
using System.Windows;

namespace ILEdit.Injection.Injectors
{
    public class DelegateInjector : IInjector
    {
        #region Properties

        public string Name
        {
            get { return "Delegate"; }
        }

        public string Description
        {
            get { return "Injects a new delegate and the relative methods .ctor, Invoke, BeginInvoke, and EndInvoke"; }
        }

        public System.Windows.Media.ImageSource Icon
        {
            get { return new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Delegate.png")); }
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
                TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.Public
            )
            {
                IsAnsiClass = true,
            };

            //Module
            ModuleDefinition module = null;

            //Adds the type
            TreeHelper.AddTreeNode(node, c,
                m => { module = m; c.BaseType = m.Import(new TypeReference("System", "MulticastDelegate", m, m.TypeSystem.Corlib)); },
                type => { module = type.Module; c.BaseType = type.Module.Import(new TypeReference("System", "MulticastDelegate", type.Module, type.Module.TypeSystem.Corlib)); }
            );

            //Adds the .ctor method
            c.Methods.Add(new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void
            )
            {
                IsPublic = true,
                IsSpecialName = true,
                IsRuntimeSpecialName = true,
                IsRuntime = true,
                IsManaged = true,
                Parameters = { 
                    new ParameterDefinition("object", ParameterAttributes.None, module.TypeSystem.Object),
                    new ParameterDefinition("method", ParameterAttributes.None, module.TypeSystem.IntPtr)
                }
            });

            //Adds the Invoke() method
            c.Methods.Add(new MethodDefinition(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                module.TypeSystem.Void
            )
            {
                IsPublic = true,
                IsNewSlot = true,
                IsVirtual = true,
                IsRuntime = true,
                IsManaged = true
            });

            //Adds the BeginInvoke() method
            c.Methods.Add(new MethodDefinition(
                "BeginInvoke",
                MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                new TypeReference("System", "IAsyncResult", module, module.TypeSystem.Corlib)
            )
            {
                IsPublic = true,
                IsNewSlot = true,
                IsVirtual = true,
                IsRuntime = true,
                IsManaged = true,
                Parameters = { 
                    new ParameterDefinition("callback", ParameterAttributes.None, new TypeReference("System", "AsyncCallback", module, module.TypeSystem.Corlib)),
                    new ParameterDefinition("object", ParameterAttributes.None, module.TypeSystem.Object)
                }
            });

            //Adds the EndInvoke() method
            c.Methods.Add(new MethodDefinition(
                "EndInvoke",
                MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                module.TypeSystem.Void
            )
            {
                IsPublic = true,
                IsNewSlot = true,
                IsVirtual = true,
                IsRuntime = true,
                IsManaged = true,
                Parameters = { 
                    new ParameterDefinition("result", ParameterAttributes.None, new TypeReference("System", "IAsyncResult", module, module.TypeSystem.Corlib))
                }
            });
        }
    }
}
