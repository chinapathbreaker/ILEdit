using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy.TreeNodes;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;
using Mono.Cecil;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.TreeView;

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

            //Loads from the settings the values of the properties of the 'existing' part
            var settings = GlobalContainer.InjectionSettings.Element("InjectExistingSettings");
            ExistingPreview = bool.Parse(settings.Attribute("Preview").Value);
            ExistingImportAsNestedTypes = bool.Parse(settings.Attribute("ImportAsNestedTypes").Value);
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

        /// <summary>
        /// Gets or sets a value indicating whether to show the preview before performing the actual importing
        /// </summary>
        public bool ExistingPreview { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to import a new type as nested type or not
        /// </summary>
        public bool ExistingImportAsNestedTypes { get; set; }

        #endregion

        #region Inject command

        private RelayCommand _InjectCommand = null;
        public RelayCommand InjectCommand { get { return _InjectCommand; } }
        private void InjectCommandImpl()
        {
            //Flag to determine whther to color the parent nodes
            var cancel = false;

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
                    
                    //Break
                    break;
               
                //Inject existing
                case 1:

                    //Checks that there's a selection
                    if (ExistingSelectedMember == null)
                    {
                        MessageBox.Show("Select a valid member to import in '" + _node.Text.ToString() + "'", "", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //Cancellation token
                    var cts = new CancellationTokenSource();
                    var ct = cts.Token;

                    //Starts a new task to perform importing
                    var t = new Task(() => {
                        
                        //Imports
                        using (var importer = ILEdit.Injection.Existing.MemberImporterFactory.Create(ExistingSelectedMember, _node is ModuleTreeNode ? ((ModuleTreeNode)_node).Module : (IMetadataTokenProvider)((IMemberTreeNode)_node).Member))
                        {
                            //Options
                            var options = new Existing.MemberImportingOptions()  {
                                ImportAsNestedType = ExistingImportAsNestedTypes,
                                CancellationToken = ct
                            };

                            //Performs scanning
                            importer.Scan(options);

                            //Checks whether to show the preview
                            if (ExistingPreview)
                            {

                                //Builds the preview
                                var root = new SharpTreeNode();
                                importer.BuildPreviewNodes(root);

                                //Shows the preview window
                                Application.Current.Dispatcher.Invoke((Action)(() => {
                                    cancel = !new Existing.PreviewWindow(root, _node).ShowDialog().GetValueOrDefault(false);
                                }), null);
                            }

                            //Performs importing on the dispatcher thread
                            if (!cancel)
                                Application.Current.Dispatcher.Invoke((Action)(() => { importer.Import(options, _node); }), null);

                        }

                    }, ct);
                    t.Start();
                    t.ContinueWith(task => {

                        //Hides the window
                        Application.Current.Dispatcher.Invoke((Action)WaitWindow.Hide, null);

                        //Checks for any exception
                        if (task.Exception != null)
                        {
                            var text = string.Join(Environment.NewLine + Environment.NewLine, task.Exception.InnerExceptions.Select(x => x.Message).ToArray());
                            Application.Current.Dispatcher.BeginInvoke((Action)(() => {
                                MessageBox.Show("An exception occurred:" + Environment.NewLine + Environment.NewLine + text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }), null);
                        }

                    });

                    //Shows the wait window
                    WaitWindow.ShowDialog("Importing in progress ...", "Please, wait while the importing is in progress ...", cts);

                    break;
                
                //Other: exception
                default:
                    throw new ArgumentException("Invalid value: it can be only 0 or 1", "TabSelectedIndex");
            }

            //Colors the parents of the node
            if (!cancel)
            {
                //Colors the ancestors of this node
                var parent = (ICSharpCode.TreeView.SharpTreeNode)_node;
                while (parent != null)
                {
                    if (parent.Foreground != GlobalContainer.NewNodesBrush)
                        parent.Foreground = GlobalContainer.ModifiedNodesBrush;
                    parent = parent.Parent;
                }

                //Refreshes the view
                ICSharpCode.ILSpy.MainWindow.Instance.RefreshDecompiledView();

                //Saves the settings
                var settings = GlobalContainer.InjectionSettings.Element("InjectExistingSettings");
                settings.SetAttributeValue("Preview", ExistingPreview);
                settings.SetAttributeValue("ImportAsNestedTypes", ExistingImportAsNestedTypes);
                GlobalContainer.SettingsManager.Instance.Save();

                //Closes the window
                _window.Close();
            }
        }
        
        #endregion
    }
}
