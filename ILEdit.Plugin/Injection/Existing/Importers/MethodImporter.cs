using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing.Importers
{
    /// <summary>
    /// Represents a method importer
    /// </summary>
    public class MethodImporter : MemberImporter
    {
        private MethodDefinition methodClone;
        private bool _createNode;

        public MethodImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session)
            : this(member, destination, session, true)
        {
        }

        public MethodImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session, bool createNode)
            : base(member, destination, session)
        {
            _createNode = createNode;
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member.MetadataToken.TokenType == TokenType.Method && destination.MetadataToken.TokenType == TokenType.TypeDef;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Method
            var originalMethod = (MethodDefinition)Member;
            methodClone = originalMethod.Clone();

            //Imports the generic parameters
            if (methodClone.HasGenericParameters)
            {
                importList.Add(new GenericParametersImporter(methodClone, methodClone, Session).Scan(options));
                methodClone.GenericParameters.Clear();
            }

            //Imports the attributes
            if (methodClone.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(methodClone, methodClone, Session).Scan(options));
                methodClone.CustomAttributes.Clear();
            }

            //Imports the return type
            var retImporter = Helpers.CreateTypeImporter(originalMethod.ReturnType, Session, importList, options);
            retImporter.ImportFinished += t => methodClone.ReturnType = (TypeReference)t;
            importList.Add(retImporter);
            
            //Imports the attributes of the return type
            if (methodClone.MethodReturnType.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(methodClone.MethodReturnType, methodClone.MethodReturnType, Session).Scan(options));
                methodClone.MethodReturnType.CustomAttributes.Clear();
            }

            //Imports the parameters
            foreach (var p in originalMethod.Parameters)
            {
                //Creates a new parameter
                var param = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType)
                {
                    Constant = p.Constant,
                    MarshalInfo = p.MarshalInfo,
                    MetadataToken = new MetadataToken(p.MetadataToken.TokenType, GlobalContainer.GetFreeRID(Session.DestinationModule))
                };
                methodClone.Parameters.Add(param);

                //Queues importing of custom attributes
                if (p.HasCustomAttributes)
                {
                    importList.Add(new CustomAttributesImporter(p, param, Session).Scan(options));
                    param.CustomAttributes.Clear();
                }

                //Queues importing of type
                var typeImporter = Helpers.CreateTypeImporter(p.ParameterType, Session, importList, options);
                typeImporter.ImportFinished += t => param.ParameterType = (TypeReference)t;
                importList.Add(typeImporter);
            }

            //TODO: overrides
        }

        protected override IEnumerable<IMetadataTokenProvider> GetMembersForPreview()
        {
            return _createNode ? base.GetMembersForPreview() : base.GetMembersForPreview().Except(new[] { Member });
        }

        protected override Mono.Cecil.IMetadataTokenProvider ImportCore(MemberImportingOptions options, ICSharpCode.TreeView.SharpTreeNode node)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Adds the field to the destination type
            ((TypeDefinition)Destination).Methods.Add(methodClone);
            if (_createNode)
                node.AddChildAndColorAncestors(new ILEditTreeNode(methodClone, false));

            //Returns the new field
            return methodClone;
        }

        protected override void DisposeCore()
        {
            methodClone = null;
        }
    }
}
