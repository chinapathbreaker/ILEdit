using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace ILEdit.Injection.Injectors
{
    /// <summary>
    /// Provides a set of methods to help the managing of tree nodes
    /// </summary>
    internal static class TreeHelper
    {
        #region SortChildren

        /// <summary>
        /// Sorts the children of a ModuleTreeNode
        /// </summary>
        /// <param name="node"></param>
        public static void SortChildren(ModuleTreeNode node)
        {
            //Groups the children by type and performs ordering
            var ordered =
                node.Children
                .GroupBy(x => x is NamespaceTreeNode)
                .OrderBy(x => x.Key)
                .SelectMany(x => x.OrderBy(y => y.Text.ToString()))
                .ToArray();

            //Clears the children
            node.Children.Clear();

            //Readds the children
            foreach (var x in ordered)
                node.Children.Add(x);
        }

        /// <summary>
        /// Sorts the children of a NamespaceTreeNode
        /// </summary>
        /// <param name="node"></param>
        public static void SortChildren(NamespaceTreeNode node)
        {
            //Groups the children by type and performs ordering
            var ordered =
                node.Children
                .OrderBy(x => x.Text.ToString())
                .ToArray();

            //Clears the children
            node.Children.Clear();

            //Readds the children
            foreach (var x in ordered)
                node.Children.Add(x);
        }

        /// <summary>
        /// Sorts the children of a TypeTreeNode
        /// </summary>
        /// <param name="node"></param>
        public static void SortChildren(TypeTreeNode node)
        {
            //Array for the type ordering
            var typeOrder = new List<Type>(new Type[] {
                typeof(TypeTreeNode),
                typeof(FieldTreeNode),
                typeof(PropertyTreeNode),
                typeof(EventTreeNode),
                typeof(MethodTreeNode)
            });

            //Groups the childen by type
            var ordered =
                node.Children
                .GroupBy(x => typeOrder.IndexOf(x.GetType()))
                .OrderBy(x => x.Key)
                .SelectMany(x => x.OrderBy(y => y.Text.ToString()))
                .ToArray();

            //Clears the children
            node.Children.Clear();

            //Readds the children
            foreach (var x in ordered)
                node.Children.Add(x);
        }

        #endregion
    }
}
