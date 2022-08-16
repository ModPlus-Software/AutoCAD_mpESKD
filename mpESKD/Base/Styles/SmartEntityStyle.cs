namespace mpESKD.Base.Styles;

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Xml.Linq;
using Enums;
using JetBrains.Annotations;
using ModPlusAPI.Mvvm;
using Properties;

/// <summary>
/// Стиль интеллектуального объекта
/// </summary>
public class SmartEntityStyle : ObservableObject
{
    private string _name;
    private string _description;
    private FontWeight _fontWeight;
    private bool _isCurrent;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartEntityStyle"/> class.
    /// </summary>
    /// <param name="entityType">Тип интеллектуального объекта</param>
    /// <param name="fillDefaultProperties">Заполнять ли значениями по умолчанию</param>
    public SmartEntityStyle(Type entityType, bool fillDefaultProperties = false)
    {
        EntityType = entityType;
        Properties = new ObservableCollection<SmartEntityProperty>();
        if (fillDefaultProperties)
        {
            this.FillStyleDefaultProperties(EntityType);
        }
    }

    public ObservableCollection<SmartEntityProperty> Properties { get; }

    /// <summary>
    /// Тип интеллектуального примитива, к которому относится стиль
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Имя стиля
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Описание стиля
    /// </summary>
    public string Description
    {
        get => _description;
        set
        {
            if (value == _description)
            {
                return;
            }

            _description = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Идентификатор стиля
    /// </summary>
    public string Guid { get; set; }

    /// <summary>
    /// Тип стиля (системный, пользовательский)
    /// </summary>
    public StyleType StyleType { get; set; }

    /// <summary>
    /// Xml данные слоя
    /// </summary>
    public XElement LayerXmlData { get; set; }

    /// <summary>
    /// Xml данные текстового стиля (может быть null)
    /// </summary>
    [CanBeNull]
    public XElement TextStyleXmlData { get; set; }

    /// <summary>
    /// Можно ли редактировать
    /// <remarks>Свойство для редактора стилей</remarks>
    /// </summary>
    public bool CanEdit => StyleType != StyleType.System;

    /// <summary>
    /// Толщина текста в редакторе
    /// <remarks>Свойство для редактора стилей</remarks>
    /// </summary>
    public FontWeight FontWeight
    {
        get => _fontWeight;
        set
        {
            _fontWeight = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Является ли стиль текущем
    /// <remarks>Свойство для редактора стилей</remarks>
    /// </summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            FontWeight = value ? FontWeights.SemiBold : FontWeights.Normal;
            _isCurrent = value;
            OnPropertyChanged(nameof(IsCurrent));
        }
    }
}