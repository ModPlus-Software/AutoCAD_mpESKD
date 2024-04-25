namespace mpESKD.Functions.mpRevisionMark;

using mpESKD.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;


public static class RevisionFrameBuilder
{
    /*
    /// <summary>
    /// Получение точек для построения базовой полилинии
    /// </summary>
    private void PointsToCreatePolyline(double scale, Point3d insertionPoint, Point3d endPoint)
    {
        if (RevisionFrameType == RevisionFrameType.Round)
        {
            CornerRadiusVisibilityDependency = false;
            _frameRevisionPolyline = null;

            try
            {
                var radius = endPoint.DistanceTo(insertionPoint);
                if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0.0)
                    radius = MinDistanceBetweenPoints * scale;

                if (!IsRevisionCloud)
                {
                    _frameRevisionCircle = new Circle
                    {
                        Center = insertionPoint,
                        Radius = radius
                    };
                }
                else
                {
                    _frameRevisionCircle = null;
                    var bevelBulge = Math.Tan((90 / 4).DegreeToRadian());

                    var cloudArcPoints = RevisionCloud.GetArcPointsOfSegment(
                        insertionPoint,
                        radius,
                        RevisionCloudArcLength * scale);

                    cloudArcPoints.Add(insertionPoint.ToPoint2d() + (Vector2d.XAxis * radius));

                    _frameRevisionPolyline = new Polyline(cloudArcPoints.Count);

                    for (int i = 0; i < cloudArcPoints.Count; i++)
                    {
                        _frameRevisionPolyline.AddVertexAt(i, cloudArcPoints[i], bevelBulge, 0, 0);
                    }
                }
            }
            catch
            {
                _frameRevisionCircle = null;
                _frameRevisionPolyline = null;
            }
        }
        else
        {
            CornerRadiusVisibilityDependency = true;
            _frameRevisionCircle = null;

            var width = Math.Abs(endPoint.X - insertionPoint.X);
            var height = Math.Abs(endPoint.Y - insertionPoint.Y);
            if (width == 0)
            {
                width = MinDistanceBetweenPoints * scale;
            }

            if (height == 0)
            {
                height = MinDistanceBetweenPoints * scale;
            }

            var cornerRadius = CornerRadius * scale;

            if (((width * 2) - (cornerRadius * 2)) < (1 * scale) ||
                ((height * 2) - (cornerRadius * 2)) < (1 * scale))
            {
                var minSize = Math.Min(width * 2, height * 2);
                cornerRadius = (int)((minSize - (1 * scale)) / 2);
            }

            var points = new[]
            {
                new Point2d(insertionPoint.X - width + cornerRadius, insertionPoint.Y - height),
                new Point2d(insertionPoint.X - width, insertionPoint.Y - height + cornerRadius),
                new Point2d(insertionPoint.X - width, insertionPoint.Y + height - cornerRadius),
                new Point2d(insertionPoint.X - width + cornerRadius, insertionPoint.Y + height),
                new Point2d(insertionPoint.X + width - cornerRadius, insertionPoint.Y + height),
                new Point2d(insertionPoint.X + width, insertionPoint.Y + height - cornerRadius),
                new Point2d(insertionPoint.X + width, insertionPoint.Y - height + cornerRadius),
                new Point2d(insertionPoint.X + width - cornerRadius, insertionPoint.Y - height)
            };

            var bevelBulge = Math.Tan((90 / 4).DegreeToRadian());

            if (!IsRevisionCloud)
            {
                var bulges = new[]
                {
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0
                };

                _frameRevisionPolyline = new Polyline(points.Length);

                for (var i = 0; i < points.Length; i++)
                {
                    _frameRevisionPolyline.AddVertexAt(i, points[i], bulges[i], 0.0, 0.0);
                }

                _frameRevisionPolyline.Closed = true;
            }
            else
            {
                _frameRevisionPolyline = null;
                var arcFramePoints = new List<Point2d>();

                for (int i = 0; i < points.Length - 1; i++)
                {
                    var segmentStartPoint = points[i];
                    var segmentEndPoint = points[i + 1];

                    arcFramePoints.AddRange(RevisionCloud.GetArcPointsOfSegment(
                        segmentStartPoint,
                        segmentEndPoint,
                        RevisionCloudArcLength * scale));
                }

                arcFramePoints.AddRange(RevisionCloud.GetArcPointsOfSegment(
                    arcFramePoints.Last(),
                    arcFramePoints.First(),
                    RevisionCloudArcLength * scale));

                var arcFramePointsDistinct = arcFramePoints.Skip(1).Distinct();

                arcFramePoints = Enumerable.Repeat(arcFramePoints[0], 1)
                    .Concat(arcFramePointsDistinct).ToList();

                var correctFramePoints = new List<Point2d>();
                var isContinue = false;

                for (int i = 0; i < arcFramePoints.Count - 1; i++)
                {
                    if (isContinue)
                    {
                        isContinue = false;
                        continue;
                    }

                    var currentPoint = arcFramePoints[i];
                    var nextPoint = arcFramePoints[i + 1];

                    var distance = currentPoint.GetDistanceTo(nextPoint);

                    if (distance < 2 * RevisionCloudArcLength)
                    {
                        var middlePoint = GeometryUtils.GetMiddlePoint2d(currentPoint, nextPoint);
                        correctFramePoints.Add(middlePoint);
                        isContinue = true;
                    }
                    else
                    {
                        correctFramePoints.Add(arcFramePoints[i]);
                        isContinue = true;
                    }
                }

                correctFramePoints.Add(correctFramePoints[0]);

                _frameRevisionPolyline = new Polyline(correctFramePoints.Count);

                for (int i = 0; i < correctFramePoints.Count; i++)
                {
                    _frameRevisionPolyline.AddVertexAt(i, correctFramePoints[i], -bevelBulge, 0, 0);
                }
            }
        }
    }

    */

    /// <summary>
    /// Построение области ревизии
    /// </summary>
    /// <param name="revisionMark"></param>
    /// <param name="insertionPoint"></param>
    /// <param name="endPoint"></param>
    /// <param name="frameType"></param>
    /// <param name="revisionFramesAsPolylines"></param>
    /// <param name="revisionFramesAsCircles"></param>
    /// <param name="scale"></param>
    public static void CreateRevisionFrame(
        this RevisionMark revisionMark, 
        Point3d insertionPoint,
        Point3d endPoint, 
        RevisionFrameType frameType, 
        List<Polyline> revisionFramesAsPolylines,
        List<Circle> revisionFramesAsCircles,
        double scale)
    {
        if (frameType == RevisionFrameType.Round)
        {
            revisionMark.CornerRadiusVisibilityDependency = false;

                var radius = endPoint.DistanceTo(insertionPoint);
                if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0.0)
                    radius = revisionMark.MinDistanceBetweenPoints * scale;

                if (!revisionMark.IsRevisionCloud)
                {
                    revisionFramesAsCircles.Add(
                        new Circle
                        {
                            Center = insertionPoint,
                            Radius = radius
                        });
                }
                else
                {
                    var bevelBulge = Math.Tan((90 / 4).DegreeToRadian());

                    var cloudArcPoints = RevisionCloud.GetArcPointsOfSegment(
                        insertionPoint,
                        radius,
                        revisionMark.RevisionCloudArcLength * scale);

                    cloudArcPoints.Add(insertionPoint.ToPoint2d() + (Vector2d.XAxis * radius));

                    var frameCloudPolyline = new Polyline(cloudArcPoints.Count);

                    for (int i = 0; i < cloudArcPoints.Count; i++)
                    {
                        frameCloudPolyline.AddVertexAt(i, cloudArcPoints[i], bevelBulge, 0, 0);
                    }

                    revisionFramesAsPolylines.Add(frameCloudPolyline);
                }
        }
        else if (frameType == RevisionFrameType.Rectangular)
        {
            revisionMark.CornerRadiusVisibilityDependency = true;

            var width = Math.Abs(endPoint.X - insertionPoint.X);
            var height = Math.Abs(endPoint.Y - insertionPoint.Y);
            if (width == 0)
            {
                width = revisionMark.MinDistanceBetweenPoints * scale;
            }

            if (height == 0)
            {
                height = revisionMark.MinDistanceBetweenPoints * scale;
            }

            var cornerRadius = revisionMark.CornerRadius * scale;

            if (((width * 2) - (cornerRadius * 2)) < (1 * scale) ||
                ((height * 2) - (cornerRadius * 2)) < (1 * scale))
            {
                var minSize = Math.Min(width * 2, height * 2);
                cornerRadius = (int)((minSize - (1 * scale)) / 2);
            }

            var points = new[]
            {
                new Point2d(insertionPoint.X - width + cornerRadius, insertionPoint.Y - height),
                new Point2d(insertionPoint.X - width, insertionPoint.Y - height + cornerRadius),
                new Point2d(insertionPoint.X - width, insertionPoint.Y + height - cornerRadius),
                new Point2d(insertionPoint.X - width + cornerRadius, insertionPoint.Y + height),
                new Point2d(insertionPoint.X + width - cornerRadius, insertionPoint.Y + height),
                new Point2d(insertionPoint.X + width, insertionPoint.Y + height - cornerRadius),
                new Point2d(insertionPoint.X + width, insertionPoint.Y - height + cornerRadius),
                new Point2d(insertionPoint.X + width - cornerRadius, insertionPoint.Y - height)
            };

            var bevelBulge = Math.Tan((90 / 4).DegreeToRadian());

            if (!revisionMark.IsRevisionCloud)
            {
                var bulges = new[]
                {
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0
                };

                var frameCloudPolyline = new Polyline(points.Length);

                for (var i = 0; i < points.Length; i++)
                {
                    frameCloudPolyline.AddVertexAt(i, points[i], bulges[i], 0.0, 0.0);
                }

                frameCloudPolyline.Closed = true;

                revisionFramesAsPolylines.Add(frameCloudPolyline);
            }
            else
            {
                var arcFramePoints = new List<Point2d>();

                for (int i = 0; i < points.Length - 1; i++)
                {
                    var segmentStartPoint = points[i];
                    var segmentEndPoint = points[i + 1];

                    arcFramePoints.AddRange(RevisionCloud.GetArcPointsOfSegment(
                        segmentStartPoint,
                        segmentEndPoint,
                        revisionMark.RevisionCloudArcLength * scale));
                }

                arcFramePoints.AddRange(RevisionCloud.GetArcPointsOfSegment(
                    arcFramePoints.Last(),
                    arcFramePoints.First(),
                    revisionMark.RevisionCloudArcLength * scale));

                var arcFramePointsDistinct = arcFramePoints.Skip(1).Distinct();

                arcFramePoints = Enumerable.Repeat(arcFramePoints[0], 1)
                    .Concat(arcFramePointsDistinct).ToList();

                var correctFramePoints = new List<Point2d>();
                var isContinue = false;

                for (int i = 0; i < arcFramePoints.Count - 1; i++)
                {
                    if (isContinue)
                    {
                        isContinue = false;
                        continue;
                    }

                    var currentPoint = arcFramePoints[i];
                    var nextPoint = arcFramePoints[i + 1];

                    var distance = currentPoint.GetDistanceTo(nextPoint);

                    if (distance < 2 * revisionMark.RevisionCloudArcLength)
                    {
                        var middlePoint = GeometryUtils.GetMiddlePoint2d(currentPoint, nextPoint);
                        correctFramePoints.Add(middlePoint);
                        isContinue = true;
                    }
                    else
                    {
                        correctFramePoints.Add(arcFramePoints[i]);
                        isContinue = true;
                    }
                }

                correctFramePoints.Add(correctFramePoints[0]);

                var frameCloudPolyline = new Polyline(correctFramePoints.Count);

                for (int i = 0; i < correctFramePoints.Count; i++)
                {
                    frameCloudPolyline.AddVertexAt(i, correctFramePoints[i], -bevelBulge, 0, 0);
                }

                revisionFramesAsPolylines.Add(frameCloudPolyline);
            }
        }
    }
}