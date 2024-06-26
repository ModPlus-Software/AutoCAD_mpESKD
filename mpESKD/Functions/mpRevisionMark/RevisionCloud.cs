﻿namespace mpESKD.Functions.mpRevisionMark; 

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Base.Utils;

/// <summary>
/// Получение точек для построения облаков ревизий
/// </summary>
internal static class RevisionCloud
{
    /// <summary>
    /// Возвращает список точек сегмента прямоугольной области ревизии
    /// для отрисовки дуг ревизионного облака 
    /// </summary>
    /// <param name="segmentStartPoint">Начальная точка сегмента</param>
    /// <param name="segmentEndPoint">Конечная точка сегмента</param>
    /// <param name="cloudArcLength">Длина дуги ревизионного облака</param>
    /// <returns>Список точек</returns>
    internal static List<Point2d> GetArcPointsOfSegment(
        Point2d segmentStartPoint,
        Point2d segmentEndPoint,
        double cloudArcLength)
    {
        var angleCloudArc = Math.PI / 4;
        var segmentArcPoints = new List<Point2d> { segmentStartPoint };

        // Длина хорды дуги облака
         var chordLength = (2 * cloudArcLength * Math.Sin(angleCloudArc / 2)) / angleCloudArc;

        var segmentLength = segmentStartPoint.GetDistanceTo(segmentEndPoint);
        var segmentArcCount = (int)(segmentLength / chordLength);

        if (segmentArcCount > 1)
        {
            for (int j = 0; j < segmentArcCount - 1; j++)
            {
                var nextPoint = GeometryUtils.Point3dAtDirection(
                    segmentStartPoint.ToPoint3d(),
                    segmentEndPoint.ToPoint3d(),
                    chordLength * (j + 1));

                segmentArcPoints.Add(nextPoint.ToPoint2d());
            }

            segmentArcPoints.Add(segmentEndPoint);
        }
        else
        {
            segmentArcPoints.Add(segmentEndPoint);
        }

        return segmentArcPoints;
    }

    /// <summary>
    /// Возвращает список точек на круглой области ревизии
    /// для отрисовки дуг ревизионного облака 
    /// </summary>
    /// <param name="insertionPoint">Точка вставки блока</param>
    /// <param name="revisionRoundRadius">Радиус круглой области ревизии</param>
    /// <param name="cloudArcLength">Радиус дуги ревизионного облака</param>
    /// <returns>Список точек</returns>
    internal static List<Point2d> GetArcPointsOfSegment(
        Point3d insertionPoint,
        double revisionRoundRadius,
        double cloudArcLength)
    {
        var angleCloudArc = Math.PI / 4;

        // Длина хорды дуги облака
        var cloudArcChordLength = (2 * cloudArcLength * Math.Sin(angleCloudArc)) / angleCloudArc;

        // Угол, образующий хорду
        var revisionRoundChordAngle = 2 * Math.Asin(cloudArcChordLength / (2 * revisionRoundRadius));

        // Длина дуги на окружности круга ревизии
        var revisionRoundArcLength = revisionRoundChordAngle * revisionRoundRadius;

        var revisionRoundCircleLength = 2 * Math.PI * revisionRoundRadius;
        var cloudArcCount = (int)(revisionRoundCircleLength / revisionRoundArcLength);

        if (cloudArcCount < 2)
            return GetArcPointsOfSegment(
                insertionPoint, 
                revisionRoundRadius,
                revisionRoundRadius * angleCloudArc);

        var cloudArcPoints = new List<Point2d>();

        for (int i = 0; i < cloudArcCount; i++)
        {
            var angle = i * revisionRoundChordAngle;

            var normalVectorPoint = new Point2d(
                revisionRoundRadius * Math.Cos(angle),
                revisionRoundRadius * Math.Sin(angle));

            var normalVector = (normalVectorPoint - new Point2d(0,0)).GetNormal();

            var cloudArcPoint = insertionPoint.ToPoint2d() + (normalVector * revisionRoundRadius);

            cloudArcPoints.Add(cloudArcPoint);
        }

        return cloudArcPoints;
    }
}