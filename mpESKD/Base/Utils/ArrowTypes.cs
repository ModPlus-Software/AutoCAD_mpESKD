namespace mpESKD.Base.Utils;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

public class ArrowTypes
{

    private Vector3d _mainNormal;
    private double _arrowSize;
    private double _scale;
    #region Arrows

    public ArrowTypes(Vector3d mainNormal, double arrowSize, double scale)
    {
        _mainNormal = mainNormal;
        _arrowSize = arrowSize;
        _scale = scale;
    }

    public Polyline CreateResectionArrow(Point3d arrowPoint, double plineWidth)
    {
        var vector = new Vector3d(0, 0, 1);

        var tmpPoint = arrowPoint - (_mainNormal * _arrowSize / 2 * _scale);
        var startPoint = tmpPoint.RotateBy(45.DegreeToRadian(), vector, arrowPoint);
        var endPoint = tmpPoint.RotateBy(225.DegreeToRadian(), vector, arrowPoint);

        var pline = new Polyline(2);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, plineWidth, plineWidth);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, plineWidth, plineWidth);

        return pline;
    }

    public Polyline CreateAngleArrow(Point3d arrowPoint, int angle, bool closed)
    {
        var vector = new Vector3d(0, 0, 1);
        var tmpPoint = arrowPoint + (_mainNormal * _arrowSize * _scale);
        var startPoint = tmpPoint.RotateBy(angle.DegreeToRadian(), vector, arrowPoint);
        var endPoint = tmpPoint.RotateBy((-1) * angle.DegreeToRadian(), vector, arrowPoint);

        var pline = new Polyline(3);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(arrowPoint), 0, 0, 0);
        pline.AddVertexAt(2, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, 0, 0);

        pline.Closed = closed;

        return pline;
    }

    public Polyline CreateHalfArrow(Point3d arrowPoint)
    {
        var vector = new Vector3d(0, 0, 1);
        var arrowEndPoint = arrowPoint + (_mainNormal * _arrowSize * _scale);
        var endPoint = arrowEndPoint.RotateBy(10.DegreeToRadian(), vector, arrowPoint);

        var pline = new Polyline(3);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(arrowPoint), 0, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(arrowEndPoint), 0, 0, 0);
        pline.AddVertexAt(2, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, 0, 0);
        pline.Closed = true;

        return pline;
    }

    public Hatch CreateArrowHatch(Polyline pline)
    {
        var vertexCollection = new Point2dCollection();
        for (var index = 0; index < pline.NumberOfVertices; ++index)
        {
            vertexCollection.Add(pline.GetPoint2dAt(index));
        }

        vertexCollection.Add(pline.GetPoint2dAt(0));
        var bulgeCollection = new DoubleCollection()
        {
            0.0, 0.0, 0.0
        };

        return CreateHatch(vertexCollection, bulgeCollection);
    }

    public Polyline CreatePointArrow(Point3d arrowPoint)
    {
        var startPoint = arrowPoint - (_arrowSize / 4 * _mainNormal * _scale);
        var endPoint = arrowPoint + (_arrowSize/ 4 * _mainNormal * _scale);

        var pline = new Polyline(2);
        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 1, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 1, 0, 0);
        pline.Closed = true;

        return pline;
    }

    public Hatch CreatePointHatch(Polyline pline)
    {
        var vertexCollection = new Point2dCollection();
        for (var index = 0; index < pline.NumberOfVertices; ++index)
        {
            vertexCollection.Add(pline.GetPoint2dAt(index));
        }

        vertexCollection.Add(pline.GetPoint2dAt(0));
        var bulgeCollection = new DoubleCollection()
        {
            1.0, 1.0
        };

        return CreateHatch(vertexCollection, bulgeCollection);
    }

    public Hatch CreateHatch(Point2dCollection vertexCollection, DoubleCollection bulgeCollection)
    {
        var hatch = new Hatch();
        hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
        hatch.AppendLoop(HatchLoopTypes.Default, vertexCollection, bulgeCollection);

        return hatch;
    }

    #endregion
}