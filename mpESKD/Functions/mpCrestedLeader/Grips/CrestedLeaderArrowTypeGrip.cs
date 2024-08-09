namespace mpESKD.Functions.mpCrestedLeader.Grips;

using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Overrules.Grips;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка вершин
/// </summary>
public class CrestedLeaderArrowTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderArrowTypeGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public CrestedLeaderArrowTypeGrip(CrestedLeader crestedLeader, int gripIndex)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.List;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp7"); // Тип стрелки выноски
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (CrestedLeader)
        {
            _win = new ContextMenuHost();

            ContextMenu cm;

            _win.Loaded += (_, _) =>
            {
                cm = (ContextMenu)_win.FindResource("Cm");

                foreach (var leaderType in Enum.GetValues(typeof(LeaderEndType)))
                {
                    var arrowIndex = CrestedLeader.ArrowType.ToString();
                    var checkedNumber = (int)Enum.Parse(typeof(LeaderEndType), leaderType.ToString());
                    var isItemChecked = Enum.Parse(typeof(LeaderEndType), leaderType.ToString()).ToString();
                    var headerOfItem = "let" + checkedNumber;
                    var menuItem = new MenuItem
                    {
                        Name = leaderType.ToString(),
                        IsCheckable = true,
                        Header = Language.GetItem(headerOfItem),
                        IsChecked = arrowIndex == isItemChecked
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

        CrestedLeader.ArrowType = (LeaderEndType)Enum.Parse(typeof(LeaderEndType), menuItem.Name);

        CrestedLeader.UpdateEntities();
        CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = CrestedLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }
    }
}