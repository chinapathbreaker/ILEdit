using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILEdit.Injection;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;
using System.Windows;

namespace ILEdit.Injection.Injectors
{
    /// <summary>
    /// Field injector
    /// </summary>
    public class FieldInjector : IInjector
    {
        #region Properties

        public string Name
        {
            get { return "Field"; }
        }

        public string Description
        {
            get { return "Injects a new field"; }
        }

        public System.Windows.Media.ImageSource Icon
        {
            get { return new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Field.png")); }
        }

        public bool NeedsMember
        {
            get { return true; }
        }

        public Predicate<Mono.Cecil.IMetadataTokenProvider> MemberFilter
        {
            get { return MemberFilters.Types; }
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

            //Can inject only in types (except interfaces)
            return type != null && !type.IsInterface;
        }

        public void Inject(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node, string name, IMetadataTokenProvider member)
        {
            //Type
            var type = (TypeDefinition)((IMemberTreeNode)node).Member;

            //Creates the field definition
            var field = new FieldDefinition(
                name,
                FieldAttributes.Public,
                type.Module.Import((TypeReference)member, type)
            ) {
                MetadataToken = new MetadataToken(TokenType.Field, ILEdit.GlobalContainer.GetFreeRID(type.Module))
            };

            //Adds the field to the type
            type.Fields.Add(field);
            if (node is TypeTreeNode)
            {
                node.Children.Add(new ILEditTreeNode(field, true));
                Helpers.Tree.SortChildren((TypeTreeNode)node);
            }
            else if (node is ILEditTreeNode)
            {
                ((ILEditTreeNode)node).RefreshChildren();
            }
        }
    }
}
