namespace mpESKD.Base.Properties
{
    using System;
    using System.Windows.Forms;
    using System.Windows.Forms.Integration;
    using Autodesk.AutoCAD.Runtime;
    using Autodesk.AutoCAD.Windows;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    public static class PropertiesPaletteFunction
    {
        private const string LangItem = "mpESKD";
        private static PropertiesPalette _propertiesPalette;

        public static PaletteSet PaletteSet { get; private set; }

        [CommandMethod("ModPlus", "mpPropertiesPalette", CommandFlags.Modal)]
        public static void Start()
        {
            try
            {
                if (!MainSettings.Instance.AddToMpPalette)
                {
                    MainFunction.RemoveFromMpPalette(false);
                    if (PaletteSet != null)
                    {
                        PaletteSet.Visible = true;
                    }
                    else
                    {
                        PaletteSet = new PaletteSet(
                            Language.GetItem(LangItem, "h11"), // Свойства примитивов ModPlus
                            "mpPropertiesPalette",
                            new Guid("1c0dc0f7-0d06-49df-a2d3-bcea4241e036"));
                        PaletteSet.Load += PaletteSet_Load;
                        PaletteSet.Save += PaletteSet_Save;
                        _propertiesPalette = new PropertiesPalette();
                        var elementHost = new ElementHost
                        {
                            AutoSize = true,
                            Dock = DockStyle.Fill,
                            Child = _propertiesPalette
                        };
                        PaletteSet.Add(
                            Language.GetItem(LangItem, "h11"), // Свойства примитивов ModPlus
                            elementHost);
                        PaletteSet.Style = PaletteSetStyles.ShowCloseButton | 
                                           PaletteSetStyles.ShowPropertiesMenu |
                                           PaletteSetStyles.ShowAutoHideButton;
                        PaletteSet.MinimumSize = new System.Drawing.Size(100, 300);
                        PaletteSet.DockEnabled = DockSides.Right | DockSides.Left;
                        PaletteSet.Visible = true;
                    }
                }
                else
                {
                    if (PaletteSet != null)
                    {
                        PaletteSet.Visible = false;
                    }

                    MainFunction.AddToMpPalette();
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void PaletteSet_Load(object sender, PalettePersistEventArgs e)
        {
            _ = (double)e.ConfigurationSection.ReadProperty("mpPropertiesPalette", 22.3);
        }

        private static void PaletteSet_Save(object sender, PalettePersistEventArgs e)
        {
            e.ConfigurationSection.WriteProperty("mpPropertiesPalette", 32.3);
        }
    }
}
