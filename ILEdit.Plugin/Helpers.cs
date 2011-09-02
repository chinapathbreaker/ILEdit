using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ICSharpCode.TreeView;
using ILEdit.Injection.Existing;
using ILEdit.Injection.Existing.Importers;
using ICSharpCode.ILSpy.TreeNodes;

namespace ILEdit
{
    /// <summary>
    /// Provides a set of generic helper methods
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Provides a set of static methods to help the managing of tree nodes
        /// </summary>
        public static class Tree
        {
            #region GetTypeNode

            /// <summary>
            /// Returns the first ancestor of the given node representing a type
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            public static TypeDefinition GetType(SharpTreeNode node)
            {
                TypeDefinition type = null;
                SharpTreeNode currentNode = node;
                while (type == null && currentNode != null)
                {
                    var memberNode = currentNode as IMemberTreeNode;
                    if (memberNode != null)
                        type = memberNode.Member as TypeDefinition;
                    currentNode = currentNode.Parent;
                }
                return type;
            }

            #endregion

            #region GetModuleNode

            /// <summary>
            /// Returns the ancestor of type ModuleTreeNode of the given node
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            public static ModuleTreeNode GetModuleNode(SharpTreeNode node)
            {
                ModuleTreeNode moduleNode = null;
                SharpTreeNode currentNode = node;
                while (moduleNode == null)
                {
                    if (currentNode.Parent == null)
                        break;
                    currentNode = currentNode.Parent;
                    moduleNode = currentNode as ModuleTreeNode;
                }
                return moduleNode;
            }

            /// <summary>
            /// Finds the node representing the given module
            /// </summary>
            /// <param name="module"></param>
            /// <returns></returns>
            public static ModuleTreeNode GetModuleNode(ModuleDefinition module)
            {
                return
                    ICSharpCode.ILSpy.MainWindow.Instance.RootNode.Children
                    .EnsureLazyChildren()
                    .SelectMany(x => x.Children.EnsureLazyChildren().Cast<ModuleTreeNode>())
                    .FirstOrDefault(x => x.Module == module);
            }

            #endregion

            #region GetAssemblyNode

            /// <summary>
            /// Returns the ancestor of type AssemblyTreeNode of the given node
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            public static AssemblyTreeNode GetAssemblyNode(SharpTreeNode node)
            {
                AssemblyTreeNode moduleNode = null;
                SharpTreeNode currentNode = node;
                while (moduleNode == null)
                {
                    if (currentNode.Parent == null)
                        break;
                    currentNode = currentNode.Parent;
                    moduleNode = currentNode as AssemblyTreeNode;
                }
                return moduleNode;
            }

            #endregion

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
            /// Sorts the children of a ReferenceFolderTreeNode
            /// </summary>
            /// <param name="node"></param>
            public static void SortChildren(ReferenceFolderTreeNode node)
            {
                //Groups the children by type and performs ordering
                var ordered =
                    node.Children
                    .GroupBy(x => x is AssemblyReferenceTreeNode)
                    .OrderByDescending(x => x.Key)
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
                var typeOrder = new List<TokenType>(new TokenType[] {
                TokenType.TypeDef,
                TokenType.Field,
                TokenType.Property,
                TokenType.Event,
                TokenType.Method
            });

                //Groups the childen by type
                var ordered =
                    node.Children
                    .GroupBy(x => x is IMemberTreeNode ? typeOrder.IndexOf(((IMemberTreeNode)x).Member.MetadataToken.TokenType == TokenType.MemberRef ? GetTokenTypeForMemberReference((MemberReference)((IMemberTreeNode)x).Member) : ((IMemberTreeNode)x).Member.MetadataToken.TokenType) : -1)
                    .OrderBy(x => x.Key)
                    .SelectMany(x => x.OrderBy(y => y.Text.ToString()))
                    .ToArray();

                //Clears the children
                node.Children.Clear();

                //Readds the children
                foreach (var x in ordered)
                    node.Children.Add(x);
            }

            private static TokenType GetTokenTypeForMemberReference(MemberReference memberRef)
            {
                if (memberRef.DeclaringType != null && memberRef.DeclaringType is GenericInstanceType)
                {
                    var giType = (GenericInstanceType)memberRef.DeclaringType;
                    var type = giType.ElementType.Resolve();
                    var memberDef = type.Fields.Cast<IMemberDefinition>()
                        .Concat(type.Methods)
                        .Concat(type.Properties)
                        .Concat(type.Events)
                        .FirstOrDefault(m => m.Name == memberRef.Name);
                    if (memberDef != null)
                        return memberDef.MetadataToken.TokenType;
                }
                return TokenType.MemberRef;
            }

            #endregion

            #region AddTreeNode

            /// <summary>
            /// Adds a TypeDefinition to the given node
            /// </summary>
            /// <param name="node">Destination node</param>
            /// <param name="type">Type to inject</param>
            /// <param name="afterModule">Action to call when the destination module is available</param>
            /// <param name="afterEnclosingType">Action to call when the enclosing type is available</param>
            public static void AddTreeNode(ILSpyTreeNode node, TypeDefinition type, Action<ModuleDefinition> afterModule, Action<TypeDefinition> afterEnclosingType)
            {
                //Ensures the lazy children of the node
                node.EnsureLazyChildren();

                //Checks if the node is a module or not
                if (node is ModuleTreeNode)
                {
                    //Module node
                    var moduleNode = (ModuleTreeNode)node;

                    //Injects in the module
                    var module = moduleNode.Module;
                    module.Types.Add(type);
                    if (afterModule != null)
                        afterModule(module);

                    //Checks for the namespace
                    var namespaceNode =
                        moduleNode.Children
                        .OfType<NamespaceTreeNode>()
                        .FirstOrDefault(x => x.Text.ToString().ToLower() == (string.IsNullOrEmpty(type.Namespace) ? "-" : type.Namespace.ToLower()));
                    if (namespaceNode != null)
                    {
                        //Adds the node to the namespace
                        namespaceNode.Children.Add(new ILEditTreeNode(type, false));
                        SortChildren(namespaceNode);
                    }
                    else
                    {
                        //Creates a new namespace containing the new type and adds it to the module node
                        namespaceNode = new NamespaceTreeNode(type.Namespace);
                        namespaceNode.Children.Add(new ILEditTreeNode(type, false));
                        moduleNode.Children.Add(namespaceNode);
                        SortChildren(moduleNode);
                    }
                }
                else
                {
                    //Marks the class as nested public
                    type.Attributes |= TypeAttributes.NestedPublic;
                    type.IsNestedPublic = true;

                    //Injects in the type
                    var type2 = (TypeDefinition)((IMemberTreeNode)node).Member;
                    type2.NestedTypes.Add(type);
                    if (afterEnclosingType != null)
                        afterEnclosingType(type2);

                    //Adds a node to the tree
                    node.Children.Add(new ILEditTreeNode(type, false));
                    if (node is ILEditTreeNode)
                        ((ILEditTreeNode)node).RefreshChildren();
                    else
                        SortChildren((TypeTreeNode)node);
                }
            }

            #endregion
        }

        #region PreOrder

        /// <summary>
        /// Converts a tree data structure into a flat list by traversing it in pre-order.
        /// </summary>
        /// <param name="input">The root element of the forest.</param>
        /// <param name="recursion">The function that gets the children of an element.</param>
        /// <returns>Iterator that enumerates the tree structure in pre-order.</returns>
        public static IEnumerable<T> PreOrder<T>(T input, Func<T, IEnumerable<T>> recursion)
        {
            return PreOrder(new T[] { input }, recursion);
        }

        //Taken from ICSharpCode.NRefactory.Utils.TreeTraversal
        /// <summary>
        /// Converts a tree data structure into a flat list by traversing it in pre-order.
        /// </summary>
        /// <param name="input">The root elements of the forest.</param>
        /// <param name="recursion">The function that gets the children of an element.</param>
        /// <returns>Iterator that enumerates the tree structure in pre-order.</returns>
        public static IEnumerable<T> PreOrder<T>(IEnumerable<T> input, Func<T, IEnumerable<T>> recursion)
        {
            Stack<IEnumerator<T>> stack = new Stack<IEnumerator<T>>();
            try
            {
                stack.Push(input.GetEnumerator());
                while (stack.Count > 0)
                {
                    while (stack.Peek().MoveNext())
                    {
                        T element = stack.Peek().Current;
                        yield return element;
                        IEnumerable<T> children = recursion(element);
                        if (children != null)
                        {
                            stack.Push(children.GetEnumerator());
                        }
                    }
                    stack.Pop().Dispose();
                }
            }
            finally
            {
                while (stack.Count > 0)
                {
                    stack.Pop().Dispose();
                }
            }
        }

        #endregion

        #region Methods to clone member definitions

        #region FieldDefinition.Clone() extension

        /// <summary>
        /// Clones this field
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static FieldDefinition Clone(this FieldDefinition field, MemberImportingSession session)
        {
            var f = new FieldDefinition(field.Name, field.Attributes, field.FieldType)
            {
                Offset = field.Offset,
                InitialValue = field.InitialValue,
                HasConstant = field.HasConstant,
                Constant = field.Constant,
                MarshalInfo = field.MarshalInfo,
                HasDefault = field.HasDefault,
                MetadataToken = new MetadataToken(field.MetadataToken.TokenType, GlobalContainer.GetFreeRID(session.DestinationModule))
            };
            foreach (var x in field.CustomAttributes)
                f.CustomAttributes.Add(x);
            return f;
        }

        #endregion

        #region MethodDefinition.Clone() extension

        /// <summary>
        /// Clones this method
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static MethodDefinition Clone(this MethodDefinition method, MemberImportingSession session)
        {
            var m = new MethodDefinition(method.Name, method.Attributes, method.ReturnType)
            {
                CallingConvention = method.CallingConvention,
                ExplicitThis = method.ExplicitThis,
                HasThis = method.HasThis,
                ImplAttributes = method.ImplAttributes,
                MetadataToken = new MetadataToken(method.MetadataToken.TokenType, GlobalContainer.GetFreeRID(session.DestinationModule)),
                MethodReturnType = method.MethodReturnType,
                PInvokeInfo = method.PInvokeInfo
            };
            m.IsPInvokeImpl = m.PInvokeInfo != null;
            var body = m.Body;
            body.GetType().GetField("code_size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(body, method.Body.CodeSize);
            foreach (var x in method.CustomAttributes)
                m.CustomAttributes.Add(x);
            foreach (var x in method.GenericParameters)
                m.GenericParameters.Add(x);
            foreach (var x in method.Overrides)
                m.Overrides.Add(x);
            foreach (var x in method.SecurityDeclarations)
                m.SecurityDeclarations.Add(x);
            return m;
        }

        #endregion

        #region PropertyDefinition.Clone() extension

        /// <summary>
        /// Clones this property
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static PropertyDefinition Clone(this PropertyDefinition prop, MemberImportingSession session)
        {
            var p = new PropertyDefinition(prop.Name, prop.Attributes, prop.PropertyType)
            {
                 GetMethod = prop.GetMethod,
                 SetMethod = prop.SetMethod,
                 MetadataToken = new MetadataToken(prop.MetadataToken.TokenType, GlobalContainer.GetFreeRID(session.DestinationModule))
            };
            foreach (var x in prop.CustomAttributes)
                p.CustomAttributes.Add(x);
            return p;
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
            var a = new CustomAttribute(attr.Constructor);
            a.GetType().GetField("blob", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(a, attr.GetBlob());
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
        public static TypeDefinition Clone(this TypeDefinition type, MemberImportingSession session)
        {
            var t = new TypeDefinition(type.Namespace, type.Name, type.Attributes, type.BaseType)
            {
                PackingSize = type.PackingSize,
                ClassSize = type.ClassSize,
                HasSecurity = type.HasSecurity,
                MetadataToken = new MetadataToken(type.MetadataToken.TokenType, GlobalContainer.GetFreeRID(session.DestinationModule))
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

        #region GenericParameter.Clone() extension

        /// <summary>
        /// Clones this generic parameter
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static GenericParameter Clone(this GenericParameter param, IGenericParameterProvider owner, MemberImportingSession session)
        {
            var p = new GenericParameter(param.Name, owner)
            {
                Attributes = param.Attributes,
                DeclaringType = param.DeclaringType,
                HasDefaultConstructorConstraint = param.HasDefaultConstructorConstraint,
                MetadataToken = new MetadataToken(param.MetadataToken.TokenType, GlobalContainer.GetFreeRID(session.DestinationModule))
            };
            foreach (var x in param.Constraints)
                p.Constraints.Add(x);
            foreach (var x in param.CustomAttributes)
                p.CustomAttributes.Add(x);
            foreach (var x in param.GenericParameters)
                p.GenericParameters.Add(x);
            return p;
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

        #region SharpTreeNodeCollection.EnsureLazyChildren() extension

        /// <summary>
        /// Makes sure that every child in this collection is ready
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static IEnumerable<SharpTreeNode> EnsureLazyChildren(this SharpTreeNodeCollection coll)
        {
            foreach (var x in coll)
            {
                x.EnsureLazyChildren();
                yield return x;
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
            if (type.IsPublic || type.IsNestedPublic)
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

        /// <summary>
        /// Returns a value indicating whether the given type is accessible from a given context
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <param name="context">Context from which the type should be accessible</param>
        /// <returns></returns>
        public static bool IsTypeAccessibleFrom(TypeDefinition type, ModuleDefinition context)
        {
            //Checks if the type is public or nested public
            if (type.IsPublic || type.IsNestedPublic)
                return true;

            //Checks if the types are nested
            if (type.DeclaringType == null)
            {
                //If it's internal returns true only if they are in the same assembly
                return
                    (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic
                    && (type.Module == context || type.Module.Assembly == context.Assembly);
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
                        return type.Module == context || type.Module.Assembly == context.Assembly;
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
        public static MemberImporter CreateTypeImporter(TypeReference type, MemberImportingSession session, List<MemberImporter> importList, MemberImportingOptions options)
        {
            //Return value
            MemberImporter ret;

            //Checks if the type is a generic instance type or a generic parameter
            if (type is GenericParameter)
                ret = MemberImporter.Create((_, __) => type);
            else if (type is TypeSpecification)
                ret = new TypeSpecificationImporter((TypeSpecification)type, session).Scan(options);
            else
            {
                //Checks if the importer is in the dictionary
                MemberImporter importer;
                if (session.RegisteredImporters.TryGetValue(type, out importer))
                    return importer;

                //Creates the importer, registers it and returns
                importer = Helpers.CreateTypeImporterForTypeDefinition(((TypeReference)type).Resolve(), session, importList, options);
                session.RegisteredImporters.Add(type, importer);
                importer.Scan(options);
                importList.Add(importer);
                return importer;
            }

            //Adds the return value to the import list and returns
            importList.Add(ret);
            return ret;
        }

        private static MemberImporter CreateTypeImporterForTypeDefinition(TypeDefinition type, MemberImportingSession session, List<MemberImporter> importList, MemberImportingOptions options)
        {
            //Checks if the type is accessible
            if (
                (session.DestinationType != null && Helpers.IsTypeAccessibleFrom(type, session.DestinationType)) ||
                Helpers.IsTypeAccessibleFrom(type, session.DestinationModule)
                )
            {
                //Queues addition of an assembly reference
                if (type.Module != session.DestinationModule)
                    if (!session.DestinationModule.AssemblyReferences.Any(x => x.FullName == type.Module.Assembly.Name.FullName))
                    {
                        //Checks if the reference importer is already in the dictionary
                        MemberImporter importer;
                        if (!session.RegisteredImporters.TryGetValue(type.Module.Assembly.Name, out importer))
                        {
                            importList.Add(importer = new AssemblyReferenceImporter(type.Module.Assembly.Name, session).Scan(options));
                            session.RegisteredImporters.Add(type.Module.Assembly.Name, importer);
                        }
                    }

                //Creates the type importer
                return new TypeReferenceInModuleImporter(type, session);
            }
            else
            {
                //Creates the type importer
                return new TypeImporter(type, options.ImportAsNestedType ? (IMetadataTokenProvider)session.DestinationType : (IMetadataTokenProvider)session.DestinationModule, session);
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
        public static MethodReference GetConstructorMatchingArguments(TypeDefinition type, IEnumerable<CustomAttributeArgument> pars, MemberImportingSession session)
        {
            var ctor =
                type.Methods
                .Where(x => x.Name == ".ctor")
                .Where(x => x.Parameters.Count == pars.Count())
                .FirstOrDefault(m => m.Parameters.Select((x, i) => Tuple.Create(x, i)).All(p => p.Item1.ParameterType.Name == pars.ElementAt(p.Item2).Type.Name && p.Item1.ParameterType.Namespace == pars.ElementAt(p.Item2).Type.Namespace));
            return ctor == null ? null : session.DestinationModule.Import(ctor);
        }

        #endregion
    }
}
