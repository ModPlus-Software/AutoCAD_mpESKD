﻿using System.Linq;

namespace mpESKD.Functions.mpRevisionMark;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Utils;

/// <inheritdoc />
public class RevisionMarkGripPointOverrule : BaseSmartEntityGripOverrule<RevisionMark>
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

                var revisionMark = EntityReaderService.Instance.GetFromEntity<RevisionMark>(entity);
                if (revisionMark != null)
                {
                    // insertion (start) grip
                    var vertexGrip = new RevisionMarkVertexGrip(revisionMark, 0)
                    {
                        GripPoint = revisionMark.InsertionPoint
                    };
                    grips.Add(vertexGrip);

                    Point3d addLeaderGripPosition;
                    if (!string.IsNullOrEmpty(revisionMark.Note))
                    {
                        addLeaderGripPosition = new Point3d(
                            revisionMark.InsertionPoint.X + revisionMark.NoteShelfLinePoints[1].X,
                            revisionMark.InsertionPoint.Y + revisionMark.NoteShelfLinePoints[1].Y,
                            revisionMark.InsertionPoint.Z);
                    }
                    else
                    {
                        addLeaderGripPosition = new Point3d(
                            revisionMark.InsertionPoint.X + revisionMark.FrameRevisionTextPoints[0].X,
                            revisionMark.InsertionPoint.Y + revisionMark.FrameRevisionTextPoints[0].Y,
                            revisionMark.InsertionPoint.Z);
                    }

                    // Добавляем ручку для создания выноски
                    grips.Add(new RevisionMarkAddLeaderGrip(revisionMark)
                    {
                        GripPoint = addLeaderGripPosition
                    });

                    if (!string.IsNullOrEmpty(revisionMark.Note))
                    {
                        var bottomLineFrameCenter = GeometryUtils.GetMiddlePoint3d(revisionMark.FrameRevisionTextPoints[0], revisionMark.FrameRevisionTextPoints[1]);

                        var shelfPointGripPosition = new Point3d(
                            revisionMark.InsertionPoint.X + bottomLineFrameCenter.X,
                            revisionMark.InsertionPoint.Y + bottomLineFrameCenter.Y,
                            revisionMark.InsertionPoint.Z
                        );

                        // Добавляем ручку для зеркалирования полки примечания
                        grips.Add(new RevisionMarkShelfPositionGrip(revisionMark)
                        {
                            GripPoint = shelfPointGripPosition
                        });
                    }

                    for (var i = 0; i < revisionMark.LeaderPoints.Count; i++)
                    {
                        // ручки переноса выносок
                        grips.Add(new RevisionMarkLeaderMoveGrip(revisionMark, i)
                        {
                            GripPoint = revisionMark.LeaderPoints[i]
                        });

                        // ручки удаления выносок
                        var deleteGripPoint = revisionMark.LeaderPoints[i] + (Vector3d.XAxis * 20 * curViewUnitSize);
                        grips.Add(new RevisionMarkLeaderRemoveGrip(revisionMark, i)
                        {
                            GripPoint = deleteGripPoint
                        });

                        // ручки типа рамки у выноски
                        var leaderEndTypeGripPoint = revisionMark.LeaderPoints[i] - (Vector3d.XAxis * 20 * curViewUnitSize);
                        grips.Add(new RevisionMarkFrameTypeGrip(revisionMark, i)
                        {
                            GripPoint = leaderEndTypeGripPoint
                        });

                        // ручки размера рамки у выноски
                        Point3d frameStretchPoint;
                        /*
                        if (revisionMark.RevisionFrameStretchPoints[i].Equals())
                        {
                            frameStretchPoint = revisionMark.LeaderPoints[i] - 
                                                ((Vector3d.XAxis * revisionMark.RevisionFrameSize[i]  * curViewUnitSize));
                        }
                        else
                        {
                            frameStretchPoint = revisionMark.LeaderPoints[i] + (Vector3d.XAxis * 20 * curViewUnitSize);
                        }*/

                        // Если нет рамки, то ручка для растягивания рамки не создается
                         if (revisionMark.RevisionFrameTypes[i] != 0)
                        {
                            frameStretchPoint = revisionMark.RevisionFrameStretchPoints[i];

                            // Если предыдущий тип рамки = None
                            if (!frameStretchPoint.Equals(revisionMark.LeaderPoints[i]))
                            {
                                frameStretchPoint += Vector3d.XAxis * 30 * curViewUnitSize;
                            }

                            grips.Add(new RevisionMarkFrameStretchGrip(revisionMark, i)
                            {
                                GripPoint = frameStretchPoint
                            });
                        }
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

        try
        {
            if (IsApplicable(entity))
            {
                foreach (var gripData in grips)
                {
                    if (gripData is RevisionMarkVertexGrip vertexGrip)
                    {
                        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkGripPointOverrule; metod: MoveGripPointsAt");

                        var revisionMark = vertexGrip.RevisionMark;

                        if (vertexGrip.GripIndex == 0)
                        {
                            ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                            revisionMark.InsertionPoint = vertexGrip.GripPoint + offset;
                        }

                        // Вот тут происходит перерисовка примитивов внутри блока
                        revisionMark.UpdateEntities();
                        revisionMark.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is RevisionMarkAddLeaderGrip addLeaderGrip)
                    {
                        addLeaderGrip.NewPoint = addLeaderGrip.GripPoint + offset;
                    }
                    else if (gripData is RevisionMarkLeaderMoveGrip moveLeaderGrip)
                    {
                        moveLeaderGrip.NewPoint = moveLeaderGrip.GripPoint + offset;
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