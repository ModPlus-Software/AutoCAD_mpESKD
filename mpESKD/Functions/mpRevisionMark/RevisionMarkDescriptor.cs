namespace mpESKD.Functions.mpRevisionMark;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

/// <inheritdoc/>
public class RevisionMarkDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(RevisionMark);

    /// <inheritdoc/>
    public string Name => "mpRevisionMark";

    /// <inheritdoc/>
    // Узловая выноска
    public string LName => Language.GetItem("h126");

    /// <inheritdoc/>
    // Создание узловой выноски
    public string Description => Language.GetItem("h127");

    /// <inheritdoc/>
    // Создание интеллектуального объекта на основе анонимного блока, описывающего узловую выноску
    public string FullDescription => Language.GetItem("h128");

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