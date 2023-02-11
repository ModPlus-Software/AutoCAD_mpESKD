using mpESKD.Base.Utils;

namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Overrules.Grips;
using System;
using System.Collections.Generic;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class CrestedLeaderGripPointOverrule : BaseSmartEntityGripOverrule<CrestedLeader>
{
    /// <inheritdoc />
    public override void GetGripPoints(
        Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
    {
        try
        {
            // Проверка дополнительных условий
            if (IsApplicable(entity))
            {
                // Чтобы "отключить" точку вставки блока, нужно получить сначала блок
                // Т.к. мы точно знаем для какого примитива переопределение, то получаем блок:
                var blkRef = (BlockReference)entity;

                // Удаляем стандартную ручку позиции блока (точки вставки)
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

                // Получаем экземпляр класса, который описывает как должен выглядеть примитив
                // т.е. правила построения графики внутри блока
                // Информация собирается по XData и свойствам самого блока
                var crestedLeader = EntityReaderService.Instance.GetFromEntity<CrestedLeader>(entity);

                if (crestedLeader != null)
                {
                    if (crestedLeader.ArrowPoints.Count == 1)
                    {
                        //// Получаем ручку на первой точке
                        var gp = new CrestedLeaderVertexGrip(crestedLeader, 1, (BlockReference)entity)
                        {
                            GripPoint = crestedLeader.InsertionPoint
                        };
                        grips.Add(gp);
                    }

                    var shelfStretchPoint = crestedLeader.InsertionPoint.X +
                                            (crestedLeader.InsertionPoint.DistanceTo(crestedLeader.EndPoint) / 2);
                    // Получаем ручку по середине
                    if (crestedLeader.ArrowPoints.Count > 1)
                    {
                        var gp = new CrestedLeaderVertexGrip(crestedLeader, 0, (BlockReference)entity)
                        {
                            GripPoint = new Point3d(shelfStretchPoint, crestedLeader.InsertionPoint.Y, 0)
                        };
                        grips.Add(gp);
                    }

                    // получаем ручку для создания стрелки
                    grips.Add(new CrestedLeaderArrowAddGrip(crestedLeader, (BlockReference)entity)
                    {
                        GripPoint = new Point3d(crestedLeader.EndPoint.X, crestedLeader.InsertionPoint.Y, 0) - Vector3d.XAxis * 20 * curViewUnitSize
                    });

                    if (crestedLeader.ArrowPoints.Count >= 1)
                    {
                        //// ручки удаления стрелки с insertionPoint
                        //grips.Add(new CrestedLeaderArrowRemoveGrip(crestedLeader, 4, (BlockReference)entity)
                        //{
                        //    GripPoint = crestedLeader.InsertionPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                        //});

                        var normal = (crestedLeader.EndPoint - crestedLeader.InsertionPoint).GetNormal();
                        for (var i = 0; i < crestedLeader.ArrowPoints.Count; i++)
                        {
                            var gripPoint = crestedLeader.ArrowPoints[i];

                            // ручки переноса стрелки
                            grips.Add(new CrestedLeaderArrowMoveGrip(crestedLeader, i, (BlockReference)entity)
                            {
                                GripPoint = gripPoint
                            });

                            if (crestedLeader.ArrowPoints.Count > 1)
                            {
                                // ручки удаления выносок
                                grips.Add(new CrestedLeaderArrowRemoveGrip(crestedLeader, i + 5, (BlockReference)entity)
                                {
                                    GripPoint = gripPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                                });
                            }
                        }
                    }

                    var textIndent = crestedLeader.TextIndent;
                    var shelfLength = crestedLeader.ShelfLength;

                    if (crestedLeader.ShelfPosition == ShelfPosition.Left)
                    {
                        textIndent = -textIndent;
                        shelfLength = -shelfLength;
                    }

                    if (crestedLeader.ScaleFactorX < 0)
                    {
                        textIndent = -textIndent;
                        shelfLength = -shelfLength;
                    }

                    var arrowTypeGripPoint = crestedLeader.EndPoint + (Vector3d.XAxis * shelfLength);
                    var alignGripPoint = arrowTypeGripPoint + (Vector3d.YAxis *
                                                                (crestedLeader.MainTextHeight + crestedLeader.TextVerticalOffset) * crestedLeader.GetFullScale());
                    var shelfMoveGripPoint = crestedLeader.EndPoint + (Vector3d.XAxis * textIndent);
                    var shelfPositionGripPoint = crestedLeader.EndPoint +
                                                 (Vector3d.YAxis *
                                                 (crestedLeader.MainTextHeight + crestedLeader.TextVerticalOffset));

                    if (crestedLeader.IsRotated & !crestedLeader.IsTextAlwaysHorizontal)
                    {
                        arrowTypeGripPoint = arrowTypeGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.EndPoint);
                        alignGripPoint = alignGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.EndPoint);
                        shelfMoveGripPoint = shelfMoveGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.EndPoint);
                        shelfPositionGripPoint = shelfPositionGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.EndPoint);
                    }

                    // Получаем ручку зеркалирования полки
                    var gp1 = new CrestedLeaderShelfPositionGrip(crestedLeader)
                    {
                        GripPoint = shelfPositionGripPoint,
                        GripType = GripType.TwoArrowsLeftRight
                    };
                    grips.Add(gp1);

                    // Получаем ручку изменения полки
                    grips.Add(new CrestedLeaderShelfMoveGrip(crestedLeader, 2)
                    {
                        GripPoint = shelfMoveGripPoint
                    });

                    // ручки выбора типа стрелки
                    grips.Add(new CrestedLeaderArrowEndTypeGrip(crestedLeader, 3)
                    {
                        GripPoint = arrowTypeGripPoint
                    });

                    if ((string.IsNullOrEmpty(crestedLeader.LeaderTextValue) | string.IsNullOrEmpty(crestedLeader.LeaderTextComment))
                        | (string.IsNullOrEmpty(crestedLeader.LeaderTextValue) & string.IsNullOrEmpty(crestedLeader.LeaderTextComment)))
                        return;

                    grips.Add(new EntityTextAlignGrip(
                        crestedLeader,
                        () => crestedLeader.ValueHorizontalAlignment,
                        setAlignEntity => crestedLeader.ValueHorizontalAlignment = setAlignEntity)
                    {
                        GripPoint = alignGripPoint
                    });
                }
            }
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }

    /// <inheritdoc/>
    public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    {
        try
        {
            if (IsApplicable(entity))
            {
                foreach (var gripData in grips)
                {
                    if (gripData is CrestedLeaderVertexGrip vertexGrip)
                    {
                        var crestedLeader = vertexGrip.CrestedLeader;
                        var newPoint = vertexGrip.GripPoint + offset;
                        vertexGrip.NewPoint = offset.Y;
                        
                        crestedLeader.TempNewStretchPoint = newPoint;

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();

                    }
                    else if (gripData is CrestedLeaderShelfMoveGrip shelfMoveGrip)
                    {
                        var crestedLeader = shelfMoveGrip.CrestedLeader;
                        if (crestedLeader.ShelfPosition == ShelfPosition.Right)
                        {
                            crestedLeader.TextIndent = shelfMoveGrip.GripPoint.X - crestedLeader.EndPoint.X + offset.X;
                        }
                        else
                        {
                            crestedLeader.TextIndent = crestedLeader.EndPoint.X - shelfMoveGrip.GripPoint.X - offset.X;
                        }

                        shelfMoveGrip.NewPoint = crestedLeader.TextIndent;
                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is CrestedLeaderArrowAddGrip addLeaderGrip)
                    {

                        var crestedLeader = addLeaderGrip.CrestedLeader;
                        var newPoint = addLeaderGrip.GripPoint + offset;

                        crestedLeader.TempNewArrowPoint = newPoint;

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is CrestedLeaderArrowMoveGrip moveLeaderGrip)
                    {
                        var crestedLeader = moveLeaderGrip.CrestedLeader;
                        var newPoint = moveLeaderGrip.GripPoint + offset;

                        crestedLeader.TempNewArrowPoint = newPoint;

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();
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

    private bool IsPointBetween(Point3d point, Point3d startPt, Point3d endPt)
    {
        var segment = new LineSegment3d(startPt, endPt);
        return segment.IsOn(point);
    }

    private Point3d CreateLeadersWithArrows(Line secondLeaderLine, Intersect intersectType, Point3d arrowPoint, Vector3d mainNormal)
    {
        var templine = new Line(arrowPoint, arrowPoint + mainNormal);
        var pts = new Point3dCollection();

        secondLeaderLine.IntersectWith(templine, intersectType, pts, IntPtr.Zero, IntPtr.Zero);

        try
        {
            if (!double.IsNaN(pts[0].X))
                return pts[0];
            return default;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return default;
        }

    }


}