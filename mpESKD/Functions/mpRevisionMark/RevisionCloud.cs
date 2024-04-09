namespace mpESKD.Functions.mpRevisionMark; 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using mpESKD.Base.Utils;

internal static class RevisionCloud
{
    /// <summary>
    /// Список точек сегмента 
    /// </summary>
    /// <param name="segmentStartPoint"></param>
    /// <param name="segmentEndPoint"></param>
    /// <param name="arcDistance"></param>
    /// <returns></returns>
    internal static List<Point2d> GetArcPointsOfSegment(
        Point2d segmentStartPoint,
        Point2d segmentEndPoint,
        double arcDistance)
    {
        
        var result = new List<Point2d>{segmentStartPoint};

        var segmentLength = segmentStartPoint.GetDistanceTo(segmentEndPoint);
        AcadUtils.WriteMessageInDebug($"segmentLength: {segmentLength}");

        var segmentArcCount = (int)(segmentLength / arcDistance);
        AcadUtils.WriteMessageInDebug($"segmentArcCount: {segmentArcCount}");

        //var isExactMultiple = (segmentLength % (distanceCloudArc * 2)) == 0 ? true : false;

        if (segmentArcCount > 1)
        {
            // AcadUtils.WriteMessageInDebug($"segmentArcCount: {segmentArcCount}");

            for (int j = 0; j < segmentArcCount - 1; j++)
            {
                AcadUtils.WriteMessageInDebug($"segmentArcCount > 1: j={j}");

                var nextPoint = GeometryUtils.Point3dAtDirection(
                    segmentEndPoint.ToPoint3d(),
                    segmentStartPoint.ToPoint3d(), 
                    arcDistance * (j + 1));

                result.Add(nextPoint.ToPoint2d());
            }

            result.Add(segmentEndPoint);
        }
        else
        {
            result.Add(segmentEndPoint);
        }

        // return new List<Point2d>{segmentStartPoint, segmentEndPoint};
        return result;
    }
}