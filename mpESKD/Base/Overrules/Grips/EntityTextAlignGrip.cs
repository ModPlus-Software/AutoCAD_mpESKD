namespace mpESKD.Base.Overrules.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Ручка выравнивания текста
/// </summary>
public class EntityTextAlignGrip : SmartEntityGripData
{
    private ContextMenuHost _win;
    private readonly SmartEntity _smartEntity;
    private readonly Func<TextHorizontalAlignment> _getAlignFunc;
    private readonly Action<TextHorizontalAlignment> _setAlignEntity;

    /// <summary>
    /// Класс получения и задания типа выравнивания текста <see cref="EntityTextAlignGrip"/> class.
    /// </summary>
    /// <param name="smartEntity">Экземпляр <see cref="SmartEntity"/></param>
    /// <param name="getAlignFunc">Метод получения типа выравнивания</param>
    /// <param name="setAlignEntity">Метод задания типа рамки</param>
    public EntityTextAlignGrip(
        SmartEntity smartEntity,
        Func<TextHorizontalAlignment> getAlignFunc, 
        Action<TextHorizontalAlignment> setAlignEntity)
    {
        _smartEntity = smartEntity;
        _getAlignFunc = getAlignFunc;
        _setAlignEntity = setAlignEntity;
        GripType = GripType.TextAlign;
    }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Выравнивание текста
        return Language.GetItem("p73"); 
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        _win = new ContextMenuHost();

        ContextMenu cm;

        _win.Loaded += (_, _) =>
        {
            cm = (ContextMenu)_win.FindResource("Cm");
            var alignment = _getAlignFunc.Invoke();
            var menuItem = new MenuItem
            {
                Name = TextHorizontalAlignment.Left.ToString(),
                IsCheckable = true,
                Header = Language.GetItem("tha1"), // Влево 
                IsChecked = alignment == TextHorizontalAlignment.Left
            };
            menuItem.Click += MenuItemOnClick;
            cm.Items.Add(menuItem);

            menuItem = new MenuItem
            {
                Name = TextHorizontalAlignment.Center.ToString(),
                IsCheckable = true,
                Header = Language.GetItem("tha2"), // По центру
                IsChecked = alignment == TextHorizontalAlignment.Center
            };
            menuItem.Click += MenuItemOnClick;
            cm.Items.Add(menuItem);

            menuItem = new MenuItem
            {
                Name = TextHorizontalAlignment.Right.ToString(),
                IsCheckable = true,
                Header = Language.GetItem("tha3"), // Вправо
                IsChecked = alignment == TextHorizontalAlignment.Right
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

        _setAlignEntity.Invoke((TextHorizontalAlignment)Enum.Parse(typeof(TextHorizontalAlignment), menuItem.Name));
      
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