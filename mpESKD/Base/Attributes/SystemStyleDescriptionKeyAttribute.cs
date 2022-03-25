namespace mpESKD.Base.Attributes;

using System;

/// <summary>
/// Атрибут, указывающий ключ локализации для описания базового системного стиля интеллектуального объекта
/// </summary>
public class SystemStyleDescriptionKeyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemStyleDescriptionKeyAttribute"/> class.
    /// </summary>
    /// <param name="localizationKey">Ключ локализации (указывает имя узла в файле xml локализации)</param>
    public SystemStyleDescriptionKeyAttribute(string localizationKey)
    {
        LocalizationKey = localizationKey;
    }

    /// <summary>
    /// Ключ локализации (указывает имя узла в файле xml локализации)
    /// </summary>
    public string LocalizationKey { get; }
}