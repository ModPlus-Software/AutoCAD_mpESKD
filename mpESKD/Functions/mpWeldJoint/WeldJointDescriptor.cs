namespace mpESKD.Functions.mpWeldJoint;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

/// <inheritdoc/>
public class WeldJointDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(WeldJoint);

    /// <inheritdoc/>
    public string Name => "mpWeldJoint";

    /// <inheritdoc/>
    /// Сварной шов
    public string LName => Language.GetItem("h139");

    /// <inheritdoc/>
    /// Создание обозначения шва сварных соединений по ГОСТ 21.502-2016
    public string Description => Language.GetItem("h140");

    /// <inheritdoc/>
    /// Создание интеллектуального объекта на основе анонимного блока, описывающего шов сварных соединений по ГОСТ 21.502-2016
    public string FullDescription => Language.GetItem("h141");

    /// <inheritdoc/>
    public string ToolTipHelpImage => string.Empty;

    /// <inheritdoc/>
    public List<string> SubFunctionsNames => new List<string>
    {
        "mpWeldJointFromPolyline"
    };

    /// <inheritdoc/>
    public List<string> SubFunctionsLNames => new List<string>
    {
        // Сварной шов из полилинии
        Language.GetItem("h143")
    };

    /// <inheritdoc/>
    public List<string> SubDescriptions => new List<string>
    {
        // Конвертирование выбранной полилинии в линию обозначения шва сварных соединений по ГОСТ 21.502-2016
        Language.GetItem("h144")
    };

    /// <inheritdoc/>
    public List<string> SubFullDescriptions => new List<string>
    {
        string.Empty
    };

    /// <inheritdoc/>
    public List<string> SubHelpImages => new List<string>
    {
        string.Empty
    };
}