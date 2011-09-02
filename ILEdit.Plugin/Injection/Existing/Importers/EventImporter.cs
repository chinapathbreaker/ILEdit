using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing.Importers
{
    public class EventImporter : MemberImporter
    {
        private EventDefinition evtClone;
        private bool _createNode;

        public EventImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session)
            : this(member, destination, session, true)
        {
        }

        public EventImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session, bool createNode)
            : base(member, destination, session)
        {
            _createNode = createNode;
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member.MetadataToken.TokenType == TokenType.Event && destination.MetadataToken.TokenType == TokenType.TypeDef;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Event
            var originalEvt = (EventDefinition)Member;
            evtClone = originalEvt.Clone(Session);

            //Registers importing of custom attributes
            if (evtClone.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(evtClone, evtClone, Session).Scan(options));
                evtClone.CustomAttributes.Clear();
            }

            //Registers importing of add, remove and invoke methods
            if (originalEvt.AddMethod != null)
            {
                var importer = new MethodImporter(originalEvt.AddMethod, Destination, Session, false).Scan(options);
                importer.ImportFinished += m =>
                {
                    var add = (MethodDefinition)m;
                    add.IsAddOn = true;
                    evtClone.AddMethod = add;
                };
                importList.Add(importer);
            }
            if (originalEvt.RemoveMethod != null)
            {
                var importer = new MethodImporter(originalEvt.RemoveMethod, Destination, Session, false).Scan(options);
                importer.ImportFinished += m =>
                {
                    var remove = (MethodDefinition)m;
                    remove.IsRemoveOn = true;
                    evtClone.RemoveMethod = remove;
                };
                importList.Add(importer);
            }
            if (originalEvt.InvokeMethod != null)
            {
                var importer = new MethodImporter(originalEvt.InvokeMethod, Destination, Session, false).Scan(options);
                importer.ImportFinished += m =>
                {
                    var invoke = (MethodDefinition)m;
                    invoke.IsFire = true;
                    evtClone.InvokeMethod = invoke;
                };
                importList.Add(importer);
            }

            //Imports other methods
            if (originalEvt.HasOtherMethods)
            {
                foreach (var m in originalEvt.OtherMethods)
                {
                    var importer = new MethodImporter(m, Destination, Session, false).Scan(options);
                    importer.ImportFinished += x =>
                    {
                        var method = (MethodDefinition)x;
                        method.IsOther = true;
                        evtClone.OtherMethods.Add(method);
                    };
                    importList.Add(importer);
                }
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
            ((TypeDefinition)Destination).Events.Add(evtClone);
            if (_createNode)
                node.AddChildAndColorAncestors(new ILEditTreeNode(evtClone, false));

            //Returns the new field
            return evtClone;
        }

        protected override void DisposeCore()
        {
            evtClone = null;
        }
    }
}
