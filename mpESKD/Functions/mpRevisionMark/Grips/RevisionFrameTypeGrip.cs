namespace mpESKD.Functions.mpRevisionMark.Grips;

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
public class RevisionFrameTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionFrameTypeGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр <see cref="mpRevisionMark.RevisionMark"/></param>
    public RevisionFrameTypeGrip(RevisionMark revisionMark)
    {
        RevisionMark = revisionMark;
        GripType = GripType.List;
    }

    /// <summary>
    /// Экземпляр <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Тип рамки
        return Language.GetItem("p82");
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
                    IsChecked = RevisionMark.FrameType == FrameType.Round
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
            _win.Show();
        }

        return ReturnValue.GetNewGripPoints;
    }

    private void MenuItemOnClick(object sender, RoutedEventArgs e)
    {
        _win?.Close();

        var menuItem = (MenuItem)sender;

        RevisionMark.FrameType = menuItem.Name == "Round" ? FrameType.Round : FrameType.Rectangular;

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