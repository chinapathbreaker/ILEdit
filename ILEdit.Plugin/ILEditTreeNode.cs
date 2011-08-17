using System;
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
    /// Node which represents a member. It can auto-fill its children of they can be filled manually. A predicate determines which items to show if the children are automatically determined.
    /// </summary>
    internal class ILEditTreeNode : ILSpyTreeNode
    {
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
            this.ForegroundColor = GlobalContainer.ModifiedNodesColor;
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
        /// Gets or sets the predicate used to filter the children of this node
        /// </summary>
        public Predicate<IMetadataTokenProvider> ChildrenFilter { get; set; }

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
                children = ((ModuleDefinition)_tokenProvider).Types;
            //Type
            else if (_tokenProvider is TypeDefinition)
            {
                var type = (TypeDefinition)_tokenProvider;
                children =
                    type.NestedTypes.Cast<IMetadataTokenProvider>()
                    .Concat(type.Fields)
                    .Concat(type.Methods)
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
                this.Children.Add(new ILEditTreeNode(x, false) { ChildrenFilter = this.ChildrenFilter });
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
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Returns the text of this node
        /// </summary>
        public override object Text
        {
            get
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
                else if (_tokenProvider is PropertyDefinition)
                    return language.FormatPropertyName((PropertyDefinition)_tokenProvider);

                //Returns the normal name
                else if (_tokenProvider is AssemblyDefinition)
                    return ((AssemblyDefinition)_tokenProvider).Name.Name;
                else if (_tokenProvider is ModuleDefinition)
                    return ((ModuleDefinition)_tokenProvider).Name;
                else if (_tokenProvider is IMemberDefinition)
                    return ((IMemberDefinition)_tokenProvider).Name;
                else if (_tokenProvider is MemberReference)
                    return ((MemberReference)_tokenProvider).Name;
                else
                    throw new NotSupportedException();
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
                default:
                    language.WriteCommentLine(output, (string)this.Text);
                    break;
            }
        }

    }
}
