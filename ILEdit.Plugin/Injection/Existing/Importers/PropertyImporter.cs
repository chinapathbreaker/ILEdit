using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing.Importers
{
    public class PropertyImporter : MemberImporter
    {
        private PropertyDefinition propClone;
        private bool _createNode;

        public PropertyImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session)
            : this(member, destination, session, true)
        {
        }

        public PropertyImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session, bool createNode)
            : base(member, destination, session)
        {
            _createNode = createNode;
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member.MetadataToken.TokenType == TokenType.Property && destination.MetadataToken.TokenType == TokenType.TypeDef;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Property
            var originalProp = (PropertyDefinition)Member;
            propClone = originalProp.Clone(Session);
         
            //Registers importing of custom attributes
            if (propClone.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(propClone, propClone, Session).Scan(options));
                propClone.CustomAttributes.Clear();
            }

            //Registers importing of get and set methods
            if (propClone.GetMethod != null)
            {
                var importer = new MethodImporter(propClone.GetMethod, Destination, Session, false).Scan(options);
                importer.ImportFinished += m => {
                    var get = (MethodDefinition)m;
                    get.IsGetter = true;
                    propClone.GetMethod = get; 
                };
                importList.Add(importer);
            }
            if (propClone.SetMethod != null)
            {
                var importer = new MethodImporter(propClone.SetMethod, Destination, Session, false).Scan(options);
                importer.ImportFinished += m =>
                {
                    var set = (MethodDefinition)m;
                    set.IsSetter = true;
                    propClone.SetMethod = set;
                };
                importList.Add(importer);
            }

            //Imports other methods
            if (originalProp.HasOtherMethods)
            {
                foreach (var m in originalProp.OtherMethods)
                {
                    var importer = new MethodImporter(m, Destination, Session, false).Scan(options);
                    importer.ImportFinished += x => {
                        var method = (MethodDefinition)x;
                        method.IsOther = true;
                        propClone.OtherMethods.Add(method);
                    };
                    importList.Add(importer);
                }
            }

            //Imports the parameters
            foreach (var p in originalProp.Parameters)
            {
                //Creates a new parameter
                var param = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType)
                {
                    Constant = p.Constant,
                    MarshalInfo = p.MarshalInfo,
                    MetadataToken = new MetadataToken(p.MetadataToken.TokenType, GlobalContainer.GetFreeRID(Session.DestinationModule))
                };
                propClone.Parameters.Add(param);

                //Queues importing of custom attributes
                if (p.HasCustomAttributes)
                {
                    importList.Add(new CustomAttributesImporter(p, param, Session).Scan(options));
                    param.CustomAttributes.Clear();
                }

                //Queues importing of type
                var typeImporter = Helpers.CreateTypeImporter(p.ParameterType, Session, importList, options);
                typeImporter.ImportFinished += t => param.ParameterType = (TypeReference)t;
            }

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
            ((TypeDefinition)Destination).Properties.Add(propClone);
            if (_createNode)
                node.AddChildAndColorAncestors(new ILEditTreeNode(propClone, false));

            //Returns the new field
            return propClone;
        }

        protected override void DisposeCore()
        {
            propClone = null;
        }
    }
}
