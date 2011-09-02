using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ILEdit.Injection.Existing.Importers;

namespace ILEdit.Injection.Existing
{
    /// <summary>
    /// Represents a single session of member importing
    /// </summary>
    public class MemberImportingSession
    {
        #region .ctor
        
        public MemberImportingSession(AssemblyDefinition asm, ModuleDefinition module, TypeDefinition type, MemberImportingOptions options)
        {
            //Checks that the parameters aren't null
            if (asm == null)
                throw new ArgumentNullException("asm");
            if (module == null)
                throw new ArgumentNullException("module");
            if (options == null)
                throw new ArgumentNullException("options");

            //Stores the parameters
            _DestinationAssembly = asm;
            _DestinationModule = module;
            _DestinationType = type;
            _Options = options;
        }

        #endregion

        #region Session properties


        private AssemblyDefinition _DestinationAssembly = null;
        /// <summary>
        /// Assembly where the member will be imported
        /// </summary>
        public AssemblyDefinition DestinationAssembly { get { return _DestinationAssembly; } }
        

        private ModuleDefinition _DestinationModule;
        /// <summary>
        /// Module where the member will be imported
        /// </summary>
        public ModuleDefinition DestinationModule { get { return _DestinationModule; } }


        private TypeDefinition _DestinationType;
        /// <summary>
        /// Type where the member will be imported
        /// </summary>
        public TypeDefinition DestinationType { get { return _DestinationType; } }


        /// <summary>
        /// Returns the effective destination: the type or the module (if the type is null)
        /// </summary>
        public IMetadataTokenProvider Destination 
        {
            get { return (IMetadataTokenProvider)_DestinationType ?? (IMetadataTokenProvider)_DestinationModule; } 
        }


        private Dictionary<IMetadataTokenProvider, MemberImporter> _RegisteredImporters = new Dictionary<IMetadataTokenProvider,MemberImporter>();
        /// <summary>
        /// Returns a dictionary containing the registered importers (used to avoid double references or double types)
        /// </summary>
        public Dictionary<IMetadataTokenProvider, MemberImporter> RegisteredImporters { get { return _RegisteredImporters; } }
        

        private MemberImportingOptions _Options;
        /// <summary>
        /// Importing options
        /// </summary>
        public MemberImportingOptions Options { get { return _Options; } }

        #endregion

        #region CreateImporter method

        /// <summary>
        /// Creates the suitable importer for the givem member
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public MemberImporter CreateImporter(IMetadataTokenProvider member)
        {
            //Switches the token type
            switch (member.MetadataToken.TokenType)
            {
                case TokenType.TypeDef:
                    return new TypeImporter(member, Destination, this);
                case TokenType.Field:
                    return new FieldImporter(member, Destination, this);
                case TokenType.Method:
                    return new MethodImporter(member, Destination, this);
                case TokenType.Property:
                    return new PropertyImporter(member, Destination, this);
                case TokenType.Event:
                default:
                    throw new ArgumentException("Cannot create an importer for " + member.ToString());
            }
        }

        #endregion
    }
}
