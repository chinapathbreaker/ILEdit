using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing.Importers
{
    internal class GenericParametersImporter : MemberImporter
    {
        GenericParameter[] parametersClone;

        public GenericParametersImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session)
            : base(member, destination, session)
        {
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member is IGenericParameterProvider && destination is IGenericParameterProvider;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Clones the generic parameters
            parametersClone = ((IGenericParameterProvider)Member).GenericParameters.Select(x => x.Clone((IGenericParameterProvider)Destination, Session)).ToArray();

            //For each parameter
            foreach (var p in parametersClone)
            {
                //Throws if cancellation was requested
                options.CancellationToken.ThrowIfCancellationRequested();

                //Registers importing of custom attributes
                var param = p;
                if (param.HasCustomAttributes)
                {
                    importList.Add(new CustomAttributesImporter(p, p, Session).Scan(options));
                    param.CustomAttributes.Clear();
                }

                //For each constraint creates registers a type importer
                foreach (var c in param.Constraints.Where(x => !(x is GenericParameter)))
                {
                    var importer = Helpers.CreateTypeImporter(c, Session, importList, options);
                    importer.ImportFinished += (constraint) => param.Constraints.Add((TypeReference)constraint);
                }
                foreach (var x in param.Constraints.Where(x => !(x is GenericParameter)).ToArray())
                    param.Constraints.Remove(x);
            }
        }

        protected override IEnumerable<IMetadataTokenProvider> GetMembersForPreview()
        {
            return base.GetMembersForPreview().Except(new IMetadataTokenProvider[] { Member });
        }

        protected override Mono.Cecil.IMetadataTokenProvider ImportCore(MemberImportingOptions options, ICSharpCode.TreeView.SharpTreeNode node)
        {
            //Throws if cancellation was requested
            options.CancellationToken.ThrowIfCancellationRequested();

            //Imports the generic parameters
            foreach (var x in parametersClone)
                ((IGenericParameterProvider)Destination).GenericParameters.Add(x);

            //Returns null
            return null;
        }

        protected override void DisposeCore()
        {
            parametersClone = null;
        }
    }
}
