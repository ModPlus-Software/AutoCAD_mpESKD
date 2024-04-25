using mpESKD.Base.Utils;

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
    // Маркер изменения
    public string LName => Language.GetItem("h203");

    /// <inheritdoc/>
    // Создание маркера изменения
    public string Description => Language.GetItem("h204");

    /// <inheritdoc/>
    // Создание интеллектуального объекта на основе анонимного блока, описывающего маркер изменения
    public string FullDescription => Language.GetItem("h205");

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