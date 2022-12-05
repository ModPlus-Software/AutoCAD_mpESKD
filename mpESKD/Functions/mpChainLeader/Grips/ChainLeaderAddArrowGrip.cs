using System;

namespace mpESKD.Functions.mpChainLeader.Grips;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class ChainLeaderAddArrowGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderAddArrowGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    public ChainLeaderAddArrowGrip(ChainLeader chainLeader, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
        Entity = entity;
    }

    public BlockReference Entity { get; }
    /// <summary>
    /// Экземпляр <see cref="mpChainLeader.ChainLeader"/>
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

    /// <inheritdoc />
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
                var tempInsPoint = new Point3d();
                if (!ChainLeader.ArrowPoints.Contains(NewPoint))
                {
                    double result;
                    //когда тянем вправо
                    if (ChainLeader.TempNewArrowPoint > 0)
                    {
                        result = ChainLeader.ArrowPoints.OrderBy(x => x).FirstOrDefault();
                        // если в списке есть значения и они положительные, то берем последнюю
                        if (result > 0)
                        {
                            result = ChainLeader.ArrowPoints.OrderBy(x => x).LastOrDefault();
                            // если последняя больше чем текущая
                            if (result > ChainLeader.TempNewArrowPoint)
                            {
                                // текущую добавлеям в список, inspoint не меняем
                                //ChainLeader.InsertionPoint = ChainLeader.EndPoint + (ChainLeader._mainNormal * result);
                                ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
                                //ChainLeader.ArrowPoints.Remove(result);
                            }
                            else
                            { // если текущая больше чем последняя она должна быть insPoint
                                tempInsPoint = ChainLeader.EndPoint + (ChainLeader._mainNormal * ChainLeader.TempNewArrowPoint);

                                ChainLeader.ArrowPoints.Add(ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint));
                            }
                        }
                    }
                    else // когда тянем влево, значения отрицательные
                    {
                        // ищем первую
                        result = ChainLeader.ArrowPoints.OrderBy(x => x).FirstOrDefault();

                        //если первая положительная, значит слева нет точек
                        if (result > 0)
                        {
                            // тогда 
                            

                            tempInsPoint = ChainLeader.EndPoint + (ChainLeader._mainNormal * ChainLeader.TempNewArrowPoint);
                            result = ChainLeader.ArrowPoints.OrderBy(x => x).LastOrDefault();
                            ChainLeader.ArrowPoints.Add(result);

                            
                        }

                        if (result > ChainLeader.TempNewArrowPoint)
                        {
                            
                            ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
                            //ChainLeader.ArrowPoints.Remove(result);
                        }
                        else
                        {
                            tempInsPoint = ChainLeader.EndPoint + (ChainLeader._mainNormal * ChainLeader.TempNewArrowPoint);
                            ChainLeader.ArrowPoints.Add(ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint));
                        }
                    }

                    ChainLeader.InsertionPoint = tempInsPoint;
                }
                //    var tempList = new List<double>();

                //    var distFromInsPoint = ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                //    //var distFromNewPoint = ChainLeader.EndPoint.DistanceTo(NewPoint);
                //    AcadUtils.WriteMessageInDebug($"distFromInsPoint {distFromInsPoint}, NewPoint {NewPoint} ");


                //    tempList.Add(distFromInsPoint);
                //    tempList.AddRange(ChainLeader.ArrowPoints);
                //    var q = tempList.OrderBy(x => x);
                //    var result = q.FirstOrDefault();
                //    tempInsPoint = ChainLeader.EndPoint + (ChainLeader._mainNormal * result);
                //    if (result > 0)
                //    {
                //        result = q.LastOrDefault();
                //        tempInsPoint = ChainLeader.EndPoint + (ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal() * result;
                //    }
                //    else if (result == 0)
                //    {
                //        tempInsPoint = ChainLeader.InsertionPoint;
                //    }

                //    if (ChainLeader.IsLeft)
                //    {
                //        if (Math.Abs(NewPoint) > distFromInsPoint)
                //        {
                //            ChainLeader.InsertionPoint = tempInsPoint;
                //            ChainLeader.ArrowPoints.Remove(NewPoint);
                //            ChainLeader.ArrowPoints.Add(-1 * distFromInsPoint);
                //        }
                //        else
                //        {
                //            ChainLeader.ArrowPoints.Add(-1 * distFromInsPoint);
                //            tempInsPoint = ChainLeader.InsertionPoint;
                //        }
                //    }
                //    else
                //    {
                //        if (Math.Abs(NewPoint) > distFromInsPoint)
                //        {
                //            ChainLeader.InsertionPoint = tempInsPoint;
                //            ChainLeader.ArrowPoints.Add(distFromInsPoint);
                //        }
                //        else
                //        {
                //            ChainLeader.ArrowPoints.Add(distFromInsPoint);
                //            tempInsPoint = ChainLeader.InsertionPoint;
                //        }
                //    }
                //}



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