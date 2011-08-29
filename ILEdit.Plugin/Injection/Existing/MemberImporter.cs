using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using Mono.Cecil;
using System.Windows.Media;

namespace ILEdit.Injection.Existing
{
    /// <summary>
    /// Represents a member importer
    /// </summary>
    public abstract class MemberImporter : IDisposable
    {
        private List<MemberImporter> _importList = null;

        #region .ctor

        /// <summary>
        /// Creates a new instance of this MemberImporter
        /// </summary>
        /// <param name="member">Member to import</param>
        /// <param name="destination">Destination of the importing</param>
        public MemberImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination)
        {
            //Checks if the member can be imported (and that member and destination aren't null)
            if (!CanImport(member, destination))
                throw new ArgumentException(string.Format("Cannot import '{0}' into '{1}'", member, destination));

            //Stores member and destination
            _member = member;
            _destination = destination;
        }

        #endregion

        #region Properties Member, Destination and Scanned

        private IMetadataTokenProvider _member;
        /// <summary>
        /// Member to import
        /// </summary>
        public IMetadataTokenProvider Member
        {
            get { return _member; }
        }

        private IMetadataTokenProvider _destination;
        /// <summary>
        /// Destination of the importing
        /// </summary>
        public IMetadataTokenProvider Destination
        {
            get { return _destination; }
        }

        /// <summary>
        /// Returns a value indicating whether the scan has been performed
        /// </summary>
        public bool Scanned
        {
            get { return _importList != null; }
        }

        #endregion

        #region CanImport method

        /// <summary>
        /// Returns a value indicating whether this importer can import a member in the destination
        /// </summary>
        /// <param name="member"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public bool CanImport(IMetadataTokenProvider member, IMetadataTokenProvider destination) 
        {
            //Checks that the parameters aren't null;
            if (member == null)
                throw new ArgumentNullException("member");
            if (destination == null)
                throw new ArgumentNullException("destination");

            //Calls the protected method
            return CanImportCore(member, destination);
        }
        protected abstract bool CanImportCore(IMetadataTokenProvider member, IMetadataTokenProvider destination);

        #endregion

        #region Scan method

        /// <summary>
        /// Performs a scanning of the member
        /// </summary>
        public MemberImporter Scan(MemberImportingOptions options)
        {
            //Checks that options isn't null
            if (options == null)
                throw new ArgumentNullException("options");

            //Checks if this is the first time that this method has been called
            if (Scanned)
                throw new InvalidOperationException("Cannot call Scan() two times");

            //Performs the scanning
            _importList = new List<MemberImporter>();
            ScanCore(options, _importList);

            //Fluid interface: returns this
            return this;
        }
        protected abstract void ScanCore(MemberImportingOptions options, List<MemberImporter> importList);

        #endregion

        #region Import method

        /// <summary>
        /// Performs the importing
        /// </summary>
        public void Import(MemberImportingOptions options) 
        {
            //Checks that options isn't null
            if (options == null)
                throw new ArgumentNullException("options");

            //Checks if must perform a scan before
            if (!Scanned)
                Scan(options);

            //Imports the members required by this importer
            foreach (var x in _importList)
                x.Import(options);
            
            //Imports and invokes the event
            var ret = ImportCore(options);
            var evt = ImportFinished;
            if (evt != null)
                evt(ret);
        }
        protected abstract IMetadataTokenProvider ImportCore(MemberImportingOptions options);

        #endregion

        #region BuildPreviewNodes method

        /// <summary>
        /// Fills the given node with the nodes necessary for the preview of the importing. This action requires a call to Scan()
        /// </summary>
        /// <param name="root"></param>
        public void BuildPreviewNodes(SharpTreeNode root)
        {
            //Checks that the root node isn't null
            if (root == null)
                throw new ArgumentNullException("root");

            //Checks if Scan() has already been called
            if (!Scanned)
                throw new InvalidOperationException("Cannot build preview nodes before a call to Scan()");

            //Gets the members needed by the import list and by this instance
            var members = GetMembersForPreview().ToArray();

            //Checks that there's at least one element
            if (members.Length == 0)
                return;

            //Builds up to the type nodes
            var typeNodes = BuildTypeNodes(members);

            //Lists of the assembly and module nodes (needed for preview of assembly references)
            var asmNodes = new List<ILEditTreeNode>();
            var moduleNodes = new List<ILEditTreeNode>();

            //Groups by assembly, module and then namespace
            var grouped =
                typeNodes.GroupBy(x => x.Member.Module)
                .OrderBy(x => x.Key.Name)
                .Select(x => x.OrderBy(y => y.Text.ToString()).GroupBy(y => ((TypeDefinition)y.Member).Namespace).OrderBy(y => y.Key).GroupBy(y => x.Key).ElementAt(0))
                .GroupBy(x => x.Key.Assembly)
                .OrderBy(x => x.Key.Name.Name)
                .Select(x =>
                {
                    //Assembly node
                    var asmNode = new ILEditTreeNode(x.Key, true) { IsExpanded = true, Foreground = GlobalContainer.NormalNodesBrush };
                    foreach (var m in x)
                    {
                        //Module node
                        var moduleNode = new ILEditTreeNode(m.Key, true) { IsExpanded = true, Foreground = GlobalContainer.NormalNodesBrush };
                        foreach (var n in m)
                        {
                            //Namespace node
                            var namespaceNode = new NamespaceTreeNode(n.Key) { IsExpanded = true, Foreground = GlobalContainer.NormalNodesBrush };
                            foreach (var t in n)
                                namespaceNode.Children.Add(t);
                            moduleNode.Children.Add(namespaceNode);
                        }
                        moduleNodes.Add(moduleNode);
                        asmNode.Children.Add(moduleNode);
                    }
                    asmNodes.Add(asmNode);
                    return asmNode;
                });

            //Clears the root
            root.Children.Clear();

            //Adds the nodes to the root
            foreach (var x in grouped)
                root.Children.Add(x);

            //Groups the references by module
            var references =
                members.OfType<Importers.AssemblyReferenceImporter>()
                .GroupBy(
                    x => (ModuleDefinition)x.Destination,
                    x => (AssemblyNameReference)x.Member
                );

            //Creates the references nodes
            foreach(var refs in references)
            {
                //Creates and populates the references node
                var refFolder = new ILEditTreeNode.ReferenceFolderNode();
                foreach (var r in refs)
                    refFolder.Children.Add(new ILEditTreeNode(r, true) { 
                        IsExpanded = true, 
                        Foreground = GlobalContainer.ModifiedNodesBrush 
                    });
                
                //Finds the module node to add to
                var moduleNode = moduleNodes.FirstOrDefault(x => (ModuleDefinition)x.TokenProvider == refs.Key);
                if (moduleNode != null)
                {
                    //Adds the references to the module node
                    moduleNode.Children.Insert(0, refFolder);
                }
                else
                {
                    //Finds or creates the assembly node
                    var asmNode = asmNodes.FirstOrDefault(x => (AssemblyDefinition)x.TokenProvider == refs.Key.Assembly);
                    if (asmNode != null)
                    {
                        //Adds a module node to the assembly
                        asmNode.Children.Add(new ILEditTreeNode(refs.Key, true) { 
                            IsExpanded = true, 
                            Foreground = GlobalContainer.NormalNodesBrush, 
                            Children = { refFolder } 
                        });
                    }
                    else
                    {
                        //Creates the nodes and adds it to the root
                        root.Children.Add(new ILEditTreeNode(refs.Key.Assembly, true) {
                            IsExpanded = true,
                            Foreground = GlobalContainer.NormalNodesBrush,
                            Children = {
                                new ILEditTreeNode(refs.Key, true) {
                                IsExpanded = true,
                                Foreground = GlobalContainer.NormalNodesBrush,
                                    Children = { refFolder }
                                }
                            }
                        });
                    }
                }
            }
        }

        protected virtual IEnumerable<IMetadataTokenProvider> GetMembersForPreview() 
        {
            return _importList.SelectMany(x => x.GetMembersForPreview()).Concat(new IMetadataTokenProvider[] { Member });
        }

        private IEnumerable<ILEditTreeNode> BuildTypeNodes(IMetadataTokenProvider[] members)
        {
            //Type-node mapping dictionary
            var typeMapDic = new Dictionary<TypeDefinition, ILEditTreeNode>();

            //Function to get or create a type-node from the dictionary
            Func<TypeDefinition, Brush, ILEditTreeNode> GetOrCreate = (x, b) => {
                ILEditTreeNode ret;
                if (!typeMapDic.TryGetValue(x, out ret))
                {
                    ret = new ILEditTreeNode(x, true) { IsExpanded = true, Foreground = b };
                    typeMapDic.Add(x, ret);
                }
                return ret;
            };

            //Foreach member
            foreach (var x in members.Where(m => m is IMemberDefinition))
            {
                //Checks if this member is a type or not
                if (x is TypeDefinition)
                {
                    GetOrCreate((TypeDefinition)x, GlobalContainer.ModifiedNodesBrush);
                }
                else 
                {
                    //Gets the node type
                    var typeNode = GetOrCreate(((IMemberDefinition)x).DeclaringType, GlobalContainer.NormalNodesBrush);

                    //Adds to the type node this member
                    typeNode.Children.Add(new ILEditTreeNode(x, false) { IsExpanded = true, Foreground = GlobalContainer.ModifiedNodesBrush });
                }
            }

            //Returns and sorts the contents of the type nodes
            return typeMapDic.Values;
        }

        #endregion

        #region Events

        /// <summary>
        /// Event invoked when the Import() method finishes
        /// </summary>
        public event Action<IMetadataTokenProvider> ImportFinished;

        #endregion

        #region Disposing

        public void Dispose()
        {
            //Disposes the import list
            if (Scanned)
            {
                foreach (var x in _importList)
                    x.Dispose();
                _importList = null;
            }

            //Clears the fields
            _destination = null;
            _member = null;
            
            //Cleares the event
            ImportFinished = null;

            //Calls the dispose core
            DisposeCore();
        }
        protected virtual void DisposeCore() { }

        #endregion
    }
}
