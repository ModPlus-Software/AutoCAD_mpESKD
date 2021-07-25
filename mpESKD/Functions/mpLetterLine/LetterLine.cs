﻿// ReSharper disable InconsistentNaming

namespace mpESKD.Functions.mpLetterLine
{
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
    [SmartEntityDisplayNameKey("h73")]
    [SystemStyleDescriptionKey("h78")]
    public class LetterLine : SmartEntity, ILinearEntity, ITextValueEntity, IWithDoubleClickEditor
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
        /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        public LetterLine(ObjectId objectId)
            : base(objectId)
        {
        }

        #region Text entities

        /// <summary>
        /// Список MText
        /// </summary>
        private readonly List<MText> _texts = new List<MText>();

        private readonly List<Wipeout> _mTextMasks = new List<Wipeout>();

        #endregion

        #region Properties

        /// <summary>
        /// Промежуточные точки
        /// </summary>
        [SaveToXData]
        public List<Point3d> MiddlePoints { get; set; } = new List<Point3d>();

        private List<Point3d> MiddlePointsOCS
        {
            get
            {
                var points = new List<Point3d>();
                MiddlePoints.ForEach(p => points.Add(p.TransformBy(BlockTransform.Inverse())));
                return points;
            }
        }

        /// <inheritdoc/>
        public override double MinDistanceBetweenPoints => 10.0;

        /// <summary>
        /// Расстояние между текстами
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 3, "p38", 50, 10, 100, nameSymbol: "b")]
        [SaveToXData]
        public int MTextOffset { get; set; } = 50;

        /// <summary>
        /// Отступ группы штрихов
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 5, "p40", 25, 1, 50, nameSymbol: "c")]
        [SaveToXData]
        public int Space { get; set; } = 25;

        /// <inheritdoc />
        public override string LineType { get; set; }

        /// <inheritdoc />
        [EntityProperty(PropertiesCategory.General, 5, "p6", 1.0, 0.0, 1.0000E+99, descLocalKey: "d6")]
        public override double LineTypeScale { get; set; }

        /// <inheritdoc />
        [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
        [SaveToXData]
        public override string TextStyle { get; set; }

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
        [PropertyVisibilityDependency(new[] {nameof(TextMaskOffset)})]
        [SaveToXData]
        public bool HideTextBackground { get; set; }

        /// <inheritdoc/>
        [EntityProperty(PropertiesCategory.Content, 5, "p86", 0.5, 0.0, 5.0)]
        [SaveToXData]
        public double TextMaskOffset { get; set; } = 0.5;

        /// <summary>
        /// Большой Текст
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 6, "p51", "", propertyScope: PropertyScope.Palette)]
        [SaveToXData]
        [ValueToSearchBy]
        public string MainText { get; set; } = "M+";

        /// <summary>
        /// Малый текст
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 8, "p53", "", propertyScope: PropertyScope.Palette,
            descLocalKey: "d53")]
        [SaveToXData]
        [ValueToSearchBy]
        public string SmallText { get; set; } = "+";

        /// <summary>
        /// Текст всегда горизонтально
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 9, "p84", false, descLocalKey: "d84")]
        [SaveToXData]
        public bool IsTextAlwaysHorizontal { get; set; }

        private bool _lineGeneration;

        /// <summary>
        /// Генерация типа линий по всей полилинии
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 10, "p85", false, descLocalKey: "d85")]
        [PropertyVisibilityDependency(new[] {nameof(SpaceAndFirstStrokeOffsetVisibilility)})]
        [SaveToXData]
        public bool LineGeneration
        {
            get => _lineGeneration;
            set
            {
                _lineGeneration = value;
                SpaceAndFirstStrokeOffsetVisibilility = !_lineGeneration;
            }
        }

        [EntityProperty(PropertiesCategory.Content, 10, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] {nameof(Space)})]
        [SaveToXData]
        public bool SpaceAndFirstStrokeOffsetVisibilility { get; private set; }

        /// <summary>
        /// Тип линии стандартная или составная
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 11, "p103", LetterLineType.Standart, descLocalKey: "d86")]
        [SaveToXData]
        public LetterLineType LetterLineType { get; set; }

        /// <summary>
        /// Формула для создания линии
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 12, "p53", "", propertyScope: PropertyScope.Palette,
            descLocalKey: "d53")]
        [RegexInputRestriction("[-0-9]")]
        [SaveToXData]
        public string StrokeFormula { get; set; } = string.Empty;

        private int _segmentsCount = 0;
        private double _scale;

        #endregion

        #region Geometry

        /// <inheritdoc />
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>();
                entities.Add(_mainPolyline);

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
        private List<Line> _lines = new List<Line>();

        /// <summary>
        /// Точки мтекста
        /// </summary>
        private List<Point3d> _textPoints = new List<Point3d>();

        /// <inheritdoc />
        public void RebasePoints()
        {
            if (!MiddlePoints.Contains(EndPoint))
            {
                MiddlePoints.Add(EndPoint);
            }
        }

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
                var length = EndPointOCS.DistanceTo(InsertionPointOCS);
                _scale = GetScale();
                if (EndPointOCS.Equals(Point3d.Origin))
                {
                    // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                    MakeSimplyEntity(UpdateVariant.SetInsertionPoint);
                    AcadUtils.WriteMessageInDebug(
                        $"{EndPointOCS.Equals(Point3d.Origin)} - Задание точки вставки. Второй точки еще нет - отрисовка типового элемента");
                }
                else if (length < MinDistanceBetweenPoints * _scale && MiddlePoints.Count == 0)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    MakeSimplyEntity(UpdateVariant.SetEndPointMinLength);
                    AcadUtils.WriteMessageInDebug(
                        "length < MinDistanceBetweenPoints * scale && MiddlePoints.Count == 0 // Задание второй точки - случай когда расстояние между точками меньше минимального");
                }
                else
                {
                    // Задание любой другой точки
                    CreateEntities(InsertionPointOCS, MiddlePointsOCS, EndPointOCS);
                    AcadUtils.WriteMessageInDebug("try else ");
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void MakeSimplyEntity(UpdateVariant variant)
        {
            if (variant == UpdateVariant.SetInsertionPoint)
            {
                /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
                 * Примерно аналогично созданию, только точки не создаются, а меняются
                */
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * _scale), InsertionPointOCS.Y,
                    InsertionPointOCS.Z);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint);
            }
            else if (variant == UpdateVariant.SetEndPointMinLength)
            {
                /* Изменение базовых примитивов в момент указания второй точки
                * при условии что расстояние от второй точки до первой больше минимального допустимого
                */
                var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * _scale);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint);
                EndPoint = tmpEndPoint.TransformBy(BlockTransform);
            }
        }

        private void CreateEntities(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint)
        {
            _texts.Clear();
            _mTextMasks.Clear();
            _lines.Clear();
            var points = Get3dPoints(insertionPoint, middlePoints, endPoint);

            if (!LineGeneration)
            {
                _texts.AddRange(CreateMTextsByInsertionPoints(points));
            }
            else
            {
                CreateMtextOnWholeLenghtOfPolyline();
            }

            if (LetterLineType == LetterLineType.Composite)
            {
                _textPoints.Add(endPoint);
                var strokeSpaceParams = GetStrokesData();
                if (strokeSpaceParams != null)
                {
                    for (int i = 1; i < _textPoints.Count; i++)
                    {
                        var previousPoint = _textPoints[i - 1];
                        var currentPoint = _textPoints[i];
                        _lines.AddRange(CreateLinesBeetwin2Points(previousPoint, currentPoint));
                    }
                }
            }
            else
            {
                _mainPolyline = new Polyline(points.Count);
                SetImmutablePropertiesToNestedEntity(_mainPolyline);
                for (var i = 0; i < points.Count; i++)
                {
                    _mainPolyline.AddVertexAt(i, points[i].ToPoint2d(), 0.0, 0.0, 0.0);
                }

                _segmentsCount = 0;
                if (!(_mainPolyline.Length >= MinDistanceBetweenPoints))
                    return;
            }
        }

        private List<Line> CreateLinesBeetwin2Points(Point3d previousPoint, Point3d currentPoint)
        {
            /*var lines = new List<Line>();
            var segmentVector = currentPoint - previousPoint;

            //TODO find to create lines
            var strokeSpaceParams = GetStrokesData();
            var strokesLength = strokeSpaceParams.Sum();
            var vectorLength = segmentVector.Length;
            var normal = segmentVector.GetNormal();
            var sumLengthOfLines = 0.0;
            if (strokeSpaceParams != null)
            {
                AcadUtils.WriteMessageInDebug(
                    $"в длину  {vectorLength} поместится {(int) (vectorLength / strokesLength)} и не полностью {vectorLength % strokesLength} ");
                int round = 1;
                while (sumLengthOfLines < vectorLength)
                {
                    for (var i = 0; i < strokeSpaceParams.Count; i++)
                    {
                        if (i % 2 != 0) continue;
                        var curLength = strokeSpaceParams.Take(i).Sum();
                        var curElemLength = strokeSpaceParams[i];
                        Point3d curPoint;
                        Point3d curNextPt;

                        if (i != 0)
                        {
                            curPoint = previousPoint + (normal * curLength) * i * round;
                            curNextPt = previousPoint + normal * (curLength + curElemLength) * i * round;
                        }
                        else
                        {
                            curPoint = previousPoint + (normal * curLength);
                            curNextPt = previousPoint + normal * (curLength + curElemLength);
                        }

                        Line line = new Line(curPoint, curNextPt);
                        lines.Add(line);
                    }

                    previousPoint += strokesLength * normal;
                    round++;
                    sumLengthOfLines += strokesLength;
                }*/

                List<Line> lines = new List<Line>();
                var strokeSpaceParams = GetStrokesData();
                var strokesLength = strokeSpaceParams.Sum();
                // TODO find to create lines
                // 1. По двум точкам находим вектор, потом нормаль
                var segmentVector = currentPoint - previousPoint;
                var normal = segmentVector.GetNormal();
                // 2. находим ближающую точку расположения мтекста
                var offsetDistance = (_texts[0].ActualWidth / 2) + TextMaskOffset;
                var vectorLength = (_texts[0].Location - previousPoint).Length - offsetDistance;
                var roundsOfStrokes = (int)(vectorLength / strokesLength);
    
        
                // 3. при первом построении от начальной точки до мтекста строим линию, длина этой линии
                // до границы Wipeout мтекста, для этого узнаем расстояние до мтекста и отнимаем половину границы и оффсета,
                // затем строим составную линию, для построения составной линии определенной длиниы,
                // сначала длину сегмента делим на длину составной линии, если целое число строим столько целых
                // составных линий, для отстатка строим пока длина составной линии не достигнет длины остатка,
                // последнюю часть если линия обрезаем, если пробел ? 
                //    
    
                var sumLengthOfLines = 0.0;
                if (strokeSpaceParams != null)
                {
                    string str =
                        $"в длину  {vectorLength} поместится {(int) (vectorLength / strokesLength)} и не полностью {vectorLength % strokesLength} ";
                    AcadUtils.WriteMessageInDebug(str);
                    int round = 1;
                    Point3d lastPoint = default(Point3d);
                    Point3d prevSumOfStrokes = default(Point3d);
    
                    for (int j = 0; j < roundsOfStrokes; j++)
                    {
                        for (var i = 0; i < strokeSpaceParams.Count; i++)
                        {
                            var curLength = strokeSpaceParams.Take(i+1).Sum();
                            if (i % 2 != 0)
                            {
                                previousPoint += (normal * curLength * (j + 1));
                                continue;
                            }
                            
                            var curElemLength = strokeSpaceParams[i];
                            Point3d curPoint;
                            Point3d curNextPt;
    
                            if (i == 0)
                            {
                                curPoint = previousPoint + (normal * curLength * (j + 1));
                                curNextPt = previousPoint + (normal * (curLength + curElemLength) * (j + 1));
                            }
                            else
                            {
                                curPoint = prevSumOfStrokes + normal * curLength * (j + 1);
                                curNextPt = prevSumOfStrokes + (normal * (curLength + curElemLength) * (j + 1));
                            }
    
                            Line line = new Line(curPoint, curNextPt);
                            lines.Add(line);
                            previousPoint = curNextPt;
                        }
                    }
    
    /*
                    while (sumLengthOfLines < vectorLength)
                    {
                        for (var i = 0; i < strokeSpaceParams.Count; i++)
                        {
                            if (i % 2 != 0)
                                continue;
                            var curLength = strokeSpaceParams.Take(i).Sum();
                            var curElemLength = strokeSpaceParams[i];
                            Point3d curPoint;
                            Point3d curNextPt;
    
                            if (i != 0)
                            {
                                curPoint = prevSumOfStrokes + normal * curLength;
                                curNextPt = prevSumOfStrokes + (normal * (curLength + curElemLength));
                            }
                            else
                            {
                                curPoint = previousPoint + (normal * curLength);
                                curNextPt = previousPoint + (normal * (curLength + curElemLength));
                            }
    
                            Line line = new Line(curPoint, curNextPt);
                            lines.Add(line);
                        }
    
                        prevSumOfStrokes += strokesLength * normal;
                        round++;
                        sumLengthOfLines += strokesLength;
                        lastPoint = previousPoint + (normal * sumLengthOfLines);
                        AcadUtils.WriteMessageInDebug($"последняя точка {lastPoint}, предыдущая точка {previousPoint}");
                    }


                var lastPartOfSegment = currentPoint - lastPoint;

                if (lastPartOfSegment.Length < strokesLength)
                {
                    AcadUtils.WriteMessageInDebug(
                        $"С точки {lastPoint} проложить линии длиной {lastPartOfSegment.Length}");
                }

                var lastPartOfSegment2 = currentPoint - previousPoint;

                if (lastPartOfSegment2.Length < strokesLength)
                {
                    AcadUtils.WriteMessageInDebug(
                        $"С предыдущией точки {previousPoint} проложить линии длиной {lastPartOfSegment2.Length}");
                }*/

                /*var rounds = segmentVector.Length / strokesLength;

                if (rounds < 1)
                {
                    var lineSum = 0.0;
                    var j = 0;
                    var elementOfStrokeParam = 0;
                    while (lineSum <= vectorLength)
                    {
                        lineSum = +strokeSpaceParams[j];
                        AcadUtils.WriteMessageInDebug($"длина вектора { vectorLength}, длина суммированая {lineSum} ");
                        j++;
                        elementOfStrokeParam = j;
                    }

                    // TODO calculate last part

                    // необходимая длина последней части
                    var neededLengthOfLastPart = lineSum - vectorLength;

                    // реальная длина последней части
                    double realLastPartLength = strokeSpaceParams[elementOfStrokeParam - 1];
                    AcadUtils.WriteMessageInDebug($"длина вектора { vectorLength}, необходимая длина последней части {neededLengthOfLastPart},  реальная длина последней части {realLastPartLength}");


                }*/
                // если длина сегмента больше длины составной линии, то линия должна повторятся
                /*else
                {
                    int i = 0;
                    while (i <= rounds)
                    {
                        for (int k = 0; k < strokeSpaceParams; k++)
                        {
                            if ()
                        }
                        i++;
                    }
                }*/
            }

            return lines;
        }

        private List<Line> CreateSegmentFromCompositeLines(double lengthOfSegment, Point3d segmentStartPoint,
            Point3d segmentEndPoint)
        {
            var lines = new List<Line>();
            var segmentVector = segmentEndPoint - segmentStartPoint;
            var strokeSpaceParams = GetStrokesData();
            var strokesLength = strokeSpaceParams.Sum();
            var segmentVectorLength = segmentVector.Length;
            var normal = segmentVector.GetNormal();
            var sumLengthOfLines = 0.0;

            var roundsOfWholeLines = segmentVectorLength / strokesLength;

            for (var i = 1; i < strokeSpaceParams.Count; i++)
            {
                if (i % 2 != 0) continue;
                var curLength = strokeSpaceParams.Take(i).Sum();
                var curElemLength = strokeSpaceParams[i];
                Point3d curPoint;
                Point3d curNextPt;

                if (i != 0)
                {
                    curPoint = segmentStartPoint + (normal * curLength) * i * round;
                    curNextPt = previousPoint + normal * (curLength + curElemLength) * i * round;
                }
                else
                {
                    curPoint = previousPoint + (normal * curLength);
                    curNextPt = previousPoint + normal * (curLength + curElemLength);
                }

                Line line = new Line(curPoint, curNextPt);
                lines.Add(line);
                
            }

            previousPoint += strokesLength * normal;
            round++;
            sumLengthOfLines += strokesLength;
            return lines;
        }
        
// TODO  сделать метод возвращаюий список линий от заданных точек в котором полностью повторяются штрихи
        private List<Line> CreateAllLinesInStrokes(Point3d previousPoint, Vector3d normal)
        {
            var lines = new List<Line>();
            var strokeSpaceParams = GetStrokesData();
            for (var i = 1; i < strokeSpaceParams.Count; i++)
            {
                if (i % 2 != 0) continue;
                var curLength = strokeSpaceParams.Take(i).Sum();
                var curElemLength = strokeSpaceParams[i];
                Point3d curPoint;
                Point3d curNextPt;
                
                if (i != 0)
                {
                    curPoint = segmentStartPoint + (normal * curLength) * i * round;
                    curNextPt = previousPoint + normal * (curLength + curElemLength) * i * round;
                }
                else
                {
                    curPoint = previousPoint + (normal * curLength);
                    curNextPt = previousPoint + normal * (curLength + curElemLength);
                }
            }

            return lines;
        }
        
        // TODO  сделать метод возвращаюий список линий  которые зависит от длины, которая меньши длины всех штрихов
        private List<Line> CreateLinesFromLength(double length, Point3d position)
        {
            
        }
        
        private void CreateMtextOnWholeLenghtOfPolyline()
        {
            int mTextsQty = (int) (_mainPolyline.Length / MTextOffset);
            var offset = _mainPolyline.Length - (mTextsQty * MTextOffset);

            for (int i = 0; i <= mTextsQty; i++)
            {
                var distAtPline = (offset / 2) + (i * MTextOffset);
                AcadUtils.WriteMessageInDebug($"Текст должен находится на длине полилинии {distAtPline} \n");

                var location = _mainPolyline.GetPointAtDist(distAtPline);
                var segmentParameterAtPoint = _mainPolyline.GetParameterAtPoint(location);

                Point3d previousPoint;
                Point3d currentPoint;
                if (segmentParameterAtPoint < 1)
                {
                    previousPoint = _mainPolyline.GetPoint3dAt(0);
                    currentPoint = _mainPolyline.GetPoint3dAt(1);
                }
                else
                {
                    previousPoint = _mainPolyline.GetPoint3dAt((int) segmentParameterAtPoint);
                    currentPoint = _mainPolyline.GetPoint3dAt((int) segmentParameterAtPoint + 1);
                }

                var segmentVector = currentPoint - previousPoint;
                var angle = Math.Atan2(segmentVector.Y, segmentVector.X);
                var mText = GetMTextAtDist(location, angle);
                // AcadUtils.WriteMessageInDebug($"точка {location} номер сегмента {segmentParameterAtPoint} \n угол {angle * 180 / Math.PI}");
                SetImmutablePropertiesToNestedEntity(mText);

                HideMtextBackground(mText);

                _texts.Add(mText);
            }
        }

        private IEnumerable<MText> CreateMTextsByInsertionPoints(Point3dCollection points)
        {
            var segmentMTextsDependencies = new List<MText>();
            for (int i = 1; i < points.Count; i++)
            {
                var previousPoint = points[i - 1];
                var currentPoint = points[i];

                var segmentVector = currentPoint - previousPoint;
                var angle = Math.Atan2(segmentVector.Y, segmentVector.X);
                var normal = segmentVector.GetNormal();

                var sumTextDistanceAtSegment = Space;
                var k = 1;

                var mTextsQty = Math.Ceiling((segmentVector.Length - Space) / MTextOffset);

                _textPoints.Add(previousPoint);
                while (k <= mTextsQty)
                {
                    var step = MTextOffset;
                    if (step > segmentVector.Length)
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

                    var mText = GetMTextAtDist(textPt, angle);
                    segmentMTextsDependencies.Add(mText);
                    HideMtextBackground(mText);
                    sumTextDistanceAtSegment += step;

                    _textPoints.Add(textPt);

                    k++;
                }
            }

            return segmentMTextsDependencies;
        }


        private IEnumerable<MText> CreateMTextsOnMainPolylineSegment(
            Point3d currentPoint, Point3d previousPoint)
        {
            AcadUtils.WriteMessageInDebug($"длина полилинии {_mainPolyline.Length}");
            var segmentMTextsDependencies = new List<MText>();
            var segmentVector = currentPoint - previousPoint;
            var angle = Math.Atan2(segmentVector.Y, segmentVector.X);
            var segmentLength = segmentVector.Length;

            var distanceAtSegmentStart = _mainPolyline.GetDistAtPoint(previousPoint);
            var overflowIndex = 0;

            AcadUtils.WriteMessageInDebug($"длина сегмента {segmentVector.Length}");
            _segmentsCount++;
            AcadUtils.WriteMessageInDebug($"количество сегментов {_segmentsCount}");

            // Индекс штриха. Возможные значения - 0, 1, 2
            var sumDistanceAtSegment = 0.0;
            while (true)
            {
                overflowIndex++;

                sumDistanceAtSegment += GetDistance(sumDistanceAtSegment);

                if (sumDistanceAtSegment >= segmentLength)
                {
                    break;
                }

                var textLocation = _mainPolyline.GetPointAtDist(distanceAtSegmentStart + sumDistanceAtSegment);
                var mText = GetMTextAtDist(textLocation, angle);
                SetImmutablePropertiesToNestedEntity(mText);

                segmentMTextsDependencies.Add(mText);

                HideMtextBackground(mText);

                if (overflowIndex >= 1000)
                {
                    break;
                }
            }

            AcadUtils.WriteMessageInDebug($"количество текстов {segmentMTextsDependencies.Count}");
            if (!string.IsNullOrEmpty(StrokeFormula))
            {
                int i = 0;
                foreach (var text in segmentMTextsDependencies)
                {
                }
            }

            return segmentMTextsDependencies;
        }

        private List<double> GetStrokesData()
        {
            if (string.IsNullOrWhiteSpace(StrokeFormula))
            {
                return null;
            }

            var formulaSplit = StrokeFormula.Split('-');
            var strokes = formulaSplit.Select(s => string.IsNullOrEmpty(s) ? 0 : int.Parse(s)).Select(d => (double) d)
                .ToList();
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

        private double GetDistance(double sumDistanceAtSegment)
        {
            return MTextOffset * _scale;
        }

        private MText GetMTextAtDist(Point3d textLocation, double textAngle)
        {
            var textStyleId = AcadUtils.GetTextStyleIdByName(TextStyle);
            var textHeight = MainTextHeight * _scale;

            if (IsTextAlwaysHorizontal)
            {
                textAngle = 0;
            }

            var mText = new MText
            {
                TextStyleId = textStyleId,
                Contents = GetTextContents(),
                TextHeight = textHeight,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = textLocation,
                Rotation = textAngle
            };

            return mText;
        }

        private void HideMtextBackground(MText mText)
        {
            if (HideTextBackground)
            {
                var maskOffset = TextMaskOffset * _scale;
                _mTextMasks.Add(mText.GetBackgroundMask(maskOffset));
            }
        }

        /// <summary>
        /// Содержимое для MText в зависимости от значений
        /// </summary>
        /// <returns></returns>
        private string GetTextContents()
        {
            var prefixAndDesignation = MainText;

            if (!string.IsNullOrEmpty(SmallText))
            {
                prefixAndDesignation = $"{prefixAndDesignation}{{\\H{SecondTextHeight / MainTextHeight}x;{SmallText}}}";
            }

            return prefixAndDesignation;
        }

        private static Point2dCollection GetPointsForMainPolyline(Point3d insertionPoint, List<Point3d> middlePoints,
            Point3d endPoint)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var points = new Point2dCollection();

            points.Add(insertionPoint.ToPoint2d());
            middlePoints.ForEach(p => points.Add(p.ToPoint2d()));
            points.Add(endPoint.ToPoint2d());

            return points;
        }

        private static Point3dCollection Get3dPoints(Point3d insertionPoint, List<Point3d> middlePoints,
            Point3d endPoint)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var points = new Point3dCollection();

            points.Add(insertionPoint);
            middlePoints.ForEach(p => points.Add(p));
            points.Add(endPoint);

            return points;
        }

        #endregion
    }
}