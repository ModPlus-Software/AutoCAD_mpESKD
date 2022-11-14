namespace mpESKD.Functions.mpChainLeader;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

/// <inheritdoc />
public class ChainLeaderDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(ChainLeader);

    /// <inheritdoc />
    public string Name => "mpChainLeader";

    /// <inheritdoc />
    /// Обозначение цепной выноски
    public string LName => Language.GetItem("h145"); //TODO Обозначение цепной выноски

    /// <inheritdoc />
    /// Создание цепной выноски
    public string Description => Language.GetItem("h147"); // TODO Создание цепной выноски

    /// <inheritdoc />
    // Создание интеллектуального объекта на основе анонимного блока, описывающий цепную выноску
    public string FullDescription => Language.GetItem("h148"); // TODO Создание интеллектуального объекта на основе анонимного блока, описывающий цепную выноску

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