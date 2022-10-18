namespace mpESKD.Functions.mpLevelPlanMark.Grips;

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
                MenuItem menuItem;
                
                foreach (var leaderType in Enum.GetValues(typeof(LeaderEndType)))
                {
                    int arrowIndex = LevelPlanMark.LeaderTypes[GripIndex];
                    var isChecked = (int)Enum.Parse(typeof(LeaderEndType),leaderType.ToString());
                    var isItemChecked = isChecked == arrowIndex;
                    
                    menuItem = new MenuItem
                    {
                        Name = leaderType.ToString(),
                        IsCheckable = true,
                        Header = leaderType.ToString(), //Language.GetItem("ft2"), // Прямоугольная 
                        IsChecked = isItemChecked
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
        var leaderTypeNum = 0;
        switch (menuItem.Name)
        {
            case "None":
                leaderTypeNum = 0;
                break;
            case "HalfArrow":
                leaderTypeNum = 1;
                break;
            case "Point":
                leaderTypeNum = 2;
                break;
            case "Resection":
                leaderTypeNum = 3;
                break;
            case "Angle":
                leaderTypeNum = 4;
                break;
            case "Arrow":
                leaderTypeNum = 5;
                break;
            case "OpenArrow":
                leaderTypeNum = 6;
                break;
            case "ClosedArrow":
                leaderTypeNum = 7;
                break;
        }

        LevelPlanMark.LeaderTypes[GripIndex] = leaderTypeNum;
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