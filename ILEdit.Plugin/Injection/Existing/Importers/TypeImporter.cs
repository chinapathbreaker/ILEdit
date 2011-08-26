using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

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
            //Clones the type
            typeClone = ((TypeDefinition)Member).Clone();

            //Registers the importing of the custom attributes of this class
            if (typeClone.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(typeClone, typeClone));
                typeClone.CustomAttributes.Clear();
            }

            //TODO: Generic parameters and constraints

            //Registers the importing of the fields
            foreach (var f in typeClone.Fields)
            {
                importList.Add(new FieldImporter(f, typeClone));
                typeClone.Fields.Clear();
            }

            //TODO: other members
        }

        protected override IMetadataTokenProvider ImportCore(MemberImportingOptions options)
        {
            //Checks if the destination is a module or a type
            if (Destination is ModuleDefinition)
            {
                //Destination
                var dest = (ModuleDefinition)Destination;
                dest.Types.Add(typeClone);
            }
            else
            {
                //Destination
                var dest = (TypeDefinition)Destination;
                dest.NestedTypes.Add(typeClone);
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
