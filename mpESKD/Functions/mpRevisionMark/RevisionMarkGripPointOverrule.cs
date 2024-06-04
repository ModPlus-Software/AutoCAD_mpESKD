namespace mpESKD.Functions.mpRevisionMark;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;

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
                    // Ручка перемещения марки изменения (номера с рамкой, примечанием и полкой примечания)
                    var vertexGrip = new RevisionMarkVertexGrip(revisionMark, 0)
                    {
                        GripPoint = revisionMark.InsertionPoint
                    };
                    grips.Add(vertexGrip);

                    // Ручка для создания выноски
                    Point3d revisionMarkAddLeaderGripPoint;

                    if (!string.IsNullOrEmpty(revisionMark.Note))
                    {
                        revisionMarkAddLeaderGripPoint = new Point3d(
                            revisionMark.InsertionPoint.X,
                            revisionMark.InsertionPoint.Y - (3 * revisionMark.GetFullScale()),
                            revisionMark.InsertionPoint.Z);
                    }
                    else
                    {
                        revisionMarkAddLeaderGripPoint = new Point3d(
                            revisionMark.InsertionPoint.X + revisionMark.FrameRevisionTextPoints[3].X,
                            revisionMark.InsertionPoint.Y + revisionMark.FrameRevisionTextPoints[3].Y,
                            revisionMark.InsertionPoint.Z);
                    }

                    grips.Add(new RevisionMarkAddLeaderGrip(revisionMark)
                    {
                        GripPoint = revisionMarkAddLeaderGripPoint
                    });

                    // Ручка для зеркалирования полки примечания
                    if (!string.IsNullOrEmpty(revisionMark.Note))
                    {
                        grips.Add(new RevisionMarkShelfPositionGrip(revisionMark)
                        {
                            GripPoint = new Point3d(
                                revisionMark.InsertionPoint.X,
                                revisionMark.InsertionPoint.Y + revisionMark.FrameRevisionTextPoints[3].Y,
                                revisionMark.InsertionPoint.Z)
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

                        // Если нет рамки, то ручка для растягивания рамки не создается
                         if (revisionMark.RevisionFrameTypes[i] != 0)
                        {
                            grips.Add(new RevisionMarkFrameStretchGrip(revisionMark, i)
                            {
                                GripPoint = revisionMark.RevisionFrameStretchPoints[i]
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
                        var revisionMark = vertexGrip.RevisionMark;

                        if (vertexGrip.GripIndex == 0)
                        {
                            ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                            revisionMark.InsertionPoint = vertexGrip.GripPoint + offset;
                        }

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
                    else if (gripData is RevisionMarkFrameStretchGrip frameStretchGrip)
                    {
                        frameStretchGrip.NewPoint = frameStretchGrip.GripPoint + offset;
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