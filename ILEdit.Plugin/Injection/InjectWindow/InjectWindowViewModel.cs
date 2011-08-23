using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy.TreeNodes;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;
using Mono.Cecil;

namespace ILEdit.Injection
{
    public class InjectWindowViewModel
    {
        #region .ctor
        ILSpyTreeNode _node;
        Window _window;
        public InjectWindowViewModel(ILSpyTreeNode node, Window window, bool canInjectExisting)
        {
            //Stores the given parameters
            _node = node;
            _window = window;
            InjectExistingEnabled = canInjectExisting;

            //Loads the injectors
            Injectors = GlobalContainer.Injectors.Where(x => x.CanInjectInNode(node)).ToArray();

            //Writes data for the header
            InjectInIcon = (ImageSource)node.Icon;
            InjectInContent = node.Text;

            //Finds the module
            var moduleNode = ILEdit.Injection.Injectors.TreeHelper.GetModuleNode(node);
            DestinationModule = moduleNode == null ? null : moduleNode.Module;

            //Finds the enclosing type (if any)
            EnclosingType = Injection.Injectors.TreeHelper.GetType(node);

            //Prepares the filters for the inject existing part
            if (canInjectExisting)
            {
                ExistingMemberFilter = node is ModuleTreeNode ? MemberFilters.Types : null;
                ExistingSelectableMembers = node is ModuleTreeNode ? new[] { TokenType.TypeDef } : new[] { TokenType.TypeDef, TokenType.Field, TokenType.Property, TokenType.Method, TokenType.Event };
            }

            //Prepares the commands
            _InjectCommand = new RelayCommand(InjectCommandImpl);
        }
        #endregion

        #region Properties

        /// <summary>
        /// Returns the module destination of the injection
        /// </summary>
        public ModuleDefinition DestinationModule { get; private set; }

        /// <summary>
        /// Returns the type enclosing the current node
        /// </summary>
        public TypeDefinition EnclosingType { get; private set; }

        /// <summary>
        /// Returns a list of all the injectors available for the given node
        /// </summary>
        public IInjector[] Injectors { get; private set; }

        /// <summary>
        /// Returns the icon to show in the header
        /// </summary>
        public ImageSource InjectInIcon { get; private set; }

        /// <summary>
        /// Returns the text to show in the header
        /// </summary>
        public object InjectInContent { get; private set; }

        /// <summary>
        /// Gets or sets the currently selected injector
        /// </summary>
        public IInjector SelectedInjector { get; set; }

        /// <summary>
        /// Gets or sets the member selected by the user (if needed to the injector)
        /// </summary>
        public IMetadataTokenProvider SelectedMember { get; set; }

        /// <summary>
        /// Gets or sets the name given by the user
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns the index of the currently selected tab (0 = New, 1 = Existing)
        /// </summary>
        public int TabSelectedIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tab 'Inject existing' is enabled or not
        /// </summary>
        public bool InjectExistingEnabled { get; private set; }

        /// <summary>
        /// Returns a value indicating which members the user can select
        /// </summary>
        public TokenType[] ExistingSelectableMembers { get; private set; }

        /// <summary>
        /// Returns the predicate used to filter the members to show in the inject existing part
        /// </summary>
        public Predicate<IMetadataTokenProvider> ExistingMemberFilter { get; private set; }

        /// <summary>
        /// Gets or sets the member selected in the inject existing part
        /// </summary>
        public IMetadataTokenProvider ExistingSelectedMember { get; set; }

        #endregion

        #region Inject command

        private RelayCommand _InjectCommand = null;
        public RelayCommand InjectCommand { get { return _InjectCommand; } }
        private void InjectCommandImpl()
        {
            //Switches on the injection type (new or existing)
            switch (TabSelectedIndex)
            {
                //Inject new
                case 0:
                    //Checks that the name has been provided
                    if (string.IsNullOrEmpty(this.Name))
                    {
                        MessageBox.Show("A name is required", "Name required", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //Checks that the member has been provided (if needed)
                    if (SelectedInjector.NeedsMember && SelectedMember == null)
                    {
                        MessageBox.Show("A type is required", "Type required", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //Checks that the selected type is valid
                    if (SelectedInjector.NeedsMember && !SelectedInjector.MemberFilter(this.SelectedMember))
                    {
                        MessageBox.Show("The selected type is not valid for '" + this.SelectedInjector.Name + "'", "Type required", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //Injects
                    this.SelectedInjector.Inject(_node, this.Name, this.SelectedInjector.NeedsMember ? this.SelectedMember : null);
                    ICSharpCode.ILSpy.MainWindow.Instance.RefreshDecompiledView();
                    
                    //Colors the parents of the node
                    var parent = (ICSharpCode.TreeView.SharpTreeNode)_node;
                    while (parent != null)
                    {
                        parent.Foreground = GlobalContainer.ModifiedNodesBrush;
                        parent = parent.Parent;
                    }

                    //Break
                    break;
               
                //Inject existing
                case 1:
                    break;
                
                //Other: exception
                default:
                    throw new ArgumentException("Invlid value: it can be only 0 or 1", "TabSelectedIndex");
            }

            //Closes the window
            _window.Close();
        }
        
        #endregion
    }
}
