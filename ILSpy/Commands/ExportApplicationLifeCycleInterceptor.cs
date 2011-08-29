using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Composition;

namespace ICSharpCode.ILSpy
{
    public interface IApplicationLifeCycleInterceptor
    {
        void OnLoaded();
        void OnClosing(CancelEventArgs e);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExportApplicationLifeCycleInterceptorAttribute : ExportAttribute
    {
        public ExportApplicationLifeCycleInterceptorAttribute()
            : base("ApplicationLifeCycleInterceptor", typeof(IApplicationLifeCycleInterceptor))
        {
        }
    }
}
