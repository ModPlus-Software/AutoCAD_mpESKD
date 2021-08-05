// ReSharper disable InconsistentNaming

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
        public string MainText { get; set; } = string.Empty;

        /// <summary>
        /// Малый текст
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 8, "p53", "", propertyScope: PropertyScope.Palette,
            descLocalKey: "d53")]
        [SaveToXData]
        [ValueToSearchBy]
        public string SmallText { get; set; } = string.Empty;

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
                if (LetterLineType == LetterLineType.Standart)
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
        private List<Line> _lines = new List<Line>();

        /// <summary>
        /// Точки мтекста
        /// </summary>
        private List<Point3d> _textPoints = new List<Point3d>();

        private Vector3d _normal;
        private List<double> _strokeSpaceParams;

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
                }
                else if (length < MinDistanceBetweenPoints * _scale && MiddlePoints.Count == 0)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    MakeSimplyEntity(UpdateVariant.SetEndPointMinLength);
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
            SetNodeNumberOnCreation();
            
            CreateMainPolyline(points);
            
            if (!LineGeneration)
            {
                _texts.AddRange(CreateMTextsByInsertionPoints(points));
            }
            else
            {
                CreateMTextOnWholeLenghtOfPolyline();
            }

            if (LetterLineType == LetterLineType.Composite)
            {
                _strokeSpaceParams = GetStrokesData();
                if (_strokeSpaceParams == null)
                    return;

                CreateLinesByInsertionPoints(points);
            }

        }

        private void CreateMainPolyline(Point3dCollection points)
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
        /// создание линий по всем вводимым точкам
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private void CreateLinesByInsertionPoints(Point3dCollection points)
        {
            
            for (int i = 1; i < points.Count; i++)
            {
                var previousPoint = points[i - 1];
                var currentPoint = points[i];
                AcadUtils.WriteMessageInDebug($"предыдущая точка {previousPoint} текущая точка {currentPoint}");
                var segmentVector = currentPoint - previousPoint;
                var normal = segmentVector.GetNormal();

                var sumTextDistanceAtSegment = Space;
                var k = 1;

                var qty = Math.Ceiling((segmentVector.Length - Space) / MTextOffset);

                AcadUtils.WriteMessageInDebug(
                    $"длина сегмента {segmentVector.Length} в этом сегменте будет {qty} частей");
                Point3d point = default(Point3d);

                var offsetPoint = normal * (_texts[0].ActualWidth / 2 + TextMaskOffset * _scale);
                while (k <= qty)
                {
                    if (MTextOffset > segmentVector.Length)
                    {
                        break;
                    }

                    if (k == 1)
                    {
                        point = previousPoint + (normal * Space);
                        CreateLinesBeetwin2Points(previousPoint, point);
                    }
                    else
                    {
                        point = previousPoint + (normal * sumTextDistanceAtSegment);
                        var curPreviousPoint = point - (normal * MTextOffset) + offsetPoint;
                                               ; 
                        CreateLinesBeetwin2Points(curPreviousPoint, point);
                        AcadUtils.WriteMessageInDebug($"текущая точка{point}  в else while");
                    }

                    sumTextDistanceAtSegment += MTextOffset;
                    k++;
                }

                var newCurPoint = currentPoint + offsetPoint;

                AcadUtils.WriteMessageInDebug(
                    $"точка {point} текущая точка {currentPoint} новая текущая точка {newCurPoint}");
                var newLastPoint = point + offsetPoint;
                CreateLinesBeetwin2Points(newLastPoint, newCurPoint);
            }
        }

        /// <summary>
        /// создание линий между 2 точками
        /// </summary>
        /// <param name="previousPoint"></param>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        private void CreateLinesBeetwin2Points(Point3d previousPoint, Point3d currentPoint)
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

            // 1. По двум точкам находим вектор, потом нормаль
            var segmentVector = currentPoint - previousPoint;
            _normal = segmentVector.GetNormal();

            var offsetDistance = (_texts[0].ActualWidth / 2) + TextMaskOffset * _scale;

            // 2. находим ближающую точку расположения мтекста
            // длина от первой точки до первого мтекста и строим от нее составные линии 
            var lengthOfPartSegment = (currentPoint - previousPoint).Length - offsetDistance;
            var strokesLength = _strokeSpaceParams.Sum();

            int rounds = (int) (lengthOfPartSegment / strokesLength);

            AcadUtils.WriteMessageInDebug($"{rounds} линий должно быть в длине {lengthOfPartSegment} ");

            var nextPoint = new Vector3d();
            var firstPoint = previousPoint;
            for (int i = 0; i < rounds; i++)
            {
                firstPoint += nextPoint;
                AcadUtils.WriteMessageInDebug($"начальная точка {firstPoint}");
                CreateAllLinesInStrokes(firstPoint);
                nextPoint = (_normal * strokesLength);
            }

            var pointAfterRounds = previousPoint + _normal * (strokesLength * rounds);
            var finalLength = lengthOfPartSegment - strokesLength * rounds;
            CreateLinesFromLength(finalLength, pointAfterRounds);
        }

        /// <summary>
        /// создает список линий из коллекции штрихов, длина линий это нечетные номера, проходимся
        /// по коллекции и создаем по одной линии на каждый нечетную цифру, потом этот метод будет
        /// повтортся в зависимости от длины заданных точек
        /// </summary>
        /// <param name="previousPoint">предыдущая точка</param>
        /// <param name="normal"> нормаль </param>
        /// <returns>возращаест список линий</returns>
        private void CreateAllLinesInStrokes(Point3d previousPoint)
        {
            for (var i = 0; i <= _strokeSpaceParams.Count; i++)
            {
                if (i % 2 == 0) continue;

                var line = GetLinesFromStrokesFromIteration(previousPoint, i);

                _lines.Add(line);
                AcadUtils.WriteMessageInDebug(
                    $"линия #{_lines.Count} длиной {line.Length} в точках{line.StartPoint} - {line.EndPoint} создана");
            }
        }

        /// <summary>
        /// построение линий в зависимости от оставщейся длины участка
        /// </summary>
        /// <param name="length"></param>
        /// <param name="position"></param>
        private void CreateLinesFromLength(double length, Point3d position)
        {
            var curLength = 0.0;
            for (var i = 1; i <= _strokeSpaceParams.Count; i++)
            {
                var curElemLength = _strokeSpaceParams[i - 1];
                if (i % 2 == 0)
                {
                    if (length < curElemLength) break;
                    length -= curElemLength;
                    continue;
                }

                if (curElemLength < length)
                {
                    var line = GetLinesFromStrokesFromIteration(position, i);
                    _lines.Add(line);
                    AcadUtils.WriteMessageInDebug(
                        $"добавлен отрезок длиной {line.Length} в точках{line.StartPoint} - {line.EndPoint} ");
                }
                else
                {
                    var newPosition = position + (_normal * _strokeSpaceParams.Take(i - 1).Sum());
                    var lastLength = length;

                    var line = GetLinesFromStrokes(newPosition, curLength, lastLength);
                    _lines.Add(line);
                    AcadUtils.WriteMessageInDebug(
                        $"добавлен недостающий отрезок длиной {line.Length} в точках{line.StartPoint} - {line.EndPoint} ");
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
            Point3d curPoint = previousPoint + (_normal * curLength);
            Point3d curNextPt = previousPoint + _normal * (curLength + curElemLength);
            Line line = new Line(curPoint, curNextPt);

            return line;
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

        private double GetDistance()
        {
            return MTextOffset * _scale;
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

        #region Mtext creation methods

        private void CreateMTextOnWholeLenghtOfPolyline()
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
                var mText = GetMTextAtDist(location);
                SetImmutablePropertiesToNestedEntity(mText);
                _texts.Add(mText);

                SetMTextAndWipeoutRotationAngle(mText, angle);
            }
        }

        private IEnumerable<MText> CreateMTextsByInsertionPoints(Point3dCollection points)
        {
            var mTexts = new List<MText>();
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
                    if (MTextOffset > segmentVector.Length)
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

                sumDistanceAtSegment += GetDistance();

                if (sumDistanceAtSegment >= segmentLength)
                {
                    break;
                }

                var textLocation = _mainPolyline.GetPointAtDist(distanceAtSegmentStart + sumDistanceAtSegment);
                var mText = GetMTextAtDist(textLocation);
                SetImmutablePropertiesToNestedEntity(mText);

                segmentMTextsDependencies.Add(mText);

                HideMTextBackground(mText);

                if (angle != 0)
                {
                    mText.Rotation = angle;
                }

                if (overflowIndex >= 1000)
                {
                    break;
                }
            }

            AcadUtils.WriteMessageInDebug($"количество текстов {segmentMTextsDependencies.Count}");

            return segmentMTextsDependencies;
        }

        private MText GetMTextAtDist(Point3d textLocation)
        {
            var textStyleId = AcadUtils.GetTextStyleIdByName(TextStyle);
            var textHeight = MainTextHeight * _scale;

            var mText = new MText
            {
                TextStyleId = textStyleId,
                Contents = GetTextContents(),
                TextHeight = textHeight,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = textLocation
            };

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

        #endregion

        private void SetNodeNumberOnCreation()
        {
            if (!IsValueCreated)
                return;

            MainText = "M+";
        }

        #endregion
    }
}