﻿namespace mpESKD.Functions.mpNodalLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;

/// <summary>
/// Обычная ручка узловой выноски
/// </summary>
public class NodalLeaderGrip : SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTemp;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodalLeaderGrip"/> class.
    /// </summary>
    /// <param name="nodalLeader">Экземпляр <see cref="mpNodalLeader.NodalLeader"/></param>
    /// <param name="gripName">Имя ручки</param>
    public NodalLeaderGrip(
        NodalLeader nodalLeader,
        GripName gripName)
    {
        NodalLeader = nodalLeader;
        GripName = gripName;
        GripType = GripType.Point;
    }

    /// <summary>
    /// Экземпляр <see cref="mpNodalLeader.NodalLeader"/>
    /// </summary>
    public NodalLeader NodalLeader { get; }

    /// <summary>
    /// Имя ручки
    /// </summary>
    public GripName GripName { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        switch (GripName)
        {
            case GripName.LeaderPoint:
            case GripName.FramePoint:
                {
                    return Language.GetItem("gp1"); // stretch
                }

            case GripName.InsertionPoint: return Language.GetItem("gp2"); // move
        }

        return base.GetTooltip();
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            // При начале перемещения запоминаем первоначальное положение ручки
            // Запоминаем начальные значения
            if (newStatus == Status.GripStart)
            {
                _gripTemp = GripPoint;
            }

            // При удачном перемещении ручки записываем новые значения в расширенные данные
            // По этим данным я потом получаю экземпляр класса LevelMark
            if (newStatus == Status.GripEnd)
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

                NodalLeader.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort && _gripTemp != null)
            {
                switch (GripName)
                {
                    case GripName.InsertionPoint:
                        NodalLeader.InsertionPoint = _gripTemp;
                        break;
                    case GripName.FramePoint:
                        NodalLeader.EndPoint = _gripTemp;
                        break;
                    case GripName.LeaderPoint:
                        NodalLeader.LeaderPoint = _gripTemp;
                        break;
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