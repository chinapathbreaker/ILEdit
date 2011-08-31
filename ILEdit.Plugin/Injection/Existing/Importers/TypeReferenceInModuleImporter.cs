using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ICSharpCode.TreeView;

namespace ILEdit.Injection.Existing.Importers
{
    /// <summary>
    /// Type reference importer. It's only a MemberImporter-way to call ModuleDefinition.Import()
    /// </summary>
    internal class TypeReferenceInModuleImporter : MemberImporter
    {
        public TypeReferenceInModuleImporter(IMetadataTokenProvider member, MemberImportingSession session)
            : base(member, session.DestinationModule, session)
        {
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member is TypeReference && destination is ModuleDefinition;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();
        }

        protected override IEnumerable<IMetadataTokenProvider> GetMembersForPreview()
        {
            return new IMetadataTokenProvider[] { };
        }

        protected override IMetadataTokenProvider ImportCore(MemberImportingOptions options, SharpTreeNode node)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Imports and returns
            return Session.DestinationModule.Import((TypeReference)Member);
        }
    }
}
