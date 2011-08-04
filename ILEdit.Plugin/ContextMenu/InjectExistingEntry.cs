using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using ICSharpCode.TreeView;
using ICSharpCode.ILSpy;

namespace ILEdit.ContextMenu
{
    [ExportContextMenuEntry(Icon = "Images/InjectExisting.png", Header = "Inject existing ...", Category = "Injection", Order = 1)]
    public class InjectExistingEntry : IContextMenuEntry
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
