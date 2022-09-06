namespace mpESKD.Functions.mpNodalLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Windows;
using System.Windows.Controls;
using Base.Overrules.Grips;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class NodalFrameTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodalFrameTypeGrip"/> class.
    /// </summary>
    /// <param name="nodalLeader">Экземпляр <see cref="mpNodalLeader.NodalLeader"/></param>
    public NodalFrameTypeGrip(NodalLeader nodalLeader)
    {
        NodalLeader = nodalLeader;
    }
        
    /// <summary>
    /// Экземпляр <see cref="mpNodalLeader.NodalLeader"/>
    /// </summary>
    public NodalLeader NodalLeader { get; }
        
    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("p78"); // ""; TODO изменить на "Тип рамки"
    }

    //public FrameType FrameType { get; set; } = FrameType.Round;
        
    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (NodalLeader)
        {
            _win = new ContextMenuHost(NodalLeader);
            _win.Show();

            ContextMenu cm;

            _win.Loaded += (_, _) =>
            {
                cm = (ContextMenu)_win.FindResource("Cm");

                var menuItem = new MenuItem
                {
                    Name = "Round",
                    IsCheckable = true,
                    Header = "Round", // todo <-- localization
                    IsChecked = NodalLeader.FrameType == FrameType.Round
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

                menuItem = new MenuItem
                {
                    Name = "Rectangular",
                    IsCheckable = true,
                    Header = "Rectangular", // todo <-- localization
                    IsChecked = NodalLeader.FrameType == FrameType.Rectangular
                };
                menuItem.Click += MenuItemOnClick;
                cm.Items.Add(menuItem);

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
        
        NodalLeader.FrameType = menuItem.Name == "Round" ? FrameType.Round : FrameType.Rectangular;
        
        NodalLeader.UpdateEntities();
        NodalLeader.BlockRecord.UpdateAnonymousBlocks();
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(NodalLeader.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = NodalLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }
    }
}