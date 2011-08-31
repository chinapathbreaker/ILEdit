using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing.Importers
{
    internal class TypeSpecificationImporter : MemberImporter
    {
        private TypeSpecification retType;

        public TypeSpecificationImporter(TypeSpecification type, MemberImportingSession session)
            : base(type, session)
        {
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member is TypeSpecification;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Type
            var type = (TypeSpecification)Member;

            //Checks if the type is a generic instance type
            MemberImporter importer;
            if (type is GenericInstanceType)
            {
                importer = new GenericInstanceTypeImporter(type, Session.Destination, Session).Scan(options);
                importList.Add(importer);
                importer.ImportFinished += t => retType = (TypeSpecification)t;
                return;
            }

            //Creates the importer for the element type
            importer = Helpers.CreateTypeImporter(type.ElementType, Session, importList, options);
            importList.Add(importer);

            //Switches on the metadata type of the specification
            switch (type.MetadataType)
            {
                case MetadataType.Pointer:
                    importer.ImportFinished += t => retType = new PointerType((TypeReference)t);
                    break;
                case MetadataType.ByReference:
                    importer.ImportFinished += t => retType = new ByReferenceType((TypeReference)t);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override Mono.Cecil.IMetadataTokenProvider ImportCore(MemberImportingOptions options, ICSharpCode.TreeView.SharpTreeNode node)
        {
            //Returns the type
            return retType;
        }
    }
}
