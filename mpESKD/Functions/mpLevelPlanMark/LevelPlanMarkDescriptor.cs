namespace mpESKD.Functions.mpLevelPlanMark;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

/// <inheritdoc/>
public class LevelPlanMarkDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(LevelPlanMark);

    /// <inheritdoc />
    public string Name => "mpLevelPlanMark";

    /// <inheritdoc />
    // Отметка уровня на плане
    public string LName => Language.GetItem("h171");

    /// <inheritdoc />
    // Создание отметки уровня на плане по ГОСТ 2.307
    public string Description => Language.GetItem("h173");

    /// <inheritdoc />
    // Создание интеллектуального объекта на основе анонимного блока, описывающего отметку уровня на плане по ГОСТ 2.307
    public string FullDescription => Language.GetItem("h174");

    /// <inheritdoc />
    public string ToolTipHelpImage => string.Empty;

    /// <inheritdoc />
    public List<string> SubFunctionsNames => new ();

    /// <inheritdoc />
    public List<string> SubFunctionsLNames => new ();

    /// <inheritdoc />
    public List<string> SubDescriptions => new ();

    /// <inheritdoc />
    public List<string> SubFullDescriptions => new ();

    /// <inheritdoc />
    public List<string> SubHelpImages => new ();
}