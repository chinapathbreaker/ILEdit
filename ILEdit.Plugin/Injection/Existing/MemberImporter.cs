using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing
{
    /// <summary>
    /// Represents a member importer
    /// </summary>
    public abstract class MemberImporter : IDisposable
    {
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

        #region Properties

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

        #region Methods

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

        private List<MemberImporter> _importList = null;

        /// <summary>
        /// Performs a scanning of the member
        /// </summary>
        public void Scan(MemberImportingOptions options)
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
        }
        protected abstract void ScanCore(MemberImportingOptions options, List<MemberImporter> importList);

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
            
            //Invokes the event
            var evt = ImportFinished;
            if (evt != null)
                evt(ImportCore(options));
        }
        protected abstract IMetadataTokenProvider ImportCore(MemberImportingOptions options);

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
