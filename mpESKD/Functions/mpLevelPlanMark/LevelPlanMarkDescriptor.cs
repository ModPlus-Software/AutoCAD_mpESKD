namespace mpESKD.Functions.mpLevelPlanMark;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

public class LevelPlanMarkDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(LevelPlanMark);

    /// <inheritdoc />
    public string Name => "mpLevelPlanMark";

    /// <inheritdoc />
    // Обозначение отметки уровня на плане
    public string LName => Language.GetItem("h173");

    /// <inheritdoc />
    // Создание отметки уровня на плане
    public string Description => Language.GetItem("h174");

    /// <inheritdoc />
    // Создание интеллектуального объекта отметки уровня на плане на основе анонимного блока
    public string FullDescription => Language.GetItem("h175");

    /// <inheritdoc />
    public string ToolTipHelpImage => string.Empty;

    /// <inheritdoc />
    public List<string> SubFunctionsNames => new List<string>
    {
        string.Empty,
    };

    /// <inheritdoc />
    public List<string> SubFunctionsLNames => new List<string>
    {
        string.Empty,
    };

    /// <inheritdoc />
    public List<string> SubDescriptions => new List<string>
    {
        string.Empty,
    };

    /// <inheritdoc />
    public List<string> SubFullDescriptions => new List<string>
    {
        string.Empty,
    };

    /// <inheritdoc />
    public List<string> SubHelpImages => new List<string>
    {
        string.Empty,
    };
}