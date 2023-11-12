namespace mpESKD.Functions.mpThickArrow.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using ThickArrow = mpThickArrow.ThickArrow;

/// <summary>
/// Ручка вершин
/// </summary>
public class ThickArrowVertexGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThickArrowVertexGrip"/> class.
    /// </summary>
    /// <param name="thickArrow">Экземпляр класса <see cref="mpThickArrow.ThickArrow"/></param>
    /// <param name="index">Индекс ручки</param>
    public ThickArrowVertexGrip(ThickArrow thickArrow, int index)
    {
        ThickArrow = thickArrow;
        GripIndex = index;
        GripType = GripType.Point;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpThickArrow.ThickArrow"/>
    /// </summary>
    public ThickArrow ThickArrow { get;  }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp1"); // stretch
    }

    // Временное значение ручки
    private Point3d _gripTmp;

    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            // При начале перемещения запоминаем первоначальное положение ручки
            // Запоминаем начальные значения
            if (newStatus == Status.GripStart)
            {
                _gripTmp = GripPoint;
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
                if (_gripTmp != null)
                {
                    if (GripIndex == 0)
                    {
                        ThickArrow.InsertionPoint = _gripTmp;
                    }
                    else if (GripIndex == 1)
                    {
                        ThickArrow.EndPoint = _gripTmp;
                    }
                }
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