using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ILEdit.Injection.Existing
{
    /// <summary>
    /// Represents the importing options
    /// </summary>
    public class MemberImportingOptions
    {
        public MemberImportingOptions()
        {
            //Sets the default values
            ImportAsNestedType = false;
            CancellationToken = System.Threading.CancellationToken.None;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the imported types should be imported as nested types if possible
        /// </summary>
        public bool ImportAsNestedType { get; set; }

        /// <summary>
        /// Gets or sets the token to observe for cancellation
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }
}
