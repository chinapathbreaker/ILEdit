using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Mono.Cecil;
using ICSharpCode.TreeView;
using ICSharpCode.ILSpy;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ILEdit
{
    /// <summary>
    /// Interaction logic for SelectMemberWindow.xaml
    /// </summary>
    public partial class SelectMemberWindow : Window
    {
        private static Tuple<string, string>[] _commonMembers = new Tuple<string, string>[] { 
            Tuple.Create("System", "Int32"),
            Tuple.Create("System", "Int64"),
            Tuple.Create("System", "Boolean"),
            Tuple.Create("System", "String"),
            Tuple.Create("System", "Double"),
            Tuple.Create("System", "Object"),
            Tuple.Create("System", "Void"),
            Tuple.Create("System", "EventHandler"),
            Tuple.Create("System", "Action"),
            Tuple.Create("System", "Action`1"),
            Tuple.Create("System", "Func`1"),
            Tuple.Create("System", "Func`2")
        };

        private XElement _recentMembersNode;
        private ModuleDefinition _destinationModule;
        private TypeDefinition _enclosingType;

        private int maxRecentMembersCount;

        private CancellationToken ct;
        private CancellationTokenSource cts;

        public SelectMemberWindow(Predicate<IMetadataTokenProvider> filter, IList<TokenType> token, ModuleDefinition destinationModule, TypeDefinition enclosingType)
        {
            //Initializes the components
            InitializeComponent();
            _destinationModule = destinationModule;
            _enclosingType = enclosingType;
            maxRecentMembersCount = int.Parse(GlobalContainer.InjectionSettings.Attribute("MaxRecentMembersCount").Value);

            //Sets the filter
            tree.MemberFilter = filter;
            tree.SelectableMembers = token;
            
            //Prepares the common types
            if (destinationModule != null)
            {
                //Fills the list
                LstCommonTypes.ItemsSource =
                    _commonMembers
                    .Select(x => new ILEditTreeNode(new TypeReference(x.Item1, x.Item2, destinationModule, destinationModule.TypeSystem.Corlib).Resolve(), true))
                    .Where(x => filter(x.TokenProvider) && token.Any(t => t == x.TokenProvider.MetadataToken.TokenType));

                //Registers the selection handler
                LstCommonTypes.SelectionChanged += (_, e) => {
                    if (e.AddedItems.Count == 1)
                    {
                        var node = ((ILEditTreeNode)e.AddedItems[0]);
                        this.SelectedMember = node.TokenProvider;
                        BtnOk.IsEnabled = true;
                        tree.SelectedIndex = -1;
                        LstRecentTypes.SelectedIndex = -1;
                        ImgSelectedMember.Source = (ImageSource)node.Icon;
                        ContentSelectedMember.Content = node.Text;
                    }
                };
            }

            //Prepares the recent types
            var recentTypes = GetRecentMembers();
            if (recentTypes.Length > 0)
            {
                LstRecentTypes.ItemsSource = 
                    recentTypes
                    .Where(x => filter(x) && token.Any(t => t == x.MetadataToken.TokenType))
                    .Select(x => new ILEditTreeNode(x, true));
                LstRecentTypes.SelectionChanged += (_, e) =>
                {
                    if (e.AddedItems.Count == 1)
                    {
                        var node = ((ILEditTreeNode)e.AddedItems[0]);
                        this.SelectedMember = node.TokenProvider;
                        BtnOk.IsEnabled = true;
                        tree.SelectedIndex = -1;
                        LstCommonTypes.SelectedIndex = -1;
                        ImgSelectedMember.Source = (ImageSource)node.Icon;
                        ContentSelectedMember.Content = node.Text;
                    }
                };
            }

            //Handler to enable or disable the Ok button
            tree.SelectionChanged += (_, e) => {
                if (tree.SelectedMember != null)
                {
                    this.SelectedMember = tree.SelectedMember;
                    BtnOk.IsEnabled = true;
                    LstCommonTypes.SelectedIndex = -1;
                    LstRecentTypes.SelectedIndex = -1;
                    var node = (SharpTreeNode)tree.SelectedItem;
                    ImgSelectedMember.Source = (ImageSource)node.Icon;
                    ContentSelectedMember.Content = node.Text;
                }
            };
        }

        private MemberReference[] GetRecentMembers()
        {
            //Return value
            var ret = new List<MemberReference>();

            //Gets the settings node
            _recentMembersNode = GlobalContainer.InjectionSettings.Element("RecentMembers");

            //Array containing the loaded assemblies
            var asms =
                MainWindow.Instance.CurrentAssemblyList.GetAssemblies()
                .Where(x => x.AssemblyDefinition != null)
                .Select(x => x.AssemblyDefinition)
                .ToArray();

            //Reiterates along the children and builds the references
            foreach (var xel in _recentMembersNode.Elements("Member"))
            {
                var asm = asms.FirstOrDefault(x => x.FullName == xel.Attribute("Assembly").Value);
                var module = asm == null ? null : asm.Modules.FirstOrDefault(x => x.Name == xel.Attribute("Module").Value);
                var member = module == null ? null : ICSharpCode.ILSpy.XmlDoc.XmlDocKeyProvider.FindMemberByKey(module, xel.Attribute("Key").Value);
                if (member == null)
                {
                    xel.Remove();
                    continue;
                }
                ret.Add(member);
            }

            //Return
            return ret.ToArray();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            //Checks that the pressed key is enter
            if (e.Key != Key.Enter)
                return;

            //Initializes the tokens
            cts = new CancellationTokenSource();
            ct = cts.Token;

            //Shows the progress and locks the textbox
            SearchWaitProgress.IsIndeterminate = true;
            SearchWaitProgress.Visibility = Visibility.Visible;
            TxtSearch.IsEnabled = false;

            //Starts the research
            var t = new Task<SharpTreeNode>(param => {

                //Return node
                SharpTreeNode ret = null;

                //Extracts the nodes and the search term
                var nodes = ((Tuple<SharpTreeNode[], string>)param).Item1;
                var term = ((Tuple<SharpTreeNode[], string>)param).Item2.ToLower();

                //Searchs the term
                foreach (var node in Helpers.PreOrder(nodes, x => { x.EnsureLazyChildren(); return x.Children; }))
                {
                    ct.ThrowIfCancellationRequested();
                    if (node.Text != null && node.Text.ToString().ToLower().StartsWith(term))
                    {
                        ret = node;
                        break;
                    }
                }

                //Return
                return ret;

            }, Tuple.Create(tree.Items.Cast<SharpTreeNode>().ToArray(), TxtSearch.Text), ct);
            t.Start();
            t.ContinueWith(task => { Application.Current.Dispatcher.BeginInvoke((Action)(() => {

                //Checks for exceptions
                if (task.Exception != null)
                {
                    MessageBox.Show("Error:" + Environment.NewLine + task.Exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (task.Result != null)
                {
                    tree.FocusNode(task.Result);
                    tree.SelectedItem = task.Result;
                }
                else
                {
                    MessageBox.Show("No members found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                //Hides the progress and unlocks the textbox
                SearchWaitProgress.IsIndeterminate = false;
                SearchWaitProgress.Visibility = Visibility.Collapsed;
                TxtSearch.IsEnabled = true;

            })); });
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            //Member
            var member = (MemberReference)this.SelectedMember;

            //Checks if the selected member has some generic parameters
            var generic = member as IGenericParameterProvider;
            if (generic != null && generic.HasGenericParameters)
            {
                var genericWindow = new GenericResolutionWindow(generic, _enclosingType);
                if (genericWindow.ShowDialog().GetValueOrDefault(false))
                {
                    this.SelectedMember = (MemberReference)genericWindow.ResolvedGeneric;
                }
                else
                {
                    return;
                }
            }

            //Computes the key for the selected member
            var key = ICSharpCode.ILSpy.XmlDoc.XmlDocKeyProvider.GetKey(member);

            //Removes it (if present) fro the recent list
            var recentNode = _recentMembersNode.Elements().FirstOrDefault(x => x.Attribute("Key").Value == key);
            if (recentNode != null)
                recentNode.Remove();

            //Creates a new node and adds it to the list
            recentNode =
                new XElement("Member",
                    new XAttribute("Assembly", member.Module.Assembly.FullName),
                    new XAttribute("Module", member.Module.Name),
                    new XAttribute("Key", key)
                );
            _recentMembersNode.AddFirst(recentNode);

            //Checks if the list has excedeed the maximum allowed size
            if (_recentMembersNode.Elements().Count() > maxRecentMembersCount)
                _recentMembersNode.Elements().Last().Remove();

            //Saves the settings
            GlobalContainer.SettingsManager.Instance.Save();

            //Returns to the caller
            this.DialogResult = true;
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //Ensures that the search task is ended
            if (cts != null && !cts.IsCancellationRequested)
                cts.Cancel();
        }

        /// <summary>
        /// Returns the member selected by the user
        /// </summary>
        public IMetadataTokenProvider SelectedMember { get; private set; }

    }
}
