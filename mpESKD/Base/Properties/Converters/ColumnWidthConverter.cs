namespace mpESKD.Base.Properties.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>Конвертер ширины колонки у Grid для использования в одновременном изменении
/// ширины колонок у всех UserControl в палитре</summary>
public class ColumnWidthConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !str.Equals("*"))
        {
            return new GridLength(double.Parse(str));
        }

        return new GridLength(1, GridUnitType.Star);
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength)
        {
            return gridLength.Value.ToString(CultureInfo.InvariantCulture);
        }

        return "*";
    }
}