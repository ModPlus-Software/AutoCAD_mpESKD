namespace mpESKD.Base.Attributes;

using System;

/// <summary>
/// Атрибут указывает, что данное свойство интеллектуального примитива сохраняется в XData блока.
/// Свойство может быть как публичное, так и приватное. Может отображаться в палитре свойств и редакторе стилей, а может не
/// отображаться
/// <remarks>Работает со свойствами типа: int, double, bool, Enum, Point3d, List(Point3d).
/// Остальные типы желательно добавить в метод <see cref="SmartEntity.SetPropertiesValuesFromXData"/></remarks>
/// </summary>
public class SaveToXDataAttribute : Attribute
{
}