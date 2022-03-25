namespace mpESKD.Base.Attributes;

using System;
using System.Text.RegularExpressions;

/// <summary>
/// Атрибут, задающий ограничение на ввод для текстовых свойств в палитре и окне настройки стилей.
/// Атрибут должен применяться только для строковых свойств интеллектуальных объектов.
/// Если атрибут задан, то вводимое значение в текстовое поле будет проверяться на совпадение через <see cref="Regex"/>,
/// используя паттерн из свойства <see cref="Pattern"/>
/// </summary>
public class RegexInputRestrictionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegexInputRestrictionAttribute"/> class.
    /// </summary>
    /// <param name="pattern">Паттерн для <see cref="Regex"/></param>
    public RegexInputRestrictionAttribute(string pattern)
    {
        Pattern = pattern;
    }
        
    /// <summary>
    /// Паттерн для <see cref="Regex"/>
    /// </summary>
    public string Pattern { get; }
}