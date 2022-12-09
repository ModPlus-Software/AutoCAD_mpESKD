namespace mpESKD.Functions.mpChainLeader.Grips;

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
public class ChainLeaderArrowEndTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public ChainLeaderArrowEndTypeGrip(ChainLeader chainLeader, int gripIndex)
    {
        ChainLeader = chainLeader;
        GripIndex = gripIndex;
        GripType = GripType.List;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

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
        using (ChainLeader)
        {
            _win = new ContextMenuHost();

            ContextMenu cm;

            _win.Loaded += (_, _) =>
            {
                cm = (ContextMenu)_win.FindResource("Cm");

                foreach (var leaderType in Enum.GetValues(typeof(LeaderEndType)))
                {
                    var arrowIndex = ChainLeader.ArrowType.ToString();
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

        ChainLeader.ArrowType = (LeaderEndType)Enum.Parse(typeof(LeaderEndType), menuItem.Name);

        ChainLeader.UpdateEntities();
        ChainLeader.BlockRecord.UpdateAnonymousBlocks();
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(ChainLeader.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = ChainLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }
    }
}