namespace mpESKD.Functions.mpConcreteJoint;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

/// <inheritdoc />
public class ConcreteJointDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(ConcreteJoint);

    /// <inheritdoc />
    public string Name => "mpConcreteJoint";

    /// <inheritdoc />
    /// Шов бетонирования
    public string LName => Language.GetItem("h191");

    /// <inheritdoc />
    /// Создание шва бетонирования
    public string Description => Language.GetItem("h192");

    /// <inheritdoc />
    /// Создание интеллектуального объекта на основе анонимного блока, описывающего шов бетонирования
    public string FullDescription => Language.GetItem("h193");

    /// <inheritdoc />
    public string ToolTipHelpImage => string.Empty;

    /// <inheritdoc />
    public List<string> SubFunctionsNames => new List<string>
    {
        "mpConcreteJointFromPolyline"
    };

    /// <inheritdoc />
    public List<string> SubFunctionsLNames => new List<string>
    {
        // Шов бетонирования из полилинии
        Language.GetItem("h194")
    };

    /// <inheritdoc />
    public List<string> SubDescriptions => new List<string>
    {
        // Конвертирование выбранной полилинии в линию обозначения шва бетонирования
        Language.GetItem("h195")
    };

    /// <inheritdoc />
    public List<string> SubFullDescriptions => new List<string>
    {
        string.Empty
    };

    /// <inheritdoc />
    public List<string> SubHelpImages => new List<string>
    {
        string.Empty
    };
}