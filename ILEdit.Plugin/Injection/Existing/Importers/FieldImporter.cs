using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ICSharpCode.TreeView;

namespace ILEdit.Injection.Existing.Importers
{
    /// <summary>
    /// Represents a field importer
    /// </summary>
    public class FieldImporter : MemberImporter
    {
        private FieldDefinition fieldClone;
        private bool _createNode;

        public FieldImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session)
            : this(member, destination, session, true)
        {
        }

        public FieldImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session, bool createNode)
            : base(member, destination, session)
        {
            _createNode = createNode;
        }

        protected override bool CanImportCore(IMetadataTokenProvider member, IMetadataTokenProvider destination)
        {
            return member.MetadataToken.TokenType == TokenType.Field && destination.MetadataToken.TokenType == TokenType.TypeDef;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Field
            fieldClone = ((FieldDefinition)Member).Clone();

            //Field type
            var fieldType = fieldClone.FieldType;

            //Imports the type
            var typeImporter = Helpers.CreateTypeImporter(fieldType, Session, importList, options);
            importList.Add(typeImporter);
            typeImporter.ImportFinished += (typeRef) => fieldClone.FieldType = (TypeReference)typeRef;

            //Checks the attributes of the field
            if (fieldClone.HasCustomAttributes)
                importList.Add(new CustomAttributesImporter(fieldClone, fieldClone, Session).Scan(options));
        }

        protected override IEnumerable<IMetadataTokenProvider> GetMembersForPreview()
        {
            return _createNode ? base.GetMembersForPreview() : base.GetMembersForPreview().Except(new[] { Member });
        }

        protected override IMetadataTokenProvider ImportCore(MemberImportingOptions options, SharpTreeNode node)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Adds the field to the destination type
            ((TypeDefinition)Destination).Fields.Add(fieldClone);
            if (_createNode)
                node.AddChildAndColorAncestors(new ILEditTreeNode(fieldClone, false));

            //Returns the new field
            return fieldClone;
        }

        protected override void DisposeCore()
        {
            //Nulls the field
            fieldClone = null;
        }
    }
}
