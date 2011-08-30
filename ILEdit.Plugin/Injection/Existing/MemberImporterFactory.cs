using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.Collections.ObjectModel;
using ICSharpCode.TreeView;

namespace ILEdit.Injection.Existing
{
    /// <summary>
    /// Represents a set of static methods to create IMemberImporter objects
    /// </summary>
    public static class MemberImporterFactory
    {
        /// <summary>
        /// Creates the IMemberImport to import the given member to the given destination
        /// </summary>
        /// <param name="member">Member to import</param>
        /// <param name="destination">Destination of the importing</param>
        /// <returns></returns>
        public static MemberImporter Create(IMetadataTokenProvider member, IMetadataTokenProvider destination, ModuleDefinition destModule)
        {
            //Switches on the token type of the member
            switch (member.MetadataToken.TokenType)
            {
                case TokenType.Field:
                    return new Importers.FieldImporter(member, destination, destModule);
                case TokenType.TypeDef:
                case TokenType.Method:
                case TokenType.Event:
                case TokenType.Property:
                default:
                    throw new NotImplementedException("Importing of " + member.MetadataToken.TokenType.ToString() + " not implemented");
            }
        }
    }
}
