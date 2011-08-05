using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ILEdit.Injection
{
    public static class GlobalContainer
    {
        #region Loading (.cctor)

        /// <summary>
        /// Static constructor which initializes the class
        /// </summary>
        static GlobalContainer()
        {
            //Adds the injectors
            Injectors = new List<IInjector>();
        }

        #endregion

        #region Injectors

        /// <summary>
        /// Complete list of the available injectors
        /// </summary>
        public static List<IInjector> Injectors { get; set; }

        #endregion
    }
}
