// ReSharper disable InconsistentNaming

using System.Linq;

namespace mpESKD.Functions.mpLetterLine
{
    using System;
    using System.Collections.Generic;
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
        /// Отступ первого текст в каждом сегменте полилинии
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "p36", LetterLineFirstTextOffset.ByHalfSpace, descLocalKey: "d36", nameSymbol: "a")]
        [SaveToXData]
        public LetterLineFirstTextOffset FirstStrokeOffset { get; set; } = LetterLineFirstTextOffset.ByHalfSpace;

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
        [PropertyVisibilityDependency(new[] { nameof(TextMaskOffset) })]
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
        [PropertyVisibilityDependency(new[] { nameof(SpaceAndFirstStrokeOffsetVisibilility) })]
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
        [PropertyVisibilityDependency(new[] { nameof(Space), nameof(FirstStrokeOffset) })]
        [SaveToXData]
        public bool SpaceAndFirstStrokeOffsetVisibilility { get; private set; }

        /// <summary>
        /// Тип вида (разрез или вид)
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
        public string StrokeFormula { get; set; } = "";

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
                    AcadUtils.WriteMessageInDebug("EndPointOCS.Equals(Point3d.Origin)");
                }
                else if (length < MinDistanceBetweenPoints * _scale && MiddlePoints.Count == 0)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    MakeSimplyEntity(UpdateVariant.SetEndPointMinLength);
                    AcadUtils.WriteMessageInDebug("length < MinDistanceBetweenPoints * scale && MiddlePoints.Count == 0");
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
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * _scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
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
            var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);
            _mainPolyline = new Polyline(points.Count);
            SetImmutablePropertiesToNestedEntity(_mainPolyline);
            for (var i = 0; i < points.Count; i++)
            {
                _mainPolyline.AddVertexAt(i, points[i], 0.0, 0.0, 0.0);
            }

            _texts.Clear();
            _mTextMasks.Clear();
            _segmentsCount = 0;
            if (!(_mainPolyline.Length >= MinDistanceBetweenPoints)) return;

            if (!LineGeneration)
            {
                for (var i = 1; i < _mainPolyline.NumberOfVertices; i++)
                {
                    var previousPoint = _mainPolyline.GetPoint3dAt(i - 1);
                    var currentPoint = _mainPolyline.GetPoint3dAt(i);
                    _texts.AddRange(CreateMTextsOnMainPolylineSegment(currentPoint, previousPoint));
                }
            }
            else
            {
                int mTextsQty = (int)(_mainPolyline.Length / MTextOffset);
                var offset = _mainPolyline.Length - mTextsQty * MTextOffset;
                AcadUtils.WriteMessageInDebug($"Длина полилинии {_mainPolyline.Length} - Расстояние между текстами {MTextOffset}, текстов поместится {mTextsQty} отступы с двух сторон {offset / 2}");

                for (int i = 0; i <= mTextsQty; i++)
                {
                    var distAtPline = offset / 2 + i * MTextOffset;
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
                        previousPoint = _mainPolyline.GetPoint3dAt((int)segmentParameterAtPoint);
                        currentPoint = _mainPolyline.GetPoint3dAt((int)segmentParameterAtPoint + 1);
                    }

                    var segmentVector = currentPoint - previousPoint;
                    var mText = GetMTextAtPolylineDist(distAtPline, segmentVector);
                    //AcadUtils.WriteMessageInDebug($"точка {location} номер сегмента {segmentParameterAtPoint} \n угол {angle * 180 / Math.PI}");
                    SetImmutablePropertiesToNestedEntity(mText);

                    HideMtextBackgoud(mText);

                    _texts.Add(mText);
                }
            }
        }

        private IEnumerable<MText> CreateMTextsOnMainPolylineSegment(
            Point3d currentPoint, Point3d previousPoint)
        {
            AcadUtils.WriteMessageInDebug($"длина полилинии {_mainPolyline.Length}");
            var segmentMTextsDependencies = new List<MText>();
            var segmentVector = currentPoint - previousPoint;
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

                var mText = GetMTextAtPolylineDist(distanceAtSegmentStart + sumDistanceAtSegment, segmentVector);
                SetImmutablePropertiesToNestedEntity(mText);

                segmentMTextsDependencies.Add(mText);

                HideMtextBackgoud(mText);

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
                    i++;
                    var lenth = InsertionPoint - text.Location;
                    AcadUtils.WriteMessageInDebug($"Положение {i} текста {lenth.Length}");
                    var strokeSpaceParams = GetStrokesData(lenth.Length);
                    if (strokeSpaceParams == null) continue;
                    for (var j = 0; j < strokeSpaceParams.Count; j++)
                    {
                        AcadUtils.WriteMessageInDebug(j % 2 == 0
                            ? $" {j} это будет штрих длиной {strokeSpaceParams[j]}"
                            : $" {j} это будет пробел длиной {strokeSpaceParams[j]}");
                    }
                }
            }

            return segmentMTextsDependencies;
        }

        private List<double> GetStrokesData(double segmentLength)
        {
            var formulaSplit = StrokeFormula.Split('-');

            return formulaSplit.Select(s => string.IsNullOrEmpty(s) ? 0 : int.Parse(s)).Select(d => (double) d).ToList();
        }

        private double GetDistance(double sumDistanceAtSegment)
        {
            double distance;
            if (Math.Abs(sumDistanceAtSegment) < 0.0001)
            {
                if (FirstStrokeOffset == LetterLineFirstTextOffset.ByHalfSpace)
                {
                    distance = Space / 2 * _scale;
                }
                else if (FirstStrokeOffset == LetterLineFirstTextOffset.BySpace)
                {
                    distance = Space * _scale;
                }
                else
                {
                    distance = MTextOffset * _scale;
                }
            }
            else
            {
                distance = MTextOffset * _scale;
            }

            return distance;
        }

        private MText GetMTextAtPolylineDist(double polylineDist, Vector3d segmentVector)
        {
            var firstStrokePoint = _mainPolyline.GetPointAtDist(polylineDist);

            var textStyleId = AcadUtils.GetTextStyleIdByName(TextStyle);
            var textHeight = MainTextHeight * _scale;

            var angle = Math.Atan2(segmentVector.Y, segmentVector.X);
            if (IsTextAlwaysHorizontal)
            {
                angle = 0;
            }

            var mText = new MText
            {
                TextStyleId = textStyleId,
                Contents = GetTextContents(),
                TextHeight = textHeight,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = firstStrokePoint,
                Rotation = angle
            };

            return mText;
        }

        private void HideMtextBackgoud(MText mText)
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

        private static Point2dCollection GetPointsForMainPolyline(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var points = new Point2dCollection();

            points.Add(insertionPoint.ToPoint2d());
            middlePoints.ForEach(p => points.Add(p.ToPoint2d()));
            points.Add(endPoint.ToPoint2d());

            return points;
        }

        #endregion
    }
}