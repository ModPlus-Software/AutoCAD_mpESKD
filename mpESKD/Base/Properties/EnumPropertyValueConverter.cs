namespace mpESKD.Base.Properties;

using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using Attributes;

/// <summary>
/// Универсальный конвертер для значений свойств, представленных перечислителем (Enum).
/// Для правильной работы перечислители должны иметь у каждого поля атрибут <see cref="EnumPropertyDisplayValueKeyAttribute"/>
/// </summary>
public class EnumPropertyValueConverter : IValueConverter
{
    private Type _enumType;

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum e)
        {
            _enumType = e.GetType();
            var fieldInfo = _enumType.GetField(Enum.GetName(_enumType, e));
            var attr = fieldInfo.GetCustomAttribute<EnumPropertyDisplayValueKeyAttribute>();
            if (attr != null)
            {
                return ModPlusAPI.Language.GetItem(attr.LocalizationKey);
            }
        }

        return value;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && _enumType != null)
        {
            var filedsInfo = _enumType.GetFields();
            foreach (FieldInfo fieldInfo in filedsInfo)
            {
                var attr = fieldInfo.GetCustomAttribute<EnumPropertyDisplayValueKeyAttribute>();
                if (attr != null &&
                    ModPlusAPI.Language.GetItem(attr.LocalizationKey) == s)
                {
                    return Enum.Parse(_enumType, fieldInfo.Name);
                }
            }
        }

        return value;
    }

    public object ConvertBack(object value, Type enumType)
    {
        _enumType = enumType;
        return ConvertBack(value, null, null, null);
    }
}