﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ICSharpCode.TreeView;

namespace ILEdit.Injection.Existing.Importers
{
    /// <summary>
    /// Imports the custom attributes of the member to the destination
    /// </summary>
    internal class CustomAttributesImporter : MemberImporter
    {
        private CustomAttribute[] attribs;

        public CustomAttributesImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session)
            : base(member, destination, session)
        {
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member is ICustomAttributeProvider && destination is ICustomAttributeProvider;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Clones the attributes
            attribs = ((ICustomAttributeProvider)Member).CustomAttributes.Select(x => x.Clone()).ToArray();

            //Checks the attributes
            foreach (var x in attribs)
            {
                //Checks that the task hasn't been canceled
                options.CancellationToken.ThrowIfCancellationRequested();

                var a = x;
                //Imports the type of the attribute
                var typeImporter = Helpers.CreateTypeImporter(a.AttributeType.Resolve(), Session, importList, options);
                typeImporter.ImportFinished += (typeRef) => a.Constructor = Helpers.GetConstructorMatchingArguments(((TypeReference)typeRef).Resolve(), a.ConstructorArguments, Session);

                //Checks if the arguments should be imported
                for (int i = 0; i < a.ConstructorArguments.Count; i++)
                {
                    var p = a.ConstructorArguments[i];
                    //Imports the type of the argument
                    var argumentTypeImporter = Helpers.CreateTypeImporter(p.Type.Resolve(), Session, importList, options);
                    var index = i;
                    argumentTypeImporter.ImportFinished += (typeRef) => {
                        a.ConstructorArguments.RemoveAt(index);
                        a.ConstructorArguments.Insert(index, new CustomAttributeArgument((TypeReference)typeRef, p.Value));
                    };
                }
            }
        }

        protected override Mono.Cecil.IMetadataTokenProvider ImportCore(MemberImportingOptions options, SharpTreeNode node)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Destination
            var dest = (ICustomAttributeProvider)Destination;

            //Adds the attributes
            dest.CustomAttributes.Clear();
            foreach (var a in attribs)
                dest.CustomAttributes.Add(a);

            //Returns null
            return null;
        }

        protected override IEnumerable<IMetadataTokenProvider> GetMembersForPreview()
        {
            return base.GetMembersForPreview().Except(new IMetadataTokenProvider[] { Member });
        }

        protected override void DisposeCore()
        {
            //Clears the array
            attribs = null;
        }
    }
}
