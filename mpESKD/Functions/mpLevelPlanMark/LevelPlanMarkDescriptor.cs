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
    // Обозначение уровня
    public string LName => Language.GetItem("h153"); // TODO localization

    /// <inheritdoc />
    // Создание обозначения уровня
    public string Description => Language.GetItem("h154"); // TODO localization

    /// <inheritdoc />
    // Создание интеллектуального объекта обозначения вида на основе анонимного блока
    public string FullDescription => Language.GetItem("h155");// TODO localization

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