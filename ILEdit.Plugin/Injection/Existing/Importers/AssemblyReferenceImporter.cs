using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ICSharpCode.TreeView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ILEdit.Injection.Existing.Importers
{
    internal class AssemblyReferenceImporter : MemberImporter, IMetadataTokenProvider
    {
        public AssemblyReferenceImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination)
            : base(member, destination)
        {
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member is AssemblyNameReference && destination is ModuleDefinition;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();
        }

        protected override IEnumerable<IMetadataTokenProvider> GetMembersForPreview()
        {
            return new IMetadataTokenProvider[] { this };
        }

        protected override IMetadataTokenProvider ImportCore(MemberImportingOptions options, SharpTreeNode node)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Assembly and module
            var asm = (AssemblyNameReference)Member;
            var module = ((ModuleDefinition)Destination);

            //Adds the reference only if it doesn't already exist
            if (module.AssemblyReferences.Any(x => x.FullName == asm.FullName))
            {
                module.AssemblyReferences.Add(asm);
                Helpers.Tree.GetModuleNode(module)
                    .Children.FirstOrDefault(x => x is ReferenceFolderTreeNode)
                    .AddChildAndColorAncestors(new ILEditTreeNode(asm, false));
            }

            //Returns null
            return null;
        }

        public MetadataToken MetadataToken
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
