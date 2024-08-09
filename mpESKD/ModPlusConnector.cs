namespace mpESKD;

using System;
using System.Collections.Generic;
using ModPlusAPI.Abstractions;
using ModPlusAPI.Enums;

/// <inheritdoc/>
public class ModPlusConnector : IModPlusPlugin
{
    private static ModPlusConnector _instance;

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static ModPlusConnector Instance => _instance ??= new ModPlusConnector();

    /// <inheritdoc/>
    public SupportedProduct SupportedProduct => SupportedProduct.AutoCAD;

    /// <inheritdoc/>
    public string Name => nameof(mpESKD);

    /// <inheritdoc/>
    public string AvailProductExternalVersion => ModPlus.VersionData.CurrentCadVersion;

    /// <inheritdoc/>
    public string FullClassName => string.Empty;

    /// <inheritdoc/>
    public string AppFullClassName => string.Empty;

    /// <inheritdoc/>
    public Guid AddInId => Guid.Empty;

    /// <inheritdoc/>
    public string Price => "0";

    /// <inheritdoc/>
    public bool CanAddToRibbon => false;

    /// <inheritdoc/>
    public string ToolTipHelpImage => string.Empty;

    /// <inheritdoc/>
    public List<string> SubPluginsNames => new ();

    /// <inheritdoc/>
    public List<string> SubHelpImages => new ();

    /// <inheritdoc/>
    public List<string> SubClassNames => new ();
}