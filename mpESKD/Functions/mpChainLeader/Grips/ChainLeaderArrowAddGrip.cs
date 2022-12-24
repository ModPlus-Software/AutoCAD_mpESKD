namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class ChainLeaderArrowAddGrip : SmartEntityGripData
{
    private readonly BlockReference _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderArrowAddGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public ChainLeaderArrowAddGrip(ChainLeader chainLeader, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Добавить выноску
        return Language.GetItem("gp5");
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

                    // когда тянем вправо
                    if (ChainLeader.TempNewArrowPoint > 0)
                    {
                        // если в списке есть значения и они положительные, то берем последнюю
                        if (result > 0)
                        {
                            result = tempList.OrderBy(x => x).LastOrDefault();

                            // если последняя больше чем текущая
                            if (result > ChainLeader.TempNewArrowPoint)
                            {
                                // текущую добавляем в список, inspoint не меняем
                                ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
                            }
                            else
                            { // если текущая больше чем последняя она должна быть insPoint
                                tempInsPoint = ChainLeader.EndPoint + ((ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal() * ChainLeader.TempNewArrowPoint);
                                ChainLeader.ArrowPoints.Add(distFromEndPointToInsPoint);
                            }
                        }
                        else
                        {
                            ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
                        }
                    }

                    // когда тянем влево, значения отрицательные
                    else
                    {
                        if (result > ChainLeader.TempNewArrowPoint && !IsOnsegment)
                        {
                            tempInsPoint = ChainLeader.EndPoint + ((ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal() * ChainLeader.TempNewArrowPoint);
                            ChainLeader.ArrowPoints.Add(-1 * ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint));
                        }
                        else
                        {
                            ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
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