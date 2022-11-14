namespace mpESKD.Functions.mpChainLeader;

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
using System.Linq;

/// <summary>
/// Отметка на плане
/// </summary>
[SmartEntityDisplayNameKey("h171")] // TODO Цепная выноска 
[SystemStyleDescriptionKey("h172")] // TODO Базовый стиль для обозначения цепной выноски
public class ChainLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastNodeNumber;
    private string _cachedNodeNumber;

    #region Text entities

    /// <summary>
    /// Верхний текст 
    /// </summary>
    private DBText _topDbText;

    /// <summary>
    /// Маскировка фона верхнего текста 
    /// </summary>
    private Wipeout _topFirstTextMask;

    /// <summary>
    /// Нижний текст 
    /// </summary>
    private DBText _bottomDbText;

    /// <summary>
    /// Маскировка нижнего текста
    /// </summary>
    private Wipeout _bottomTextMask;

    #endregion

    #region Entities

    /// <summary>
    /// Главная полилиния примитива
    /// </summary>
    private Polyline _mainPolyline;

    /// <summary>
    /// Линия выноски
    /// </summary>
    private Line _leaderLine;

    /// <summary>
    /// Полка выноски
    /// </summary>
    private Line _shelfLine;

    private Point3d _leaderFirstPoint;

    private readonly List<Line> _leaderLines = new();
    private readonly List<Polyline> _leaderEndLines = new();
    private readonly List<Hatch> _hatches = new();
    private Polyline _framePolyline;
    private double _scale;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeader"/> class.
    /// </summary>
    public ChainLeader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeader"/> class.
    /// </summary>
    /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
    public ChainLeader(string lastNodeNumber)
    {
        _lastNodeNumber = lastNodeNumber;
    }

    /// <inheritdoc />
    public override string LineType { get; set; }

    /// <inheritdoc />
    public override double LineTypeScale { get; set; }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => 1;

    /// <summary>
    /// Состояние Jig при создании узловой выноски
    /// </summary>
    public ChainLeaderJigState? JigState { get; set; }

    /// <summary>
    /// Отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p61", 1.0, 0.0, 3.0, nameSymbol: "o")]
    [SaveToXData]
    public double TextIndent { get; set; } = 1.0;

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 6, "p62", 1.0, 0.0, 3.0, nameSymbol: "v")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;

    /// <summary>
    /// Выступ полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 7, "p63", 1, 0, 3, descLocalKey: "d63", nameSymbol: "l")]
    [SaveToXData]
    public int ShelfLedge { get; set; } = 1;

    /// <summary>
    /// Положение полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 8, "p78", ShelfPosition.Right)]
    [SaveToXData]
    public ShelfPosition ShelfPosition { get; set; } = ShelfPosition.Right;

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double MainTextHeight { get; set; } = 3.5;

    /// <summary>
    /// Высота малого текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 3, "p50", 2.5, 0.000000001, 1.0000E+99, nameSymbol: "h2")]
    [SaveToXData]
    public double SecondTextHeight { get; set; } = 2.5;

    /// <summary>
    /// Текст всегда горизонтально
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 4, "p84", false, descLocalKey: "d84")]
    [SaveToXData]
    public bool IsTextAlwaysHorizontal { get; set; }

    /// <summary>
    /// Основной текст
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p101", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string MainText { get; set; } = string.Empty;

    /// <summary>
    /// Малый текст
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 9, "p102", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string SmallText { get; set; } = string.Empty;

    /// <summary>
    /// Размер стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p29", 5, 0.1, 10, nameSymbol: "d")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 3;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double TextHeight { get; set; } = 3.5;

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 4, "p85", false, descLocalKey: "d85-1")]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 5, "p86", 0.5, 0.0, 5.0, descLocalKey: "d86")]
    [SaveToXData]
    public double TextMaskOffset { get; set; }

    /// <summary>
    /// Выноска
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 9, "p104", true, descLocalKey: "d104")]
    [SaveToXData]
    public bool Leader { get; set; } = true;

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _topFirstTextMask,
                _bottomTextMask,
                _mainPolyline,
                _leaderLine,
                _shelfLine,
                _topDbText,
                _bottomDbText
            };

            entities.AddRange(_leaderEndLines);
            entities.AddRange(_hatches);

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <summary>
    /// Точка выноски
    /// </summary>
    [SaveToXData]
    public Point3d LeaderPoint { get; set; } = new();

    /// <summary>
    /// Типы выносок
    /// </summary>
    [SaveToXData]
    public List<int> LeaderTypes { get; set; } = new();

    /// <summary>
    /// Точка выноски в внутренней системе координат блока
    /// </summary>
    private Point3d LeaderPointOCS => LeaderPoint.TransformBy(BlockTransform.Inverse());

    public Vector3d MainNormal { get; set; } = new();
    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            var length = EndPointOCS.DistanceTo(InsertionPointOCS);
            _scale = GetScale();
            if (JigState == ChainLeaderJigState.InsertionPoint)
            {
                var tempEndPoint = new Point3d(
                    InsertionPointOCS.X,
                    InsertionPointOCS.Y + (MinDistanceBetweenPoints * _scale),
                    InsertionPointOCS.Z);

                //CreateEntities(InsertionPointOCS, tempEndPoint, _scale);

                // Задание точки вставки (т.е. второй точки еще нет)
                MakeSimplyEntity(_scale);
            }
            else 
            {
                if (length < MinDistanceBetweenPoints * _scale)
                {
                    var v = (EndPointOCS - InsertionPointOCS).GetNormal();
                    var tempEndPoint = InsertionPointOCS + (MinDistanceBetweenPoints * _scale * v);
                    MakeSimplyEntity(_scale);
                    //_mainPolyline = new Line(InsertionPoint, tempEndPoint);
                }
                else
                {
                    MakeSimplyEntity(_scale);
                    //_mainPolyline = new Line(InsertionPoint, EndPointOCS);
                }

                CreateEntities(InsertionPointOCS, EndPointOCS, _scale);
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void MakeSimplyEntity(double scale)
    {
        var tmpEndPoint = JigState == ChainLeaderJigState.InsertionPoint ?
            new Point3d(InsertionPointOCS.X + (MinDistanceBetweenPoints * scale), InsertionPointOCS.Y, InsertionPointOCS.Z) :
            ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);


        _mainPolyline = new Polyline(2);
        _mainPolyline.AddVertexAt(0, InsertionPoint.ToPoint2d(), 0.0, 0, 0);
        _mainPolyline.AddVertexAt(1, tmpEndPoint.ToPoint2d(), 0.0, 0, 0);

        //_leaderFirstPoint = tmpEndPoint;
        EndPoint = tmpEndPoint.TransformBy(BlockTransform);

        //CreateEntities(scale);
    }

    private void CreateEntities(Point3d insertionPoint, Point3d leaderPoint, double scale)
    {
        //_leaderLines.Clear();
        //_leaderEndLines.Clear();
        //_hatches.Clear();
        var arrowSize = ArrowSize * scale;

        var mainNormal = (leaderPoint - insertionPoint).GetNormal();
        //_mainPolyline = new Line(InsertionPoint, leaderPoint);
        //_leaderLine = new Line(insertionPoint, leaderSecondPoint);
        //if (!Leader || (string.IsNullOrEmpty(MainText) && string.IsNullOrEmpty(SmallText)))
        //{
        //    _leaderLine = null;
        //    _shelfLine = null;
        //}

        //// Дальше код идентичен коду в SecantNodalLeader! Учесть при внесении изменений

        SetNodeNumberOnCreation();

        var mainTextHeight = MainTextHeight * scale;
        var secondTextHeight = SecondTextHeight * scale;
        var textIndent = TextIndent * scale;
        var textVerticalOffset = TextVerticalOffset * scale;
        var shelfLedge = ShelfLedge * scale;
        var isRight = ShelfPosition == ShelfPosition.Right;

        var topTextLength = 0.0;
        var bottomTextLength = 0.0;
        var bottomTextHeight = 0.0;

        if (!string.IsNullOrEmpty(MainText))
        {
            _topDbText = new DBText { TextString = MainText };
            _topDbText.SetProperties(TextStyle, mainTextHeight);
            _topDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
            topTextLength = _topDbText.GetLength();
        }
        else
        {
            _topDbText = null;
        }

        if (!string.IsNullOrEmpty(SmallText))
        {
            _bottomDbText = new DBText { TextString = SmallText };
            _bottomDbText.SetProperties(TextStyle, secondTextHeight);
            _bottomDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
            bottomTextLength = _bottomDbText.GetLength();
            bottomTextHeight = _bottomDbText.GetHeight();
        }
        else
        {
            _bottomDbText = null;
        }

        var largestTextLength = Math.Max(topTextLength, bottomTextLength);
        var shelfLength = textIndent + largestTextLength + shelfLedge;
        var topTextPosition = default(Point3d);
        var bottomTextPosition = default(Point3d);
        if (isRight)
        {
            if (_topDbText != null)
            {
                topTextPosition = new Point3d(
                    leaderPoint.X + (topTextLength / 2) + ((shelfLength - topTextLength) / 2),
                    leaderPoint.Y + textVerticalOffset + (mainTextHeight / 2), 0);
                _topDbText.Position = topTextPosition;
                _topDbText.AlignmentPoint = topTextPosition;
            }

            if (_bottomDbText != null)
            {
                bottomTextPosition = new Point3d(
                    leaderPoint.X + (bottomTextLength / 2) + ((shelfLength - bottomTextLength) / 2),
                    leaderPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }
        else
        {
            if (_topDbText != null)
            {
                topTextPosition = new Point3d(
                    leaderPoint.X - (topTextLength / 2) - ((shelfLength - topTextLength) / 2),
                    leaderPoint.Y + textVerticalOffset + (mainTextHeight / 2), 0);
                _topDbText.Position = topTextPosition;
                _topDbText.AlignmentPoint = topTextPosition;
            }

            if (_bottomDbText != null)
            {
                bottomTextPosition = new Point3d(
                    leaderPoint.X - (bottomTextLength / 2) - ((shelfLength - bottomTextLength) / 2),
                    leaderPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }

        var shelfEndPoint = ShelfPosition == ShelfPosition.Right
            ? leaderPoint + (Vector3d.XAxis * shelfLength)
            : leaderPoint - (Vector3d.XAxis * shelfLength);

        var offset = TextMaskOffset * scale;
        if (HideTextBackground)
        {
            _topFirstTextMask = _topDbText.GetBackgroundMask(offset, topTextPosition);
            _bottomTextMask = _bottomDbText.GetBackgroundMask(offset, bottomTextPosition);
        }

        if (IsTextAlwaysHorizontal && IsRotated)
        {
            var backRotationMatrix = GetBackRotationMatrix(leaderPoint);
            shelfEndPoint = shelfEndPoint.TransformBy(backRotationMatrix);
            _topDbText?.TransformBy(backRotationMatrix);
            _topFirstTextMask?.TransformBy(backRotationMatrix);
            _bottomDbText?.TransformBy(backRotationMatrix);
            _bottomTextMask?.TransformBy(backRotationMatrix);
        }

        //if (_leaderLine != null && (_bottomDbText != null || _topDbText != null))
        //{
            _shelfLine = new Line(EndPointOCS, shelfEndPoint);
        //}

        MirrorIfNeed(new[] { _topDbText, _bottomDbText });

        //for (var i = 0; i < LeaderPointsOCS.Count; i++)
        //{

        //    var pline = new Polyline();

        //    if (_leaderLines[i].Length - (ArrowSize * _scale) > 0)
        //    {
        //        if (LeaderTypes.Count <= 0)
        //        {
        //            pline = CreateResectionArrow(_leaderLines[i]);
        //        }
        //        else
        //        {
        //            switch ((LeaderEndType)LeaderTypes[i])
        //            {
        //                case LeaderEndType.None:
        //                    break;
        //                case LeaderEndType.HalfArrow:
        //                    _hatches.Add(CreateArrowHatch(CreateHalfArrow(_leaderLines[i])));
        //                    break;
        //                case LeaderEndType.Point:
        //                    _hatches.Add(CreatePointHatch(CreatePointArrow(_leaderLines[i])));
        //                    break;
        //                case LeaderEndType.Resection:
        //                    pline = CreateResectionArrow(_leaderLines[i]);
        //                    break;
        //                case LeaderEndType.Angle:
        //                    pline = CreateAngleArrow(_leaderLines[i], 45, false);
        //                    break;
        //                case LeaderEndType.Arrow:
        //                    _hatches.Add(CreateArrowHatch(CreateAngleArrow(_leaderLines[i], 10, true)));
        //                    break;
        //                case LeaderEndType.OpenArrow:
        //                    pline = CreateAngleArrow(_leaderLines[i], 10, false);
        //                    break;
        //                case LeaderEndType.ClosedArrow:
        //                    pline = CreateAngleArrow(_leaderLines[i], 10, true);
        //                    break;
        //            }
        //        }
        //    }

        //    _leaderEndLines.Add(pline);
        //}
    }

    private void SetNodeNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        MainText = EntityUtils.GetNodeNumberByLastNodeNumber(_lastNodeNumber, ref _cachedNodeNumber);
    }

    #region Arrows
    private Polyline CreateResectionArrow(Line leaderLine)
    {
        var vector = new Vector3d(0, 0, 1);
        var tmpPoint = leaderLine.GetPointAtDist(leaderLine.Length - (ArrowSize / 2 * _scale));
        var startPoint = tmpPoint.RotateBy(45.DegreeToRadian(), vector, leaderLine.EndPoint);
        var endPoint = tmpPoint.RotateBy(225.DegreeToRadian(), vector, leaderLine.EndPoint);

        var pline = new Polyline(2);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, 0.3, 0.3);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, 0.3, 0.3);

        return pline;
    }

    private Polyline CreateAngleArrow(Line leaderLine, int angle, bool closed)
    {
        var vector = new Vector3d(0, 0, 1);
        var tmpPoint = leaderLine.GetPointAtDist(leaderLine.Length - (ArrowSize * _scale));
        var startPoint = tmpPoint.RotateBy(angle.DegreeToRadian(), vector, leaderLine.EndPoint);
        var endPoint = tmpPoint.RotateBy((-1) * angle.DegreeToRadian(), vector, leaderLine.EndPoint);

        var pline = new Polyline(3);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(leaderLine.EndPoint), 0, 0, 0);
        pline.AddVertexAt(2, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, 0, 0);

        pline.Closed = closed;

        return pline;
    }

    private Polyline CreateHalfArrow(Line leaderLine)
    {
        var vector = new Vector3d(0, 0, 1);
        var startPoint = leaderLine.GetPointAtDist(leaderLine.Length - (ArrowSize * _scale));
        var endPoint = startPoint.RotateBy(10.DegreeToRadian(), vector, leaderLine.EndPoint);

        var pline = new Polyline(3);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(leaderLine.EndPoint), 0, 0, 0);
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

    private Polyline CreatePointArrow(Line leaderLine)
    {
        var startPoint = leaderLine.GetPointAtDist(leaderLine.Length - (ArrowSize / 2 * _scale));
        var endPoint = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(leaderLine.StartPoint, leaderLine.EndPoint, leaderLine.Length + (ArrowSize / 2 * _scale));

        var pline = new Polyline(2);
        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 1, 0, 0);
        pline.AddVertexAt(1, endPoint, 1, 0, 0);
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

    #endregion
}