namespace mpESKD.Base.Utils;

using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

public class ArrowBuilder
{
    private readonly Vector3d _mainNormal;
    private readonly double _arrowSize;
    private readonly double _scale;
    
    /// <summary>
    /// Класс для создания стрелок выноски
    /// </summary>
    /// <param name="mainNormal">Нормаль по которой строится стрелка</param>
    /// <param name="arrowSize">Размер стрелки</param>
    /// <param name="scale">Масштаб объекта</param>
    public ArrowBuilder(Vector3d mainNormal, double arrowSize, double scale)
    {
        _mainNormal = mainNormal;
        _arrowSize = arrowSize;
        _scale = scale;
    }

    /// <summary>
    /// Метод создающий стрелки для выносок, в зависимости от типа выбранной стрелки
    /// </summary>
    /// <param name="arrowType">Тип стрелки</param>
    /// <param name="point3d">Точка расположения стрелки</param>
    /// <param name="hatches">Штриховка стрелки, если стрелка состоит из штриховки</param>
    /// <param name="plines">Полилиния стрелки, если стрелка состоит из полилинии </param>
    public void BuildArrow(LeaderEndType arrowType, Point3d point3d, List<Hatch> hatches, List<Polyline> plines)
    {
        switch (arrowType)
        {
            case LeaderEndType.None:
                break;
            case LeaderEndType.HalfArrow:
                hatches.Add(CreateArrowHatch(CreateHalfArrow(point3d)));
                break;
            case LeaderEndType.Point:
                hatches.Add(CreatePointHatch(CreatePointArrow(point3d)));
                break;
            case LeaderEndType.Section:
                plines.Add(CreateResectionArrow(point3d, 0));
                break;
            case LeaderEndType.Resection:
                plines.Add(CreateResectionArrow(point3d, 0.3));
                break;
            case LeaderEndType.Angle:
                plines.Add(CreateAngleArrow(point3d, 45, false));
                break;
            case LeaderEndType.Arrow:
                hatches.Add(CreateArrowHatch(CreateAngleArrow(point3d, 10, true)));
                break;
            case LeaderEndType.OpenArrow:
                plines.Add(CreateAngleArrow(point3d, 10, false));
                break;
            case LeaderEndType.ClosedArrow:
                plines.Add(CreateAngleArrow(point3d, 10, true));
                break;
        }
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

    private Polyline CreateAngleArrow(Point3d arrowPoint, int angle, bool closed)
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

    private Polyline CreateHalfArrow(Point3d arrowPoint)
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

    private Hatch CreateArrowHatch(Polyline pline)
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

    private Polyline CreatePointArrow(Point3d arrowPoint)
    {
        var startPoint = arrowPoint - (_arrowSize / 4 * _mainNormal * _scale);
        var endPoint = arrowPoint + (_arrowSize/ 4 * _mainNormal * _scale);

        var pline = new Polyline(2);
        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 1, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 1, 0, 0);
        pline.Closed = true;

        return pline;
    }

    private Hatch CreatePointHatch(Polyline pline)
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

    private Hatch CreateHatch(Point2dCollection vertexCollection, DoubleCollection bulgeCollection)
    {
        var hatch = new Hatch();
        hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
        hatch.AppendLoop(HatchLoopTypes.Default, vertexCollection, bulgeCollection);

        return hatch;
    }
}