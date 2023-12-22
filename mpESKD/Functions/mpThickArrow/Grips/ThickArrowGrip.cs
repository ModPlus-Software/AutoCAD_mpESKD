namespace mpESKD.Functions.mpThickArrow.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;

/// <summary>
/// Ручка вершин
/// </summary>
public class ThickArrowGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThickArrowGrip"/> class.
    /// </summary>
    /// <param name="thickArrow">Экземпляр класса <see cref="mpThickArrow.ThickArrow"/></param>
    /// <param name="gripName">Название ручки из <see cref="GripName"/></param>
    public ThickArrowGrip(ThickArrow thickArrow, GripName gripName)
    {
        ThickArrow = thickArrow;
        GripType = GripType.Point;
        GripName = gripName;    
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpThickArrow.ThickArrow"/>
    /// </summary>
    public ThickArrow ThickArrow { get; }

    /// <summary>
    /// Имя ручки
    /// </summary>
    public GripName GripName { get;  }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        switch (GripName)
        {
            case GripName.StartGrip:
            case GripName.EndGrip:
                {
                    return Language.GetItem("gp1"); // stretch
                }

            case GripName.MiddleGrip: return Language.GetItem("gp2"); // move
        }

        return base.GetTooltip();
    }

    // Временное значение первой ручки
    private Point3d _startGripTmp;

    // временное значение последней ручки
    private Point3d _endGripTmp;

    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            // При начале перемещения запоминаем первоначальное положение ручки
            // Запоминаем начальные значения
            if (newStatus == Status.GripStart)
            {
                _startGripTmp = ThickArrow.InsertionPoint;
                _endGripTmp = ThickArrow.EndPoint;
            }

            // При удачном перемещении ручки записываем новые значения в расширенные данные
            // По этим данным я потом получаю экземпляр класса section
            if (newStatus == Status.GripEnd)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(ThickArrow.BlockId, OpenMode.ForWrite, true, true);
                    using (var resBuf = ThickArrow.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }

                ThickArrow.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                ThickArrow.InsertionPoint = _startGripTmp;
                ThickArrow.EndPoint = _endGripTmp;
            }

            base.OnGripStatusChanged(entityId, newStatus);
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }
}