namespace mpESKD.Functions.mpCrestedLeader;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

/// <inheritdoc/>
public class CrestedLeaderDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(CrestedLeader);

    /// <inheritdoc/>
    public string Name => "mpCrestedLeader";

    /// <inheritdoc/>
    // Гребенчатая выноска
    public string LName => Language.GetItem("h207");

    /// <inheritdoc/>
    // Создание гребенчатой выноски
    public string Description => Language.GetItem("h209");

    /// <inheritdoc/>
    // Создание интеллектуального объекта на основе анонимного блока, описывающего гребенчатую выноску
    public string FullDescription => Language.GetItem("h210");

    /// <inheritdoc/>
    public string ToolTipHelpImage => string.Empty;

    /// <inheritdoc/>
    public List<string> SubFunctionsNames => new List<string>();

    /// <inheritdoc/>
    public List<string> SubFunctionsLNames => new List<string>();

    /// <inheritdoc/>
    public List<string> SubDescriptions => new List<string>();

    /// <inheritdoc/>
    public List<string> SubFullDescriptions => new List<string>();

    /// <inheritdoc/>
    public List<string> SubHelpImages => new List<string>();
}