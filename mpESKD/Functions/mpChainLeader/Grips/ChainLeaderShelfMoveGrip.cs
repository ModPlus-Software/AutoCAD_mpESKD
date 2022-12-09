﻿namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;

public class ChainLeaderShelfMoveGrip : SmartEntityGripData
{
    // Временное значение ручки
    private double _gripTmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderShelfMoveGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр класса <see cref="mpChainLeader.ChainLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public ChainLeaderShelfMoveGrip(ChainLeader chainLeader, int gripIndex)
    {
        ChainLeader = chainLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
    }

    /// <summary>
    /// Новое значение точки вершины
    /// </summary>
    public double NewPoint { get; set; }

    /// <summary>
    /// Экземпляр класса <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

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
            if (newStatus == Status.GripStart | newStatus == Status.Stretch)
            {
                ChainLeader.TextIndent += NewPoint;
                ChainLeader.UpdateEntities();
            }

            if (newStatus == Status.GripEnd)
            {
                using (ChainLeader)
                {
                    ChainLeader.TextIndent = _gripTmp + NewPoint;
                    ChainLeader.UpdateEntities();
                    ChainLeader.BlockRecord.UpdateAnonymousBlocks();

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

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_gripTmp != null)
                {
                    ChainLeader.ShelfLedge = _gripTmp;
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