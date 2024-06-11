﻿namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
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
                    // insertion (start) grip
                    var MoveGrip = new CrestedLeaderGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.InsertionPoint
                    };
                    grips.Add(MoveGrip);


                    grips.Add(new CrestedLeaderShelfMoveGrip(crestedLeader, 1)
                    {
                        GripPoint = crestedLeader.EndPoint
                    });

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

                    for (var i = 0; i < crestedLeader.LeaderPoints.Count; i++)
                    {
                        // ручки переноса выносок
                        grips.Add(new CrestedLeaderShelfMoveGrip(crestedLeader, i)
                        {
                            GripPoint = crestedLeader.LeaderPoints[i]
                        });

                        // ручки удаления выносок
                        var deleteGripPoint = crestedLeader.LeaderPoints[i] + (Vector3d.XAxis * 20 * curViewUnitSize);
                        grips.Add(new CrestedLeaderLeaderRemoveGrip(crestedLeader, i)
                        {
                            GripPoint = deleteGripPoint
                        });

                        // ручки типа рамки у выноски
                        var leaderEndTypeGripPoint = crestedLeader.LeaderPoints[i] - (Vector3d.XAxis * 20 * curViewUnitSize);
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
    }

    /// <inheritdoc />
    public override void MoveGripPointsAt(
        Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    {
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
                            ((BlockReference)entity).Position = moveGrip.GripPoint + offset;
                            crestedLeader.InsertionPoint = moveGrip.GripPoint + offset;
                        }

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();

                    }
                    else if (gripData is CrestedLeaderShelfMoveGrip shelfMoveGrip)
                    {
                        shelfMoveGrip.NewPoint = shelfMoveGrip.GripPoint + offset;
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