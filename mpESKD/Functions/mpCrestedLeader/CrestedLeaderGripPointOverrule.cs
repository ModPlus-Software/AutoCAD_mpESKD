namespace mpESKD.Functions.mpCrestedLeader;

using Base.Enums;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using Base.Utils;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
using mpESKD.Base.Overrules.Grips;

/// <inheritdoc />
public class CrestedLeaderGripPointOverrule : BaseSmartEntityGripOverrule<CrestedLeader>
{
    /// <inheritdoc />
    public override void GetGripPoints(
        Entity entity,
        GripDataCollection grips,
        double curViewUnitSize,
        int gripSize,
        Vector3d curViewDir,
        GetGripPointsFlags bitFlags)
    {
        Loggerq.WriteRecord("[CrestedLeaderGripPointOverrule: GetGripPoints]");

        try
        {
            if (IsApplicable(entity))
            {
                // Удаляю все ручки - это удалит ручку вставки блока
                var blkRef = (BlockReference)entity;
                GripData toRemove = null;

                foreach (var gd in grips)
                {
                    if (gd.GripPoint == blkRef.Position)
                    {
                        toRemove = gd;
                        break;
                    }
                }

                if (toRemove != null)
                {
                    grips.Remove(toRemove);
                }

                var crestedLeader = EntityReaderService.Instance.GetFromEntity<CrestedLeader>(entity);
                if (crestedLeader != null)
                {
                    var baseVector = crestedLeader.BaseVectorNormal;

                    var insertGrip = new CrestedLeaderGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.ShelfStartPoint
                    };

                    grips.Add(insertGrip);

                    // Ручка перемещения полки
                    var shelfMoveGrip = new CrestedLeaderShelfMoveGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.ShelfLedgePoint
                    };
                    grips.Add(shelfMoveGrip);

                    // Ручка смены позиции полки
                    var shelfPositionGripPoint = crestedLeader.ShelfLedgePoint + (baseVector.GetPerpendicularVector() * 20 * curViewUnitSize);

                    var addVector = baseVector * 20 * curViewUnitSize;

                    if (crestedLeader.ShelfPosition == ShelfPosition.Right)
                    {
                        shelfPositionGripPoint -= addVector;
                    }
                    else
                    {
                        shelfPositionGripPoint += addVector;
                    }

                    var shelfPositionGrip = new CrestedLeaderShelfPositionGrip(crestedLeader)
                    {
                        GripPoint = shelfPositionGripPoint 
                    };
                    grips.Add(shelfPositionGrip);

                    // Ручки переноса выносок - в начале выносок
                    for (var i = 0; i < crestedLeader.LeaderStartPoints.Count; i++)
                    {
                        if (!crestedLeader.InsertionPoint.Equals(crestedLeader.LeaderStartPoints[i]))
                        {
                            var leaderMoveGrip = new CrestedLeaderStartPointLeaderGrip(crestedLeader, i)
                            {
                                GripPoint = crestedLeader.LeaderStartPoints[i]
                            };

                            grips.Add(leaderMoveGrip);
                        }
                    }

                    // Ручки переноса выносок - в конце выносок
                    for (var i = 0; i < crestedLeader.LeaderEndPoints.Count; i++)
                    {
                        var leaderMoveGrip = new CrestedLeaderEndPointLeaderGrip(crestedLeader, i)
                        {
                            GripPoint = crestedLeader.LeaderEndPoints[i]
                        };

                        grips.Add(leaderMoveGrip);
                    }

                    // Ручка добавления выносок 
                    var addLeaderGripPoint = crestedLeader.ShelfLedgePoint - (baseVector.GetPerpendicularVector().GetNormal() * 20 * curViewUnitSize);
                    if (crestedLeader.ShelfPosition == ShelfPosition.Right)
                    {
                        addLeaderGripPoint -= baseVector.GetNormal() * 20 * curViewUnitSize;
                    }
                    else
                    {
                        addLeaderGripPoint += baseVector.GetNormal() * 20 * curViewUnitSize;
                    }

                    var addLeaderGrip = new CrestedLeaderAddLeaderGrip(crestedLeader)
                    {
                        
                        GripPoint = addLeaderGripPoint
                    };

                    grips.Add(addLeaderGrip);

                    // Ручка удаления выносок 
                    if (crestedLeader.LeaderStartPoints.Count > 1)
                    {
                        for (int i = 0; i < crestedLeader.LeaderEndPoints.Count; i++)
                        {
                            var removeLeaderGripPoint = crestedLeader.LeaderEndPointsOCS[i].Y > crestedLeader.ShelfStartPointOCS.Y
                                    ? crestedLeader.LeaderStartPoints[i] - (baseVector.GetPerpendicularVector().GetNormal() * 20 * curViewUnitSize)
                                    : crestedLeader.LeaderStartPoints[i] + (baseVector.GetPerpendicularVector().GetNormal() * 20 * curViewUnitSize);

                            grips.Add(new CrestedLeaderLeaderRemoveGrip(crestedLeader, i)
                            {
                                GripPoint = removeLeaderGripPoint
                            });
                        }
                    }

                    // Ручка выравнивания текста

                    if ((!string.IsNullOrEmpty(crestedLeader.TopText)) & (!string.IsNullOrEmpty(crestedLeader.BottomText)) &&
                        (crestedLeader.TopText.Length != crestedLeader.BottomText.Length))
                    {
                        var alignGripPoint = crestedLeader.ShelfEndPoint +
                                             (baseVector.GetPerpendicularVector().GetNormal() * 20 * curViewUnitSize);

                        if (crestedLeader.ShelfPosition == ShelfPosition.Right)
                        {
                            alignGripPoint += baseVector.GetNormal() * 20 * curViewUnitSize;
                        }
                        else
                        {
                            alignGripPoint -= baseVector.GetNormal() * 20 * curViewUnitSize;
                        }

                        grips.Add(new EntityTextAlignGrip(crestedLeader,
                            () => crestedLeader.ValueHorizontalAlignment, 
                            (setAlignEntity) => crestedLeader.ValueHorizontalAlignment = setAlignEntity)
                        {
                            GripPoint = alignGripPoint
                        });
                    }
                }
            }
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }

    /// <inheritdoc />
    public override void MoveGripPointsAt(
        Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    {
        Loggerq.WriteRecord("[CrestedLeaderGripPointOverrule: MoveGripPointsAt]");

        try
        {
            if (IsApplicable(entity))
            {
                foreach (var gripData in grips)
                {
                    if (gripData is CrestedLeaderGrip insertGrip)
                    {
                        var crestedLeader = insertGrip.CrestedLeader;

                        if (insertGrip.GripIndex == 0)
                        {
                            insertGrip.NewPoint = insertGrip.GripPoint + offset;
                            var newPoint = insertGrip.NewPoint;

                            // Проверка на приближение курсора к концам выносок
                            if (crestedLeader.LeaderEndPoints.Any(p => newPoint.DistanceTo(p) < crestedLeader.MinDistanceBetweenPoints))
                            {
                                newPoint = newPoint.GetNormalizedPointByDistToPointSet(
                                    crestedLeader.LeaderEndPoints,
                                    crestedLeader.MinDistanceBetweenPoints);
                            }

                            ((BlockReference)entity).Position = newPoint;
                            crestedLeader.InsertionPoint = newPoint;

                            crestedLeader.IsBasePointMovedByOverrule = true;

                            crestedLeader.UpdateEntities();
                            crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                        }
                    }
                    else if (gripData is CrestedLeaderShelfMoveGrip shelfMoveGrip)
                    {
                        var crestedLeader = shelfMoveGrip.CrestedLeader;

                        shelfMoveGrip.NewPoint = shelfMoveGrip.GripPoint + offset;

                        ShelfActions.ShelfPositionMove(ref crestedLeader, shelfMoveGrip.NewPoint);

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is CrestedLeaderEndPointLeaderGrip leaderEndGrip)
                    {
                        var crestedLeader = leaderEndGrip.CrestedLeader;

                        leaderEndGrip.NewPoint = leaderEndGrip.GripPoint + offset;
                        var newPoint = leaderEndGrip.NewPoint;

                        // Точка проекции newPoint на центральную линию
                        var newPointProject = newPoint.GetProjectPointToBaseLine(crestedLeader);

                        var leaderStartPoint = crestedLeader.LeaderStartPoints[leaderEndGrip.GripIndex];
                        var leaderEndPoint = crestedLeader.LeaderEndPoints[leaderEndGrip.GripIndex];

                        if (newPoint.DistanceTo(newPointProject) < crestedLeader.MinDistanceBetweenPoints)
                        {
                            var vectorToNewPointNormal = (newPoint - newPointProject).GetNormal();
                            var vectorToCorrectNewPoint = vectorToNewPointNormal * crestedLeader.MinDistanceBetweenPoints;

                            newPoint = newPointProject + vectorToCorrectNewPoint;
                        }

                        var vectorLeader = leaderEndPoint.ToPoint2d() - leaderStartPoint.ToPoint2d();

                        if (Intersections.GetIntersectionBetweenVectors(
                                crestedLeader.InsertionPoint,
                                crestedLeader.BaseVectorNormal.ToVector2d(),
                                newPoint,
                                vectorLeader) is { } tempLineStartPoint)
                        {
                            bool isValidPoint =
                                !(newPoint.Point3dToPoint3dOcs(crestedLeader).Y.Equals(leaderStartPoint.Point3dToPoint3dOcs(crestedLeader).Y) || 
                                  crestedLeader.LeaderStartPoints.Any(p => p.Equals(newPoint)) || 
                                  crestedLeader.LeaderEndPoints.Any(p => p.Equals(newPoint) && !p.Equals(leaderEndPoint)));

                            if (isValidPoint)
                            {
                                crestedLeader.LeaderStartPoints[leaderEndGrip.GripIndex] = tempLineStartPoint;
                                crestedLeader.LeaderEndPoints[leaderEndGrip.GripIndex] = newPoint;
                                
                                
                                var leaderStartPointsSort = crestedLeader.LeaderStartPointsSorted;

                                crestedLeader.ShelfStartPoint = crestedLeader.ShelfPosition == ShelfPosition.Right 
                                    ? leaderStartPointsSort.Last() 
                                    : leaderStartPointsSort.First();

                                crestedLeader.IsStartPointsAssigned = true;
                                crestedLeader.IsBasePointMovedByOverrule = true;
                                crestedLeader.IsShelfPositionByGrip = true;

                                crestedLeader.UpdateEntities();
                                crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                            }
                        }
                    }
                    else if (gripData is CrestedLeaderStartPointLeaderGrip leaderStartGrip)
                    {
                        var crestedLeader = leaderStartGrip.CrestedLeader;

                        crestedLeader.ToLogAnyString("CrestedLeaderGripPointOverrule: MoveGripPointsAt: CrestedLeaderStartPointLeaderGrip");

                        var leaderStartPoint = crestedLeader.LeaderStartPoints[leaderStartGrip.GripIndex];
                        var leaderEndPoint = crestedLeader.LeaderEndPoints[leaderStartGrip.GripIndex];

                        leaderStartGrip.NewPoint = leaderStartGrip.GripPoint + offset;
                        var newPoint = leaderStartGrip.NewPoint;

                        // Точка проекции newPoint на центральную линию
                        var newPointProject = newPoint.GetProjectPointToBaseLine(crestedLeader);

                        bool isValidPoint = 
                            !(crestedLeader.LeaderStartPoints.Any(p => p.Equals(newPoint)) ||
                              crestedLeader.LeaderStartPoints.Any(p => p.Equals(newPointProject)) ||
                              crestedLeader.LeaderEndPoints.Any(p => p.Equals(newPoint)) ||
                              newPoint.Equals(crestedLeader.InsertionPoint) ||
                              newPoint.Equals(crestedLeader.ShelfLedgePoint) ||
                              newPoint.Equals(crestedLeader.ShelfEndPoint));

                        if (isValidPoint)
                        {
                            crestedLeader.LeaderStartPoints[leaderStartGrip.GripIndex] = newPointProject;

                            var vectorTempLineToEndPoint = leaderEndPoint - leaderStartPoint;
                            crestedLeader.LeaderEndPoints[leaderStartGrip.GripIndex] = newPointProject + vectorTempLineToEndPoint;

                            var leaderStartPointsSort = crestedLeader.LeaderStartPointsSorted;

                            crestedLeader.ShelfStartPoint = crestedLeader.ShelfPosition == ShelfPosition.Right
                                ? leaderStartPointsSort.Last()
                                : leaderStartPointsSort.First();

                            crestedLeader.IsStartPointsAssigned = true;

                            crestedLeader.UpdateEntities();
                            crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                        }
                    }
                    else if (gripData is CrestedLeaderAddLeaderGrip addLeaderGrip)
                    {
                        var crestedLeader = addLeaderGrip.CrestedLeader;

                        addLeaderGrip.NewPoint = addLeaderGrip.GripPoint + offset;
                        var newPoint = addLeaderGrip.NewPoint;

                        // Точка проекции newPoint на центральную линию
                        var newPointProject = newPoint.GetProjectPointToBaseLine(crestedLeader);

                        var leaderStartPoint = crestedLeader.LeaderStartPoints.Last();

                        if (newPoint.DistanceTo(newPointProject) < crestedLeader.MinDistanceBetweenPoints)
                        {
                            var vectorToNewPointNormal = (newPoint - newPointProject).GetNormal();
                            var vectorToCorrectNewPoint = vectorToNewPointNormal * crestedLeader.MinDistanceBetweenPoints;

                            newPoint = newPointProject + vectorToCorrectNewPoint;
                        }

                        var vectorLeader = crestedLeader.BaseLeaderEndPoint - crestedLeader.InsertionPoint;

                        if (Intersections.GetIntersectionBetweenVectors(
                                crestedLeader.InsertionPoint,
                                crestedLeader.BaseVectorNormal.ToVector2d(),
                                newPoint,
                                vectorLeader.ToVector2d()) is { } tempLineStartPoint)
                        {
                            bool isValidPoint =
                                !(newPoint.Point3dToPoint3dOcs(crestedLeader).Y.Equals(leaderStartPoint.Point3dToPoint3dOcs(crestedLeader).Y) ||
                                  crestedLeader.LeaderStartPoints.Any(p => p.Equals(newPoint)));

                            if (isValidPoint)
                            {
                                crestedLeader.LeaderStartPoints[crestedLeader.LeaderStartPoints.Count - 1] = tempLineStartPoint;
                                crestedLeader.LeaderEndPoints[crestedLeader.LeaderStartPoints.Count - 1] = newPoint;

                                var leaderStartPointsSort = crestedLeader.LeaderStartPointsSorted;

                                crestedLeader.ShelfStartPoint = crestedLeader.ShelfPosition == ShelfPosition.Right
                                    ? leaderStartPointsSort.Last()
                                    : leaderStartPointsSort.First();

                                crestedLeader.IsStartPointsAssigned = true;

                                crestedLeader.UpdateEntities();
                                crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                            }
                        }
                    }
                    else
                    {
                        base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                    }
                }
            }
            else
            {
                base.MoveGripPointsAt(entity, grips, offset, bitFlags);
            }
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }
}