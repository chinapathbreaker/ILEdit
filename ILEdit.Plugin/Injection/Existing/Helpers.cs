using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ILEdit.Injection.Existing.Importers;
using ICSharpCode.TreeView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ILEdit.Injection.Existing
{
    /// <summary>
    /// Provides a set of helper methods
    /// </summary>
    internal static partial class Helpers
    {
        #region Cloning methods

        #region FieldDefinition.Clone() extension

        /// <summary>
        /// Clones this field
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static FieldDefinition Clone(this FieldDefinition field)
        {
            var f = new FieldDefinition(field.Name, field.Attributes, field.FieldType)
            {
                Offset = field.Offset,
                InitialValue = field.InitialValue,
                HasConstant = field.HasConstant,
                Constant = field.Constant,
                MarshalInfo = field.MarshalInfo,
                HasDefault = field.HasDefault,
                MetadataToken = field.MetadataToken
            };
            foreach (var x in field.CustomAttributes)
                f.CustomAttributes.Add(x);
            return f;
        }

        #endregion

        #region CustomAttribute.Clone() extension

        /// <summary>
        /// Clones this custom attribute
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static CustomAttribute Clone(this CustomAttribute attr)
        {
            var a = new CustomAttribute(attr.Constructor)
            {

            };
            foreach (var x in attr.ConstructorArguments)
                a.ConstructorArguments.Add(x);
            foreach (var x in attr.Fields)
                a.Fields.Add(x);
            foreach (var x in attr.Properties)
                a.Properties.Add(x);
            return a;
        }

        #endregion

        #region TypeDefinition.Clone() extension

        /// <summary>
        /// Clones this type
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static TypeDefinition Clone(this TypeDefinition type)
        {
            var t = new TypeDefinition(type.Namespace, type.Name, type.Attributes, type.BaseType)
            {
                PackingSize = type.PackingSize,
                ClassSize = type.ClassSize,
                HasSecurity = type.HasSecurity,
                MetadataToken = type.MetadataToken
            };
            foreach (var x in type.Interfaces)
                t.Interfaces.Add(x);
            foreach (var x in type.SecurityDeclarations)
                t.SecurityDeclarations.Add(x);
            foreach (var x in type.CustomAttributes)
                t.CustomAttributes.Add(x);
            foreach (var x in type.GenericParameters)
                t.GenericParameters.Add(x);
            return t;
        }

        #endregion

        #endregion

        #region SharpTreeNode.AddChildAndColorAncestors extension

        /// <summary>
        /// Adds a node as child of this node and colors the ancestors
        /// </summary>
        /// <param name="node">Destination node</param>
        /// <param name="child">Node to add</param>
        public static void AddChildAndColorAncestors(this SharpTreeNode node, SharpTreeNode child)
        {
            //Checks that the nodes aren't null
            if (node == null)
                throw new ArgumentNullException("node");
            if (child == null)
                throw new ArgumentNullException("child");

            //Adds the child
            node.Children.Add(child);

            //Colors the ancestors
            var parent = node;
            while (parent != null)
            {
                if (parent.Foreground == GlobalContainer.ModifiedNodesBrush)
                    break;
                parent.Foreground = GlobalContainer.ModifiedNodesBrush;
                parent = parent.Parent;
            }
        }

        #endregion

        #region TypeDefinition.EnclosingAncestors() extension

        /// <summary>
        /// Returns the ancestors enclosing this type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<TypeDefinition> EnclosingAncestors(this TypeDefinition type)
        {
            var enclosing = type.DeclaringType;
            while (enclosing != null)
            {
                yield return enclosing;
                enclosing = enclosing.DeclaringType;
            }
        }

        #endregion

        #region IsTypeAccessibleFrom

        /// <summary>
        /// Returns a value indicating whether the given type is accessible from a given context
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <param name="context">Context from which the type should be accessible</param>
        /// <returns></returns>
        public static bool IsTypeAccessibleFrom(TypeDefinition type, TypeDefinition context)
        {
            //Checks if the types are the same
            if (type == context)
                return true;

            //Checks if the type is public or nested public
            if ((type.Attributes & (TypeAttributes.Public | TypeAttributes.NestedPublic)) != 0)
                return true;

            //Checks if the types are nested
            if (type.DeclaringType == null && context.DeclaringType == null)
            {
                //If it's internal returns true only if they are in the same assembly
                return
                    (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic
                    && (type.Module == context.Module || type.Module.Assembly == context.Module.Assembly);
            }
            else
            {
                //Switches on the visibility of the type
                switch (type.Attributes & TypeAttributes.VisibilityMask)
                {
                    case TypeAttributes.NestedFamily:
                    case TypeAttributes.NestedAssembly:
                    case TypeAttributes.NestedFamANDAssem:
                    case TypeAttributes.NestedFamORAssem:
                        return type.Module == context.Module || type.Module.Assembly == context.Module.Assembly;
                    case TypeAttributes.NestedPrivate:
                        return context.EnclosingAncestors().Any(x => x.Name == type.Name && x.Namespace == type.Namespace);
                }
            }

            //Returns false
            return false;
        }

        #endregion

        #region CreateTypeImporter

        /// <summary>
        /// Creates a new MemberImporter to import a type to destination, automatically adding any other required importer
        /// </summary>
        /// <param name="type"></param>
        /// <param name="destType"></param>
        /// <param name="importList"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static MemberImporter CreateTypeImporter(TypeDefinition type, TypeDefinition destType, List<MemberImporter> importList, MemberImportingOptions options)
        {
            if (Helpers.IsTypeAccessibleFrom(type, destType))
            {
                //Queues addition of an assembly reference
                if (type.Module != destType.Module)
                    importList.Add(new AssemblyReferenceImporter(type.Module.Assembly.Name, destType.Module).Scan(options));

                //Creates the type importer
                return new TypeReferenceInModuleImporter(type, destType.Module).Scan(options);
            }
            else
            {
                //Creates the type importer
                return new TypeImporter(type, options.ImportAsNestedType ? (IMetadataTokenProvider)destType : (IMetadataTokenProvider)destType.Module).Scan(options);
            }
        }

        #endregion

        #region GetConstructorMatchingArguments

        /// <summary>
        /// Returns the constructor matching the provided arguments
        /// </summary>
        /// <param name="type">Type in which the constructor resides</param>
        /// <param name="pars">Parameters of the constructor</param>
        /// <returns></returns>
        public static MethodDefinition GetConstructorMatchingArguments(TypeDefinition type, IEnumerable<CustomAttributeArgument> pars)
        {
            return
                type.Methods
                .Where(x => x.Name == ".ctor")
                .Where(x => x.Parameters.Count == pars.Count())
                .FirstOrDefault(m => m.Parameters.Select((x, i) => Tuple.Create(x, i)).All(p => p.Item1.ParameterType.Name == pars.ElementAt(p.Item2).Type.Name && p.Item1.ParameterType.Namespace == pars.ElementAt(p.Item2).Type.Namespace));
        }

        #endregion

        #region FindModuleNode

        /// <summary>
        /// Finds the node representing the given module
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public static ModuleTreeNode FindModuleNode(ModuleDefinition module)
        {
            return
                ICSharpCode.ILSpy.MainWindow.Instance.RootNode.Children
                .SelectMany(x => x.Children.Cast<ModuleTreeNode>())
                .FirstOrDefault(x => x.Module == module);
        }

        #endregion
    }
}
