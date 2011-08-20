using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace ILEdit
{
    /// <summary>
    /// Represents a set of attached properties to use in the XAML
    /// </summary>
    internal class XAMLExtensions
    {
        #region TextFilter

        public enum TextFilter
        {
            None = 0,
            Digits = 1,
            Chars = 2,
            DigitsAndChars = 3
        }

        public static TextFilter GetTextFilter(TextBox obj)
        {
            return (TextFilter)obj.GetValue(TextFilterProperty);
        }

        public static void SetTextFilter(TextBox obj, TextFilter value)
        {
            obj.SetValue(TextFilterProperty, value);
        }

        // Using a DependencyProperty as the backing store for TextFilter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextFilterProperty =
            DependencyProperty.RegisterAttached("TextFilter", typeof(TextFilter), typeof(XAMLExtensions), new PropertyMetadata(TextFilter.None, TextFilterChanged));


        private static void TextFilterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender == null)
                return;
            var obj = sender as TextBox;
            if (obj == null)
                throw new ArgumentException("This property can be attached only to TextBoxes");
            if ((TextFilter)e.NewValue == TextFilter.None)
                obj.PreviewTextInput -= TextFilterHandler;
            else
                obj.PreviewTextInput += TextFilterHandler;
        }

        static void TextFilterHandler(object sender, TextCompositionEventArgs e)
        {
            var filter = GetTextFilter((TextBox)sender);
            switch (filter)
            {
                case TextFilter.Digits:
                    e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
                    break;
                case TextFilter.Chars:
                    e.Handled = Regex.IsMatch(e.Text, "[^a-zA-Z]+");
                    break;
                case TextFilter.DigitsAndChars:
                    e.Handled = Regex.IsMatch(e.Text, "[^a-zA-Z0-9]+");
                    break;
            }
       }

        #endregion
    }
}
