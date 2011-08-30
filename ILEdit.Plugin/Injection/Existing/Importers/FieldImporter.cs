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
        
        public FieldImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination)
            : base(member, destination)
        {
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

            //Destination type
            var destType = (TypeDefinition)Destination;

            //Imports the type
            var typeImporter = Helpers.CreateTypeImporter(fieldType, destType, importList, options);
            importList.Add(typeImporter);
            typeImporter.ImportFinished += (typeRef) => fieldClone.FieldType = (TypeReference)typeRef;

            //Checks the attributes of the field
            if (fieldClone.HasCustomAttributes)
                importList.Add(new CustomAttributesImporter(fieldClone, fieldClone).Scan(options));
        }

        protected override IMetadataTokenProvider ImportCore(MemberImportingOptions options, SharpTreeNode node)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Adds the field to the destination type
            ((TypeDefinition)Destination).Fields.Add(fieldClone);
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
