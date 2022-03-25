namespace mpESKD.Base.Properties;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using Autodesk.AutoCAD.DatabaseServices;
using Enums;
using ModPlusAPI.Mvvm;
using Utils;

/// <summary>
/// Суммарное свойство
/// </summary>
public class SummaryProperty : ObservableObject
{
    private bool _isDifferentOrUndefined;

    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryProperty"/> class.
    /// </summary>
    /// <param name="entityType">Тип интеллектуального объекта</param>
    public SummaryProperty(Type entityType)
    {
        EntityType = entityType;
        EntityPropertyDataCollection = new ObservableCollection<SmartEntityProperty>();
    }

    /// <summary>
    /// Добавление свойства <see cref="SmartEntityProperty"/> в коллекцию
    /// </summary>
    /// <param name="property">Свойство <see cref="SmartEntityProperty"/></param>
    public void AddProperty(SmartEntityProperty property)
    {
        property.PropertyChanged += Property_PropertyChanged;
        EntityPropertyDataCollection.Add(property);
    }

    private void Property_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SummaryValue));
        OnPropertyChanged(nameof(DoubleValue));
        OnPropertyChanged(nameof(IntValue));
    }

    /// <summary>
    /// Тип интеллектуального объекта
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Коллекция исходных свойств
    /// </summary>
    public ObservableCollection<SmartEntityProperty> EntityPropertyDataCollection { get; }

    /// <summary>
    /// Категория свойства
    /// </summary>
    public PropertiesCategory Category
    {
        get
        {
            var p = EntityPropertyDataCollection.FirstOrDefault();
            if (p != null)
            {
                return p.Category;
            }

            return PropertiesCategory.Undefined;
        }
    }

    /// <summary>
    /// Порядок свойства в палитре
    /// </summary>
    public int OrderIndex
    {
        get
        {
            var property = EntityPropertyDataCollection.FirstOrDefault();
            if (property != null)
            {
                return property.OrderIndex;
            }

            return 0;
        }
    }

    /// <summary>
    /// Имя свойства
    /// </summary>
    public string PropertyName => EntityPropertyDataCollection.FirstOrDefault()?.Name;

    /// <summary>
    /// Ключ локализации для отображаемого имени
    /// </summary>
    public string DisplayNameLocalizationKey => EntityPropertyDataCollection.FirstOrDefault()?.DisplayNameLocalizationKey;

    /// <summary>
    /// Ключ локализации для отображаемого описания свойства
    /// </summary>
    public string DescriptionLocalizationKey => EntityPropertyDataCollection.FirstOrDefault()?.DescriptionLocalizationKey;

    /// <summary>
    /// Область видимости свойства
    /// </summary>
    public PropertyScope? PropertyScope => EntityPropertyDataCollection.FirstOrDefault()?.PropertyScope;

    /// <summary>
    /// Имеет ли суммарное значение значение "РАЗЛИЧНЫЕ" или "НЕ ОПРЕДЕЛЕНО"
    /// </summary>
    public bool IsDifferentOrUndefined
    {
        get => _isDifferentOrUndefined;
        set
        {
            if (_isDifferentOrUndefined == value)
                return;
            _isDifferentOrUndefined = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Foreground));
        }
    }

    /// <summary>
    /// Цвет текста. Меняется в зависимости от значения <see cref="IsDifferentOrUndefined"/>
    /// </summary>
    public SolidColorBrush Foreground
    {
        get
        {
            if (IsDifferentOrUndefined)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#707070");
            return Brushes.Black;
        }
    }

    /// <summary>
    /// Суммарное значение
    /// </summary>
    public object SummaryValue
    {
        get
        {
            IsDifferentOrUndefined = false;
            var different = $"*{ModPlusAPI.Language.GetItem("vc1")}*";
            var undefined = $"*{ModPlusAPI.Language.GetItem("vc2")}*";
            var values = EntityPropertyDataCollection.Select(e => e.Value).ToList();
            var value = values.FirstOrDefault();
            if (value != null)
            {
                var valueType = value.GetType();

                if (valueType == typeof(AnnotationScale))
                {
                    var scales = values.Select(v => ((AnnotationScale)v).Name).ToList();
                    if (scales.Distinct().Count() > 1)
                    {
                        IsDifferentOrUndefined = true;
                        return different;
                    }

                    return scales.First();
                }

                if (valueType == typeof(string))
                {
                    if (values.Cast<string>().Distinct().Count() > 1)
                    {
                        IsDifferentOrUndefined = true;
                        return different;
                    }
                }

                if (valueType == typeof(bool))
                {
                    if (values.Cast<bool>().Distinct().Count() > 1)
                    {
                        IsDifferentOrUndefined = true;
                        return null;
                    }
                }

                if (value is Enum)
                {
                    if (values.Distinct().Count() > 1)
                    {
                        IsDifferentOrUndefined = true;
                        return different;
                    }
                }

                return value;
            }

            IsDifferentOrUndefined = true;
            return undefined;
        }

        set
        {
            foreach (var property in EntityPropertyDataCollection)
            {
                if (property.Value.GetType() == typeof(AnnotationScale))
                {
                    property.Value = AcadUtils.GetAnnotationScaleByName(value.ToString());
                }
                else if (property.Value is Enum)
                {
                    var converter = new EnumPropertyValueConverter();
                    property.Value = converter.ConvertBack(value, property.Value.GetType());
                }
                else 
                {
                    property.Value = value;
                }
            }

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Подробнее смотри в описании метода
    /// <see cref="PropertiesPalette.CreateTwoWayBindingForPropertyForNumericValue"/>
    /// </summary>
    public double? DoubleValue
    {
        get
        {
            var values = EntityPropertyDataCollection.Select(e => e.DoubleValue).ToList();
            if (values.Distinct(new DoubleEqComparer()).Count() > 1)
            {
                IsDifferentOrUndefined = true;
                return null;
            }

            return values.FirstOrDefault();
        }

        set
        {
            if (!value.HasValue)
                return;
            var minimum = EntityPropertyDataCollection.Select(p => Convert.ToDouble(p.Minimum)).Max();
            var maximum = EntityPropertyDataCollection.Select(p => Convert.ToDouble(p.Maximum)).Min();
            var valueToSet = value.Value;
            if (value.Value < minimum)
                valueToSet = minimum;
            if (valueToSet > maximum)
                valueToSet = maximum;

            foreach (var property in EntityPropertyDataCollection)
            {
                property.DoubleValue = valueToSet;
            }

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Подробнее смотри в описании метода
    /// <see cref="PropertiesPalette.CreateTwoWayBindingForPropertyForNumericValue"/>
    /// </summary>
    public int? IntValue
    {
        get
        {
            var values = EntityPropertyDataCollection.Select(e => e.IntValue).ToList();
            if (values.Distinct().Count() > 1)
            {
                IsDifferentOrUndefined = true;
                return null;
            }

            return values.FirstOrDefault();
        }

        set
        {
            if (!value.HasValue)
                return;
            var minimum = EntityPropertyDataCollection.Select(p => Convert.ToInt32(p.Minimum)).Max();
            var maximum = EntityPropertyDataCollection.Select(p => Convert.ToInt32(p.Maximum)).Min();
            var valueToSet = value.Value;
            if (value.Value < minimum)
                valueToSet = minimum;
            if (valueToSet > maximum)
                valueToSet = maximum;

            foreach (var property in EntityPropertyDataCollection)
            {
                property.IntValue = valueToSet;
            }

            OnPropertyChanged();
        }
    }
}