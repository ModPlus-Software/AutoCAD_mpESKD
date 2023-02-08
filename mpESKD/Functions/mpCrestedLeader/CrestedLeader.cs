using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;
using System;
using System.Collections.Generic;

/// <summary>
/// Цепная выноска Todo
/// </summary>
[SmartEntityDisplayNameKey("h175")] // Цепная выноска Todo
[SystemStyleDescriptionKey("h176")] // Базовый стиль для обозначения цепной выноски Todo
public class CrestedLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastNodeNumber;
    private string _cachedNodeNumber;
    private readonly List<Hatch> _hatches = new();
    private readonly List<Polyline> _leaderEndLines = new();
    private double _scale;

    /// <summary>
    /// нормаль выноски
    /// </summary>
    private Vector3d _mainNormal;

    /// <summary>
    /// нормаль полки
    /// </summary>
    private Vector3d _secondNormal;
    private Line _shelfLineFromEndPoint;
    private readonly List<Line> _leaderLines = new();

    #region Entities

    /// <summary>
    /// Линия выноски
    /// </summary>
    private Line _leaderLine;

    /// <summary>
    /// Значение
    /// </summary>
    private DBText _topDbText;

    /// <summary>
    /// Маскировка фона значения
    /// </summary>
    private Wipeout _topTextMask;

    /// <summary>
    /// Примечание
    /// </summary>
    private DBText _bottomDbText;

    /// <summary>
    /// Маскировка фона примечания
    /// </summary>
    private Wipeout _bottomTextMask;

    private Line _leaderShelfLine;

    [SaveToXData]
    private double _mainAngle;

    private Line _secondTempLeaderLine;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeader"/> class.
    /// </summary>
    public CrestedLeader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeader"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public CrestedLeader(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeader"/> class.
    /// </summary>
    /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
    public CrestedLeader(string lastNodeNumber)
    {
        _lastNodeNumber = lastNodeNumber;
    }

    /// <summary>
    /// Состояние Jig при создании узловой выноски
    /// </summary>
    public CrestedLeaderJigState? JigState { get; set; }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => ArrowSize + 1;

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _topTextMask,
                _bottomTextMask,
                _leaderLine,
                _shelfLineFromEndPoint,
                _topDbText,
                _bottomDbText,
                _leaderShelfLine,
                _secondTempLeaderLine
            };
            entities.AddRange(_leaderLines);
            entities.AddRange(_hatches);
            entities.AddRange(_leaderEndLines);

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <inheritdoc />
    public override string LineType { get; set; }

    /// <inheritdoc />
    public override double LineTypeScale { get; set; }

    /// <summary>
    /// Отступ текста
    /// </summary>
    [SaveToXData]
    public double TextIndent { get; set; } = 1.0;

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p62", 1.0, 0.0, 3.0, nameSymbol: "h2")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;

    /// <summary>
    /// Выступ полки 
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p63", 0, 0, 100, descLocalKey: "d63", nameSymbol: "l")]
    [SaveToXData]
    public double ShelfLedge { get; set; }

    /// <summary>
    /// Положение полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p78", ShelfPosition.Right)]
    [SaveToXData]
    public ShelfPosition ShelfPosition { get; set; } = ShelfPosition.Right;

    /// <summary>
    /// Размер стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p29", 5, 0.1, 10, nameSymbol: "d")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 3;

    /// <summary>
    /// Тип стрелки
    /// </summary> 
    [EntityProperty(PropertiesCategory.Geometry, 6, "gp7", LeaderEndType.Point)]
    [SaveToXData]
    public LeaderEndType ArrowType { get; set; } = LeaderEndType.Point;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double MainTextHeight { get; set; } = 3.5;

    /// <summary>
    /// Высота малого текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 3, "p50", 2.5, 0.000000001, 1.0000E+99)]
    [SaveToXData]
    public double SecondTextHeight { get; set; } = 2.5;

    /// <summary>
    /// Текст всегда горизонтально
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 4, "p84", false, descLocalKey: "d84")]
    [SaveToXData]
    public bool IsTextAlwaysHorizontal { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 5, "p85", false, descLocalKey: "d85")]
    [PropertyVisibilityDependency(new[] { nameof(TextMaskOffset) })]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 6, "p86", 0.5, 0.0, 5.0)]
    [SaveToXData]
    public double TextMaskOffset { get; set; } = 0.5;

    /// <summary>
    /// Значение
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p113", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string LeaderTextValue { get; set; } = string.Empty;

    /// <summary>
    /// Примечание
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 9, "p68", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string LeaderTextComment { get; set; } = string.Empty;

    /// <summary>
    /// Выравнивание текста по горизонтали
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 10, "p73", TextHorizontalAlignment.Left, descLocalKey: "d73")]
    [SaveToXData]
    public TextHorizontalAlignment ValueHorizontalAlignment { get; set; } = TextHorizontalAlignment.Left;

    /// <summary>
    /// Точки стрелок
    /// </summary>
    [SaveToXData]
    public List<Point3d> ArrowPoints { get; set; } = new();

    private List<Point3d> LeaderPointsOCS => ArrowPoints.Select(p => p.TransformBy(BlockTransform.Inverse())).ToList();

    /// <summary>
    /// Расстояние от Endpoint для отображения при растягивании
    /// </summary>
    public Point3d TempNewArrowPoint { get; set; } = new Point3d(Double.NaN, Double.NaN, Double.NaN);

    private Point3d TempNewArrowPointOCS => TempNewArrowPoint.TransformBy(BlockTransform.Inverse());

    /// <summary>
    /// Длина полки
    /// </summary>
    [SaveToXData]
    public double ShelfLength { get; set; }

    /// <summary>
    /// Точка выноски
    /// </summary>
    [SaveToXData]
    public Point3d LeaderPoint { get; set; } = Point3d.Origin;

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
        foreach (var arrowPoint in ArrowPoints)
        {
            yield return arrowPoint;
        }
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            _scale = GetScale();
            var length = EndPointOCS.DistanceTo(InsertionPointOCS);

            // Задание первой точки (точки вставки). Она же точка начала отсчета
            if (JigState == CrestedLeaderJigState.InsertionPoint)
            {
                var arrows = new List<Point3d>()
            {
                InsertionPointOCS
            };

                var leaderStart = new Point3d((InsertionPointOCS.X) * _scale, InsertionPointOCS.Y, 0);
                
                var tempEndPoint = new Point3d(
                                InsertionPointOCS.X + (MinDistanceBetweenPoints * _scale),
                                InsertionPointOCS.Y + (MinDistanceBetweenPoints * _scale),
                                InsertionPointOCS.Z);
                AcadUtils.WriteMessageInDebug($"ждем первую точку InsertionPoint {InsertionPoint} EndPoint {EndPoint}");
                CreateEntities(leaderStart, tempEndPoint, arrows, _scale);
            }
            else if (JigState == CrestedLeaderJigState.LeaderStart)
            {
                FirstArrowFirstPoint = InsertionPointOCS;
                FirstArrowSecondPoint = EndPointOCS;

                var arrows = new List<Point3d> { InsertionPointOCS };

                var leaderStart = InsertionPointOCS;
                var leaderEnd = new Point3d((leaderStart.X + 30) * _scale, leaderStart.Y, 0);
                AcadUtils.WriteMessageInDebug($"ждем вторую точку InsertionPoint {InsertionPoint} EndPoint {EndPoint}");
                if (length < MinDistanceBetweenPoints * _scale)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    AcadUtils.WriteMessageInDebug("должно сработать когда расстояние от inspoint близко к leaderpoint");
                    leaderEnd = new Point3d(
                        leaderStart.X + (MinDistanceBetweenPoints * _scale),
                        leaderStart.Y,
                        0);
               
                    var leaderMinPoint = new Point3d(
                        leaderStart.X + (MinDistanceBetweenPoints * _scale),
                        leaderStart.Y,
                        0);

                    if (ArrowPoints.Count == 0)
                    {
                        if (arrows[0].DistanceTo(leaderStart) < MinDistanceBetweenPoints)
                        {
                            leaderStart = arrows[0];
                        }


                        AcadUtils.WriteMessageInDebug($"первое построение прямых первая точка {arrows[0]} leaderStart {leaderStart} вторая точка leaderEnd {leaderEnd}");
                    }

                    CreateEntities(leaderStart, EndPointOCS, arrows, _scale);
                }
                else
                {
                    AcadUtils.WriteMessageInDebug("должно сработать когда расстояние от inspoint не близко к leaderpoint ервая точка {arrows[0]} leaderStart {leaderStart} вторая точка leaderEnd {leaderEnd}");
                    var tempEndPoint = new Point3d(EndPointOCS.X, InsertionPointOCS.Y, 0);
                    CreateEntities(leaderStart, tempEndPoint, arrows, _scale);
                }
            }
            else
            {
                var tempEndPoint = new Point3d(EndPointOCS.X, InsertionPointOCS.Y ,0);
                if (InsertionPointOCS.DistanceTo(EndPointOCS) < MinDistanceBetweenPoints * _scale)
                {
                    tempEndPoint = new Point3d( InsertionPointOCS.X + MinDistanceBetweenPoints, InsertionPointOCS.Y, 0);
                }

                AcadUtils.WriteMessageInDebug($"in else InsertionPoint {InsertionPoint} InsertionPointOCS {InsertionPointOCS}, EndPoint {EndPoint} EndPointOCS {EndPointOCS}");
                CreateEntities(InsertionPointOCS, tempEndPoint, LeaderPointsOCS, _scale);
            }

        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    [SaveToXData]
    public Point3d FirstArrowSecondPoint { get; set; }

    [SaveToXData]
    public Point3d FirstArrowFirstPoint { get; set; }

    private void CreateEntities(Point3d leaderStart, Point3d leaderEnd, List<Point3d> arrows, double scale)
    {
        _leaderLines.Clear();
        _leaderEndLines.Clear();
        _hatches.Clear();

        var mainNormal = (FirstArrowSecondPoint - FirstArrowFirstPoint).GetNormal();

        _leaderShelfLine = new Line(leaderStart, leaderEnd);
        var leaderMinPoint = leaderStart + (mainNormal * MinDistanceBetweenPoints * _scale);
        if (ArrowPoints.Count == 0)
        {
            if (leaderStart.DistanceTo(leaderEnd) < MinDistanceBetweenPoints)
            {
                leaderEnd = leaderMinPoint;
            }

            _leaderLine = new Line(leaderStart, leaderMinPoint);
            _leaderShelfLine = new Line(leaderStart, leaderEnd);
            AcadUtils.WriteMessageInDebug($"первое построение прямых первая точка {arrows[0]} вторая точка {leaderStart}");
        }
        else if (ArrowPoints.Count == 1)
        {
            leaderMinPoint = arrows[0] + (mainNormal * MinDistanceBetweenPoints);
            _leaderLine = new Line(arrows[0], leaderStart);

            AcadUtils.WriteMessageInDebug($"первое построение прямых первая точка {arrows[0]} вторая точка {leaderStart}");
        }
        else
        {
            foreach (var arrowPoint in LeaderPointsOCS)
            {
                var templine = new Line(arrowPoint, arrowPoint + mainNormal);
                var pts = new Point3dCollection();

                _leaderShelfLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
                if (pts.Count > 0)
                {
                    templine = new Line(arrowPoint, pts[0]);
                    AcadUtils.WriteMessageInDebug($"line StartPoint {templine.StartPoint} EndPoint {templine.EndPoint} -  {_leaderLines.Count}");
                    _leaderLines.Add(templine);
                    CreateArrows(TempNewArrowPointOCS, mainNormal, ArrowSize, _scale);
                }
            }
        }

        if (!double.IsNaN(TempNewArrowPointOCS.X))
        {
            var pointOnPolyline = CreateLeadersWithArrows(TempNewArrowPointOCS);

            //var secondNormal = (FirstArrowSecondPoint - FirstArrowFirstPoint).GetNormal();
            var templine = new Line(TempNewArrowPointOCS, TempNewArrowPointOCS + mainNormal);
            var pts = new Point3dCollection();

            _leaderShelfLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count > 0)
            {
                templine = new Line(TempNewArrowPointOCS, pts[0]);
                _leaderLines.Add(templine);
                var tempNormal = (pts[0] - TempNewArrowPointOCS).GetNormal();
                CreateArrows(TempNewArrowPointOCS, tempNormal, ArrowSize, _scale);
            }

            var distToEndPoint = pointOnPolyline.DistanceTo(leaderStart);
            var distToLeaderPoint = pointOnPolyline.DistanceTo(leaderEnd);
            if (distToLeaderPoint < distToEndPoint)
            {
                _secondTempLeaderLine = new Line(leaderStart, pointOnPolyline);
                AcadUtils.WriteMessageInDebug($"меняться должно со стороны leaderPoint {pointOnPolyline}");
            }
            else
            {
                _secondTempLeaderLine = new Line(pointOnPolyline, leaderEnd);
                AcadUtils.WriteMessageInDebug($"меняться должно со стороны endPoint {pointOnPolyline}");
            }
        }

        //CreateArrows(arrows[0], _mainNormal, ArrowSize, _scale);
        //var tempList = new List<Point3d>
        //{
        //    leaderStart,
        //    leaderEnd
        //};

        //foreach (var arrowPoint in LeaderPointsOCS)
        //{
        //    //tempList.Add(CreateLeadersWithArrows(arrowPoint));
        //    var secondNormal = (FirstArrowSecondPoint - FirstArrowFirstPoint).GetNormal();
        //    var templine = new Line(arrowPoint, arrowPoint + secondNormal);
        //    var pts = new Point3dCollection();

        //    _leaderShelfLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
        //    if (pts.Count > 0)
        //    {
        //        templine = new Line(arrowPoint, pts[0]);
        //        AcadUtils.WriteMessageInDebug($"line StartPoint {templine.StartPoint} EndPoint {templine.EndPoint} -  {_leaderLines.Count}");
        //        _leaderLines.Add(templine);
        //        //tempList.Add(pts[0]);
        //    }
        //}



        //var furthest = tempList.GetFurthestPoints();
        //SecondPoint = furthest.Item1;
        //ThirdPoint = furthest.Item2;
        //AcadUtils.WriteMessageInDebug($"SecondPoint {SecondPoint} ThirdPoint {ThirdPoint}");
        //_leaderShelfLine = new Line(leaderStart, leaderEnd);

        // Дальше код идентичен коду в NodalLeader! Учесть при внесении изменений

        #region TextCreation

        SetNodeNumberOnCreation();

        var mainTextHeight = MainTextHeight * scale;
        var secondTextHeight = SecondTextHeight * scale;
        var textVerticalOffset = TextVerticalOffset * scale;
        var shelfLedge = ShelfLedge * scale;
        var isRight = ShelfPosition == ShelfPosition.Right;

        var topTextLength = 0.0;
        var bottomTextLength = 0.0;
        var bottomTextHeight = 0.0;

        if (!string.IsNullOrEmpty(LeaderTextValue))
        {
            _topDbText = new DBText { TextString = LeaderTextValue };
            _topDbText.SetProperties(TextStyle, mainTextHeight);
            _topDbText.SetPosition(
                TextHorizontalMode.TextCenter,
                TextVerticalMode.TextVerticalMid,
                AttachmentPoint.MiddleCenter);
            topTextLength = _topDbText.GetLength();
        }
        else
        {
            _topDbText = null;
        }

        if (!string.IsNullOrEmpty(LeaderTextComment))
        {
            _bottomDbText = new DBText { TextString = LeaderTextComment };
            _bottomDbText.SetProperties(TextStyle, secondTextHeight);
            _bottomDbText.SetPosition(
                TextHorizontalMode.TextCenter,
                TextVerticalMode.TextVerticalMid,
                AttachmentPoint.MiddleCenter);
            bottomTextLength = _bottomDbText.GetLength();
            bottomTextHeight = _bottomDbText.GetHeight();
        }
        else
        {
            _bottomDbText = null;
        }

        var largestTextLength = Math.Max(topTextLength, bottomTextLength);
        ShelfLength = TextIndent + largestTextLength + shelfLedge;

        Point3d topTextPosition;
        Point3d bottomTextPosition;

        if (isRight)
        {
            topTextPosition = new Point3d(
                leaderEnd.X + TextIndent + (largestTextLength / 2),
                leaderEnd.Y + textVerticalOffset + (mainTextHeight / 2),
                0);
            bottomTextPosition = new Point3d(
                leaderEnd.X + TextIndent + (largestTextLength / 2),
                leaderEnd.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

            if (_topDbText != null)
            {
                _topDbText.Position = topTextPosition;
                _topDbText.AlignmentPoint = topTextPosition;
            }

            if (_bottomDbText != null)
            {
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }
        else
        {
            topTextPosition = new Point3d(
                leaderEnd.X - TextIndent - (largestTextLength / 2),
                leaderEnd.Y + textVerticalOffset + (mainTextHeight / 2), 0);
            bottomTextPosition = new Point3d(
                leaderEnd.X - TextIndent - (largestTextLength / 2),
                leaderEnd.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

            if (_topDbText != null)
            {
                _topDbText.Position = topTextPosition;
                _topDbText.AlignmentPoint = topTextPosition;
            }

            if (_bottomDbText != null)
            {
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }

        var shelfEndPoint = ShelfPosition == ShelfPosition.Right
            ? leaderEnd + (Vector3d.XAxis * ShelfLength)
            : leaderEnd - (Vector3d.XAxis * ShelfLength);

        if (_bottomDbText != null && _topDbText != null)
        {
            var horV = (shelfEndPoint - leaderEnd).GetNormal();
            var diff = Math.Abs(topTextLength - bottomTextLength);
            var textHalfMovementHorV = diff / 2 * horV;
            var movingPosition = EntityUtils.GetMovementPositionVector(ValueHorizontalAlignment, isRight, textHalfMovementHorV, ScaleFactorX);
            if (topTextLength > bottomTextLength)
            {
                bottomTextPosition += movingPosition;
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
            else
            {
                topTextPosition += movingPosition;
                _topDbText.Position = topTextPosition;
                _topDbText.AlignmentPoint = topTextPosition;
            }
        }

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            if (_topDbText != null)
                _topTextMask = _topDbText.GetBackgroundMask(offset, topTextPosition);
            if (_bottomDbText != null)
                _bottomTextMask = _bottomDbText.GetBackgroundMask(offset, bottomTextPosition);
        }

        if (IsTextAlwaysHorizontal && IsRotated)
        {
            var backRotationMatrix = GetBackRotationMatrix(leaderStart);
            if (ScaleFactorX < 0)
            {
                backRotationMatrix = GetBackMirroredRotationMatrix(leaderStart);
            }

            shelfEndPoint = shelfEndPoint.TransformBy(backRotationMatrix);
            _topDbText?.TransformBy(backRotationMatrix);
            _topTextMask?.TransformBy(backRotationMatrix);
            _bottomDbText?.TransformBy(backRotationMatrix);
            _bottomTextMask?.TransformBy(backRotationMatrix);
        }

        _shelfLineFromEndPoint = new Line(leaderEnd, shelfEndPoint);

        MirrorIfNeed(new[] { _topDbText, _bottomDbText });
        AcadUtils.WriteMessageInDebug($"__________________________");

        #endregion
    }

    private void MakeSimpleEntity()
    {

        var tempEndPoint = new Point3d(
            InsertionPointOCS.X + (MinDistanceBetweenPoints * _scale),
            InsertionPointOCS.Y + (MinDistanceBetweenPoints * _scale),
            InsertionPointOCS.Z);
        var tempLeaderPoint = new Point3d(
            InsertionPointOCS.X + (ShelfLength * _scale),
            tempEndPoint.Y,
            InsertionPointOCS.Z);
        AcadUtils.WriteMessageInDebug($"tempEndPoint {tempEndPoint} tempLeaderPoint {tempLeaderPoint} ");
        //CreateEntities(InsertionPointOCS, tempEndPoint, tempLeaderPoint, _scale);
    }
    private void SetNodeNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        LeaderTextValue = EntityUtils.GetNodeNumberByLastNodeNumber(_lastNodeNumber, ref _cachedNodeNumber);
    }

    private void CreateArrows(Point3d point3d, Vector3d mainNormal, double arrowSize, double scale)
    {
        new ArrowBuilder(mainNormal, arrowSize, scale).BuildArrow(ArrowType, point3d, _hatches, _leaderEndLines);
    }

    private Point3d CreateLeadersWithArrows(Point3d arrowPoint)
    {
        var secondNormal = (FirstArrowSecondPoint - FirstArrowFirstPoint).GetNormal();
        var templine = new Line(arrowPoint, arrowPoint + secondNormal);
        var pts = new Point3dCollection();

        _leaderShelfLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
        if (pts.Count > 0)
        {
            templine = new Line(arrowPoint, pts[0]);
        }

        _leaderLines.Add(templine);
        var tempNormal = (pts[0] - arrowPoint).GetNormal();
        CreateArrows(arrowPoint, tempNormal, ArrowSize, _scale);
        return pts[0];
    }
}