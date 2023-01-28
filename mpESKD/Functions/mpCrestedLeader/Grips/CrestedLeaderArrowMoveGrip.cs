using System;
using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка вершин
/// </summary>
public class CrestedLeaderArrowMoveGrip : SmartEntityGripData
{
    // Экземпляр анонимного блока
    private readonly BlockReference _entity;
    private Point3d _startGripTmp;
    private Point3d _endGripTmp;
    private Point3d _leaderGripTmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public CrestedLeaderArrowMoveGrip(CrestedLeader crestedLeader, int gripIndex, BlockReference entity)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

    /// <summary>
    /// Свойство для определения точки в существующем сегменте
    /// </summary>
    public bool IsOnsegment { get; set; }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripStart)
        {
            _startGripTmp = CrestedLeader.InsertionPoint;
            _endGripTmp = CrestedLeader.EndPoint;
            _leaderGripTmp = CrestedLeader.LeaderPoint;
        }
        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                var tempInsPoint = CrestedLeader.InsertionPoint;
                //TODO 
                if (!CrestedLeader.ArrowPoints.Contains(CrestedLeader.TempNewArrowPoint))
                {
                    //if (!CrestedLeader.IsLeft)
                    //{
                    //    distFromEndPointToInsPoint = -1 * CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint);
                    //}

                    //var tempList = new List<Point3d>();
                    //tempList.AddRange(CrestedLeader.ArrowPoints);
                    //var result = tempList.OrderBy(x => x).FirstOrDefault();

                    //if (CrestedLeader.TempNewArrowPoint >= 0)
                    //{
                    //    // если в списке есть значения и они положительные, то берем последнюю
                    //    if (result > 0)
                    //    {
                    //        result = tempList.OrderBy(x => x).LastOrDefault();

                    //        // если последняя больше чем текущая
                    //        if (result > CrestedLeader.TempNewArrowPoint)
                    //        {
                    //            // текущую добавляем в список, inspoint не меняем
                    //            CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    //        }
                    //        else
                    //        {
                    //            // если текущая больше чем последняя она должна быть insPoint
                    //            tempInsPoint = CrestedLeader.EndPoint + ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * CrestedLeader.TempNewArrowPoint);
                    //            CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    //    }
                    //}
                    //else
                    //{
                    //    // ищем первую
                    //    tempInsPoint = CrestedLeader.EndPoint + ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * CrestedLeader.TempNewArrowPoint);

                    //    // если первая положительная, значит слева нет точек
                    //    if (IsOnsegment)
                    //    {
                            CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    //        tempInsPoint = CrestedLeader.InsertionPoint;
                    //    }
                    //    else if (CrestedLeader.TempNewArrowPoint > distFromEndPointToInsPoint)
                    //    {
                    //        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    //        tempInsPoint = CrestedLeader.InsertionPoint;
                    //    }
                    //    else if (CrestedLeader.TempNewArrowPoint < result)
                    //    {
                    //        CrestedLeader.ArrowPoints[GripIndex] = -1 * distFromEndPointToInsPoint;
                    //    }
                    //    else
                    //    {
                    //        CrestedLeader.ArrowPoints[GripIndex] = distFromEndPointToInsPoint;
                    //    }
                    //}
                }

                //CrestedLeader.InsertionPoint = tempInsPoint;
                CrestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);

                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);
                    _entity.Position = tempInsPoint;
                    using (var resBuf = CrestedLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }
        }

        if (newStatus == Status.GripAbort)
        {
            CrestedLeader.TempNewArrowPoint = default;
            CrestedLeader.InsertionPoint = _startGripTmp;
            CrestedLeader.EndPoint = _endGripTmp;
            CrestedLeader.LeaderPoint = _leaderGripTmp;

        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}