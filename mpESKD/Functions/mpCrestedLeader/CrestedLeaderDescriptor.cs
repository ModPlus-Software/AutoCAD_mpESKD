namespace mpESKD.Functions.mpCrestedLeader;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

/// <inheritdoc />
public class CrestedLeaderDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(CrestedLeader);

    /// <inheritdoc />
    public string Name => "mpCrestedLeader";

    /// <inheritdoc />
    /// Гребенчатая выноска
    public string LName => Language.GetItem("h183");

    /// <inheritdoc />
    /// Создание гребенчатой выноски по ГОСТ 2.307
    public string Description => Language.GetItem("h185");
    
    /// <inheritdoc />
    /// Создание интеллектуального объекта на основе анонимного блока, описывающий цепную выноску по ГОСТ 2.307
    public string FullDescription => Language.GetItem("h186");

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