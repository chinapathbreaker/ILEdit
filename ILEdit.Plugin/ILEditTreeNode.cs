﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.TreeView;
using Mono.Cecil;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TreeNodes;
using System.Windows.Media.Imaging;
using ICSharpCode.ILSpy;
using System.Windows.Media;

namespace ILEdit
{
    /// <summary>
    /// Node which represents a member. It can auto-fill its children or they can be filled manually. A predicate determines which items to show if the children are automatically determined.
    /// </summary>
    internal class ILEditTreeNode : ILSpyTreeNode, IMemberTreeNode
    {
        #region ReferenceFolderNode
        internal class ReferenceFolderNode : ILSpyTreeNode
        {
            public ReferenceFolderNode()
            {
                this.IsExpanded = true;
            }
            public override object Icon
            {
                get { return new BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/ReferenceFolder.Closed.png")); }
            }
            public override object ExpandedIcon
            {
                get { return new BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/ReferenceFolder.Open.png")); }
            }
            public override object Text
            {
                get { return "References"; }
            }
            public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        private IMetadataTokenProvider _tokenProvider;
        private Language language;
        private bool _manuallyFilledChildren;

        /// <summary>
        /// Creates a new instance of the class ILEditTreeNode
        /// </summary>
        /// <param name="tokenProvider"></param>
        public ILEditTreeNode(IMetadataTokenProvider tokenProvider, bool manuallyFilledChildren)
        {
            //Stores the parameters
            language = MainWindow.Instance.CurrentLanguage;
            _tokenProvider = tokenProvider;
            this.LazyLoading = !manuallyFilledChildren;
            _manuallyFilledChildren = manuallyFilledChildren;

            //Sets the foreground
            this.Foreground = GlobalContainer.NewNodesBrush;
        }

        /// <summary>
        /// Returns a value indicating whether the children of this node are manually added or not
        /// </summary>
        public bool ManuallyFilledChildren
        {
            get { return _manuallyFilledChildren; }
        }

        /// <summary>
        /// Returns the IMetadataTokenProvider represented by this node
        /// </summary>
        public IMetadataTokenProvider TokenProvider
        {
            get { return _tokenProvider; }
        }

        /// <summary>
        /// Returns the MemberReference represented by this node
        /// </summary>
        public MemberReference Member
        {
            get { return TokenProvider as MemberReference; }
        }

        /// <summary>
        /// Gets or sets the predicate used to filter the children of this node
        /// </summary>
        public Predicate<IMetadataTokenProvider> ChildrenFilter { get; set; }

        /// <summary>
        /// Ensures the children are always ready (avoids showing the expander icon when there are no children)
        /// </summary>
        protected override void OnExpanding()
        {
            base.OnExpanding();
            foreach (var x in Children)
                x.EnsureLazyChildren();
        }

        /// <summary>
        /// Loads the children
        /// </summary>
        protected override void LoadChildren()
        {
            //Checks if the children must be filled manually
            if (ManuallyFilledChildren)
                return;

            //IEnumerable containing the childrenot the node
            IEnumerable<IMetadataTokenProvider> children = null;

            //Assembly
            if (_tokenProvider is AssemblyDefinition)
                children = ((AssemblyDefinition)_tokenProvider).Modules;
            //Module
            else if (_tokenProvider is ModuleDefinition)
            {
                //Types
                IEnumerable<TypeDefinition> types = ((ModuleDefinition)_tokenProvider).Types;
                if (this.ChildrenFilter != null)
                    types = types.Where(x => this.ChildrenFilter(x));

                //Groups the types by namespace and adds
                foreach (var node in types.GroupBy(x => x.Namespace)
                                     .OrderBy(x => x.Key)
                                     .Select(x =>
                                     {
                                         var n = new NamespaceTreeNode(x.Key);
                                         foreach (var t in x.OrderBy(y => y.Name))
                                             n.Children.Add(new ILEditTreeNode(t, false) { ChildrenFilter = this.ChildrenFilter, Foreground = this.Foreground });
                                         return n;
                                     })) 
                {
                    this.Children.Add(node);
                }

                //Returns
                return;
            }
            //Type
            else if (_tokenProvider is TypeDefinition)
            {
                var type = (TypeDefinition)_tokenProvider;
                children =
                    type.NestedTypes.Cast<IMetadataTokenProvider>()
                    .Concat(type.Fields)
                    .Concat(type.Methods.Where(x => !(x.IsGetter || x.IsSetter || x.IsAddOn || x.IsRemoveOn || x.IsFire)))
                    .Concat(type.Properties)
                    .Concat(type.Events);
            }
            //Property
            else if (_tokenProvider is PropertyDefinition)
            {
                var prop = (PropertyDefinition)_tokenProvider;
                children =
                    new IMetadataTokenProvider[] { prop.GetMethod, prop.SetMethod }
                    .Where(x => x != null)
                    .Concat(prop.HasOtherMethods ? prop.OtherMethods.Cast<IMetadataTokenProvider>() : new IMetadataTokenProvider[] { });
            }
            //Event
            else if (_tokenProvider is EventDefinition)
            {
                var evt = (EventDefinition)_tokenProvider;
                children =
                    new IMetadataTokenProvider[] { evt.AddMethod, evt.RemoveMethod, evt.InvokeMethod }
                    .Where(x => x != null)
                    .Concat(evt.HasOtherMethods ? evt.OtherMethods.Cast<IMetadataTokenProvider>() : new IMetadataTokenProvider[] { });
            }

            //If there are no children returns
            if (children == null)
                return;

            //Applies the filtering
            if (ChildrenFilter != null)
                children = children.Where(x => ChildrenFilter(x));

            //Adds the children
            foreach (var x in children)
                this.Children.Add(new ILEditTreeNode(x, false) { ChildrenFilter = this.ChildrenFilter, Foreground = this.Foreground });
        }

        /// <summary>
        /// Forces a complete refresh of all the children (if automatically filled)
        /// </summary>
        public void RefreshChildren()
        {
            if (!_manuallyFilledChildren)
            {
                this.Children.Clear();
                LoadChildren();
                foreach (var x in Children)
                    x.EnsureLazyChildren();
            }
        }

        /// <summary>
        /// Returns the icon to show
        /// </summary>
        public override object Icon
        {
            get
            {
                //Switches the token type
                switch (_tokenProvider.MetadataToken.TokenType)
                {
                    //Assembly
                    case TokenType.Assembly:
                    case TokenType.AssemblyRef:
                        return new BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Assembly.png"));
                    //Module
                    case TokenType.Module:
                    case TokenType.ModuleRef:
                        return new BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Library.png"));
                    //Type
                    case TokenType.TypeDef:
                        return TypeTreeNode.GetIcon((TypeDefinition)_tokenProvider);
                    //Type specification (generic instance type)
                    case TokenType.TypeSpec:
                        return TypeTreeNode.GetIcon(((TypeSpecification)_tokenProvider).ElementType.Resolve());
                    //Field
                    case TokenType.Field:
                        return FieldTreeNode.GetIcon((FieldDefinition)_tokenProvider);
                    //Method
                    case TokenType.Method:
                        return MethodTreeNode.GetIcon((MethodDefinition)_tokenProvider);
                    //Event
                    case TokenType.Event:
                        return EventTreeNode.GetIcon((EventDefinition)_tokenProvider);
                    //Property
                    case TokenType.Property:
                        return PropertyTreeNode.GetIcon((PropertyDefinition)_tokenProvider);
                    //Member reference
                    case TokenType.MemberRef:
                        var memberRef = (MemberReference)_tokenProvider;
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
                                return new ILEditTreeNode(memberDef, true).Icon;
                        }
                        break;
                }

                //No icons available
                return null;
            }
        }

        /// <summary>
        /// Returns the text of this node
        /// </summary>
        public override object Text
        {
            get
            {
                try
                {
                    //Checks if the member is a member with particular text formatting
                    if (_tokenProvider is MethodDefinition)
                        return MethodTreeNode.GetText((MethodDefinition)_tokenProvider, language);
                    else if (_tokenProvider is PropertyDefinition)
                        return PropertyTreeNode.GetText((PropertyDefinition)_tokenProvider, language);
                    else if (_tokenProvider is EventDefinition)
                        return EventTreeNode.GetText((EventDefinition)_tokenProvider, language);
                    else if (_tokenProvider is TypeDefinition)
                        return language.FormatTypeName((TypeDefinition)_tokenProvider);
                    else if (_tokenProvider is GenericInstanceType)
                        return language.TypeToString((GenericInstanceType)_tokenProvider, false);
                    else if (_tokenProvider is PropertyDefinition)
                        return language.FormatPropertyName((PropertyDefinition)_tokenProvider);

                    //Returns the normal name
                    else if (_tokenProvider is AssemblyDefinition)
                        return ((AssemblyDefinition)_tokenProvider).Name.Name;
                    else if (_tokenProvider is ModuleDefinition)
                        return ((ModuleDefinition)_tokenProvider).Name;
                    else if (_tokenProvider is AssemblyNameReference)
                        return ((AssemblyNameReference)_tokenProvider).Name;
                    else if (_tokenProvider is IMemberDefinition)
                        return ((IMemberDefinition)_tokenProvider).Name;
                    else if (_tokenProvider is MemberReference)
                        return ((MemberReference)_tokenProvider).Name;
                    else
                        throw new NotSupportedException();
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        public override void Decompile(ICSharpCode.ILSpy.Language language, ITextOutput output, ICSharpCode.ILSpy.DecompilationOptions options)
        {
            //Switches on the token
            switch (_tokenProvider.MetadataToken.TokenType)
            {
                case TokenType.Module:
                    language.DecompileModule((ModuleDefinition)_tokenProvider, output, options);
                    break;
                case TokenType.Assembly:
                    language.WriteCommentLine(output, ((AssemblyNameReference)_tokenProvider).FullName);
                    break;
                case TokenType.TypeDef:
                    language.DecompileType((TypeDefinition)_tokenProvider, output, options);
                    break;
                case TokenType.Field:
                    language.DecompileField((FieldDefinition)_tokenProvider, output, options);
                    break;
                case TokenType.Method:
                    language.DecompileMethod((MethodDefinition)_tokenProvider, output, options);
                    break;
                case TokenType.Event:
                    language.DecompileEvent((EventDefinition)_tokenProvider, output, options);
                    break;
                case TokenType.Property:
                    language.DecompileProperty((PropertyDefinition)_tokenProvider, output, options);
                    break;
                case TokenType.MemberRef:
                    var memberRef = (MemberReference)_tokenProvider;
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
                            new ILEditTreeNode(memberDef, true).Decompile(language, output, options);
                    }
                    break;
                default:
                    language.WriteCommentLine(output, (string)this.Text);
                    break;
            }
        }
    }
}
