using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILEdit.Injection;

namespace ILEdit.Injection.Injectors
{
    /// <summary>
    /// Class injector
    /// </summary>
    public class ClassInjector : IInjector
    {
        #region Properties

        public string Name
        {
            get { return "Class"; }
        }

        public string Description
        {
            get { return "Injects a new class"; }
        }

        public System.Windows.Media.ImageSource Icon
        {
            get { return new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Class.png")); }
        }

        public bool NeedsMember
        {
            get { return false; }
        }

        public Predicate<Mono.Cecil.IMetadataTokenProvider> MemberFilter
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        public bool CanInjectInNode(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node)
        {
            return true;
        }

        public void Inject(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node)
        {
            throw new NotImplementedException();
        }
    }
}
