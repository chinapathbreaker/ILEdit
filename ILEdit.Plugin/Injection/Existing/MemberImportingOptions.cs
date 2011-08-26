using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        }

        /// <summary>
        /// Gets or sets a value indicating whether the imported types should be imported as nested types if possible
        /// </summary>
        public bool ImportAsNestedType { get; set; }
    }
}
