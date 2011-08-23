using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil.Cil;

namespace ILEdit.Injection.Injectors
{
    /// <summary>
    /// Event injector
    /// </summary>
    public class EventInjector : IInjector
    {
        #region Properties

        public string Name
        {
            get { return "Event"; }
        }

        public string Description
        {
            get { return "Injects a new event, its addon and removeon methods and the backing field"; }
        }

        public System.Windows.Media.ImageSource Icon
        {
            get { return new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Event.png")); }
        }

        public bool NeedsMember
        {
            get { return true; }
        }

        public Predicate<Mono.Cecil.IMetadataTokenProvider> MemberFilter
        {
            get { return MemberFilters.Delegates; }
        }

        public TokenType[] SelectableMembers
        {
            get { return new TokenType[] { TokenType.TypeDef }; }
        } 

        #endregion

        public bool CanInjectInNode(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node)
        {
            //Try-cast
            var memberNode = node as IMemberTreeNode;
            var type = memberNode == null ? null : (memberNode.Member as TypeDefinition);

            //Can inject only in types
            return type != null;
        }

        public void Inject(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node, string name, Mono.Cecil.IMetadataTokenProvider member)
        {
            //Type
            var type = (TypeDefinition)((IMemberTreeNode)node).Member;

            //Event type
            var eventType = type.Module.Import((TypeReference)member, type);

            //Creates the event
            var evt = new EventDefinition(name, EventAttributes.None, eventType) { MetadataToken = new MetadataToken(TokenType.Event, GlobalContainer.GetFreeRID(type.Module)) };

            //Creates the field
            FieldReference backingField = new FieldDefinition(
                name,
                FieldAttributes.Private,
                eventType
            )
            {
                MetadataToken = new MetadataToken(TokenType.Field, GlobalContainer.GetFreeRID(type.Module))
            };
            type.Fields.Add((FieldDefinition)backingField);

            //Checks if the type is generic
            if (type.HasGenericParameters)
            {
                var giType = new GenericInstanceType(type);
                foreach (var x in type.GenericParameters)
                    giType.GenericArguments.Add(x);
                backingField = new FieldReference(backingField.Name, eventType, giType);
            }

            //Creates the addon method
            evt.AddMethod = new MethodDefinition(
                "add_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName,
                type.Module.TypeSystem.Void
            )
            {
                IsSynchronized = true,
                IsAddOn = true,
                MetadataToken = new MetadataToken(TokenType.Method, GlobalContainer.GetFreeRID(type.Module)),
                Parameters = { 
                    new ParameterDefinition("value", ParameterAttributes.None, eventType)
                }
            };

            //Writes the instruction of the addon method
            var addBody = evt.AddMethod.Body;
            addBody.MaxStackSize = 8;
            addBody.GetType().GetField("code_size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(addBody, 1);
            var addIL = addBody.GetILProcessor();
            addIL.Emit(OpCodes.Ldarg_0);
            addIL.Emit(OpCodes.Ldarg_0);
            addIL.Emit(OpCodes.Ldfld, backingField);
            addIL.Emit(OpCodes.Ldarg_1);
            //Delegate.Combine(Delegate, Delegate)
            addIL.Emit(OpCodes.Call, type.Module.Import(new TypeReference("System", "Delegate", type.Module, type.Module.TypeSystem.Corlib).Resolve().Methods.First(x => x.Name == "Combine" && x.IsStatic && x.Parameters.Count == 2 && x.Parameters.All(p => p.ParameterType.FullName == "System.Delegate"))));
            addIL.Emit(OpCodes.Castclass, eventType);
            addIL.Emit(OpCodes.Stfld, backingField);
            addIL.Emit(OpCodes.Ret);

            //Creates the removeon method
            evt.RemoveMethod = new MethodDefinition(
                "remove_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName,
                type.Module.TypeSystem.Void
            )
            {
                IsSynchronized = true,
                IsRemoveOn = true,
                MetadataToken = new MetadataToken(TokenType.Method, GlobalContainer.GetFreeRID(type.Module)),
                Parameters = { 
                    new ParameterDefinition("value", ParameterAttributes.None, eventType)
                }
            };

            //Writes the instruction of the removeon method
            var removeBody = evt.RemoveMethod.Body;
            removeBody.MaxStackSize = 8;
            removeBody.GetType().GetField("code_size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(removeBody, 1);
            var removeIL = removeBody.GetILProcessor();
            removeIL.Emit(OpCodes.Ldarg_0);
            removeIL.Emit(OpCodes.Ldarg_0);
            removeIL.Emit(OpCodes.Ldfld, backingField);
            removeIL.Emit(OpCodes.Ldarg_1);
            //Delegate.Remove(Delegate, Delegate)
            removeIL.Emit(OpCodes.Call, type.Module.Import(new TypeReference("System", "Delegate", type.Module, type.Module.TypeSystem.Corlib).Resolve().Methods.First(x => x.Name == "Remove" && x.IsStatic && x.Parameters.Count == 2 && x.Parameters.All(p => p.ParameterType.FullName == "System.Delegate"))));
            removeIL.Emit(OpCodes.Castclass, eventType);
            removeIL.Emit(OpCodes.Stfld, backingField);
            removeIL.Emit(OpCodes.Ret);

            //Adds the members to the type
            type.Methods.Add(evt.AddMethod);
            type.Methods.Add(evt.RemoveMethod);
            type.Events.Add(evt);

            //Creates the nodes
            if (node is TypeTreeNode)
            {
                node.Children.Add(new ILEditTreeNode(backingField, true));
                node.Children.Add(new ILEditTreeNode(evt, false));
                TreeHelper.SortChildren((TypeTreeNode)node);
            }
            else if (node is ILEditTreeNode)
            {
                ((ILEditTreeNode)node).RefreshChildren();
            }

        }
    }
}
