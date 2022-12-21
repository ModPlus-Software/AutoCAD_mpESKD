namespace mpESKD.Functions.mpChainLeader.Grips;

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
public class ChainLeaderArrowMoveGrip : SmartEntityGripData
{
    // Экземпляр анонимного блока
    private readonly BlockReference _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public ChainLeaderArrowMoveGrip(ChainLeader chainLeader, int gripIndex, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
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

    /// <summary>
    /// Свойство для определения точки в существующем сегменте
    /// </summary>
    public bool IsOnsegment { get; set; }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripEnd)
        {
            using (ChainLeader)
            {
                var tempInsPoint = ChainLeader.InsertionPoint;

                if (!ChainLeader.ArrowPoints.Contains(ChainLeader.TempNewArrowPoint))
                {
                    var distFromEndPointToInsPoint = ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                    if (!ChainLeader.IsLeft)
                    {
                        distFromEndPointToInsPoint = -1 * ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                    }

                    var tempList = new List<double>
                    {
                        distFromEndPointToInsPoint
                    };
                    tempList.AddRange(ChainLeader.ArrowPoints);
                    var result = tempList.OrderBy(x => x).FirstOrDefault();

                    if (ChainLeader.TempNewArrowPoint >= 0)
                    {
                        // если в списке есть значения и они положительные, то берем последнюю
                        if (result > 0)
                        {
                            result = tempList.OrderBy(x => x).LastOrDefault();

                            // если последняя больше чем текущая
                            if (result > ChainLeader.TempNewArrowPoint)
                            {
                                // текущую добавляем в список, inspoint не меняем
                                ChainLeader.ArrowPoints[GripIndex] = ChainLeader.TempNewArrowPoint;
                            }
                            else
                            {
                                // если текущая больше чем последняя она должна быть insPoint
                                tempInsPoint = ChainLeader.EndPoint + ((ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal() * ChainLeader.TempNewArrowPoint);
                                ChainLeader.ArrowPoints[GripIndex] = ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                            }
                        }
                        else
                        {
                            ChainLeader.ArrowPoints[GripIndex] = ChainLeader.TempNewArrowPoint;
                        }
                    }
                    else
                    {
                        // ищем первую
                        tempInsPoint = ChainLeader.EndPoint + ((ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal() * ChainLeader.TempNewArrowPoint);

                        // если первая положительная, значит слева нет точек
                        if (IsOnsegment)
                        {
                            ChainLeader.ArrowPoints[GripIndex] = ChainLeader.TempNewArrowPoint;
                            tempInsPoint = ChainLeader.InsertionPoint;
                        }
                        else if (ChainLeader.TempNewArrowPoint > distFromEndPointToInsPoint)
                        {
                            ChainLeader.ArrowPoints[GripIndex] = ChainLeader.TempNewArrowPoint;
                            tempInsPoint = ChainLeader.InsertionPoint;
                        }
                        else if (ChainLeader.TempNewArrowPoint < result)
                        {
                            ChainLeader.ArrowPoints[GripIndex] = -1 * distFromEndPointToInsPoint;
                        }
                        else
                        {
                            ChainLeader.ArrowPoints[GripIndex] = distFromEndPointToInsPoint;
                        }
                    }
                }

                ChainLeader.InsertionPoint = tempInsPoint;
                ChainLeader.TempNewArrowPoint = double.NaN;

                ChainLeader.UpdateEntities();
                ChainLeader.BlockRecord.UpdateAnonymousBlocks();

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
            }
        }

        if (newStatus == Status.GripAbort)
        {
            ChainLeader.TempNewArrowPoint = double.NaN;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}