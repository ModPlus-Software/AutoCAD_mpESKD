// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpAxis;

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
/// Прямая ось
/// </summary>
[SmartEntityDisplayNameKey("h41")]
[SystemStyleDescriptionKey("h68")]
public class Axis : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    // last values
    private readonly string _lastHorizontalValue = string.Empty;
    private readonly string _lastVerticalValue = string.Empty;
        
    private int _bottomFractureOffset;
    private int _markersCount = 1;
    private bool _bottomOrientMarkerVisible;
    private bool _topOrientMarkerVisible;
    private Point3d _topMarkerPoint;
    private Point3d _bottomMarkerPoint;

    #region Entities

    /// <summary>
    /// Средняя (основная) линия оси
    /// </summary>
    private Line _mainLine;

    private Line _bottomOrientLine;

    private Line _topOrientLine;

    private Polyline _bottomOrientArrow;

    private Polyline _topOrientArrow;

    #region Fractures

    /// <summary>"Палочка" от конечной точки до кружка (маркера)</summary>
    private Line _bottomMarkerLine;

    /// <summary>Палочка отступа нижнего излома</summary>
    private Line _bottomFractureOffsetLine;

    /// <summary>Палочка отступа верхнего излома</summary>
    private Line _topFractureOffsetLine;

    /// <summary>Палочка от точки вставки до кружка (маркера)</summary>
    private Line _topMarkerLine;

    #endregion

    #region Circles

    #region Bottom

    private Circle _bottomFirstMarker;

    private Circle _bottomFirstMarkerType2;

    private Circle _bottomSecondMarker;

    private Circle _bottomSecondMarkerType2;

    private Circle _bottomThirdMarker;

    private Circle _bottomThirdMarkerType2;

    #endregion

    #region Top

    private Circle _topFirstMarker;

    private Circle _topFirstMarkerType2;

    private Circle _topSecondMarker;

    private Circle _topSecondMarkerType2;

    private Circle _topThirdMarker;

    private Circle _topThirdMarkerType2;

    #endregion

    #region Orient

    private Circle _bottomOrientMarker;

    private Circle _bottomOrientMarkerType2;

    private Circle _topOrientMarker;

    private Circle _topOrientMarkerType2;

    #endregion

    #endregion

    #region Texts

    private DBText _bottomFirstDBText;
    private Wipeout _bottomFirstTextMask;

    private DBText _topFirstDBText;
    private Wipeout _topFirstTextMask;

    private DBText _bottomSecondDBText;
    private Wipeout _bottomSecondTextMask;

    private DBText _topSecondDBText;
    private Wipeout _topSecondTextMask;

    private DBText _bottomThirdDBText;
    private Wipeout _bottomThirdTextMask;

    private DBText _topThirdDBText;
    private Wipeout _topThirdTextMask;

    private DBText _bottomOrientDBText;
    private Wipeout _bottomOrientTextMask;

    private DBText _topOrientDBText;
    private Wipeout _topOrientTextMask;

    #endregion

    #endregion
        
    /// <summary>
    /// Initializes a new instance of the <see cref="Axis"/> class.
    /// </summary>
    public Axis()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Axis"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public Axis(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Инициализация экземпляра класса для BreakLine для создания
    /// </summary>
    /// <param name="lastHorizontalValue">Значение последней горизонтальной оси</param>
    /// <param name="lastVerticalValue">Значение последней вертикальной оси</param>
    public Axis(string lastHorizontalValue, string lastVerticalValue)
    {
        // last values
        _lastHorizontalValue = lastHorizontalValue;
        _lastVerticalValue = lastVerticalValue;
    }

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 1.0;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.General, 4, "p19", "осевая")]
    public override string LineType { get; set; } = "осевая";

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.General, 5, "p6", 10.0, 0.0, 1.0000E+99, descLocalKey: "d6")]
    public override double LineTypeScale { get; set; } = 10;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p17", "Standard", descLocalKey: "d17")]
    public override string TextStyle { get; set; } = "Standard";
        
    /// <summary>Положение маркеров</summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p8", AxisMarkersPosition.Bottom, descLocalKey: "d8")]
    [SaveToXData]
    public AxisMarkersPosition MarkersPosition { get; set; } = AxisMarkersPosition.Bottom;

    /// <summary>Излом</summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p9", 10, 1, 20, descLocalKey: "d9", nameSymbol: "b")]
    [SaveToXData]
    public int Fracture { get; set; } = 10;

    /// <summary>Нижний отступ излома</summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p15", 0, 0, 30, descLocalKey: "d15", nameSymbol: "a")]
    [SaveToXData]
    public int BottomFractureOffset
    {
        get => _bottomFractureOffset;
        set
        {
            var oldFracture = BottomFractureOffset;
            _bottomFractureOffset = value;

            // нужно сместить зависимые точки
            var vecNorm = (EndPoint - InsertionPoint).GetNormal() * (value - oldFracture) * GetScale();
            BottomOrientPoint += vecNorm;
        }
    }

    /// <summary>Верхний отступ излома</summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p16", 0, 0, 30, descLocalKey: "d16", nameSymbol: "a")]
    [SaveToXData]
    public int TopFractureOffset { get; set; } = 0;

    /// <summary>Диаметр маркеров</summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p10", 10, 6, 12, descLocalKey: "d10", nameSymbol: "d")]
    [SaveToXData]
    public int MarkersDiameter { get; set; } = 10;

    /// <summary>Количество маркеров</summary>
    [EntityProperty(PropertiesCategory.Geometry, 6, "p11", 1, 1, 3, descLocalKey: "d11")]
    [SaveToXData]
    public int MarkersCount
    {
        get => _markersCount;
        set
        {
            _markersCount = value;
            if (value == 1)
            {
                SecondTextVisibility = false;
                ThirdTextVisibility = false;
            }
            else if (value == 2)
            {
                SecondTextVisibility = true;
                ThirdTextVisibility = false;
            }
            else if (value == 3)
            {
                SecondTextVisibility = true;
                ThirdTextVisibility = true;
            }
        }
    }

    /// <summary>
    /// Тип первого маркера: Type 1 - один кружок, Type 2 - два кружка
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 7, "p12", AxisMarkerType.Type1, descLocalKey: "d12")]
    [SaveToXData]
    public AxisMarkerType FirstMarkerType { get; set; } = AxisMarkerType.Type1;

    /// <summary>
    /// Тип второго маркера: Type 1 - один кружок, Type 2 - два кружка
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 8, "p13", AxisMarkerType.Type1, descLocalKey: "d13")]
    [SaveToXData]
    public AxisMarkerType SecondMarkerType { get; set; } = AxisMarkerType.Type1;

    /// <summary>
    /// Тип третьего маркера: Type 1 - один кружок, Type 2 - два кружка
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 8, "p14", AxisMarkerType.Type1, descLocalKey: "d14")]
    [SaveToXData]
    public AxisMarkerType ThirdMarkerType { get; set; } = AxisMarkerType.Type1;

    // Orient markers

    /// <summary>
    /// Видимость нижнего бокового кружка
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 9, "p32", false, descLocalKey: "d32")]
    [PropertyVisibilityDependency(new[] { nameof(BottomOrientText) })]
    [SaveToXData]
    public bool BottomOrientMarkerVisible
    {
        get => _bottomOrientMarkerVisible;
        set
        {
            _bottomOrientMarkerVisible = value;
            if (value)
            {
                OrientMarkerVisibilityDependency = true;
            }
            else if (!TopOrientMarkerVisible)
            {
                OrientMarkerVisibilityDependency = false;
            }
        }
    }

    /// <summary>
    /// Видимость верхнего бокового кружка
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 10, "p33", false, descLocalKey: "d33")]
    [PropertyVisibilityDependency(new[] { nameof(TopOrientText) })]
    [SaveToXData]
    public bool TopOrientMarkerVisible
    {
        get => _topOrientMarkerVisible;
        set
        {
            _topOrientMarkerVisible = value;
            if (value)
            {
                OrientMarkerVisibilityDependency = true;
            }
            else if (!BottomOrientMarkerVisible)
            {
                OrientMarkerVisibilityDependency = false;
            }
        }
    }

    [EntityProperty(PropertiesCategory.Geometry, 10, "", "", propertyScope: PropertyScope.Hidden)]
    [PropertyVisibilityDependency(new[] { nameof(OrientMarkerType), nameof(ArrowsSize) })]
    [SaveToXData]
    public bool OrientMarkerVisibilityDependency { get; private set; }

    [EntityProperty(PropertiesCategory.Geometry, 11, "p34", AxisMarkerType.Type1, descLocalKey: "d34")]
    [SaveToXData]
    public AxisMarkerType OrientMarkerType { get; set; } = AxisMarkerType.Type1;

    /// <summary>
    /// Размер стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 12, "p29", 3, 0, 10, nameSymbol: "c")]
    [SaveToXData]
    public int ArrowsSize { get; set; } = 3;

    // Отступы маркеров-ориентиров
    [SaveToXData]
    public double BottomOrientMarkerOffset { get; set; } = double.NaN;

    [SaveToXData]
    public double TopOrientMarkerOffset { get; set; } = double.NaN;

    //// текст и текстовые значения

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p18", 3.5, 0.000000001, 1.0000E+99, descLocalKey: "d18", nameSymbol: "h")]
    [SaveToXData]
    public double TextHeight { get; set; } = 3.5;
        
    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 3, "p85", false, descLocalKey: "d85")]
    [PropertyVisibilityDependency(new[] { nameof(TextMaskOffset) })]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 4, "p86", 0.5, 0.0, 5.0)]
    [SaveToXData]
    public double TextMaskOffset { get; set; } = 0.5;

    /// <summary>
    /// Угол поворота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 5, "p71", 0, 0, 359)]
    [SaveToXData]
    public int TextRotationAngle { get; set; } = 0;

    [EntityProperty(PropertiesCategory.Content, 6, "p20", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string FirstTextPrefix { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 7, "p22", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string FirstText { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 8, "p21", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string FirstTextSuffix { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 9, "", "", propertyScope: PropertyScope.Hidden)]
    [PropertyVisibilityDependency(new[] { nameof(SecondText), nameof(SecondTextPrefix), nameof(SecondTextSuffix), nameof(SecondMarkerType) })]
    [SaveToXData]
    public bool SecondTextVisibility { get; set; }

    [EntityProperty(PropertiesCategory.Content, 10, "p23", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string SecondTextPrefix { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 11, "p25", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string SecondText { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 12, "p24", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string SecondTextSuffix { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 13, "", "", propertyScope: PropertyScope.Hidden)]
    [PropertyVisibilityDependency(new[] { nameof(ThirdText), nameof(ThirdTextPrefix), nameof(ThirdTextSuffix), nameof(ThirdMarkerType) })]
    [SaveToXData]
    public bool ThirdTextVisibility { get; set; }

    [EntityProperty(PropertiesCategory.Content, 14, "p26", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string ThirdTextPrefix { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 15, "p28", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string ThirdText { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 16, "p27", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string ThirdTextSuffix { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 17, "p30", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string BottomOrientText { get; set; } = string.Empty;

    [EntityProperty(PropertiesCategory.Content, 18, "p31", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string TopOrientText { get; set; } = string.Empty;

    /// <summary>Средняя точка. Нужна для перемещения  примитива</summary>
    public Point3d MiddlePoint => new Point3d(
        (InsertionPoint.X + EndPoint.X) / 2,
        (InsertionPoint.Y + EndPoint.Y) / 2,
        (InsertionPoint.Z + EndPoint.Z) / 2);

    [SaveToXData]
    public double BottomLineAngle { get; set; }

    /// <summary>
    /// Нижняя точка расположения маркеров
    /// </summary>  
    public Point3d BottomMarkerPoint
    {
        get
        {
            var baseVector = new Vector3d(1.0, 0.0, 0.0);
            var angleA = baseVector.GetAngleTo(EndPoint - InsertionPoint, Vector3d.ZAxis);
            var bottomLineLength = Fracture / Math.Cos(BottomLineAngle) * GetFullScale();
            _bottomMarkerPoint = new Point3d(
                EndPoint.X + (bottomLineLength * Math.Cos(angleA + BottomLineAngle)),
                EndPoint.Y + (bottomLineLength * Math.Sin(angleA + BottomLineAngle)),
                EndPoint.Z);
            return _bottomMarkerPoint + ((EndPoint - InsertionPoint).GetNormal() * BottomFractureOffset * GetFullScale());
        }

        set
        {
            _bottomMarkerPoint = value;
            BottomLineAngle = (EndPoint - InsertionPoint).GetAngleTo(value - EndPoint - ((EndPoint - InsertionPoint).GetNormal() * BottomFractureOffset * GetFullScale()), Vector3d.ZAxis);
        }
    }

    [SaveToXData]
    public double TopLineAngle { get; set; }

    /// <summary>
    /// Верхняя точка расположения маркеров
    /// </summary>
    public Point3d TopMarkerPoint
    {
        get
        {
            var baseVector = new Vector3d(1.0, 0.0, 0.0);
            var angleA = baseVector.GetAngleTo(InsertionPoint - EndPoint, Vector3d.ZAxis);
            var topLineLength = Fracture / Math.Cos(TopLineAngle) * GetFullScale();
            _topMarkerPoint = new Point3d(
                InsertionPoint.X + (topLineLength * Math.Cos(angleA + TopLineAngle)),
                InsertionPoint.Y + (topLineLength * Math.Sin(angleA + TopLineAngle)),
                InsertionPoint.Z);
            return _topMarkerPoint + ((InsertionPoint - EndPoint).GetNormal() * TopFractureOffset * GetFullScale());
        }

        set
        {
            _topMarkerPoint = value;
            TopLineAngle = (InsertionPoint - EndPoint).GetAngleTo(value - InsertionPoint - ((InsertionPoint - EndPoint).GetNormal() * TopFractureOffset * GetFullScale()), Vector3d.ZAxis);
        }
    }

    /// <summary>
    /// Нижняя точка маркера ориентира
    /// </summary>
    public Point3d BottomOrientPoint
    {
        get
        {
            var mainLineVectorNormal = (EndPoint - InsertionPoint).GetPerpendicularVector().GetNormal();
            if (double.IsNaN(BottomOrientMarkerOffset) || Math.Abs(BottomOrientMarkerOffset) < 0.0001)
            {
                BottomOrientMarkerOffset = MarkersDiameter + 10.0;
            }

            return BottomMarkerPoint + (mainLineVectorNormal * BottomOrientMarkerOffset * GetFullScale());
        }

        set
        {
            var mainLineVectorNormal = (EndPoint - InsertionPoint).GetPerpendicularVector().GetNormal();
            var vector = value - BottomMarkerPoint;
            BottomOrientMarkerOffset = mainLineVectorNormal.DotProduct(vector) / GetScale() / BlockTransform.GetScale();
        }
    }

    /// <summary>
    /// Верхняя точка маркера ориентации
    /// </summary>
    public Point3d TopOrientPoint
    {
        get
        {
            var mainLineVectorNormal = (InsertionPoint - EndPoint).GetPerpendicularVector().GetNormal();
            if (double.IsNaN(TopOrientMarkerOffset) || Math.Abs(TopOrientMarkerOffset) < 0.0001)
            {
                TopOrientMarkerOffset = MarkersDiameter + 10.0;
            }

            return TopMarkerPoint - (mainLineVectorNormal * TopOrientMarkerOffset * GetFullScale());
        }

        set
        {
            var mainLineVectorNormal = (EndPoint - InsertionPoint).GetPerpendicularVector().GetNormal();
            var vector = value - TopMarkerPoint;
            TopOrientMarkerOffset = mainLineVectorNormal.DotProduct(vector) / GetScale() / BlockTransform.GetScale();
        }
    }
        
    // Получение управляющих точек в системе координат блока для отрисовки содержимого
    private Point3d BottomMarkerPointOCS => BottomMarkerPoint.TransformBy(BlockTransform.Inverse());
        
    private Point3d TopMarkerPointOCS => TopMarkerPoint.TransformBy(BlockTransform.Inverse());

    private Point3d BottomOrientPointOCS => BottomOrientPoint.TransformBy(BlockTransform.Inverse());

    private Point3d TopOrientPointOCS => TopOrientPoint.TransformBy(BlockTransform.Inverse());

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _bottomFirstTextMask,
                _topFirstTextMask,
                _bottomSecondTextMask,
                _topSecondTextMask,
                _bottomThirdTextMask,
                _topThirdTextMask,
                _bottomOrientTextMask,
                _topOrientTextMask,

                _bottomOrientLine,
                _topOrientLine,
                _bottomOrientArrow,
                _topOrientArrow,
                _bottomMarkerLine,
                _bottomFractureOffsetLine,
                _topFractureOffsetLine,
                _topMarkerLine,
                _bottomFirstMarker,
                _bottomFirstMarkerType2,
                _bottomSecondMarker,
                _bottomSecondMarkerType2,
                _bottomThirdMarker,
                _bottomThirdMarkerType2,
                _topFirstMarker,
                _topFirstMarkerType2,
                _topSecondMarker,
                _topSecondMarkerType2,
                _topThirdMarker,
                _topThirdMarkerType2,
                _bottomOrientMarker,
                _bottomOrientMarkerType2,
                _topOrientMarker,
                _topOrientMarkerType2,
                _bottomFirstDBText,
                _topFirstDBText,
                _bottomSecondDBText,
                _topSecondDBText,
                _bottomThirdDBText,
                _topThirdDBText,
                _bottomOrientDBText,
                _topOrientDBText
            };

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            SetChangeablePropertiesToNestedEntity(_mainLine);

            entities.Add(_mainLine);

            return entities;
        }
    }
        
    /// <inheritdoc/>
    protected override void ProcessScaleChange(AnnotationScale oldScale, AnnotationScale newScale)
    {
        base.ProcessScaleChange(oldScale, newScale);
        if (oldScale != null && newScale != null)
        {
            if (MainSettings.Instance.AxisLineTypeScaleProportionScale)
            {
                LineTypeScale = LineTypeScale * newScale.GetNumericScale() / oldScale.GetNumericScale();
            }
        }
    }

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
            
        if (MarkersPosition == AxisMarkersPosition.Both ||
            MarkersPosition == AxisMarkersPosition.Bottom)
        {
            yield return BottomMarkerPoint;
            if (BottomOrientMarkerVisible)
            {
                yield return BottomOrientPoint;
            }
        }

        if (MarkersPosition == AxisMarkersPosition.Both ||
            MarkersPosition == AxisMarkersPosition.Top)
        {
            yield return TopMarkerPoint;
            if (TopOrientMarkerVisible)
            {
                yield return TopOrientPoint;
            }
        }
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            var length = EndPointOCS.DistanceTo(InsertionPointOCS);
            var scale = GetScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки (т.е. второй точки еще нет)
                MakeSimplyEntity(UpdateVariant.SetInsertionPoint, scale);
            }
            else if (length < MinDistanceBetweenPoints * scale)
            {
                // Задание второй точки - случай когда расстояние между точками меньше минимального
                MakeSimplyEntity(UpdateVariant.SetEndPointMinLength, scale);
            }
            else
            {
                // Задание второй точки
                SetEntitiesPoints(InsertionPointOCS, EndPointOCS, BottomMarkerPointOCS, TopMarkerPointOCS, scale);
            }

            UpdateTextEntities();
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    /// <summary>
    /// Построение "базового" простого варианта ЕСКД примитива
    /// Тот вид, который висит на мышке при создании и указании точки вставки
    /// </summary>
    private void MakeSimplyEntity(UpdateVariant variant, double scale)
    {
        // Создание вершин полилинии
        if (variant == UpdateVariant.SetInsertionPoint)
        {
            /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
             * Примерно аналогично созданию, только точки не создаются, а меняются
            */
            var tmpEndPoint = new Point3d(
                InsertionPointOCS.X, InsertionPointOCS.Y - (MinDistanceBetweenPoints * scale), InsertionPointOCS.Z);
            var tmpBottomMarkerPoint = new Point3d(tmpEndPoint.X, tmpEndPoint.Y - (Fracture * scale), tmpEndPoint.Z);
            var tmpTopMarkerPoint = new Point3d(InsertionPointOCS.X, InsertionPointOCS.Y + (Fracture * scale), InsertionPointOCS.Z);

            SetEntitiesPoints(InsertionPointOCS, tmpEndPoint, tmpBottomMarkerPoint, tmpTopMarkerPoint, scale);
        }
        else if (variant == UpdateVariant.SetEndPointMinLength) //// изменение вершин полилинии
        {
            /* Изменение базовых примитивов в момент указания второй точки
            * при условии что расстояние от второй точки до первой больше минимального допустимого
            */
            var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);
            SetEntitiesPoints(InsertionPointOCS, tmpEndPoint, BottomMarkerPointOCS, TopMarkerPointOCS, scale);
            EndPoint = tmpEndPoint.TransformBy(BlockTransform);
        }
    }

    /// <summary>
    /// Изменение примитивов по точкам
    /// </summary>
    private void SetEntitiesPoints(Point3d insertionPoint, Point3d endPoint, Point3d bottomMarkerPoint, Point3d topMarkerPoint, double scale)
    {
        var textHeight = TextHeight * GetScale();
            
        // main line
        _mainLine = new Line
        {
            StartPoint = insertionPoint,
            EndPoint = endPoint
        };
        var mainVector = endPoint - insertionPoint;

        #region Bottom

        if (MarkersPosition == AxisMarkersPosition.Both ||
            MarkersPosition == AxisMarkersPosition.Bottom)
        {
            var firstMarkerCenter = bottomMarkerPoint + (mainVector.GetNormal() * MarkersDiameter / 2 * scale);

            // bottom line
            var bottomLineStartPoint = endPoint + (mainVector.GetNormal() * BottomFractureOffset * scale);
            if (BottomFractureOffset > 0)
            {
                _bottomFractureOffsetLine = new Line
                {
                    StartPoint = endPoint,
                    EndPoint = bottomLineStartPoint
                };
            }

            var markerLineVector = firstMarkerCenter - bottomLineStartPoint;
            _bottomMarkerLine = new Line
            {
                StartPoint = bottomLineStartPoint,
                EndPoint = bottomLineStartPoint + (markerLineVector.GetNormal() * (markerLineVector.Length - (MarkersDiameter * scale / 2.0)))
            };

            // markers
            _bottomFirstMarker = new Circle
            {
                Center = firstMarkerCenter,
                Diameter = MarkersDiameter * scale
            };

            // text
            if (!string.IsNullOrEmpty(FirstTextPrefix) ||
                !string.IsNullOrEmpty(FirstText) ||
                !string.IsNullOrEmpty(FirstTextSuffix))
            {
                _bottomFirstDBText = new DBText();
                _bottomFirstDBText.SetProperties(TextStyle, textHeight);
                _bottomFirstDBText.Position = firstMarkerCenter;
            }
            else
            {
                _bottomFirstDBText = null;
            }

            // Второй кружок первого маркера
            if (FirstMarkerType == AxisMarkerType.Type2)
            {
                _bottomFirstMarkerType2 = new Circle
                {
                    Center = firstMarkerCenter,
                    Diameter = (MarkersDiameter - 2) * scale
                };
            }

            // Если количество маркеров больше 1
            if (MarkersCount > 1)
            {
                // Значит второй маркер точно есть (независимо от 3-го)
                var secondMarkerCenter = firstMarkerCenter + (mainVector.GetNormal() * MarkersDiameter * scale);
                _bottomSecondMarker = new Circle
                {
                    Center = secondMarkerCenter,
                    Diameter = MarkersDiameter * scale
                };

                // text
                if (!string.IsNullOrEmpty(SecondTextPrefix) ||
                    !string.IsNullOrEmpty(SecondText) ||
                    !string.IsNullOrEmpty(SecondTextSuffix))
                {
                    _bottomSecondDBText = new DBText();
                    _bottomSecondDBText.SetProperties(TextStyle, textHeight);
                    _bottomSecondDBText.Position = secondMarkerCenter;
                }
                else
                {
                    _bottomSecondDBText = null;
                }

                // второй кружок второго маркера
                if (SecondMarkerType == AxisMarkerType.Type2)
                {
                    _bottomSecondMarkerType2 = new Circle
                    {
                        Center = secondMarkerCenter,
                        Diameter = (MarkersDiameter - 2) * scale
                    };
                }

                // Если количество маркеров больше двух, тогда рисую 3-ий маркер
                if (MarkersCount > 2)
                {
                    var thirdMarkerCenter = secondMarkerCenter + (mainVector.GetNormal() * MarkersDiameter * scale);
                    _bottomThirdMarker = new Circle
                    {
                        Center = thirdMarkerCenter,
                        Diameter = MarkersDiameter * scale
                    };

                    // text
                    if (!string.IsNullOrEmpty(ThirdTextPrefix) ||
                        !string.IsNullOrEmpty(ThirdText) ||
                        !string.IsNullOrEmpty(ThirdTextSuffix))
                    {
                        _bottomThirdDBText = new DBText();
                        _bottomThirdDBText.SetProperties(TextStyle, textHeight);
                        _bottomThirdDBText.Position = thirdMarkerCenter;
                    }
                    else
                    {
                        _bottomThirdDBText = null;
                    }

                    // второй кружок третьего маркера
                    if (ThirdMarkerType == AxisMarkerType.Type2)
                    {
                        _bottomThirdMarkerType2 = new Circle
                        {
                            Center = thirdMarkerCenter,
                            Diameter = (MarkersDiameter - 2) * scale
                        };
                    }
                }
            }

            #region Orient marker

            if (BottomOrientMarkerVisible)
            {
                var bottomOrientMarkerCenter = BottomOrientPointOCS + (mainVector.GetNormal() * MarkersDiameter / 2.0 * scale);
                _bottomOrientMarker = new Circle
                {
                    Center = bottomOrientMarkerCenter,
                    Diameter = MarkersDiameter * scale
                };

                // line
                var bottomOrientLineStartPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    firstMarkerCenter, bottomOrientMarkerCenter, firstMarkerCenter,
                    MarkersDiameter / 2.0 * scale);
                var bottomOrientLineEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    bottomOrientMarkerCenter, firstMarkerCenter, bottomOrientMarkerCenter,
                    MarkersDiameter / 2.0 * scale);

                if (!bottomOrientLineEndPoint.IsEqualTo(bottomOrientLineStartPoint, Tolerance.Global))
                {
                    _bottomOrientLine = new Line
                    {
                        StartPoint = bottomOrientLineStartPoint,
                        EndPoint = bottomOrientLineEndPoint
                    };

                    // arrow
                    if (!(Math.Abs((bottomOrientLineEndPoint - bottomOrientLineStartPoint).Length) < ArrowsSize * scale) &&
                        ArrowsSize != 0)
                    {
                        _bottomOrientArrow = new Polyline(2);
                        var arrowStartPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                            bottomOrientLineEndPoint,
                            bottomOrientLineStartPoint,
                            bottomOrientLineEndPoint, ArrowsSize * scale);
                        _bottomOrientArrow.AddVertexAt(0, arrowStartPoint.ToPoint2d(), 0.0, ArrowsSize * scale * 1 / 3, 0.0);
                        _bottomOrientArrow.AddVertexAt(1, bottomOrientLineEndPoint.ToPoint2d(), 0.0, 0.0, 0.0);
                    }
                }

                // text
                if (!string.IsNullOrEmpty(BottomOrientText))
                {
                    _bottomOrientDBText = new DBText();
                    _bottomOrientDBText.SetProperties(TextStyle, textHeight);
                    _bottomOrientDBText.Position = bottomOrientMarkerCenter;
                }
                else
                {
                    _bottomOrientDBText = null;
                }

                // type2
                if (OrientMarkerType == AxisMarkerType.Type2)
                {
                    _bottomOrientMarkerType2 = new Circle
                    {
                        Center = bottomOrientMarkerCenter,
                        Diameter = (MarkersDiameter - 2) * scale
                    };
                }
            }

            #endregion
        }
        else
        {
            _bottomFractureOffsetLine = null;
            _bottomMarkerLine = null;
            _bottomOrientArrow = null;
            _bottomOrientLine = null;
            _bottomFirstDBText = null;
            _bottomFirstMarker = null;
            _bottomFirstMarkerType2 = null;
            _bottomSecondDBText = null;
            _bottomSecondMarker = null;
            _bottomSecondMarkerType2 = null;
            _bottomOrientDBText = null;
            _bottomOrientMarkerType2 = null;
            _bottomOrientMarker = null;
            _bottomThirdDBText = null;
            _bottomThirdMarker = null;
            _bottomThirdMarkerType2 = null;
        }

        #endregion

        #region Top

        if (MarkersPosition == AxisMarkersPosition.Both ||
            MarkersPosition == AxisMarkersPosition.Top)
        {
            var firstMarkerCenter = topMarkerPoint - (mainVector.GetNormal() * MarkersDiameter / 2 * scale);

            // top line
            var topLineStartPoint = insertionPoint - (mainVector.GetNormal() * TopFractureOffset * scale);
            if (TopFractureOffset > 0)
            {
                _topFractureOffsetLine = new Line
                {
                    StartPoint = insertionPoint,
                    EndPoint = topLineStartPoint
                };
            }

            var markerLineVector = firstMarkerCenter - topLineStartPoint;
            _topMarkerLine = new Line
            {
                StartPoint = topLineStartPoint,
                EndPoint = topLineStartPoint + (markerLineVector.GetNormal() * (markerLineVector.Length - (MarkersDiameter * scale / 2.0)))
            };

            // markers
            _topFirstMarker = new Circle
            {
                Center = firstMarkerCenter,
                Diameter = MarkersDiameter * scale
            };

            // text
            if (!string.IsNullOrEmpty(FirstTextPrefix) ||
                !string.IsNullOrEmpty(FirstText) ||
                !string.IsNullOrEmpty(FirstTextSuffix))
            {
                _topFirstDBText = new DBText();
                _topFirstDBText.SetProperties(TextStyle, textHeight);
                _topFirstDBText.Position = firstMarkerCenter;
            }
            else
            {
                _topFirstDBText = null;
            }

            // Второй кружок первого маркера
            if (FirstMarkerType == AxisMarkerType.Type2)
            {
                _topFirstMarkerType2 = new Circle
                {
                    Center = firstMarkerCenter,
                    Diameter = (MarkersDiameter - 2) * scale
                };
            }

            // Если количество маркеров больше 1
            if (MarkersCount > 1)
            {
                // Значит второй маркер точно есть (независимо от 3-го)
                var secondMarkerCenter = firstMarkerCenter - (mainVector.GetNormal() * MarkersDiameter * scale);
                _topSecondMarker = new Circle
                {
                    Center = secondMarkerCenter,
                    Diameter = MarkersDiameter * scale
                };

                // text
                if (!string.IsNullOrEmpty(SecondTextPrefix) ||
                    !string.IsNullOrEmpty(SecondText) ||
                    !string.IsNullOrEmpty(SecondTextSuffix))
                {
                    _topSecondDBText = new DBText();
                    _topSecondDBText.SetProperties(TextStyle, textHeight);
                    _topSecondDBText.Position = secondMarkerCenter;
                }
                else
                {
                    _topSecondDBText = null;
                }

                // второй кружок второго маркера
                if (SecondMarkerType == AxisMarkerType.Type2)
                {
                    _topSecondMarkerType2 = new Circle
                    {
                        Center = secondMarkerCenter,
                        Diameter = (MarkersDiameter - 2) * scale
                    };
                }

                // Если количество маркеров больше двух, тогда рисую 3-ий маркер
                if (MarkersCount > 2)
                {
                    var thirdMarkerCenter = secondMarkerCenter - (mainVector.GetNormal() * MarkersDiameter * scale);
                    _topThirdMarker = new Circle
                    {
                        Center = thirdMarkerCenter,
                        Diameter = MarkersDiameter * scale
                    };

                    // text
                    if (!string.IsNullOrEmpty(ThirdTextPrefix) ||
                        !string.IsNullOrEmpty(ThirdText) ||
                        !string.IsNullOrEmpty(ThirdTextSuffix))
                    {
                        _topThirdDBText = new DBText();
                        _topThirdDBText.SetProperties(TextStyle, textHeight);
                        _topThirdDBText.Position = thirdMarkerCenter;
                    }
                    else
                    {
                        _topThirdDBText = null;
                    }

                    // второй кружок третьего маркера
                    if (ThirdMarkerType == AxisMarkerType.Type2)
                    {
                        _topThirdMarkerType2 = new Circle
                        {
                            Center = thirdMarkerCenter,
                            Diameter = (MarkersDiameter - 2) * scale
                        };
                    }
                }
            }

            #region Orient marker

            if (TopOrientMarkerVisible)
            {
                var topOrientMarkerCenter = TopOrientPointOCS - (mainVector.GetNormal() * MarkersDiameter / 2.0 * scale);
                _topOrientMarker = new Circle
                {
                    Center = topOrientMarkerCenter,
                    Diameter = MarkersDiameter * scale
                };

                // line
                var topOrientLineStartPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    firstMarkerCenter, topOrientMarkerCenter, firstMarkerCenter,
                    MarkersDiameter / 2.0 * scale);
                var topOrientLineEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    topOrientMarkerCenter, firstMarkerCenter, topOrientMarkerCenter,
                    MarkersDiameter / 2.0 * scale);
                if (!topOrientLineEndPoint.IsEqualTo(topOrientLineStartPoint, Tolerance.Global))
                {
                    _topOrientLine = new Line
                    {
                        StartPoint = topOrientLineStartPoint,
                        EndPoint = topOrientLineEndPoint
                    };

                    // arrow
                    if (!(Math.Abs((topOrientLineEndPoint - topOrientLineStartPoint).Length) < ArrowsSize * scale) &&
                        ArrowsSize != 0)
                    {
                        _topOrientArrow = new Polyline(2);

                        // arrow draw
                        var arrowStartPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                            topOrientLineEndPoint,
                            topOrientLineStartPoint,
                            topOrientLineEndPoint, ArrowsSize * scale);
                        _topOrientArrow.AddVertexAt(0, arrowStartPoint.ToPoint2d(), 0.0, ArrowsSize * scale * 1 / 3, 0.0);
                        _topOrientArrow.AddVertexAt(1, topOrientLineEndPoint.ToPoint2d(), 0.0, 0.0, 0.0);
                    }
                }

                // text
                if (!string.IsNullOrEmpty(TopOrientText))
                {
                    _topOrientDBText = new DBText();
                    _topOrientDBText.SetProperties(TextStyle, textHeight);
                    _topOrientDBText.Position = topOrientMarkerCenter;
                }
                else
                {
                    _topOrientDBText = null;
                }

                // type2
                if (OrientMarkerType == AxisMarkerType.Type2)
                {
                    _topOrientMarkerType2 = new Circle
                    {
                        Center = topOrientMarkerCenter,
                        Diameter = (MarkersDiameter - 2) * scale
                    };
                }
            }

            #endregion
        }
        else
        {
            _topFractureOffsetLine = null;
            _topMarkerLine = null;
            _topOrientArrow = null;
            _topOrientLine = null;
            _topFirstDBText = null;
            _topFirstMarker = null;
            _topFirstMarkerType2 = null;
            _topSecondDBText = null;
            _topSecondMarker = null;
            _topSecondMarkerType2 = null;
            _topOrientDBText = null;
            _topOrientMarkerType2 = null;
            _topOrientMarker = null;
            _topThirdDBText = null;
            _topThirdMarker = null;
            _topThirdMarkerType2 = null;
        }

        #endregion
    }

    /// <summary>
    /// Установка текстового значения, смещение однострочного текста из точки вставки на половину ширины влево и
    /// половину высоты вниз (чтобы геометрический центр оказался в точке вставки), создание маскировки при
    /// необходимости и поворот текста и маскировки при необходимости для всех созданных текстовых объектов
    /// </summary>
    private void UpdateTextEntities()
    {
        SetFirstTextOnCreation();

        UpdateTextEntity(
            _bottomFirstDBText, FirstTextPrefix + FirstText + FirstTextSuffix, ref _bottomFirstTextMask);

        UpdateTextEntity(
            _bottomSecondDBText, SecondTextPrefix + SecondText + SecondTextSuffix, ref _bottomSecondTextMask);

        UpdateTextEntity(
            _bottomThirdDBText, ThirdTextPrefix + ThirdText + ThirdTextSuffix, ref _bottomThirdTextMask);

        UpdateTextEntity(
            _topFirstDBText, FirstTextPrefix + FirstText + FirstTextSuffix, ref _topFirstTextMask);

        UpdateTextEntity(
            _topSecondDBText, SecondTextPrefix + SecondText + SecondTextSuffix, ref _topSecondTextMask);

        UpdateTextEntity(
            _topThirdDBText, ThirdTextPrefix + ThirdText + ThirdTextSuffix, ref _topThirdTextMask);

        UpdateTextEntity(_bottomOrientDBText, BottomOrientText, ref _bottomOrientTextMask);

        UpdateTextEntity(_topOrientDBText, TopOrientText, ref _topOrientTextMask);
    }

    /// <summary>
    /// Установка текстового значения, смещение однострочного текста из точки вставки на половину ширины влево и
    /// половину высоты вниз (чтобы геометрический центр оказался в точке вставки), создание маскировки при
    /// необходимости и поворот текста и маскировки при необходимости
    /// </summary>
    /// <param name="dbText">Изменяемый экземпляр <see cref="DBText"/></param>
    /// <param name="textString">Текстовое значение</param>
    /// <param name="mask">Маскировка фона</param>
    private void UpdateTextEntity(DBText dbText, string textString, ref Wipeout mask)
    {
        if (dbText == null)
            return;

        var maskOffset = TextMaskOffset * GetScale();
        var textRotation = TextRotationAngle.DegreeToRadian();
        dbText.TextString = textString;
        var rotationMatrix = Matrix3d.Rotation(textRotation, Vector3d.ZAxis, dbText.Position);
        dbText.Position = dbText.Position -
                          (Vector3d.XAxis * (dbText.GetLength() / 2)) -
                          (Vector3d.YAxis * (dbText.GetHeight() / 2));
        if (textRotation != 0.0)
            dbText.TransformBy(rotationMatrix);
                
        if (HideTextBackground)
        {
            mask = dbText.GetBackgroundMask(maskOffset);
            if (textRotation != 0.0)
                mask.TransformBy(rotationMatrix);
        }
    }

    private void SetFirstTextOnCreation()
    {
        if (!IsValueCreated)
            return;
            
        var check = 1 / Math.Sqrt(2);
        var v = (EndPointOCS - InsertionPointOCS).GetNormal();
        if ((v.X > check || v.X < -check) && (v.Y < check || v.Y > -check))
        {
            FirstText = GetFirstTextValueByLastAxis("Horizontal");
        }
        else
        {
            FirstText = GetFirstTextValueByLastAxis("Vertical");
        }
    }

    // Чтобы не вычислять каждый раз заново создам переменные
    private string _newVerticalMarkValue = string.Empty;

    private string _newHorizontalMarkValue = string.Empty;

    private string GetFirstTextValueByLastAxis(string direction)
    {
        if (direction.Equals("Horizontal"))
        {
            if (!string.IsNullOrEmpty(_lastHorizontalValue))
            {
                if (string.IsNullOrEmpty(_newHorizontalMarkValue))
                {
                    if (int.TryParse(_lastHorizontalValue, out int i))
                    {
                        _newHorizontalMarkValue = (i + 1).ToString();
                        return _newHorizontalMarkValue;
                    }

                    if (Invariables.AxisRusAlphabet.Contains(_lastHorizontalValue))
                    {
                        var index = Invariables.AxisRusAlphabet.IndexOf(_lastHorizontalValue);
                        if (index == Invariables.AxisRusAlphabet.Count - 1)
                        {
                            _newHorizontalMarkValue = Invariables.AxisRusAlphabet[0];
                            return _newHorizontalMarkValue;
                        }

                        _newHorizontalMarkValue = Invariables.AxisRusAlphabet[index + 1];
                        return _newHorizontalMarkValue;
                    }

                    _newHorizontalMarkValue = "А";
                    return _newHorizontalMarkValue;
                }

                return _newHorizontalMarkValue;
            }

            _newHorizontalMarkValue = "А";
            return _newHorizontalMarkValue;
        }

        if (direction.Equals("Vertical"))
        {
            if (!string.IsNullOrEmpty(_lastVerticalValue))
            {
                if (string.IsNullOrEmpty(_newVerticalMarkValue))
                {
                    if (int.TryParse(_lastVerticalValue, out int i))
                    {
                        _newVerticalMarkValue = (i + 1).ToString();
                        return _newVerticalMarkValue;
                    }

                    if (Invariables.AxisRusAlphabet.Contains(_lastVerticalValue))
                    {
                        var index = Invariables.AxisRusAlphabet.IndexOf(_lastVerticalValue);
                        if (index == Invariables.AxisRusAlphabet.Count - 1)
                        {
                            _newVerticalMarkValue = Invariables.AxisRusAlphabet[0];
                            return _newVerticalMarkValue;
                        }

                        _newVerticalMarkValue = Invariables.AxisRusAlphabet[index + 1];
                        return _newVerticalMarkValue;
                    }

                    _newVerticalMarkValue = "1";
                    return _newVerticalMarkValue;
                }

                return _newVerticalMarkValue;
            }

            _newVerticalMarkValue = "1";
            return _newVerticalMarkValue;
        }

        return string.Empty;
    }
}