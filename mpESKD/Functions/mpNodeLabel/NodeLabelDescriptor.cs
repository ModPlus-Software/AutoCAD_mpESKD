namespace mpESKD.Functions.mpNodeLabel;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

 public class NodeLabelDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(NodeLabel);

    /// <inheritdoc/>
    public string Name => "mpNodeLabel";

    /// <inheritdoc/>
    // Обозначение узла
    public string LName => Language.GetItem("h197");

    /// <inheritdoc/>
    // Создание обозначения узла
    public string Description => Language.GetItem("h198");

    /// <inheritdoc/>
    // Создание интеллектуального объекта на основе анонимного блока, описывающего обозначение узла
    public string FullDescription => Language.GetItem("h199");

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