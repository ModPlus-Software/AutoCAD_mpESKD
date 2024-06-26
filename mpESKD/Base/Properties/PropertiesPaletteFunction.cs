﻿namespace mpESKD.Base.Properties;

using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using ModPlusAPI;
using ModPlusAPI.Windows;

public static class PropertiesPaletteFunction
{
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
                    PaletteSet.Name = Language.GetItem("h11");
                    PaletteSet.Visible = true;
                }
                else
                {
                    PaletteSet = new PaletteSet(
                        Language.GetItem("h11"), // Свойства Smart
                        "mpPropertiesPalette",
                        new Guid("9a1a888f-8ad1-4bb5-bd51-f52529530e91"));
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
                        Language.GetItem("h11"), // Свойства Smart
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
                    PaletteSet.Name = Language.GetItem("h11");
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