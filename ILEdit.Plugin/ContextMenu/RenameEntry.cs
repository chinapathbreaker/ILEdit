using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ILEdit.ContextMenu
{
    [ExportContextMenuEntry(Icon = "Images/Rename.png", Header = "Rename ...", Category = "Rename", Order = 0)]
    public class RenameEntry : IContextMenuEntry
    {
        public bool IsVisible(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            var node = selectedNodes[0];
            return
                (node is IMemberTreeNode && ((IMemberTreeNode)node).Member is Mono.Cecil.IMemberDefinition) &&
                !(node is ICSharpCode.ILSpy.TreeNodes.Analyzer.AnalyzerTreeNode);
        }

        public bool IsEnabled(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            return true;
        }

        public void Execute(ICSharpCode.TreeView.SharpTreeNode[] selectedNodes)
        {
            //Member
            var member = (IMemberDefinition)((IMemberTreeNode)selectedNodes[0]).Member;
            var rename = GetObjectsToRename(member).ToArray();

            //Content of the input box
            var content = new StackPanel();
            content.Children.Add(new TextBlock() { Inlines = { new Run() { Text = "Insert new name for " }, new Run() { Text = member.Name, FontWeight = System.Windows.FontWeights.Bold }, new Run() { Text = "." } } });
            if (rename.Length > 1)
            {
                content.Children.Add(new TextBlock() { Text = "This action will automatically update the following members:", Margin = new System.Windows.Thickness(0, 3, 0, 0) });
                foreach (var x in rename.Skip(1))
                    content.Children.Add(new TextBlock() { Text = x.Key.Name, FontWeight = System.Windows.FontWeights.Bold, Margin = new System.Windows.Thickness(0, 3, 0, 0) });
            }

            //Asks for the new name and performs the renaming
            var input = new InputBox("New name", content);
            if (input.ShowDialog().GetValueOrDefault(false) && !string.IsNullOrEmpty(input.Value))
                foreach (var x in rename)
                    x.Key.Name = string.Format(x.Value, input.Value);
        }

        public IEnumerable<KeyValuePair<IMemberDefinition, string>> GetObjectsToRename(IMemberDefinition member)
        {
            //Returns ths given intem
            yield return new KeyValuePair<IMemberDefinition, string>(member, "{0}");

            //Checks if the member is a property or an event
            if (member is PropertyDefinition)
            {
                var prop = (PropertyDefinition)member;
                if (prop.GetMethod != null)
                    yield return new KeyValuePair<IMemberDefinition, string>(prop.GetMethod, "get_{0}");
                if (prop.SetMethod != null)
                    yield return new KeyValuePair<IMemberDefinition, string>(prop.SetMethod, "set_{0}");
                if (prop.HasOtherMethods)
                    foreach (var m in prop.OtherMethods)
                        yield return new KeyValuePair<IMemberDefinition, string>(m, "{0}");
            }
            else if (member is EventDefinition)
            {
                var evt = (EventDefinition)member;
                if (evt.AddMethod != null)
                    yield return new KeyValuePair<IMemberDefinition, string>(evt.AddMethod, "add_{0}");
                if (evt.RemoveMethod != null)
                    yield return new KeyValuePair<IMemberDefinition, string>(evt.RemoveMethod, "remove_{0}");
                if (evt.InvokeMethod != null)
                    yield return new KeyValuePair<IMemberDefinition, string>(evt.InvokeMethod, "raise_{0}");
                if (evt.HasOtherMethods)
                    foreach (var m in evt.OtherMethods)
                        yield return new KeyValuePair<IMemberDefinition, string>(m, "{0}");
            }
            yield break;
        }

    }
}
