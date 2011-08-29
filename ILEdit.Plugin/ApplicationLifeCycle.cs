using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using System.Windows;

namespace ILEdit
{
    [ExportApplicationLifeCycleInterceptor()]
    public class ApplicationLifeCycle : IApplicationLifeCycleInterceptor
    {
        public void OnLoaded()
        {
        }

        public void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //Checks that there are no unsaved edited assemblies
            if (MainWindow.Instance.RootNode.Foreground == GlobalContainer.ModifiedNodesBrush)
                e.Cancel = MessageBox.Show("There are some pending changes." + Environment.NewLine + "Are you sure you want to exit?", "Exit confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No;
        }
    }
}
