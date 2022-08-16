namespace mpESKD.Base.View;

using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Присоединяемые свойства для <see cref="TextBox"/>
/// </summary>
public class TextBoxAttachedProperties
{
    /// <summary>
    /// Паттерн, ограничивающий ввод значения в <see cref="TextBox"/> с использованием Regex
    /// </summary>
    public static readonly DependencyProperty InputRestrictionRegexPatternProperty = DependencyProperty.RegisterAttached(
        "InputRestrictionRegexPattern", typeof(string), typeof(TextBoxAttachedProperties), new PropertyMetadata(default(string)));

    /// <summary>
    /// Set <see cref="InputRestrictionRegexPatternProperty"/>
    /// </summary>
    /// <param name="element"><see cref="DependencyObject"/></param>
    /// <param name="value">Value</param>
    [AttachedPropertyBrowsableForType(typeof(TextBox))]
    public static void SetInputRestrictionRegexPattern(DependencyObject element, string value)
    {
        element.SetValue(InputRestrictionRegexPatternProperty, value);
    }

    /// <summary>
    /// Get <see cref="InputRestrictionRegexPatternProperty"/>
    /// </summary>
    /// <param name="element"><see cref="DependencyObject"/></param>
    [AttachedPropertyBrowsableForType(typeof(TextBox))]
    public static string GetInputRestrictionRegexPattern(DependencyObject element)
    {
        return (string)element.GetValue(InputRestrictionRegexPatternProperty);
    }
}