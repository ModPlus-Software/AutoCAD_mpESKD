using System;
using System.Linq;

namespace mpESKD.Functions.mpLevelPlanMark.Grips;

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

    private LeaderEndType leaderType;

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
                MenuItem menuItem;

                var leaderTypes = Enum.GetValues(typeof(LeaderType)).Cast<mpLevelPlanMark.LeaderEndType>();

                foreach (var leaderType in Enum.GetValues(typeof(LeaderEndType)))
                {
                    
                    menuItem = new MenuItem
                    {
                        Name = leaderType.ToString(),
                        IsCheckable = true,
                        Header = leaderType.ToString(), //Language.GetItem("ft2"), // Прямоугольная 
                        IsChecked = LevelPlanMark.LeaderEndType == (LeaderEndType) leaderType
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

        switch (menuItem.Name)
        {
            case "HalfArrow":
                LevelPlanMark.LeaderEndType = LeaderEndType.HalfArrow;
                break;
            case "Point":
                LevelPlanMark.LeaderEndType = LeaderEndType.Point;
                break;
            case "Resection":
                LevelPlanMark.LeaderEndType = LeaderEndType.Resection;
                break;
            case "Angle":
                LevelPlanMark.LeaderEndType = LeaderEndType.Angle;
                break;
            case "Arrow":
                LevelPlanMark.LeaderEndType = LeaderEndType.Arrow;
                break;
            case "OpenArrow":
                LevelPlanMark.LeaderEndType = LeaderEndType.OpenArrow;
                break;
            case "ClosedArrow":
                LevelPlanMark.LeaderEndType = LeaderEndType.ClosedArrow;
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