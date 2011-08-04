using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using ICSharpCode.TreeView;

namespace ILEdit.ContextMenu
{
    [ExportContextMenuEntry(Icon = "Images/InjectNew.png", Header = "Inject new ...", Category = "Injection", Order = 0)]
    public class InjectNewEntry : IContextMenuEntry
    {
        public bool IsVisible(SharpTreeNode[] selectedNodes)
        {
            return true;
        }

        public bool IsEnabled(SharpTreeNode[] selectedNodes)
        {
            return true;
        }

        public void Execute(SharpTreeNode[] selectedNodes)
        {
            throw new NotImplementedException();
        }
    }
}
