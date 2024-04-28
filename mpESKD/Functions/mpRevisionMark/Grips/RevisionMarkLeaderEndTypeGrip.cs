using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpRevisionMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Overrules.Grips;
using Base.Utils;
using ModPlusAPI;
using mpESKD.Functions.mpLevelPlanMark;
using System;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Ручка вершин
/// </summary>
public class RevisionMarkFrameTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkFrameTypeGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public RevisionMarkFrameTypeGrip(RevisionMark revisionMark, int gripIndex)
    {
        RevisionMark = revisionMark;
        GripIndex = gripIndex;
        GripType = GripType.List;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("p82"); // todo Тип рамки ревизии для выноски
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (RevisionMark)
        {
            _win = new ContextMenuHost();

            ContextMenu cm;

            _win.Loaded += (_, _) =>
            {
                cm = (ContextMenu)_win.FindResource("Cm");

                var frameIndex = RevisionMark.RevisionFrameTypes[GripIndex];
                
                {
                    var menuItem = new MenuItem
                    {
                        Name = "None",
                        IsCheckable = true,
                        Header = Language.GetItem("ft4"), // Нет 
                        IsChecked = frameIndex == (int)RevisionFrameType.None,
                    };
                    menuItem.Click += MenuItemOnClick;
                    cm.Items.Add(menuItem);
                }

                {
                    var menuItem = new MenuItem
                    {
                        Name = "Round",
                        IsCheckable = true,
                        Header = Language.GetItem("ft1"), // Круглая 
                        IsChecked = frameIndex == (int)RevisionFrameType.Round,
                    };
                    menuItem.Click += MenuItemOnClick;
                    cm.Items.Add(menuItem);
                }

                {
                    var menuItem = new MenuItem
                    {
                        Name = "Rectangular",
                        IsCheckable = true,
                        Header = Language.GetItem("ft2"), // Прямоугольная 
                        IsChecked = frameIndex == (int)RevisionFrameType.Rectangular,
                    };
                    menuItem.Click += MenuItemOnClick;
                    cm.Items.Add(menuItem);
                }

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

        // RevisionMark.LeaderTypes[GripIndex] = (int)Enum.Parse(typeof(LeaderEndType), menuItem.Name);

        var selectedItemNumber = (int)Enum.Parse(typeof(RevisionFrameType), menuItem.Name);

        RevisionMark.RevisionFrameTypes[GripIndex] = selectedItemNumber;

        if (selectedItemNumber != 0)
        {
            if (RevisionMark.RevisionFrameStretchPoints[GripIndex]
                .Equals(RevisionMark.LeaderPoints[GripIndex]))
            {
                RevisionMark.RevisionFrameStretchPoints[GripIndex] =
                    RevisionMark.LeaderPoints[GripIndex] + Vector3d.XAxis * 5 * RevisionMark.GetFullScale();
            }
        }

        RevisionMark.UpdateEntities();
        RevisionMark.BlockRecord.UpdateAnonymousBlocks();
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(RevisionMark.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = RevisionMark.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }
    }
}