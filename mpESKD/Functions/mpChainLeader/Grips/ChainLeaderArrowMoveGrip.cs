namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Collections.Generic;
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
        if (newStatus == Status.GripEnd)
        {
            using (ChainLeader)
            {
                var tempInsPoint = new Point3d();

                if (!ChainLeader.ArrowPoints.Contains(NewPoint))
                {
                    var mainNormal = (ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal();
                    var distFromEndPointToInsPoint = ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                    if (ChainLeader.IsLeft)
                    {
                        distFromEndPointToInsPoint = -1 * ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                    }

                    double result;
                    var tempList = new List<double>();
                    tempList.Add(distFromEndPointToInsPoint);
                    tempList.AddRange(ChainLeader.ArrowPoints);
                    if (NewPoint > 0)
                    {
                        result = tempList.OrderBy(x => x).FirstOrDefault();

                        // если в списке есть значения и они положительные, то берем последнюю
                        if (result > 0)
                        {
                            result = tempList.OrderBy(x => x).LastOrDefault();

                            // если последняя больше чем текущая
                            if (result > NewPoint)
                            {
                                // текущую добавляем в список, inspoint не меняем
                                ChainLeader.ArrowPoints[GripIndex] = NewPoint;
                                tempInsPoint = ChainLeader.InsertionPoint;
                            }
                            else
                            { // если текущая больше чем последняя она должна быть insPoint
                                tempInsPoint = ChainLeader.EndPoint + (mainNormal * NewPoint);

                                ChainLeader.ArrowPoints[GripIndex] = ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                            }
                        }
                        else
                        {
                            ChainLeader.ArrowPoints[GripIndex] = NewPoint;
                            tempInsPoint = ChainLeader.InsertionPoint;
                        }
                    }
                    else // когда тянем влево, значения отрицательные
                    {
                        // ищем первую
                        result = tempList.OrderBy(x => x).FirstOrDefault();

                        //если первая положительная, значит слева нет точек
                        if (result > 0)
                        {
                            // тогда 

                            tempInsPoint = ChainLeader.EndPoint + (mainNormal * NewPoint);
                            result = ChainLeader.ArrowPoints.OrderBy(x => x).LastOrDefault();
                            ChainLeader.ArrowPoints[GripIndex] = result;
                        }
                        else if (NewPoint > distFromEndPointToInsPoint)
                        {
                            ChainLeader.ArrowPoints[GripIndex] = NewPoint;
                            tempInsPoint = ChainLeader.InsertionPoint;
                        }
                        else
                        {
                            ChainLeader.ArrowPoints[GripIndex] = distFromEndPointToInsPoint;
                            tempInsPoint = ChainLeader.EndPoint + mainNormal * NewPoint;
                        }
                    }
                }
                else
                {
                    tempInsPoint = ChainLeader.InsertionPoint;
                }

                ChainLeader.InsertionPoint = tempInsPoint;
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