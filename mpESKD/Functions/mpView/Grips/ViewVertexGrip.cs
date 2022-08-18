namespace mpESKD.Functions.mpView.Grips;

using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using View = mpView.View;

/// <summary>
/// Ручка вершин
/// </summary>
public class ViewVertexGrip : SmartEntityGripData
{
    private readonly List<Point3d> _points;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewVertexGrip"/> class.
    /// </summary>
    /// <param name="view">Экземпляр класса <see cref="mpView.View"/></param>
    /// <param name="index">Индекс ручки</param>
    public ViewVertexGrip(View view, int index)
    {
        View = view;
        GripIndex = index;
        GripType = GripType.Point;
            
        /* При инициализации ручки нужно собрать все точки вида и поместить их в поле _points.
         * Это создаст кэш точек вида. Если в методе WorldDraw брать точки из самого вида (View),
         * то вспомогательные линии будут меняться при зуммировании. Это связано с тем, что в методе
         * MoveGripPointsAt происходит вызов метода UpdateEntities */
        _points = new List<Point3d> { View.InsertionPoint };
        _points.Add(View.EndPoint);
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpView.View"/>
    /// </summary>
    public View View { get; }

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

    /// <inheritdoc />
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
                    var blkRef = tr.GetObject(View.BlockId, OpenMode.ForWrite, true, true);
                    using (var resBuf = View.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }

                View.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_gripTmp != null)
                {
                    if (GripIndex == 0)
                    {
                        View.InsertionPoint = _gripTmp;
                    }
                    else if (GripIndex == 1)
                    {
                        View.EndPoint = _gripTmp;
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