using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy.TreeNodes;
using System.Windows.Media;

namespace ILEdit.Injection
{
    public class InjectWindowViewModel
    {
        #region .ctor
        public InjectWindowViewModel(ILSpyTreeNode node)
        {
            //Loads the injectors
            Injectors = GlobalContainer.Injectors.Where(x => x.CanInjectInNode(node)).ToArray();

            //Writes data for the header
            InjectInIcon = (ImageSource)node.Icon;
            InjectInContent = node.Text;

            //Prepares the commands
            _InjectCommand = new RelayCommand(InjectCommandImpl);
        }
        #endregion

        #region Properties

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
        /// Gets or sets the name given by the user
        /// </summary>
        public string Name { get; set; }

        #endregion

        #region Inject command

        private RelayCommand _InjectCommand = null;
        public RelayCommand InjectCommand { get { return _InjectCommand; } }
        private void InjectCommandImpl()
        {
            System.Windows.MessageBox.Show("Command invoked on " + this.SelectedInjector.Name + " with name '" + this.Name + "'");
        }
        
        #endregion
    }
}
