﻿using System.Collections.Generic;

namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Linq;

/// <summary>
/// Ручка вершин
/// </summary>
public class ChainLeaderArrowMoveGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public ChainLeaderArrowMoveGrip(ChainLeader chainLeader, int gripIndex, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;
        Entity = entity;
    }

    public BlockReference Entity { get; }
    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

    /// <summary>
    /// Новое значение точки вершины
    /// </summary>
    public double NewPoint { get; set; }

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
       
        //if (newStatus == Status.GripStart)
        //{
        //    //TODO
        //    ChainLeader.TempNewArrowPoint = NewPoint;
        //    ChainLeader.UpdateEntities();
        //}

        //if (newStatus == Status.Move)
        //{
        //    ChainLeader.TempNewArrowPoint = NewPoint;
        //    ChainLeader.UpdateEntities();
        //    AcadUtils.WriteMessageInDebug($"OnGripStatusChanged if (newStatus == Status.Move) TempNewArrowPoint {ChainLeader.TempNewArrowPoint} \n");
        //}

        if (newStatus == Status.GripEnd)
        {
            using (ChainLeader)
            {
                var tempList = new List<double>();
                ChainLeader.ArrowPoints[GripIndex] = NewPoint;
                tempList.AddRange(ChainLeader.ArrowPoints);

                var q = tempList.OrderBy(x => x);
                var result = q.FirstOrDefault();
                if (result > 0)
                {
                    result = q.LastOrDefault();
                }

                var tempInsPoint = ChainLeader.EndPoint + (ChainLeader._mainNormal * result);
                ChainLeader.InsertionPoint = tempInsPoint;
                
                
                AcadUtils.WriteMessageInDebug($"ChainLeader.InsertionPoint {ChainLeader.InsertionPoint}, ChainLeader.InsertionPointOCS {ChainLeader.InsertionPointOCS},ChainLeader.EndPointOCS {ChainLeader.EndPointOCS}");
                
                ChainLeader.TempNewArrowPoint = double.NaN;
                
                ChainLeader.UpdateEntities();
                ChainLeader.BlockRecord.UpdateAnonymousBlocks();

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    
                    var blkRef = tr.GetObject(ChainLeader.BlockId, OpenMode.ForWrite, true, true);
                    Entity.Position = tempInsPoint;
                    using (var resBuf = ChainLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }
        }

        if (newStatus == Status.GripAbort)
        {
            ChainLeader.TempNewArrowPoint = double.NaN;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}