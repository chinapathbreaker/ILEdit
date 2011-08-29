using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ICSharpCode.TreeView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ILEdit.Injection.Existing.Importers
{
    public class TypeImporter : MemberImporter
    {
        private TypeDefinition typeClone;

        public TypeImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination)
            : base(member, destination)
        {
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member.MetadataToken.TokenType == TokenType.TypeDef && (destination.MetadataToken.TokenType == TokenType.TypeDef || destination.MetadataToken.TokenType == TokenType.Module);
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Clones the type
            typeClone = ((TypeDefinition)Member).Clone();

            //Registers the importing of the custom attributes of this class
            if (typeClone.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(typeClone, typeClone).Scan(options));
                typeClone.CustomAttributes.Clear();
            }

            //TODO: Generic parameters and constraints

            //Registers the importing of the fields
            foreach (var f in typeClone.Fields)
            {
                options.CancellationToken.ThrowIfCancellationRequested();
                importList.Add(new FieldImporter(f, typeClone).Scan(options));
                typeClone.Fields.Clear();
            }

            //TODO: other members
        }

        protected override IMetadataTokenProvider ImportCore(MemberImportingOptions options, SharpTreeNode node)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Checks if the destination is a module or a type
            if (Destination is ModuleDefinition)
            {
                //Destination
                var dest = (ModuleDefinition)Destination;
                dest.Types.Add(typeClone);

                //Finds the correct namespace
                var ns = typeClone.Namespace;
                var moduleNode = Helpers.Tree.GetModuleNode((ModuleDefinition)Destination);
                var nsNode = moduleNode.Children.OfType<NamespaceTreeNode>().FirstOrDefault(x => x.Name == ns);
                if (nsNode != null)
                    nsNode.AddChildAndColorAncestors(new ILEditTreeNode(typeClone, false));
                else
                {
                    nsNode = new NamespaceTreeNode(typeClone.Namespace) { Foreground = GlobalContainer.ModifiedNodesBrush };
                    nsNode.Children.Add(new ILEditTreeNode(typeClone, false));
                    moduleNode.AddChildAndColorAncestors(nsNode);
                }
            }
            else
            {
                //Destination
                var dest = (TypeDefinition)Destination;
                dest.NestedTypes.Add(typeClone);
                node.AddChildAndColorAncestors(new ILEditTreeNode(typeClone, false));
            }

            //Return
            return typeClone;
        }

        protected override void DisposeCore()
        {
            //Clears the fields
            typeClone = null;
        }
    }
}
