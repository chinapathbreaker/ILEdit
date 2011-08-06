using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using Microsoft.Win32;
using Mono.Cecil;
using System.IO;

namespace ILEdit.ContextMenu
{
    [ExportContextMenuEntry(Icon = "/Images/SaveFile.png", Header = "Save ...", Category = "Injection", Order = 2)]
    public class SaveEntry : IContextMenuEntry
    {
        public bool IsVisible(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            return selectedNodes.Length == 1 && selectedNodes[0] is AssemblyTreeNode;
        }

        public bool IsEnabled(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            return true;
        }

        public void Execute(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            //Gets the loaded assembly
            var loadedAssembly = ((AssemblyTreeNode)selectedNodes[0]).LoadedAssembly;

            //Shows the dialog
            var dialog = new SaveFileDialog()
            {
                AddExtension = false,
                Filter = "Patched version of " + loadedAssembly.AssemblyDefinition.Name.Name + "|*.*",
                FileName = Path.GetFileName(Path.ChangeExtension(loadedAssembly.FileName, ".Patched" + Path.GetExtension(loadedAssembly.FileName))),
                InitialDirectory = Path.GetDirectoryName(loadedAssembly.FileName),
                OverwritePrompt = true,
                Title = "Save patched assembly"
            };
            if (dialog.ShowDialog().GetValueOrDefault(false))
                loadedAssembly.AssemblyDefinition.Write(dialog.FileName);
        }
    }
}
