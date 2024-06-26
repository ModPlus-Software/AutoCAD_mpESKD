﻿// ReSharper disable InconsistentNaming

namespace mpESKD.Functions.mpLetterLine;

using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;

/// <summary>
/// Буквенная линия
/// </summary>
[SmartEntityDisplayNameKey("h161")]
[SystemStyleDescriptionKey("h162")]
public class LetterLine : SmartLinearEntity, ITextValueEntity, IWithDoubleClickEditor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LetterLine"/> class.
    /// </summary>
    public LetterLine()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LetterLine"/> class.
    /// </summary>
    /// <param name="blockId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public LetterLine(ObjectId blockId)
        : base(blockId)
    {
    }

    #region Text entities

    /// <summary>
    /// Список MText
    /// </summary>
    private readonly List<MText> _texts = new ();

    private readonly List<Wipeout> _mTextMasks = new ();

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 2.0;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.General, 2, "p35", "Continuous", descLocalKey: "d35")]
    public override string LineType { get; set; }

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.General, 5, "p6", 1.0, 0.0, 1.0000E+99, descLocalKey: "d6")]
    public override double LineTypeScale { get; set; }

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; }

    /// <summary>
    /// Расстояние между текстом
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p106", 50, 10, 100, nameSymbol: "a")]
    [SaveToXData]
    public int MTextOffset { get; set; } = 50;

    /// <summary>
    /// Отступ первого текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p108", 25, 1, 50, nameSymbol: "b")]
    [SaveToXData]
    public int Space { get; set; } = 25;

    private bool _lineGeneration;

    /// <summary>
    /// Генерация типа линий по всей полилинии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p105", false, descLocalKey: "d105")]
    [PropertyVisibilityDependency(new[] { nameof(SpaceAndFirstStrokeOffsetVisibility) })]
    [SaveToXData]
    public bool LineGeneration
    {
        get => _lineGeneration;
        set
        {
            _lineGeneration = value;
            SpaceAndFirstStrokeOffsetVisibility = !_lineGeneration;
        }
    }

    [EntityProperty(PropertiesCategory.Geometry, 4, "", "", propertyScope: PropertyScope.Hidden)]
    [PropertyVisibilityDependency(new[] { nameof(Space) })]
    [SaveToXData]
    public bool SpaceAndFirstStrokeOffsetVisibility { get; private set; }

    /// <summary>
    /// Тип линии стандартная или составная
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p35", LetterLineType.Standard, descLocalKey: "d35-1")]
    [SaveToXData]
    public LetterLineType LetterLineType { get; set; }

    /// <summary>
    /// Формула для создания линии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 6, "p107", "", descLocalKey: "d107")]
    [RegexInputRestriction("[-0-9]")]
    [SaveToXData]
    public string StrokeFormula { get; set; } = "10-5";

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

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 4, "p85", true, descLocalKey: "d85")]
    [PropertyVisibilityDependency(new[] { nameof(TextMaskOffset) })]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 5, "p86", 0.5, 0.0, 5.0)]
    [SaveToXData]
    public double TextMaskOffset { get; set; } = 0.5;

    /// <summary>
    /// Основной текст
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 6, "p101", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string MainText { get; set; } = string.Empty;

    /// <summary>
    /// Малый текст
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p102", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string SmallText { get; set; } = string.Empty;

    /// <summary>
    /// Текст всегда горизонтально
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 8, "p84", false, descLocalKey: "d84")]
    [SaveToXData]
    public bool IsTextAlwaysHorizontal { get; set; }

    private double _scale;

    #endregion

    #region Geometry

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>();
            if (LetterLineType == LetterLineType.Standard)
            {
                entities.Add(_mainPolyline);
            }

            entities.AddRange(_mTextMasks);
            entities.AddRange(_texts);
            entities.AddRange(_lines);

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            SetChangeablePropertiesToNestedEntity(_mainPolyline);

            return entities;
        }
    }

    /// <summary>
    /// Главная полилиния примитива
    /// </summary>
    private Polyline _mainPolyline;

    /// <summary>
    /// Составные линии
    /// </summary>
    private List<Line> _lines = new ();

    /// <summary>
    /// Точки мтекста
    /// </summary>
    private List<Point3d> _textPoints = new ();

    private Vector3d _normal;
    private List<double> _strokeSpaceParams;
        
    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
        foreach (var middlePoint in MiddlePoints)
        {
            yield return middlePoint;
        }
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            _scale = GetScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (30 * _scale), InsertionPointOCS.Y,
                    InsertionPointOCS.Z);
                CreateEntities(tmpEndPoint);
            }
            else
            {
                // Задание любой другой точки
                CreateEntities(null);
                AcadUtils.WriteMessageInDebug("try else ");
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(Point3d? endPoint)
    {
        _texts.Clear();
        _mTextMasks.Clear();
        _lines.Clear();
        var points = GetOcsAll3dPointsForDraw(endPoint).ToList();

        SetNodeNumberOnCreation();

        CreateMainPolyline(points);

        if (LineGeneration)
        {
            CreateMTextOnWholeLengthOfPolyline();
            if (LetterLineType == LetterLineType.Composite)
            {
                CreateLinesByLineGeneration();
            }
        }
        else
        {
            _texts.AddRange(CreateMTextsByInsertionPoints(points));
            if (LetterLineType == LetterLineType.Composite)
            {
                CreateLinesByInsertionPoints(points);
            }
        }
    }

    private void CreateMainPolyline(List<Point3d> points)
    {
        _mainPolyline = new Polyline(points.Count);
        SetImmutablePropertiesToNestedEntity(_mainPolyline);
        for (var i = 0; i < points.Count; i++)
        {
            _mainPolyline.AddVertexAt(i, points[i].ToPoint2d(), 0.0, 0.0, 0.0);
        }
    }

    #region Line Creation Methods

    /// <summary>
    /// Создание линий по всем вводимым точкам
    /// </summary>
    /// <param name="points">Points</param>
    private void CreateLinesByInsertionPoints(List<Point3d> points)
    {
        var mTextWidthWithOffset = (_texts[0].ActualWidth / 2) + (TextMaskOffset * _scale);

        for (var i = 1; i < points.Count; i++)
        {
            var previousPoint = points[i - 1];
            var currentPoint = points[i];
            AcadUtils.WriteMessageInDebug($"предыдущая точка {previousPoint} текущая точка {currentPoint}");
            var segmentVector = currentPoint - previousPoint;

            var normal = segmentVector.GetNormal();

            var sumTextDistanceAtSegment = Space;
            var k = 0;

            var qty = Math.Ceiling((segmentVector.Length - Space) / MTextOffset);

            AcadUtils.WriteMessageInDebug(
                $"длина сегмента {segmentVector.Length} в этом сегменте будет {qty} частей");
            var point = default(Point3d);

            var offsetPoint = normal * mTextWidthWithOffset;
            while (k < qty)
            {
                var location = previousPoint + (normal * sumTextDistanceAtSegment);
                var checkLength = (currentPoint - location).Length;
                if (Space > segmentVector.Length)
                {
                    CreateLinesBetween2Points(previousPoint, currentPoint);
                }

                if (k == 0)
                {
                    point = previousPoint + (normal * Space);
                    CreateLinesBetween2Points(previousPoint, point);
                    AcadUtils.WriteMessageInDebug($"текущая точка{previousPoint} point {point} в else while");

                    if (checkLength < MTextOffset)
                    {
                        CreateLinesBetween2Points(
                            location + (normal * mTextWidthWithOffset),
                            currentPoint + (normal * mTextWidthWithOffset));
                    }
                }
                else
                {
                    if (checkLength < MTextOffset)
                    {
                        CreateLinesBetween2Points(
                            location + (normal * mTextWidthWithOffset),
                            currentPoint + (normal * mTextWidthWithOffset));
                    }

                    AcadUtils.WriteMessageInDebug(
                        $"point - normal * mTextWidthWithOffset{point - (normal * mTextWidthWithOffset)}  currentPoint{currentPoint} в else while");

                    point = previousPoint + (normal * sumTextDistanceAtSegment);
                    var curPreviousPoint = point - (normal * MTextOffset) + offsetPoint;

                    CreateLinesBetween2Points(curPreviousPoint, point);
                    AcadUtils.WriteMessageInDebug($"текущая точка{point}  в else while");
                }

                sumTextDistanceAtSegment += MTextOffset;
                k++;
            }

            if (qty == 0)
                CreateLinesBetween2Points(previousPoint, currentPoint + offsetPoint);
        }
    }

    /// <summary>
    /// создание линий по всем вводимым точкам
    /// </summary>
    private void CreateLinesByLineGeneration()
    {
        var mTextsQty = (int)(_mainPolyline.Length / MTextOffset);
        var offset = _mainPolyline.Length - (mTextsQty * MTextOffset);

        var j = 0;
        var segment = 0;
        var direction = 0;

        var mTextWidthWithOffset = (_texts[0].ActualWidth / 2) + (TextMaskOffset * _scale);
        for (var i = 0; i <= mTextsQty; i++)
        {
            var distAtPline = (offset / 2) + (i * MTextOffset);

            var location = _mainPolyline.GetPointAtDist(distAtPline);
            var segmentParameterAtPoint = _mainPolyline.GetParameterAtPoint(location);

            Point3d previousPoint;
            Point3d currentPoint;
            if (segmentParameterAtPoint <= 1)
            {
                previousPoint = _mainPolyline.GetPoint3dAt(0);
                currentPoint = _mainPolyline.GetPoint3dAt(1);
            }
            else
            {
                previousPoint = _mainPolyline.GetPoint3dAt((int)segmentParameterAtPoint);
                currentPoint = _mainPolyline.GetPoint3dAt((int)segmentParameterAtPoint + 1);
            }

            var normal = (currentPoint - previousPoint).GetNormal();
            var offsetPoint = normal * mTextWidthWithOffset;
            if (location == previousPoint)
                continue;

            var checkLength = (currentPoint - location).Length;

            if (j == 0)
            {
                if (offset != 0)
                {
                    CreateLinesBetween2Points(previousPoint, location);
                    segment++;

                    Debug.Print($"previouspoint {previousPoint}, newpoint {location} ");

                    AcadUtils.WriteMessageInDebug(
                        $"  сегмент {segment} отрисован, previouspoint {previousPoint}, newpoint {location} это направление {direction}  \n ");
                    AcadUtils.WriteMessageInDebug("_____________________");

                    if (checkLength > MTextOffset)
                    {
                        CreateLinesBetween2Points(location + offsetPoint, location + (normal * MTextOffset));
                        segment++;
                        AcadUtils.WriteMessageInDebug(
                            $" посл сегмент в offset != 0 сегмент {segment} отрисован, location {location}, location + normal * MTextOffset {location + (normal * MTextOffset)} это направление {direction}  \n ");
                        AcadUtils.WriteMessageInDebug("_____________________");
                    }

                    if (checkLength < MTextOffset)
                    {
                        CreateLinesBetween2Points(location + offsetPoint, currentPoint + offsetPoint);
                        segment++;
                        AcadUtils.WriteMessageInDebug(
                            $" посл сегмент в offset != 0 сегмент {segment} отрисован, location {location}, location + normal * MTextOffset {location + (normal * MTextOffset)} это направление {direction}  \n ");
                        AcadUtils.WriteMessageInDebug("_____________________");
                    }
                }
                else
                {
                    var newPoint = previousPoint + (normal * MTextOffset);
                    var newPrevPoint = previousPoint + offsetPoint;
                    CreateLinesBetween2Points(newPrevPoint, newPoint);
                    segment++;
                    Debug.Print($"previouspoint {previousPoint + offsetPoint}, newpoint {newPoint} ");

                    AcadUtils.WriteMessageInDebug(
                        $" сегмент {segment} отрисован previouspoint {previousPoint + offsetPoint}, newpoint {newPoint} , это направление {direction} \n ");
                    AcadUtils.WriteMessageInDebug("_____________________");
                }
            }
            else
            {
                var locPointWithOffset = location + (normal * MTextOffset);

                if (checkLength < MTextOffset)
                {
                    CreateLinesBetween2Points(location + offsetPoint, currentPoint + offsetPoint);

                    segment++;
                    AcadUtils.WriteMessageInDebug(
                        $" в if locPointWithOffset {locPointWithOffset} currentPoint {currentPoint} сегмент {segment} отрисован , это направление {direction} \n ");
                    AcadUtils.WriteMessageInDebug($"_________________");
                }
                else
                {
                    CreateLinesBetween2Points(location + offsetPoint, locPointWithOffset);

                    segment++;
                    AcadUtils.WriteMessageInDebug(
                        $"в else location + offsetPoint {location + offsetPoint} locPointWithOffset {locPointWithOffset} сегмент {segment} отрисован , это направление {direction} \n ");
                    AcadUtils.WriteMessageInDebug("________________________");
                }
            }

            j++;
            if (checkLength < MTextOffset)
            {
                j = 0;
                direction++;
                AcadUtils.WriteMessageInDebug("j обнулен");
            }
        }
    }

    /// <summary>
    /// создание линий между 2 точками
    /// </summary>
    private void CreateLinesBetween2Points(Point3d previousPoint, Point3d currentPoint)
    {
        /*
      // 3. при первом построении от начальной точки до мтекста строим линию, длина этой линии
      // до границы Wipeout мтекста, для этого узнаем расстояние до мтекста и отнимаем половину границы и оффсета,
      // затем строим составную линию, для построения составной линии определенной длиниы,
      // сначала длину сегмента делим на длину составной линии, если целое число строим столько целых
      // составных линий, для отстатка строим пока длина составной линии не достигнет длины остатка,
      // последнюю часть если линия обрезаем, если пробел проверяем 
      
      // если длина сегмента больше длины составной линии, то линия должна повторятся
      }*/

        var segmentVector = currentPoint - previousPoint;
        _normal = segmentVector.GetNormal();

        var offsetDistance = (_texts[0].ActualWidth / 2) + (TextMaskOffset * _scale);

        var lengthOfPartSegment = (currentPoint - previousPoint).Length - offsetDistance;

        _strokeSpaceParams = GetStrokesData();
        if (_strokeSpaceParams == null)
            return;
        var strokesLength = _strokeSpaceParams.Sum();

        var rounds = (int)(lengthOfPartSegment / strokesLength);

        var nextPoint = default(Vector3d);
        var firstPoint = previousPoint;
        for (var i = 0; i < rounds; i++)
        {
            firstPoint += nextPoint;
            CreateAllLinesInStrokes(firstPoint);
            nextPoint = _normal * strokesLength;
        }

        var pointAfterRounds = previousPoint + (_normal * (strokesLength * rounds));
        var finalLength = lengthOfPartSegment - (strokesLength * rounds);
        CreateLinesFromLength(finalLength, pointAfterRounds);
    }

    /// <summary>
    /// создает список линий из коллекции штрихов, длина линий это нечетные номера, проходимся
    /// по коллекции и создаем по одной линии на каждый нечетную цифру, потом этот метод будет
    /// повторяться в зависимости от длины заданных точек
    /// </summary>
    /// <param name="previousPoint">предыдущая точка</param>
    private void CreateAllLinesInStrokes(Point3d previousPoint)
    {
        for (var i = 0; i <= _strokeSpaceParams.Count; i++)
        {
            if (i % 2 == 0)
                continue;

            var line = GetLinesFromStrokesFromIteration(previousPoint, i);

            _lines.Add(line);
        }
    }

    /// <summary>
    /// построение линий в зависимости от оставшейся длины участка
    /// </summary>
    private void CreateLinesFromLength(double length, Point3d position)
    {
        var curLength = 0.0;
        for (var i = 1; i <= _strokeSpaceParams.Count; i++)
        {
            var curElemLength = _strokeSpaceParams[i - 1];
            if (i % 2 == 0)
            {
                if (length < curElemLength)
                    break;
                length -= curElemLength;
                continue;
            }

            if (curElemLength < length)
            {
                var line = GetLinesFromStrokesFromIteration(position, i);
                _lines.Add(line);
            }
            else
            {
                var newPosition = position + (_normal * _strokeSpaceParams.Take(i - 1).Sum());
                var lastLength = length;

                var line = GetLinesFromStrokes(newPosition, curLength, lastLength);
                _lines.Add(line);
                break;
            }

            length -= curElemLength;
        }
    }

    private Line GetLinesFromStrokesFromIteration(Point3d previousPoint, int i)
    {
        var curLength = _strokeSpaceParams.Take(i - 1).Sum();
        var curElemLength = _strokeSpaceParams[i - 1];
        var line = GetLinesFromStrokes(previousPoint, curLength, curElemLength);

        return line;
    }

    private Line GetLinesFromStrokes(Point3d previousPoint, double curLength, double curElemLength)
    {
        var curPoint = previousPoint + (_normal * curLength);
        var curNextPt = previousPoint + (_normal * (curLength + curElemLength));
        var line = new Line(curPoint, curNextPt);

        return line;
    }

    private List<double> GetStrokesData()
    {
        if (string.IsNullOrWhiteSpace(StrokeFormula))
        {
            return null;
        }

        var formulaSplit = StrokeFormula.Split('-');
        var strokes = formulaSplit.Select(s => double.TryParse(s, out var d) ? d : 0).ToList();

        if (strokes.Count == 1)
        {
            strokes.Add(strokes[strokes.Count - 1]);
        }

        if (IsOdd(strokes.Count))
        {
            strokes.Add(strokes[strokes.Count - 2]);
        }

        return strokes;
    }

    private bool IsOdd(int value)
    {
        return value % 2 != 0;
    }
    
    #endregion

    #region Mtext creation methods

    private void CreateMTextOnWholeLengthOfPolyline()
    {
        var mTextsQty = (int)(_mainPolyline.Length / MTextOffset);
        var firstPointOffset = _mainPolyline.Length - (mTextsQty * MTextOffset);

        for (var i = 0; i <= mTextsQty; i++)
        {
            var distAtPline = (firstPointOffset / 2) + (i * MTextOffset);
            AcadUtils.WriteMessageInDebug($"Текст должен находится на длине полилинии {distAtPline} \n");

            var location = _mainPolyline.GetPointAtDist(distAtPline);
            var segmentParameterAtPoint = _mainPolyline.GetParameterAtPoint(location);
            var plineSegmentQty = _mainPolyline.NumberOfVertices - 1;
            Point3d previousPoint;
            Point3d currentPoint;

            if (segmentParameterAtPoint <= 1)
            {
                previousPoint = _mainPolyline.GetPoint3dAt(0);
                currentPoint = _mainPolyline.GetPoint3dAt(1);
            }
            else
            {
                previousPoint = _mainPolyline.GetPoint3dAt((int)segmentParameterAtPoint);
                currentPoint = segmentParameterAtPoint < plineSegmentQty
                    ? _mainPolyline.GetPoint3dAt((int)segmentParameterAtPoint + 1)
                    : _mainPolyline.GetPoint3dAt(plineSegmentQty);
            }

            var segmentVector = currentPoint - previousPoint;
            var angle = Math.Atan2(segmentVector.Y, segmentVector.X);
            var mText = GetMTextAtDist(location);
            SetImmutablePropertiesToNestedEntity(mText);
            _texts.Add(mText);

            SetMTextAndWipeoutRotationAngle(mText, angle);
        }
    }

    private IEnumerable<MText> CreateMTextsByInsertionPoints(List<Point3d> points)
    {
        var mTexts = new List<MText>();
        for (var i = 1; i < points.Count; i++)
        {
            var previousPoint = points[i - 1];
            var currentPoint = points[i];

            var segmentVector = currentPoint - previousPoint;
            var segmentLength = segmentVector.Length;
            var angle = Math.Atan2(segmentVector.Y, segmentVector.X);
            var normal = segmentVector.GetNormal();

            var sumTextDistanceAtSegment = Space;
            var k = 1;

            var mTextsQty = Math.Ceiling((segmentVector.Length - Space) / MTextOffset);

            _textPoints.Add(previousPoint);
            while (k <= mTextsQty)
            {
                if (segmentLength < Space)
                {
                    break;
                }

                Point3d textPt;
                if (k == 1)
                {
                    textPt = previousPoint + (normal * Space);
                }
                else
                {
                    textPt = previousPoint + (normal * sumTextDistanceAtSegment);
                }

                var mText = GetMTextAtDist(textPt);
                mTexts.Add(mText);

                SetMTextAndWipeoutRotationAngle(mText, angle);

                sumTextDistanceAtSegment += MTextOffset;

                k++;
            }
        }

        return mTexts;
    }

    private void SetMTextAndWipeoutRotationAngle(MText mText, double angle)
    {
        if (IsTextAlwaysHorizontal)
        {
            angle = 0;
        }

        if (HideTextBackground)
        {
            var wipeout = HideMTextBackground(mText);

            if (angle != 0)
            {
                wipeout.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, mText.Location));
            }

            _mTextMasks.Add(wipeout);
        }

        mText.Rotation = angle;
    }

    private MText GetMTextAtDist(Point3d textLocation)
    {
        var mText = new MText
        {
            Contents = GetTextContents(),
            Attachment = AttachmentPoint.MiddleCenter,
            Location = textLocation,
        };

        mText.SetProperties(TextStyle, MainTextHeight * _scale);
        return mText;
    }

    private Wipeout HideMTextBackground(MText mText)
    {
        var maskOffset = TextMaskOffset * _scale;
        return mText.GetBackgroundMask(maskOffset);
    }

    /// <summary>
    /// Содержимое для MText в зависимости от значений
    /// </summary>
    /// <returns>форматированное содержимое</returns>
    private string GetTextContents()
    {
        var prefixAndDesignation = $"{{\\H1x;{MainText}}}";

        if (!string.IsNullOrEmpty(SmallText))
        {
            prefixAndDesignation += $"{{\\H{SecondTextHeight / MainTextHeight}x;{SmallText}}}";
        }

        return prefixAndDesignation;
    }

    #endregion

    private void SetNodeNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        MainText = "M+";
    }

    #endregion
}