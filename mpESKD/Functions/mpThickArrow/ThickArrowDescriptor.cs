namespace mpESKD.Functions.mpThickArrow;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;

/// <inheritdoc />
public class ThickArrowDescriptor : ISmartEntityDescriptor
{
    /// <inheritdoc/>
    public Type EntityType => typeof(ThickArrow);

    /// <inheritdoc />
    public string Name => "mpThickArrow";

    /// <inheritdoc />
    // Толстая стрелка
    public string LName => Language.GetItem("h187");

    /// <inheritdoc />
    // Создание толстой стрелки
    public string Description => Language.GetItem("h189");

    /// <inheritdoc />
    // Создание интеллектуального объекта на основе анонимного блока, описывающего толстую стрелку
    public string FullDescription => Language.GetItem("h190");

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