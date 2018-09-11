﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using static System.Diagnostics.Debug;


namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    internal class InvertableBooleanToVisibilityConverter : IValueConverter
    {
        private enum Parameters
        {
            Normal, Inverted
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            Assert(parameter != null, "you need to specify Normal or Inverted to your InvertableBooleanToVisibilityConverter!");
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            bool boolVal = (bool)value;
            var direction = (Parameters)Enum.Parse(typeof(Parameters), (string)parameter);

            if (direction == Parameters.Normal)
            {
                return boolVal ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return boolVal ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    internal class MultiValueBooleansToVisibilityConverter_OR : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool res = false;
            foreach (object v in values)
            {
                if (v is bool b)
                {
                    res = res || b;
                }
            }
            return res ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack should NOT be invoked");
        }
    }

    internal class MultiValueBooleansToVisibilityConverter_AND : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool res = true;
            foreach (object v in values)
            {
                if (v is bool b)
                {
                    res = res && b;
                }
            }
            return res ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack should NOT be invoked");
        }
    }

    internal sealed class MultiValueBoolToBool_And : IMultiValueConverter
    {
        private static readonly object s_false = false;
        private static readonly object s_true = true;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (object v in values)
            {
                // Any false, the result is false
                if (s_false.Equals(v))
                {
                    return s_false;
                }
            }

            return s_true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack should NOT be invoked");
        }
    }

    // Returns Visibility.Visible if the string is not null or empy, otherwise  Visibility.Collapsed;
    internal class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack should NOT be invoked");
        }
    }

    // Returns true if null or empty, otherwise false;
    internal class NullToTrueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? true : false;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack should NOT be invoked");
        }
    }

    internal class NullToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool status = true;
            if (value == null)
            {
                status = false;
            }

            if (value is string s)
            {
                status = string.IsNullOrWhiteSpace(s) ? false : true;
            }

            return status;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack should NOT be invoked");
        }
    }

    // Enum boolean converter
    internal class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string parameterString))
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string parameterString) || value.Equals(false))
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
    }

    /// <summary>
    /// Samve a BoolToVisiabilityConverter except instead of false= collapse, false = Hidden
    /// </summary>
    internal class BoolToVisibleOrHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is bool))
            {
                return Visibility.Hidden;
            }

            return ((bool)value) ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack should NOT be invoked");
        }
    }

    // Just like it says. Converts !Bool to true.
    internal class NotBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            throw new ArgumentException("The target must be a bool");
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }

    }
    /// Converts null to Collapsed and non-null to Visible
    internal class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// Converts null to false, non-null to true
    internal class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null) ? false : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value + double.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    internal class TextBlockFormatToHyperlinkConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 3)
            {
                var textBlock = values[0] as TextBlock;
                string format = values[1] as string;
                List<Token> tokens = Tokenizer.ParseTokens(format);
                int hyperLinkIndex = 0;
                for (int i = 2; i < values.Length; i++)
                {
                    Token token = tokens.FirstOrDefault((p) => string.Equals(p.Value as string, "{" + hyperLinkIndex + "}"));
                    if (token != null)
                    {
                        token.Value = values[i];
                    }

                    hyperLinkIndex++;
                }

                textBlock.Inlines.Clear();
                foreach (Token token in tokens)
                {
                    if (token.Value is Hyperlink hyperlink)
                    {
                        textBlock.Inlines.Add(hyperlink);
                    }
                    else
                    {
                        textBlock.Inlines.Add(new Run(token.Value as string));
                    }
                }

                return textBlock;
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private class Token
        {
            public object Value { get; set; }
        }

        private static class Tokenizer
        {
            public static List<Token> ParseTokens(string format)
            {
                var tokens = new List<Token>();
                string[] strings = Regex.Split(format, @"({\d+})");
                foreach (string str in strings)
                {
                    tokens.Add(new Token() { Value = str });
                }
                return tokens;
            }
        }
    }
}
