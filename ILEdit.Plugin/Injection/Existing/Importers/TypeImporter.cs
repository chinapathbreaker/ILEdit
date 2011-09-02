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

        public TypeImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session)
            : base(member, destination, session)
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
            typeClone = ((TypeDefinition)Member).Clone(Session);

            //Adjusts the visibility of the type
            if (Destination.MetadataToken.TokenType == TokenType.Module)
            {
                //Makes sure that the type isn't marked as nested
                var visibility = typeClone.Attributes & TypeAttributes.VisibilityMask;
                typeClone.Attributes &= ~TypeAttributes.VisibilityMask;
                switch (visibility)
                {
                    case TypeAttributes.Public:
                    case TypeAttributes.NestedPublic:
                        typeClone.Attributes |= TypeAttributes.Public;
                        break;
                    case TypeAttributes.NotPublic:
                    case TypeAttributes.NestedPrivate:
                    case TypeAttributes.NestedFamily:
                    case TypeAttributes.NestedAssembly:
                    case TypeAttributes.NestedFamANDAssem:
                    case TypeAttributes.NestedFamORAssem:
                        typeClone.Attributes |= TypeAttributes.NotPublic;
                        break;
                }
            }
            else
            {
                //Makes sure that the type is marked as nested
                if (typeClone.IsPublic)
                {
                    typeClone.Attributes = typeClone.Attributes & ~TypeAttributes.VisibilityMask | TypeAttributes.NestedPublic;
                }
                else if (typeClone.IsNotPublic)
                {
                    typeClone.Attributes = typeClone.Attributes & ~TypeAttributes.VisibilityMask | TypeAttributes.NestedAssembly;
                }
            }

            //Registers the importing of the custom attributes of this class
            if (typeClone.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(typeClone, typeClone, Session).Scan(options));
                typeClone.CustomAttributes.Clear();
            }
            
            //Throws if cancellation was requested
            options.CancellationToken.ThrowIfCancellationRequested();

            //Registers importing of generic parameters constraints
            if (typeClone.HasGenericParameters)
            {
                importList.Add(new GenericParametersImporter(typeClone, typeClone, Session).Scan(options));
                typeClone.GenericParameters.Clear();
            }

            //Throws if cancellation was requested
            options.CancellationToken.ThrowIfCancellationRequested();

            //Registers importing of base type
            if (typeClone.BaseType != null && typeClone.BaseType.FullName != "System.Object")
            {
                var baseTypeImporter = Helpers.CreateTypeImporter(typeClone.BaseType, Session, importList, options);
                baseTypeImporter.ImportFinished += t => typeClone.BaseType = (TypeReference)t;
            }

            //Throws if cancellation was requested
            options.CancellationToken.ThrowIfCancellationRequested();

            //Registers the importing of the members
            var type = (TypeDefinition)Member;
            var importers =
                type.Fields.Select(f => new FieldImporter(f, typeClone, Session, false).Scan(options))
                .Concat(
                    type.Methods
                    .Where(m => !(m.IsGetter || m.IsSetter || m.IsAddOn || m.IsRemoveOn || m.IsFire || m.IsOther))
                    .Select(m => new MethodImporter(m, typeClone, Session, false).Scan(options))
                ).Concat(type.Properties.Select(p => new PropertyImporter(p, typeClone, Session, false).Scan(options)))
                .Concat(type.Events.Select(e => new EventImporter(e, typeClone, Session, false).Scan(options)));
            foreach (var x in importers)
            {
                options.CancellationToken.ThrowIfCancellationRequested();
                importList.Add(x);
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
                var nsNode = moduleNode.Children.EnsureLazyChildren().OfType<NamespaceTreeNode>().FirstOrDefault(x => x.Name == ns);
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
