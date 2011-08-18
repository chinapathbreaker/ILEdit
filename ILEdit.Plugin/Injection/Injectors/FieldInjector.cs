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

        public TokenType SelectableMembers
        {
            get { return TokenType.TypeDef; }
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

        public void Inject(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node, string name, IMetadataTokenProvider member)
        {
            //Creates the field definition
            var field = new FieldDefinition(
                name,
                FieldAttributes.Public,
                (TypeDefinition)member
            ) {
                MetadataToken = new MetadataToken(TokenType.Field)
            };

            //Adds the field to the type
            var typeNode = (TypeTreeNode)node;
            typeNode.TypeDefinition.Fields.Add(field);
            typeNode.Children.Add(new ILEditTreeNode(field, false));
            TreeHelper.SortChildren(typeNode);
        }
    }
}
