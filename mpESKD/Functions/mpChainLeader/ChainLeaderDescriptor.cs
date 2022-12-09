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
    /// Цепная выноска
    public string LName => Language.GetItem("h175");

    /// <inheritdoc />
    /// Создание цепной выноске по ГОСТ 2.307
    public string Description => Language.GetItem("h177");
    /// <inheritdoc />
    /// Создание интеллектуального объекта на основе анонимного блока, описывающий цепную выноску по ГОСТ 2.307
    public string FullDescription => Language.GetItem("h178");

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