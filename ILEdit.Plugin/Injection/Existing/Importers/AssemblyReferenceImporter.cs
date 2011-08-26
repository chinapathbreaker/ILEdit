using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing.Importers
{
    internal class AssemblyReferenceImporter : MemberImporter
    {
        public AssemblyReferenceImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination)
            : base(member, destination)
        {
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member is AssemblyDefinition && destination is ModuleDefinition;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();
        }

        protected override IMetadataTokenProvider ImportCore(MemberImportingOptions options)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Assembly and module
            var asm = (AssemblyDefinition)Member;
            var module = ((ModuleDefinition)Destination);

            //Adds the reference only if it doesn't already exist
            if (module.AssemblyReferences.Any(x => x.FullName == asm.Name.FullName))
                module.AssemblyReferences.Add(asm.Name);

            //Returns null
            return null;
        }
    }
}
