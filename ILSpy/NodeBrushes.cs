using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace ICSharpCode.ILSpy
{
    /// <summary>
    /// Contains the brushes used to paint the nodes
    /// </summary>
    public static class NodeBrushes
    {

        static NodeBrushes()
        {
            //Sets the brushes
            //(performs this action on the UI thread)
            Action createBrushes = () =>
            {
                _Normal = new SolidColorBrush(Colors.Black);
                _Private = new SolidColorBrush(Colors.Gray);
            };
            if (Application.Current.Dispatcher.CheckAccess())
                createBrushes();
            else
                Application.Current.Dispatcher.Invoke(createBrushes, null);
        }

        private static Brush _Normal;
        /// <summary>
        /// Normal brush, used to paint public APIs
        /// </summary>
        public static Brush Normal { get { return _Normal; } }


        private static Brush _Private;
        /// <summary>
        /// Brush used to paint private APIs
        /// </summary>
        public static Brush Private { get { return _Private; } }
        

    }
}
