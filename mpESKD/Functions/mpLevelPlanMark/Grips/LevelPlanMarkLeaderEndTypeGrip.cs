﻿namespace mpESKD.Functions.mpLevelPlanMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Windows;
using System.Windows.Controls;
using Base.Enums;
using Base.Overrules.Grips;

/// <summary>
/// Ручка вершин
/// </summary>
public class LevelPlanMarkLeaderEndTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;
    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public LevelPlanMark LevelPlanMark { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }
    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // TODO type
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMarkVertexGrip"/> class.
    /// </summary>
    /// <param name="levelPlanMark">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public LevelPlanMarkLeaderEndTypeGrip(LevelPlanMark levelPlanMark, int gripIndex)
    {
        LevelPlanMark = levelPlanMark;
        GripIndex = gripIndex;
        GripType = GripType.List;
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
                //TODO
                var menuItem = new MenuItem
                {
                    Name = "Rectangular",
                    IsCheckable = true,
                    Header = Language.GetItem("ft2"), // Прямоугольная 
                    IsChecked = LevelPlanMark.FrameType == FrameType.Rectangular
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                menuItem = new MenuItem
                {
                    Name = "Line",
                    IsCheckable = true,
                    Header = Language.GetItem("ft3"), // Линия
                    IsChecked = LevelPlanMark.FrameType == FrameType.Line
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                menuItem = new MenuItem
                {
                    Name = "None",
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

        switch (menuItem.Name)
        {
            case "Rectangular":
                LevelPlanMark.FrameType = FrameType.Rectangular;
                break;
            case "Line":
                LevelPlanMark.FrameType = FrameType.Line;
                break;
            case "None":
                LevelPlanMark.FrameType = FrameType.None;
                break;
        }

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