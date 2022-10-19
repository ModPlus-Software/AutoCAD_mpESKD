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
    // Создание интеллектуального объекта на основе анонимного блока, описывающего отметку уровня на плане по ГОСТ 2.307
    public string FullDescription => Language.GetItem("h175");

    /// <inheritdoc />
    public string ToolTipHelpImage => string.Empty;

    /// <inheritdoc />
    public List<string> SubFunctionsNames
    {
        get; set;
    }

    /// <inheritdoc />
    public List<string> SubFunctionsLNames
    {
        get; set;
    }

    /// <inheritdoc />
    public List<string> SubDescriptions
    {
        get; set;
    }

    /// <inheritdoc />
    public List<string> SubFullDescriptions 
    {
        get; set;
    }

    /// <inheritdoc />
    public List<string> SubHelpImages 
    {
        get; set;
    }
}