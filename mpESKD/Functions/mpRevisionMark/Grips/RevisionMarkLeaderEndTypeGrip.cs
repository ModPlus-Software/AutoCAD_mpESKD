namespace mpESKD.Functions.mpRevisionMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Overrules.Grips;
using Base.Utils;
using ModPlusAPI;
using System;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Ручка вершин
/// </summary>
public class RevisionMarkLeaderEndTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkVertexGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public RevisionMarkLeaderEndTypeGrip(RevisionMark revisionMark, int gripIndex)
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
        return Language.GetItem("gp7"); // Тип рамки ревизии для выноски
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

                var menuItem = new MenuItem
                {
                    Name = "Round",
                    IsCheckable = true,
                    Header = Language.GetItem("ft1"), // Круглая 
                    // todo Тест
                    //IsChecked = RevisionMark.FrameType == FrameType.Round
                    IsChecked = 
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                menuItem = new MenuItem
                {
                    Name = "Rectangular",
                    IsCheckable = true,
                    Header = Language.GetItem("ft2"), // Прямоугольная 
                    IsChecked = RevisionMark.FrameType == FrameType.Rectangular
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);
                cm.MouseMove += (_, _) => _win.Close();
                cm.Closed += (_, _) => Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();

                cm.IsOpen = true;
            };
        }

        return ReturnValue.GetNewGripPoints;
    }

    private void MenuItemOnClick(object sender, RoutedEventArgs e)
    {
        _win?.Close();

        var menuItem = (MenuItem)sender;

        RevisionMark.LeaderTypes[GripIndex] = (int)Enum.Parse(typeof(LeaderEndType), menuItem.Name);

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