namespace mpESKD.Base.Utils;

using System.Windows;

/// <summary>
/// UserControls utils
/// </summary>
internal static class UserControlUtils
{
    /// <summary>
    /// Set theme and language
    /// </summary>
    /// <param name="resource">Resource dictionary</param>
    public static void SetModPlusResources(this ResourceDictionary resource)
    {
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(resource);
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(resource, "LangCommon");
        ModPlusAPI.Windows.Helpers.WindowHelpers.ChangeStyleForResourceDictionary(resource);
    }
}