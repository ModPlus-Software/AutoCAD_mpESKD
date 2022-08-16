namespace mpESKD.Base.Attributes;

using System;

/// <summary>
/// Атрибут, указывающий зависимость видимости свойства от другого свойства.
/// Принцип работы: в классе примитива нужно создать специальное свойство типа bool к которому
/// нужно указать данный атрибут. 
/// В список зависимых свойств VisibleDependencyProperties нужно внести имена тех свойств, видимость которых
/// зависит от свойства, для которого установлен атрибут.
/// В список зависимых свойств HiddenDependencyProperties нужно внести имена тех свойств, видимость которых
/// зависит инвертировано (т.е. для true - скрыть, для false - показать) от свойства, для которого установлен атрибут.
/// Самой свойство, для которого установлен атрибут, должно менять внутри класса примитива
/// </summary>
public class PropertyVisibilityDependencyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyVisibilityDependencyAttribute"/> class.
    /// </summary>
    /// <param name="visibleDependencyProperties">
    /// Свойства, видимость которых зависит от свойства, для которого установлен атрибут</param>
    /// <param name="hiddenDependencyProperties">
    /// Свойства, видимость которых инвертировано зависит от свойства, для которого установлен атрибут</param>
    public PropertyVisibilityDependencyAttribute(
        string[] visibleDependencyProperties, 
        string[] hiddenDependencyProperties = null)
    {
        VisibleDependencyProperties = visibleDependencyProperties;
        HiddenDependencyProperties = hiddenDependencyProperties;
    }

    /// <summary>
    /// Свойства, видимость которых зависит от свойства, для которого установлен атрибут
    /// </summary>
    public string[] VisibleDependencyProperties { get; }

    /// <summary>
    /// Свойства, видимость которых инвертировано зависит от свойства, для которого установлен атрибут
    /// </summary>
    public string[] HiddenDependencyProperties { get; }
}