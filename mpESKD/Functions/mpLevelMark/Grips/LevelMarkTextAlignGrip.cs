namespace mpESKD.Functions.mpLevelMark.Grips;

using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using ModPlusAPI;
using Base.Enums;
using Base.Overrules;
using mpESKD.Base.Overrules.Grips;
using Base.Utils;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class LevelMarkTextAlignGrip : SmartEntityGripData
{
    private ContextMenuHost _win;

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelMarkTextAlignGrip"/> class.
    /// </summary>
    /// <param name="levelMark">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    public LevelMarkTextAlignGrip(LevelMark levelMark)
    {
        LevelMark = levelMark;
        GripType = GripType.TextAlign;
    }

    /// <summary>
    /// Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public LevelMark LevelMark { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // TODO Выравнивание текста по горизонтали
        return Language.GetItem("p82"); 
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (LevelMark)
        {
            _win = new ContextMenuHost();

            ContextMenu cm;

            _win.Loaded += (_, _) =>
            {
                cm = (ContextMenu)_win.FindResource("Cm");

                var menuItem = new MenuItem
                {
                    Name = TextHorizontalAlignment.Left.ToString(),
                    IsCheckable = true,
                    Header = Language.GetItem("tha1"), // Влево 
                    IsChecked = LevelMark.ValueHorizontalAlignment == TextHorizontalAlignment.Left
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                menuItem = new MenuItem
                {
                    Name = TextHorizontalAlignment.Center.ToString(),
                    IsCheckable = true,
                    Header = Language.GetItem("tha2"), // По центру
                    IsChecked = LevelMark.ValueHorizontalAlignment == TextHorizontalAlignment.Center
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                menuItem = new MenuItem
                {
                    Name = TextHorizontalAlignment.Right.ToString(),
                    IsCheckable = true,
                    Header = Language.GetItem("tha3"), // Вправо
                    IsChecked = LevelMark.ValueHorizontalAlignment == TextHorizontalAlignment.Right
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                cm.MouseMove += (_, _) => _win.Close();
                cm.Closed += (_, _) => Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
                cm.IsOpen = true;
            };
            _win.Show();
        }

        return ReturnValue.GetNewGripPoints;
    }

    private void MenuItemOnClick(object sender, RoutedEventArgs e)
    {
        _win?.Close();

        var menuItem = (MenuItem)sender;

        LevelMark.ValueHorizontalAlignment = (TextHorizontalAlignment)Enum.Parse(typeof(TextHorizontalAlignment), menuItem.Name);
      
        LevelMark.UpdateEntities();
        LevelMark.BlockRecord.UpdateAnonymousBlocks();
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(LevelMark.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = LevelMark.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }
    }
}