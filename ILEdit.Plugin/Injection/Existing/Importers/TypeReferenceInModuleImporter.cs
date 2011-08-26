using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing.Importers
{
    /// <summary>
    /// Type reference importer. It's only a MemberImporter-way to call ModuleDefinition.Import()
    /// </summary>
    internal class TypeReferenceInModuleImporter : MemberImporter
    {
        public TypeReferenceInModuleImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination)
            : base(member, destination)
        {
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member is TypeReference && destination is ModuleDefinition;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
        }

        protected override IMetadataTokenProvider ImportCore(MemberImportingOptions options)
        {
            //Imports and returns
            return ((ModuleDefinition)Destination).Import((TypeReference)Member);
        }
    }
}
