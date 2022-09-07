namespace mpESKD.Base.Overrules;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Enums;

/// <inheritdoc />
public abstract class SmartEntityGripData : GripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SmartEntityGripData"/> class.
    /// </summary>
    protected SmartEntityGripData()
    {
        // отключение контекстного меню и возможности менять команду
        // http://help.autodesk.com/view/OARX/2018/ENU/?guid=OREF-AcDbGripData__disableModeKeywords_bool
        ModeKeywordsDisabled = true;
    }

    /// <summary>
    /// Тип ручки примитива
    /// </summary>
    public GripType GripType { get; set; }

    /// <inheritdoc />
    public override bool ViewportDraw(ViewportDraw worldDraw, ObjectId entityId, DrawType type, Point3d? imageGripPoint, int gripSizeInPixels)
    {
        var ecs = GetECS(entityId);
        var numPixelsInUnitSquare = worldDraw.Viewport.GetNumPixelsInUnitSquare(GripPoint);
        var num = gripSizeInPixels / numPixelsInUnitSquare.X;

        var point3dCollections = new Point3dCollection();
        switch (GripType)
        {
            case GripType.Point:
                point3dCollections = PointsForSquareGrip(num, ecs);
                break;
            case GripType.Plus:
                point3dCollections = PointsForPlusGrip(num, ecs);
                break;
            case GripType.Minus:
                point3dCollections = PointsForMinusGrip(num, ecs);
                break;
            case GripType.BasePoint:
                point3dCollections = PointsForSquareGrip(num, ecs);
                break;
        }

        var backupColor = worldDraw.SubEntityTraits.Color;
        var backupFillType = worldDraw.SubEntityTraits.FillType;

        // Дополнительный круг и отрезки для точки отсчета
        if (GripType == GripType.BasePoint)
        {
            worldDraw.SubEntityTraits.Color = 110;
            worldDraw.Geometry.WorldLine(GripPoint - (num * 3 * ecs.Xaxis), GripPoint + (num * 3 * ecs.Xaxis));
            worldDraw.Geometry.WorldLine(GripPoint - (num * 3 * ecs.Yaxis), GripPoint + (num * 3 * ecs.Yaxis));
            worldDraw.Geometry.Circle(GripPoint, num * 2, Vector3d.ZAxis);
        }

        worldDraw.SubEntityTraits.FillType = FillType.FillAlways;
        worldDraw.SubEntityTraits.Color = GetGripColor();
        if (GripType == GripType.TwoArrowsUpDown)
        {
            worldDraw.Geometry.Polygon(PointsForTwoArrowsGripUpDownFirstArrow(num, ecs));
            worldDraw.Geometry.Polygon(PointsForTwoArrowsGripUpDownSecondArrow(num, ecs));
        }
        else if (GripType == GripType.TwoArrowsLeftRight)
        {
            worldDraw.Geometry.Polygon(PointsForTwoArrowsGripLeftRightFirstArrow(num, ecs));
            worldDraw.Geometry.Polygon(PointsForTwoArrowsGripLeftRightSecondArrow(num, ecs));
        }
        else if (GripType == GripType.List)
        {
            worldDraw.Geometry.Polygon(PointsForListMinusGrip(num, ecs));
            worldDraw.Geometry.Polygon(PointsForListTriangle(num, ecs));
        }
        else
        {
            worldDraw.Geometry.Polygon(point3dCollections);
        }

        worldDraw.SubEntityTraits.FillType = FillType.FillNever;

        // обводка
        worldDraw.SubEntityTraits.Color = 250;
        if (GripType == GripType.TwoArrowsUpDown)
        {
            worldDraw.Geometry.Polygon(PointsForTwoArrowsGripUpDownFirstArrow(num, ecs));
            worldDraw.Geometry.Polygon(PointsForTwoArrowsGripUpDownSecondArrow(num, ecs));
        }
        else if (GripType == GripType.TwoArrowsLeftRight)
        {
            worldDraw.Geometry.Polygon(PointsForTwoArrowsGripLeftRightFirstArrow(num, ecs));
            worldDraw.Geometry.Polygon(PointsForTwoArrowsGripLeftRightSecondArrow(num, ecs));
        }
        else
        {
            worldDraw.Geometry.Polygon(point3dCollections);
        }

        // restore
        worldDraw.SubEntityTraits.Color = backupColor;
        worldDraw.SubEntityTraits.FillType = backupFillType;

        return true;
    }

    // ReSharper disable once InconsistentNaming
    private static CoordinateSystem3d GetECS(ObjectId entityId)
    {
        var coordinateSystem3D = new CoordinateSystem3d(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);
        if (!entityId.IsNull)
        {
            using (var openCloseTransaction = new OpenCloseTransaction())
            {
                var obj = (Entity)openCloseTransaction.GetObject(entityId, OpenMode.ForRead);
                if (obj != null)
                {
                    var plane = obj.GetPlane();
                    var plane1 = new Plane(plane.PointOnPlane, plane.Normal);
                    coordinateSystem3D = plane1.GetCoordinateSystem();
                }

                openCloseTransaction.Commit();
            }
        }

        return coordinateSystem3D;
    }

    private Point3dCollection PointsForSquareGrip(double num, CoordinateSystem3d ecs)
    {
        var horUnit = num * ecs.Xaxis;
        var verUnit = num * ecs.Yaxis;
        return new Point3dCollection
        {
            GripPoint - horUnit - verUnit,
            GripPoint - horUnit + verUnit,
            GripPoint + horUnit + verUnit,
            GripPoint + horUnit - verUnit
        };
    }

    private Point3dCollection PointsForPlusGrip(double num, CoordinateSystem3d ecs)
    {
        var num2 = num / 3;
        var horUnit = num * ecs.Xaxis;
        var verUnit = num * ecs.Yaxis;
        return new Point3dCollection
        {
            GripPoint + horUnit + (num2 * ecs.Yaxis),
            GripPoint + (num2 * ecs.Xaxis) + (num2 * ecs.Yaxis),
            GripPoint + (num2 * ecs.Xaxis) + verUnit,
            GripPoint - (num2 * ecs.Xaxis) + verUnit,
            GripPoint - (num2 * ecs.Xaxis) + (num2 * ecs.Yaxis),
            GripPoint - horUnit + (num2 * ecs.Yaxis),
            GripPoint - horUnit - (num2 * ecs.Yaxis),
            GripPoint - (num2 * ecs.Xaxis) - (num2 * ecs.Yaxis),
            GripPoint - (num2 * ecs.Xaxis) - verUnit,
            GripPoint + (num2 * ecs.Xaxis) - verUnit,
            GripPoint + (num2 * ecs.Xaxis) - (num2 * ecs.Yaxis),
            GripPoint + horUnit - (num2 * ecs.Yaxis)
        };
    }

    private Point3dCollection PointsForMinusGrip(double num, CoordinateSystem3d ecs)
    {
        var num2 = num / 3;
        var horUnit = num * ecs.Xaxis;
        return new Point3dCollection
        {
            GripPoint - horUnit + (num2 * ecs.Yaxis),
            GripPoint + horUnit + (num2 * ecs.Yaxis),
            GripPoint + horUnit - (num2 * ecs.Yaxis),
            GripPoint - horUnit - (num2 * ecs.Yaxis)
        };
    }

    private Point3dCollection PointsForTwoArrowsGripUpDownFirstArrow(double num, CoordinateSystem3d ecs)
    {
        var horUnit = num * ecs.Xaxis;
        var verUnit = num * ecs.Yaxis;
        return new Point3dCollection
        {
            GripPoint - (verUnit * 0.25),
            GripPoint - (horUnit * 0.75) + (verUnit * 1.25),
            GripPoint - (horUnit * 1.5) - (verUnit * 0.25),
            GripPoint - (horUnit * 1.0) - (verUnit * 0.25),
            GripPoint - (horUnit * 1.0) - (verUnit * 1.25),
            GripPoint - (horUnit * 0.5) - (verUnit * 1.25),
            GripPoint - (horUnit * 0.5) - (verUnit * 0.25)
        };
    }

    private Point3dCollection PointsForTwoArrowsGripLeftRightFirstArrow(double num, CoordinateSystem3d ecs)
    {
        var horUnit = num * ecs.Xaxis;
        var verUnit = num * ecs.Yaxis;
        return new Point3dCollection
        {
            GripPoint - (horUnit * 0.25),
            GripPoint + (horUnit * 1.25) + (verUnit * 0.75),
            GripPoint - (horUnit * 0.25) + (verUnit * 1.5),
            GripPoint - (horUnit * 0.25) + (verUnit * 1.0),
            GripPoint - (horUnit * 1.25) + (verUnit * 1.0),
            GripPoint - (horUnit * 1.25) + (verUnit * 0.5),
            GripPoint - (horUnit * 0.25) + (verUnit * 0.5)
        };
    }

    private Point3dCollection PointsForTwoArrowsGripUpDownSecondArrow(double num, CoordinateSystem3d ecs)
    {
        var horUnit = num * ecs.Xaxis;
        var verUnit = num * ecs.Yaxis;
        return new Point3dCollection
        {
            GripPoint + (verUnit * 0.25),
            GripPoint + (horUnit * 0.75) - (verUnit * 1.25),
            GripPoint + (horUnit * 1.5) + (verUnit * 0.25),
            GripPoint + (horUnit * 1.0) + (verUnit * 0.25),
            GripPoint + (horUnit * 1.0) + (verUnit * 1.25),
            GripPoint + (horUnit * 0.5) + (verUnit * 1.25),
            GripPoint + (horUnit * 0.5) + (verUnit * 0.25)
        };
    }

    private Point3dCollection PointsForTwoArrowsGripLeftRightSecondArrow(double num, CoordinateSystem3d ecs)
    {
        var horUnit = num * ecs.Xaxis;
        var verUnit = num * ecs.Yaxis;
        return new Point3dCollection
        {
            GripPoint + (horUnit * 0.25),
            GripPoint - (horUnit * 1.25) - (verUnit * 0.75),
            GripPoint + (horUnit * 0.25) - (verUnit * 1.5),
            GripPoint + (horUnit * 0.25) - (verUnit * 1.0),
            GripPoint + (horUnit * 1.25) - (verUnit * 1.0),
            GripPoint + (horUnit * 1.25) - (verUnit * 0.5),
            GripPoint + (horUnit * 0.25) - (verUnit * 0.5)
        };
    }

    private Point3dCollection PointsForListTriangle(double num, CoordinateSystem3d ecs)
    {
        var horUnit = num  * 0.8 * ecs.Xaxis;
        var verUnit = num  * 0.8 * ecs.Yaxis;
        return new Point3dCollection
        {
            GripPoint - verUnit,
            GripPoint - horUnit + verUnit,
            GripPoint + horUnit + verUnit
        };
    }
    private Point3dCollection PointsForListMinusGrip(double num, CoordinateSystem3d ecs)
    {
        var horUnit = num * 0.8 * ecs.Xaxis;
        var verUnit = num * 0.8 * ecs.Yaxis;
        return new Point3dCollection
        {
            GripPoint - horUnit + verUnit * 1.5,
            GripPoint + horUnit + verUnit * 1.5,
            GripPoint + horUnit + verUnit * 2,
            GripPoint - horUnit + verUnit * 2
        };
    }

    private short GetGripColor()
    {
        switch (GripType)
        {
            case GripType.Plus:
                return 110;
            case GripType.Minus:
                return 20;
        }

        return 150;
    }
}