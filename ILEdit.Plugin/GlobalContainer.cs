using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using ILEdit.Injection;
using ILEdit.Injection.Injectors;
using Mono.Cecil;

namespace ILEdit
{
    public static class GlobalContainer
    {
        #region Loading (.cctor)

        /// <summary>
        /// Static constructor which initializes the class
        /// </summary>
        static GlobalContainer()
        {
            //Sets the color
            ModifiedNodesColor = Colors.Red;

            //Adds the injectors
            Injectors = new List<IInjector>() { 
                
                new AssemblyReferenceInjector(),
                
                new ClassInjector(),
                new StructInjector(),
                new DelegateInjector(),
                new InterfaceInjector(),
                new EnumInjector(),

                new FieldInjector(),
                new PropertyInjector()

            };
        }

        #endregion

        #region Injectors

        /// <summary>
        /// Complete list of the available injectors
        /// </summary>
        public static List<IInjector> Injectors { get; set; }

        #endregion

        #region ModifiedNodesColor

        /// <summary>
        /// Returns the color used to identify the edited nodes
        /// </summary>
        public static Color ModifiedNodesColor { get; private set; }

        #endregion

        #region GetFreeRID

        private static Dictionary<ModuleDefinition, int> _ridCache = new Dictionary<ModuleDefinition, int>();

        /// <summary>
        /// Returns the first free RID for the given module
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public static int GetFreeRID(ModuleDefinition module)
        {
            int ret;
            if (_ridCache.TryGetValue(module, out ret))
            {
                return (_ridCache[module] = (ret + 1));
            }
            else
            {
                _ridCache.Add(module, ret = (module.GetMemberReferences().Count() + 1));
                return ret;
            }
        }

        #endregion
    }
}
