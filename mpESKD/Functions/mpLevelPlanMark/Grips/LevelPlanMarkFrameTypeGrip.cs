namespace mpESKD.Functions.mpLevelPlanMark.Grips;

using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using Base;
using ModPlusAPI;
using Base.Enums;
using Base.Overrules;
using mpESKD.Base.Overrules.Grips;
using Base.Utils;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class LevelPlanMarkFrameTypeGrip : SmartEntityGripData
{
    private ContextMenuHost _win;
    private readonly SmartEntity _smartEntity;
    private readonly Func<FrameType> _getFrameTypeFunc;
    private readonly Action<FrameType> _setFrameTypeEntity;

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMarkFrameTypeGrip"/> class.
    /// </summary>
    /// <param name="smartEntity">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="getFrameTypeFunc">Метод получения типа рамки</param>
    /// <param name="setFrameTypeEntity">Метод задания типа рамки</param>
    public LevelPlanMarkFrameTypeGrip(
        SmartEntity smartEntity,
        Func<FrameType> getFrameTypeFunc, 
        Action<FrameType> setFrameTypeEntity)
    {
        _smartEntity = smartEntity;
        _getFrameTypeFunc = getFrameTypeFunc;
        _setFrameTypeEntity = setFrameTypeEntity;
        GripType = GripType.List;
    }
    
    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Тип рамки
        return Language.GetItem("p82"); 
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        _win = new ContextMenuHost();

        ContextMenu cm;

        _win.Loaded += (_, _) =>
        {
            cm = (ContextMenu)_win.FindResource("Cm");
            var frameType = _getFrameTypeFunc.Invoke();
            var menuItem = new MenuItem
            {
                Name = FrameType.Rectangular.ToString(),
                IsCheckable = true,
                Header = Language.GetItem("ft2"), // Прямоугольная 
                IsChecked = frameType == FrameType.Rectangular
            };
            menuItem.Click += MenuItemOnClick;
            cm.Items.Add(menuItem);

            menuItem = new MenuItem
            {
                Name = FrameType.Line.ToString(),
                IsCheckable = true,
                Header = Language.GetItem("ft3"), // Линия
                IsChecked = frameType == FrameType.Line
            };
            menuItem.Click += MenuItemOnClick;
            cm.Items.Add(menuItem);

            menuItem = new MenuItem
            {
                Name = FrameType.None.ToString(),
                IsCheckable = true,
                Header = Language.GetItem("ft4"), // Без рамки
                IsChecked = frameType == FrameType.None
            };
            menuItem.Click += MenuItemOnClick;
            cm.Items.Add(menuItem);

            cm.MouseMove += (_, _) => _win.Close();
            cm.Closed += (_, _) => Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
            cm.IsOpen = true;
        };
        _win.Show();

        return ReturnValue.GetNewGripPoints;
    }

    private void MenuItemOnClick(object sender, RoutedEventArgs e)
    {
        _win?.Close();

        var menuItem = (MenuItem)sender;

        _setFrameTypeEntity.Invoke((FrameType)Enum.Parse(typeof(FrameType), menuItem.Name));
      
        _smartEntity.UpdateEntities();
        _smartEntity.BlockRecord.UpdateAnonymousBlocks();
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(_smartEntity.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = _smartEntity.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }
    }
}