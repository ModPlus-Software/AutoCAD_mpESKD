using System.Linq;

namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Enums;
using mpESKD.Base.Utils;
using System.Collections.Generic;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

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
        Loggerq.WriteRecord($"GetGripPoints start");
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
                    /*
                    Loggerq.WriteRecord($"GetGripPoints: *");
                    Loggerq.WriteRecord($"GetGripPoints: LeaderStartPoints: {crestedLeader.LeaderStartPoints.Count} =>");

                    for (int i = 0; i < crestedLeader.LeaderStartPoints.Count; i++)
                    {
                        Loggerq.WriteRecord($"GetGripPoints: pt[{i}]: {crestedLeader.LeaderStartPoints[i].ToString()}");
                        
                    }*/

                    /*
                    List<Point3d> leaderStartPointsTmp = new();
                    leaderStartPointsTmp.AddRange(crestedLeader.LeaderStartPoints);

                    var leaderStartPointsSort = crestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();

                    if (crestedLeader.ShelfPosition == ShelfPosition.Right)
                        crestedLeader.InsertionPoint = leaderStartPointsSort.Last();
                    else
                        crestedLeader.InsertionPoint = leaderStartPointsSort.First();

                    crestedLeader.UpdateEntities();
                    crestedLeader.BlockRecord.UpdateAnonymousBlocks();

                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef1 = tr.GetObject(crestedLeader.BlockId, OpenMode.ForWrite, true, true);
                        // перемещение точки вставки в точку первой точки полки
                        ((BlockReference)blkRef1).Position = crestedLeader.InsertionPoint;

                        using (var resBuf = crestedLeader.GetDataForXData())
                        {
                            blkRef1.XData = resBuf;
                        }

                        tr.Commit();
                    }

                    crestedLeader.LeaderStartPoints.Clear();
                    crestedLeader.LeaderStartPoints.AddRange(leaderStartPointsTmp);

                    crestedLeader.IsBasePointMoved = true;

                    crestedLeader.UpdateEntities();
                    crestedLeader.BlockRecord.UpdateAnonymousBlocks();

                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef2 = tr.GetObject(crestedLeader.BlockId, OpenMode.ForWrite, true, true);
                        using (var resBuf = crestedLeader.GetDataForXData())
                        {
                            blkRef2.XData = resBuf;
                        }

                        tr.Commit();
                    }












                    */


                    // insertion (start) grip
                    var MoveGrip = new CrestedLeaderGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.InsertionPoint
                        ////GripPoint = crestedLeader.LeaderStartPoints != null 
                        ////    ? crestedLeader.LeaderStartPoints.Last()
                        ////    : crestedLeader.InsertionPoint
                    };

                    grips.Add(MoveGrip);

                    //grips.Add(new CrestedLeaderShelfMoveGrip(crestedLeader, 1)
                    //{
                    //    GripPoint = crestedLeader.EndPoint
                    //});

                    /*
                    Point3d addLeaderGripPosition;
                    if (!string.IsNullOrEmpty(crestedLeader.Note))
                    {
                        addLeaderGripPosition = new Point3d(
                            crestedLeader.InsertionPoint.X + crestedLeader.NoteShelfLinePoints[1].X,
                            crestedLeader.InsertionPoint.Y + crestedLeader.NoteShelfLinePoints[1].Y,
                            crestedLeader.InsertionPoint.Z);
                    }
                    else
                    {
                        addLeaderGripPosition = new Point3d(
                            crestedLeader.InsertionPoint.X + crestedLeader.FrameRevisionTextPoints[0].X,
                            crestedLeader.InsertionPoint.Y + crestedLeader.FrameRevisionTextPoints[0].Y,
                            crestedLeader.InsertionPoint.Z);
                    }

                    // Добавляем ручку для создания выноски
                    grips.Add(new CrestedLeaderAddLeaderGrip(crestedLeader)
                    {
                        GripPoint = addLeaderGripPosition
                    });

                    if (!string.IsNullOrEmpty(crestedLeader.Note))
                    {
                        var bottomLineFrameCenter = GeometryUtils.GetMiddlePoint3d(crestedLeader.FrameRevisionTextPoints[0], crestedLeader.FrameRevisionTextPoints[1]);

                        var shelfPointGripPosition = new Point3d(
                            crestedLeader.InsertionPoint.X + bottomLineFrameCenter.X,
                            crestedLeader.InsertionPoint.Y + bottomLineFrameCenter.Y,
                            crestedLeader.InsertionPoint.Z
                        );

                        // Добавляем ручку для зеркалирования полки примечания
                        grips.Add(new CrestedLeaderShelfPositionGrip(crestedLeader)
                        {
                            GripPoint = shelfPointGripPosition
                        });
                    }

                    for (var i = 0; i < crestedLeader.LeaderEndPoints.Count; i++)
                    {
                        // ручки переноса выносок
                        grips.Add(new CrestedLeaderShelfMoveGrip(crestedLeader, i)
                        {
                            GripPoint = crestedLeader.LeaderEndPoints[i]
                        });

                        // ручки удаления выносок
                        var deleteGripPoint = crestedLeader.LeaderEndPoints[i] + (Vector3d.XAxis * 20 * curViewUnitSize);
                        grips.Add(new CrestedLeaderLeaderRemoveGrip(crestedLeader, i)
                        {
                            GripPoint = deleteGripPoint
                        });

                        // ручки типа рамки у выноски
                        var leaderEndTypeGripPoint = crestedLeader.LeaderEndPoints[i] - (Vector3d.XAxis * 20 * curViewUnitSize);
                        grips.Add(new CrestedLeaderFrameTypeGrip(crestedLeader, i)
                        {
                            GripPoint = leaderEndTypeGripPoint
                        });

                        // Если нет рамки, то ручка для растягивания рамки не создается
                        if (crestedLeader.RevisionFrameTypes[i] != 0)
                        {
                            grips.Add(new CrestedLeaderFrameStretchGrip(crestedLeader, i)
                            {
                                GripPoint = crestedLeader.RevisionFrameStretchPoints[i]
                            });
                        }
                    }*/
                }
            }
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
        Loggerq.WriteRecord($"GetGripPoints end");
    }

    /// <inheritdoc />
    public override void MoveGripPointsAt(
        Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    {
        Loggerq.WriteRecord($"MoveGripPointsAt start");
        try
        {
            if (IsApplicable(entity))
            {
                foreach (var gripData in grips)
                {
                    if (gripData is CrestedLeaderGrip moveGrip)
                    {
                        var crestedLeader = moveGrip.CrestedLeader;

                        if (moveGrip.GripIndex == 0)
                        {
                            moveGrip.NewPoint = moveGrip.GripPoint + offset;

                          //  crestedLeader.ShelfLedgePoint = crestedLeader.ShelfLedgePointPreviousForGripMove + offset;

                          //  crestedLeader.ShelfEndPoint = crestedLeader.ShelfEndPointPreviousForGripMove + offset;

                            //crestedLeader.LeaderEndPoints = crestedLeader
                            //    .LeaderPointsPreviousForGripMove.Select(x => x + offset)
                            //    .ToList();

                            var pos = moveGrip.GripPoint + offset;
                            ((BlockReference)entity).Position = pos;
                            crestedLeader.InsertionPoint = pos;
                        }

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is CrestedLeaderShelfMoveGrip shelfMoveGrip)
                    {
                        // shelfMoveGrip.NewPoint = shelfMoveGrip.GripPoint + offset;
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
        Loggerq.WriteRecord($"MoveGripPointsAt end");
    }
}