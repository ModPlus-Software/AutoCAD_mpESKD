using System;

namespace mpESKD.Functions.mpLevelPlanMark;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Windows;
using System.Windows.Controls;
using Base.Enums;
using Base.Overrules.Grips;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class LevelPlanMarkFrameTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMarkFrameTypeGrip"/> class.
    /// </summary>
    /// <param name="levelPlanMark">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    public LevelPlanMarkFrameTypeGrip(LevelPlanMark levelPlanMark)
    {
        LevelPlanMark = levelPlanMark;
        GripType = GripType.List;
    }

    /// <summary>
    /// Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public LevelPlanMark LevelPlanMark { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Тип рамки
        return Language.GetItem("p82"); 
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (LevelPlanMark)
        {
            _win = new ContextMenuHost();

            ContextMenu cm;

            _win.Loaded += (_, _) =>
            {
                cm = (ContextMenu)_win.FindResource("Cm");

                var menuItem = new MenuItem
                {
                    Name = FrameType.Rectangular.ToString(),
                    IsCheckable = true,
                    Header = Language.GetItem("ft2"), // Прямоугольная 
                    IsChecked = LevelPlanMark.FrameType == FrameType.Rectangular
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                menuItem = new MenuItem
                {
                    Name = FrameType.Line.ToString(),
                    IsCheckable = true,
                    Header = Language.GetItem("ft3"), // Линия
                    IsChecked = LevelPlanMark.FrameType == FrameType.Line
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                menuItem = new MenuItem
                {
                    Name = FrameType.None.ToString(),
                    IsCheckable = true,
                    Header = Language.GetItem("ft4"), // Без рамки
                    IsChecked = LevelPlanMark.FrameType == FrameType.None
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

        LevelPlanMark.FrameType = (FrameType)Enum.Parse(typeof(FrameType), menuItem.Name);
      
        LevelPlanMark.UpdateEntities();
        LevelPlanMark.BlockRecord.UpdateAnonymousBlocks();
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(LevelPlanMark.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = LevelPlanMark.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }
    }
}