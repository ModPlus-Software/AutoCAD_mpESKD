namespace mpESKD.Functions.mpChainLeader.Grips;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

public class ChainLeaderArrowBaseGrip : SmartEntityGripData
{
    public ChainLeaderArrowBaseGrip(ChainLeader chainLeader, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
        Entity = entity;
    }

    public BlockReference Entity { get; }
    /// <summary>
    /// Экземпляр <see cref="mpChainLeader.ChainLeaderArrowBaseGrip"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

    /// <summary>
    /// Новое значение точки вершины
    /// </summary>
    public double NewPoint { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Добавить выноску
        return Language.GetItem("gp5");
    }

    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        //if (newStatus == Status.GripStart)
        //{
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
                ChainLeader.ArrowPoints.Add(NewPoint);
                var distFromInsPoint = ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                var tempList = new List<double>();
                tempList.Add(distFromInsPoint);
                tempList.AddRange(ChainLeader.ArrowPoints);
                var q = tempList.OrderBy(x => x);
                var result = q.FirstOrDefault();
                if (result > 0)
                {
                    result = q.LastOrDefault();
                }
                
                var tempInsPoint = ChainLeader.EndPoint + (ChainLeader._mainNormal * result);
                ChainLeader.ArrowPoints.Add(distFromInsPoint);
                ChainLeader.InsertionPoint = tempInsPoint;
                
                ChainLeader.TempNewArrowPoint = double.NaN;
                AcadUtils.WriteMessageInDebug($"NewPoint {NewPoint} -  ChainLeader.InsertionPoint {ChainLeader.InsertionPoint}\n");
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