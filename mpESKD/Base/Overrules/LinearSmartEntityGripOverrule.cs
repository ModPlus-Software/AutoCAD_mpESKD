namespace mpESKD.Base.Overrules;

using System.Collections.Generic;
using System.Linq;
using Abstractions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Grips;
using ModPlusAPI.Windows;
using Utils;

/// <inheritdoc />
public class LinearSmartEntityGripOverrule<TEntity> : BaseSmartEntityGripOverrule<TEntity>
    where TEntity : SmartEntity, ILinearEntity
{
    /// <inheritdoc />
    public override void GetGripPoints(
        Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
    {
        try
        {
            if (IsApplicable(entity))
            {
                // Удаляю все ручки - это удалит ручку вставки блока
                grips.Clear();

                var groundLine = EntityReaderService.Instance.GetFromEntity<TEntity>(entity);
                if (groundLine != null)
                {
                    foreach (var grip in GetLinearEntityGeneralGrips(groundLine, curViewUnitSize).Reverse())
                    {
                        if (grip is LinearEntityRemoveVertexGrip &&
                            grips.OfType<LinearEntityRemoveVertexGrip>().OrderBy(g => g.GripIndex).Any(g => g.GripPoint.DistanceTo(grip.GripPoint) == 0.0))
                        {
                            continue;
                        }

                        grips.Add(grip);
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
                    if (gripData is LinearEntityVertexGrip vertexGrip)
                    {
                        var linearEntity = (SmartLinearEntity)vertexGrip.SmartEntity;
                        var vertexesPoints = linearEntity.GetAllPoints();
                        var scale = linearEntity.GetFullScale();
                        var minDistance = linearEntity.MinDistanceBetweenPoints * scale;
                        var newPoint = vertexGrip.GripPoint + offset;

                        Point3d? previousGrip = null;
                        Point3d? nextGrip = null;

                        if (vertexGrip.GripIndex == 0)
                        {
                            nextGrip = vertexesPoints[1];
                        }
                        else if (vertexGrip.GripIndex == vertexesPoints.Count - 1)
                        {
                            previousGrip = vertexesPoints[vertexGrip.GripIndex - 1];
                        }
                        else
                        {
                            previousGrip = vertexesPoints[vertexGrip.GripIndex - 1];
                            nextGrip = vertexesPoints[vertexGrip.GripIndex + 1];
                        }

                        if (nextGrip != null && newPoint.DistanceTo(nextGrip.Value) < minDistance)
                        {
                            newPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                                nextGrip.Value, 
                                newPoint, 
                                nextGrip.Value,
                                minDistance);
                        }
                        else if (previousGrip != null && newPoint.DistanceTo(previousGrip.Value) < minDistance)
                        {
                            newPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                                previousGrip.Value, 
                                newPoint, 
                                previousGrip.Value, 
                                minDistance);
                        }

                        vertexGrip.NewPoint = newPoint;
                    }
                    else if (gripData is LinearEntityAddVertexGrip addVertexGrip)
                    {
                        var linearEntity = (SmartLinearEntity)addVertexGrip.SmartEntity;
                        var scale = linearEntity.GetFullScale();
                        var minDistance = linearEntity.MinDistanceBetweenPoints * scale;
                        var newPoint = addVertexGrip.GripPoint + offset;
                        var previousGrip = addVertexGrip.GripLeftPoint;
                        var nextGrip = addVertexGrip.GripRightPoint;

                        if (nextGrip.HasValue && newPoint.DistanceTo(nextGrip.Value) < minDistance)
                        {
                            newPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                                nextGrip.Value, 
                                newPoint, 
                                nextGrip.Value, 
                                minDistance);
                        }
                        else if (previousGrip.HasValue && newPoint.DistanceTo(previousGrip.Value) < minDistance)
                        {
                            newPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                                previousGrip.Value, 
                                newPoint, 
                                previousGrip.Value, 
                                minDistance);
                        }

                        addVertexGrip.NewPoint = newPoint;
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

    /// <summary>
    /// Возвращает стандартные ручки для линейного интеллектуального объекта:
    /// ручки вершин, добавить вершину, удалить вершину, реверс объекта
    /// </summary>
    /// <param name="linearEntity">Линейный интеллектуальный объекты</param>
    /// <param name="curViewUnitSize">Размер единиц текущего вида</param>
    private static IEnumerable<SmartEntityGripData> GetLinearEntityGeneralGrips(
        ILinearEntity linearEntity, double curViewUnitSize)
    {
        var smartEntity = (SmartEntity)linearEntity;

        // Если средних точек нет, значит линия состоит всего из двух точек
        // в этом случае не нужно добавлять точки удаления крайних вершин

        // insertion (start) grip
        var vertexGrip = new LinearEntityVertexGrip(smartEntity, 0)
        {
            GripPoint = linearEntity.InsertionPoint
        };
        yield return vertexGrip;

        if (linearEntity.MiddlePoints.Any())
        {
            var removeVertexGrip = new LinearEntityRemoveVertexGrip(smartEntity, 0)
            {
                GripPoint = linearEntity.InsertionPoint - (Vector3d.YAxis * 20 * curViewUnitSize)
            };
            yield return removeVertexGrip;
        }

        // middle points
        for (var index = 0; index < linearEntity.MiddlePoints.Count; index++)
        {
            vertexGrip = new LinearEntityVertexGrip(smartEntity, index + 1)
            {
                GripPoint = linearEntity.MiddlePoints[index]
            };
            yield return vertexGrip;

            var removeVertexGrip = new LinearEntityRemoveVertexGrip(smartEntity, index + 1)
            {
                GripPoint = linearEntity.MiddlePoints[index] - (Vector3d.YAxis * 20 * curViewUnitSize)
            };
            yield return removeVertexGrip;
        }

        // end point
        vertexGrip = new LinearEntityVertexGrip(smartEntity, linearEntity.MiddlePoints.Count + 1)
        {
            GripPoint = linearEntity.EndPoint
        };
        yield return vertexGrip;

        if (linearEntity.MiddlePoints.Any())
        {
            var removeVertexGrip = new LinearEntityRemoveVertexGrip(smartEntity, linearEntity.MiddlePoints.Count + 1)
            {
                GripPoint = linearEntity.EndPoint - (Vector3d.YAxis * 20 * curViewUnitSize)
            };
            yield return removeVertexGrip;
        }

        #region AddVertex grips

        // add vertex grips
        for (var i = 0; i < linearEntity.MiddlePoints.Count; i++)
        {
            if (i == 0)
            {
                var addVertexGrip = new LinearEntityAddVertexGrip(
                    smartEntity,
                    linearEntity.InsertionPoint, linearEntity.MiddlePoints[i])
                {
                    GripPoint = GeometryUtils.GetMiddlePoint3d(linearEntity.InsertionPoint, linearEntity.MiddlePoints[i])
                };
                yield return addVertexGrip;
            }
            else
            {
                var addVertexGrip = new LinearEntityAddVertexGrip(
                    smartEntity,
                    linearEntity.MiddlePoints[i - 1], linearEntity.MiddlePoints[i])
                {
                    GripPoint = GeometryUtils.GetMiddlePoint3d(linearEntity.MiddlePoints[i - 1], linearEntity.MiddlePoints[i])
                };
                yield return addVertexGrip;
            }

            // last segment
            if (i == linearEntity.MiddlePoints.Count - 1)
            {
                var addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, linearEntity.MiddlePoints[i], linearEntity.EndPoint)
                {
                    GripPoint = GeometryUtils.GetMiddlePoint3d(linearEntity.MiddlePoints[i], linearEntity.EndPoint)
                };
                yield return addVertexGrip;
            }
        }

        {
            if (linearEntity.MiddlePoints.Any())
            {
                var addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, linearEntity.EndPoint, null)
                {
                    GripPoint = linearEntity.EndPoint +
                                ((linearEntity.EndPoint - linearEntity.MiddlePoints.Last()).GetNormal() * 40 * curViewUnitSize)
                };
                yield return addVertexGrip;

                addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, null, linearEntity.InsertionPoint)
                {
                    GripPoint = linearEntity.InsertionPoint + ((linearEntity.InsertionPoint - linearEntity.MiddlePoints.First()).GetNormal() * 40 * curViewUnitSize)
                };
                yield return addVertexGrip;
            }
            else
            {
                // Если остается только одна точка (точка вставки и точка конца совпадают),
                // ручки добавления вершин не создаются
                if (!linearEntity.InsertionPoint.Equals(linearEntity.EndPoint))
                {
                    var addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, linearEntity.EndPoint, null)
                    {
                        GripPoint = linearEntity.EndPoint - ((linearEntity.InsertionPoint - linearEntity.EndPoint).GetNormal() * 40 * curViewUnitSize)
                    };
                    yield return addVertexGrip;

                    addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, null, linearEntity.EndPoint)
                    {
                        GripPoint = linearEntity.InsertionPoint - ((linearEntity.EndPoint - linearEntity.InsertionPoint).GetNormal() * 40 * curViewUnitSize)
                    };
                    yield return addVertexGrip;

                    addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, linearEntity.InsertionPoint, linearEntity.EndPoint)
                    {
                        GripPoint = GeometryUtils.GetMiddlePoint3d(linearEntity.InsertionPoint, linearEntity.EndPoint)
                    };
                    yield return addVertexGrip;
                }
            }
        }

        #endregion

        // Если остается только одна точка (точка вставки и точка конца совпадают),
        // клик по ручке реверса приведет к Fatal Error.
        // поэтому если точек всего 2 и точка вставки и точка конца совпадают, ручка реверса не создается
        if (!(linearEntity.MiddlePoints.Count == 0 && linearEntity.InsertionPoint.Equals(linearEntity.EndPoint)))
        {
            var reverseGrip = new LinearEntityReverseGrip(smartEntity)
            {
                GripPoint = linearEntity.InsertionPoint + (Vector3d.YAxis * 20 * curViewUnitSize)
            };
            yield return reverseGrip;

            reverseGrip = new LinearEntityReverseGrip(smartEntity)
            {
                GripPoint = linearEntity.EndPoint + (Vector3d.YAxis * 20 * curViewUnitSize)
            };
            yield return reverseGrip;
        }
    }
}