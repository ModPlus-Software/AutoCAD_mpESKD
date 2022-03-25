namespace mpESKD.Base.Styles;

using System;
using System.Globalization;
using System.Windows.Data;
using Autodesk.AutoCAD.DatabaseServices;
using Utils;

/// <summary>
/// Конвертер <see cref="AnnotationScale"/> в строку (имя масштаба) и обратно
/// </summary>
public class AnnotationScaleValueConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AnnotationScale annotationScale)
        {
            return annotationScale.Name;
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return AcadUtils.AnnotationScaleFromString(value?.ToString());
    }
}