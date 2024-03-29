﻿namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка вершин
/// </summary>
public class ChainLeaderVertexGrip : SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTmp;

    // Экземпляр анонимного блока
    private readonly BlockReference _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр класса <see cref="mpChainLeader.ChainLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public ChainLeaderVertexGrip(ChainLeader chainLeader, int gripIndex, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    public List<double> TempPoint3ds { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            if (newStatus == Status.GripStart)
            {
                if (GripIndex == 0)
                {
                    _gripTmp = ChainLeader.InsertionPoint;
                }

                if (GripIndex == 1)
                {
                    _gripTmp = ChainLeader.EndPoint;
                }
            }

            if (newStatus == Status.GripEnd)
            {
                var tempInsPoint = ChainLeader.InsertionPoint;
                using (ChainLeader)
                {
                    if (GripIndex == 0)
                    {
                        var mainNormal = (ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal();
                        var result = ChainLeader.ArrowPoints.OrderBy(x => x).FirstOrDefault();

                        var distFromEndPointToInsPoint = -1 * ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);

                        if (result < distFromEndPointToInsPoint)
                        {
                            tempInsPoint = ChainLeader.EndPoint + (mainNormal * result);
                            ChainLeader.ArrowPoints.Remove(result);
                            ChainLeader.ArrowPoints.Add(distFromEndPointToInsPoint);
                        }

                        ChainLeader.InsertionPoint = tempInsPoint;
                    }

                    ChainLeader.UpdateEntities();
                    ChainLeader.BlockRecord.UpdateAnonymousBlocks();
                }

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(ChainLeader.BlockId, OpenMode.ForWrite, true, true);
                    _entity.Position = tempInsPoint;
                    using (var resBuf = ChainLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }

                ChainLeader.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_gripTmp != null)
                {
                    if (GripIndex == 0)
                    {
                        ChainLeader.InsertionPoint = _gripTmp;
                    }

                    if (GripIndex == 1)
                    {
                        ChainLeader.EndPoint = _gripTmp;
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