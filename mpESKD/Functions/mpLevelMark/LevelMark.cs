// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpLevelMark;

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
/// Отметка уровня
/// </summary>
[SmartEntityDisplayNameKey("h105")]
[SystemStyleDescriptionKey("h108")]
public class LevelMark : SmartEntity, ITextValueEntity, INumericValueEntity, IWithDoubleClickEditor
{
    private bool _objectLine;
    private int _objectLineOffset = 5;
    private int _bottomShelfLength = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelMark"/> class.
    /// </summary>
    public LevelMark()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelMark"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public LevelMark(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Точка уровня (точка объекта измерения)
    /// </summary>
    [SaveToXData]
    public Point3d ObjectPoint { get; set; }

    /// <summary>
    /// Точка уровня в внутренней системе координат блока
    /// </summary>
    private Point3d ObjectPointOCS => ObjectPoint.TransformBy(BlockTransform.Inverse());

    /// <summary>
    /// Точка начала (со стороны объекта) нижней полки
    /// </summary>
    [SaveToXData]
    public Point3d BottomShelfStartPoint { get; set; }

    /// <summary>
    /// Точка начала (со стороны объекта) нижней полки в системе координат блока
    /// </summary>
    private Point3d BottomShelfStartPointOCS => BottomShelfStartPoint.TransformBy(BlockTransform.Inverse());

    /// <summary>
    /// Точка начала верхней полки. Задает высоту от нижней полки до верхней
    /// </summary>
    public Point3d ShelfPoint
    {
        get =>
            new Point3d(
                EndPoint.X,
                IsDownState
                    ? EndPoint.Y - (DistanceBetweenShelfs * GetFullScale())
                    : EndPoint.Y + (DistanceBetweenShelfs * GetFullScale()),
                EndPoint.Z);
        set
        {
            var p1 = EndPoint;
            var p2 = value;
            var v = (p2 - p1).GetNormal();
            IsDownState = v.Y < 0;
            var minDistance =
                (int)Math.Round(Math.Max(MainTextHeight, SecondTextHeight) + TextVerticalOffset, MidpointRounding.AwayFromZero);
            var distance = (int)(Math.Abs(p2.Y - p1.Y) / GetFullScale());
            var shelfOffset = distance < minDistance ? minDistance : distance;
            if (shelfOffset != DistanceBetweenShelfs)
                DistanceBetweenShelfs = shelfOffset;
        }
    }

    /// <summary>
    /// Точка начала верхней полки в системе координат блока. Задает высоту от нижней полки до верхней
    /// </summary>
    private Point3d ShelfPointOCS => ShelfPoint.TransformBy(BlockTransform.Inverse());

    /// <inheritdoc/>
    [SaveToXData]
    public override Point3d EndPoint
    {
        get => base.EndPoint;
        set => base.EndPoint = LevelMarkJigState == mpLevelMark.LevelMarkJigState.ObjectPoint || LevelMarkJigState == null
            ? value
            : new Point3d(value.X, ObjectPoint.Y, value.Z);
    }

    /// <summary>
    /// Состояние Jig при создании высотной отметки
    /// </summary>
    public LevelMarkJigState? LevelMarkJigState { get; set; }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints
    {
        get
        {
            if (ObjectLine)
                return 1.0;
            return BottomShelfLength;
        }
    }

    /// <inheritdoc />
    /// Не используется!
    public override string LineType { get; set; }

    /// <inheritdoc />
    /// Не используется!
    public override double LineTypeScale { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Линия объекта
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p55", false, descLocalKey: "d55")]
    [PropertyVisibilityDependency(new[] { nameof(ObjectLineOffset) }, new[] { nameof(BottomShelfLength) })]
    [SaveToXData]
    public bool ObjectLine
    {
        get => _objectLine;
        set
        {
            if (_objectLine == value)
                return;
            _objectLine = value;
            var horV = (EndPoint - ObjectPoint).GetNormal();
            BottomShelfStartPoint = value
                ? ObjectPoint + (horV * ObjectLineOffset * GetFullScale())
                : EndPoint - (horV * BottomShelfLength * GetFullScale());
        }
    }

    /// <summary>
    /// Отступ линии объекта
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p56", 5, 0, 20, descLocalKey: "d56", nameSymbol: "o1")]
    [SaveToXData]
    public int ObjectLineOffset
    {
        get => _objectLineOffset;
        set
        {
            if (_objectLineOffset == value)
                return;
            _objectLineOffset = value;

            if (ObjectLine)
            {
                var horV = (EndPoint - ObjectPoint).GetNormal();
                BottomShelfStartPoint = ObjectPoint + (horV * value * GetFullScale());
            }
        }
    }

    /// <summary>
    /// Длина нижней полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p57", 10, 1, 20, descLocalKey: "d57", nameSymbol: "l2")]
    [SaveToXData]
    public int BottomShelfLength
    {
        get => _bottomShelfLength;
        set
        {
            if (_bottomShelfLength == value)
                return;
            _bottomShelfLength = value;

            if (!ObjectLine)
            {
                var horV = (EndPoint - ObjectPoint).GetNormal();
                BottomShelfStartPoint = EndPoint - (horV * BottomShelfLength * GetFullScale());
            }
        }
    }

    /// <summary>
    /// Выступ нижней полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p58", 2, 1, 5, descLocalKey: "d58", nameSymbol: "l3")]
    [SaveToXData]
    public int BottomShelfLedge { get; set; } = 2;

    /// <summary>
    /// Находится ли отметка уровня в положении "Низ" (т.е. TopShelf находится ниже BottomShelf)
    /// </summary>
    [SaveToXData]
    public bool IsDownState { get; set; }

    /// <summary>
    /// Высота стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p59", 3, 2, 4, nameSymbol: "a")]
    [SaveToXData]
    public int ArrowHeight { get; set; } = 3;

    /// <summary>
    /// Толщина стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 6, "p60", 0.5, 0.0, 2.0, nameSymbol: "t")]
    [SaveToXData]
    public double ArrowThickness { get; set; } = 0.5;

    /// <summary>
    /// Отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 7, "p61", 1.0, 0.0, 3.0, nameSymbol: "o2")]
    [SaveToXData]
    public double TextIndent { get; set; } = 1.0;

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 8, "p62", 1.0, 0.0, 3.0, nameSymbol: "v")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;

    /// <summary>
    /// Выступ полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 9, "p63", 1, 0, 3, descLocalKey: "d63", nameSymbol: "l1")]
    [SaveToXData]
    public int ShelfLedge { get; set; } = 1;

    /// <summary>
    /// Расстояние между полками
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 10, "p63-1", 6, 1, int.MaxValue, nameSymbol: "o3")]
    [SaveToXData]
    public int DistanceBetweenShelfs { get; set; } = 6;

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 2, "p85", false, descLocalKey: "d85")]
    [PropertyVisibilityDependency(new[] { nameof(TextMaskOffset) })]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 3, "p86", 0.5, 0.0, 5.0)]
    [SaveToXData]
    public double TextMaskOffset { get; set; } = 0.5;

    /// <summary>
    /// Измеренное значение
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 4, "p65", null, isReadOnly: true, propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public double MeasuredValue { get; set; }

    /// <summary>
    /// Отображаемое значение
    /// </summary>
    [ValueToSearchBy]
    public string DisplayedValue
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(OverrideValue))
                return OverrideValue;

            var asterisk = AddAsterisk ? "*" : string.Empty;

            if (MeasuredValue >= 0)
            {
                var plus = ShowPlus ? "+" : string.Empty;
                return ReplaceSeparator($"{plus}{Math.Round(MeasuredValue, Accuracy).ToString($"F{Accuracy}")}{asterisk}");
            }

            return ReplaceSeparator($"{Math.Round(MeasuredValue, Accuracy).ToString($"F{Accuracy}")}{asterisk}");
        }
    }

    /// <summary>
    /// Выравнивание основного значения по горизонтали
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 5, "p73", TextHorizontalAlignment.Left, descLocalKey: "d73")]
    [SaveToXData]
    public TextHorizontalAlignment ValueHorizontalAlignment { get; set; } = TextHorizontalAlignment.Left;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 6, "p72", NumberSeparator.Dot, descLocalKey: "d72")]
    [SaveToXData]
    public NumberSeparator NumberSeparator { get; set; } = NumberSeparator.Dot;

    /// <summary>
    /// Переопределение текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p66", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string OverrideValue { get; set; } = string.Empty;

    /// <summary>
    /// Показывать плюс
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 8, "p64", true, descLocalKey: "d64")]
    [SaveToXData]
    public bool ShowPlus { get; set; } = true;

    /// <summary>
    /// Добавление звездочки
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 9, "p75", false, descLocalKey: "d75")]
    [SaveToXData]
    public bool AddAsterisk { get; set; }

    /// <summary>
    /// Точность
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 10, "p67", 3, 0, 5, descLocalKey: "d67")]
    [SaveToXData]
    public int Accuracy { get; set; } = 3;

    /// <summary>
    /// Примечание
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 11, "p68", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Выравнивание примечания по горизонтали
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 12, "p74", TextHorizontalAlignment.Left, descLocalKey: "d73")]
    [SaveToXData]
    public TextHorizontalAlignment NoteHorizontalAlignment { get; set; } = TextHorizontalAlignment.Left;

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 13, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double MainTextHeight { get; set; } = 3.5;

    /// <summary>
    /// Высота малого текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 14, "p50", 2.5, 0.000000001, 1.0000E+99, nameSymbol: "h2")]
    [SaveToXData]
    public double SecondTextHeight { get; set; } = 2.5;

    /// <summary>
    /// Масштаб измерений
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 15, "p69", 1.0, 0.000001, 1000000, descLocalKey: "d69")]
    [SaveToXData]
    public double MeasurementScale { get; set; } = 1.0;

    /// <summary>
    /// Нижняя полка
    /// </summary>
    private Line _bottomShelfLine;

    /// <summary>
    /// Вертикальная линия между полками
    /// </summary>
    private Line _verticalLine;

    /// <summary>
    /// Верхняя полка
    /// </summary>
    private Line _topShelfLine;

    /// <summary>
    /// Стрелка
    /// </summary>
    private Polyline _arrowPolyline;

    /// <summary>
    /// Верхний (основной) текст
    /// </summary>
    private DBText _topDbText;
    private Wipeout _topTextMask;

    /// <summary>
    /// Нижний (второстепенный) текст
    /// </summary>
    private DBText _bottomDbText;
    private Wipeout _bottomTextMask;

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _topTextMask,
                _bottomTextMask,

                _bottomShelfLine,
                _topShelfLine,
                _verticalLine,
                _arrowPolyline,
                _topDbText,
                _bottomDbText
            };

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return ObjectPoint;
        yield return BottomShelfStartPoint;
        yield return EndPoint;
        yield return ShelfPoint;
    }

    /// <summary>
    /// Установка нового значения для точки стрелки с обработкой зависимых значений
    /// </summary>
    /// <param name="point3d">Новое значение точки стрелки</param>
    public void SetArrowPoint(Point3d point3d)
    {
        EndPoint = point3d;
        ObjectPoint = new Point3d(ObjectPoint.X, EndPoint.Y, ObjectPoint.Z);

        var horV = (EndPoint - ObjectPoint).GetNormal();

        if (ObjectLine)
        {
            BottomShelfStartPoint = ObjectPoint + (horV * ObjectLineOffset * GetFullScale());
        }
        else
        {
            BottomShelfStartPoint = EndPoint - (horV * BottomShelfLength * GetFullScale());
        }
    }

    /// <inheritdoc />
    protected override void ProcessScaleChange(AnnotationScale oldScale, AnnotationScale newScale)
    {
        base.ProcessScaleChange(oldScale, newScale);
        var horV = (EndPoint - ObjectPoint).GetNormal();

        if (ObjectLine)
        {
            BottomShelfStartPoint = ObjectPoint + (horV * ObjectLineOffset * GetFullScale());
        }
        else
        {
            BottomShelfStartPoint = EndPoint - (horV * BottomShelfLength * GetFullScale());
        }
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();

            //// Задание первой точки (точки вставки). Она же точка начала отсчета
            if (LevelMarkJigState == mpLevelMark.LevelMarkJigState.InsertionPoint)
            {
                var tempEndPoint = new Point3d(
                    InsertionPointOCS.X + (BottomShelfLength * scale),
                    InsertionPointOCS.Y,
                    InsertionPointOCS.Z);
                var tempShelfPoint = new Point3d(
                    tempEndPoint.X,
                    tempEndPoint.Y + (DistanceBetweenShelfs * scale),
                    tempEndPoint.Z);

                AcadUtils.WriteMessageInDebug(
                    "Create when LevelMarkJigState == mpLevelMark.LevelMarkJigState.InsertionPoint");

                BottomShelfStartPoint = InsertionPoint;
                CreateEntities(
                    InsertionPointOCS, InsertionPointOCS, BottomShelfStartPointOCS, tempEndPoint, tempShelfPoint, scale);
            }
            //// Задание второй точки - точки уровня. При этом в jig устанавливается EndPoint, которая по завершении
            //// будет перемещена в ObjectPoint. Минимальные расстояния не учитываются
            else if (LevelMarkJigState == mpLevelMark.LevelMarkJigState.ObjectPoint)
            {
                var tempEndPoint = new Point3d(
                    EndPointOCS.X + (BottomShelfLength * scale),
                    EndPointOCS.Y,
                    EndPointOCS.Z);
                var tempShelfPoint = new Point3d(
                    tempEndPoint.X,
                    tempEndPoint.Y + (DistanceBetweenShelfs * scale),
                    tempEndPoint.Z);

                AcadUtils.WriteMessageInDebug(
                    "Create when LevelMarkJigState == mpLevelMark.LevelMarkJigState.ObjectPoint");

                BottomShelfStartPoint = EndPoint;
                CreateEntities(
                    InsertionPointOCS, EndPointOCS, BottomShelfStartPointOCS, tempEndPoint, tempShelfPoint, scale);
            }
            //// Прочие случаи
            else
            {
                //// Если указывается EndPoint (она же точка начала стрелки) и расстояние до ObjectPoint меньше допустимого
                if (EndPointOCS.DistanceTo(ObjectPointOCS) < MinDistanceBetweenPoints * scale)
                {
                    var isLeft = EndPointOCS.X < ObjectPointOCS.X;

                    var tempEndPoint = new Point3d(
                        isLeft
                            ? ObjectPointOCS.X - (MinDistanceBetweenPoints * scale)
                            : ObjectPointOCS.X + (MinDistanceBetweenPoints * scale),
                        ObjectPointOCS.Y,
                        ObjectPointOCS.Z);
                    var tempShelfPoint = new Point3d(
                        tempEndPoint.X,
                        tempEndPoint.Y + (DistanceBetweenShelfs * scale),
                        tempEndPoint.Z);

                    AcadUtils.WriteMessageInDebug(
                        "Create when EndPointOCS.DistanceTo(ObjectPointOCS) < MinDistanceBetweenPoints * scale");

                    BottomShelfStartPoint = ObjectPoint;
                    CreateEntities(
                        InsertionPointOCS, ObjectPointOCS, BottomShelfStartPointOCS, tempEndPoint, tempShelfPoint, scale);
                }
                else if (LevelMarkJigState == mpLevelMark.LevelMarkJigState.EndPoint)
                {
                    var isLeft = EndPointOCS.X < ObjectPointOCS.X;

                    var tempBottomShelfStartPoint = ObjectLine
                        ? new Point3d(
                            isLeft
                                ? ObjectPointOCS.X - (ObjectLineOffset * scale)
                                : ObjectPointOCS.X + (ObjectLineOffset * scale),
                            ObjectPointOCS.Y,
                            ObjectPointOCS.Z)
                        : new Point3d(
                            isLeft
                                ? EndPointOCS.X + (BottomShelfLength * scale)
                                : EndPointOCS.X - (BottomShelfLength * scale),
                            EndPointOCS.Y,
                            EndPointOCS.Z);
                    var tempShelfPoint = new Point3d(
                        EndPointOCS.X,
                        EndPointOCS.Y + (DistanceBetweenShelfs * scale),
                        EndPointOCS.Z);

                    AcadUtils.WriteMessageInDebug(
                        "Create when LevelMarkJigState == mpLevelMark.LevelMarkJigState.EndPoint");

                    BottomShelfStartPoint = tempBottomShelfStartPoint.TransformBy(BlockTransform);
                    CreateEntities(
                        InsertionPointOCS, ObjectPointOCS, tempBottomShelfStartPoint, EndPointOCS, tempShelfPoint, scale);
                }
                else
                {
                    AcadUtils.WriteMessageInDebug($"Create other variant InsertionPointOCS- {InsertionPointOCS}, BottomShelfStartPointOCS - {BottomShelfStartPointOCS}, EndPointOCS {EndPointOCS}, EndPointOCS ");
                    CreateEntities(
                        InsertionPointOCS, ObjectPointOCS, BottomShelfStartPointOCS, EndPointOCS, EndPointOCS, scale);
                }
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(
        Point3d insertionPoint,
        Point3d objectPoint,
        Point3d bottomShelfStartPoint,
        Point3d arrowPoint,
        Point3d shelfPoint,
        double scale)
    {
        MeasuredValue = (objectPoint.Y - insertionPoint.Y) * MeasurementScale;

        var horV = (arrowPoint - objectPoint).GetNormal();
        var verV = (shelfPoint - arrowPoint).GetNormal();
        var isLeft = horV.X < 0;
        var isTop = verV.Y > 0;

        var mainTextHeight = MainTextHeight * scale;
        var secondTextHeight = SecondTextHeight * scale;
        var textIndent = TextIndent * scale;
        var textVerticalOffset = TextVerticalOffset * scale;

        if (ObjectLine)
        {
            _bottomShelfLine = new Line(
                objectPoint + (horV * ObjectLineOffset * scale),
                arrowPoint + (horV * BottomShelfLedge * scale));
        }
        else
        {
            _bottomShelfLine = new Line(
                bottomShelfStartPoint,
                bottomShelfStartPoint + (horV * (BottomShelfLength + BottomShelfLedge) * scale));
        }

        _verticalLine = new Line(arrowPoint, shelfPoint);
        _arrowPolyline = GetArrow(objectPoint, arrowPoint, shelfPoint, scale);

        var topTextPosition = isTop
            ? shelfPoint + (textVerticalOffset * verV) + (textIndent * horV)
            : shelfPoint - (textVerticalOffset * verV) + (textIndent * horV);
        var bottomTextPosition = isTop
            ? shelfPoint - (textVerticalOffset * verV) + (textIndent * horV)
            : shelfPoint + (textVerticalOffset * verV) + (textIndent * horV);

        _topDbText = new DBText
        {
            TextString = DisplayedValue,
            Position = topTextPosition
        };

        _topDbText.SetProperties(TextStyle, mainTextHeight);

        if (!string.IsNullOrEmpty(Note))
        {
            _bottomDbText = new DBText
            {
                TextString = Note,
                Position = bottomTextPosition
            };

            _bottomDbText.SetProperties(TextStyle, secondTextHeight);

            if (isTop)
                _bottomDbText.Position -= verV * _bottomDbText.GetHeight();
            else
                _bottomDbText.Position += verV * _bottomDbText.GetHeight();
        }
        else
        {
            _bottomDbText = null;
        }

        if (isLeft)
        {
            _topDbText.Position += horV * _topDbText.GetLength();

            if (_bottomDbText != null)
                _bottomDbText.Position += horV * _bottomDbText.GetLength();
        }

        // верхний текст всегда имеет содержимое
        var topTextLength = _topDbText.GetLength();

        var bottomTextLength = _bottomDbText != null ? _bottomDbText.GetLength() : double.NaN;

        var maxTextWidth = double.IsNaN(bottomTextLength)
            ? topTextLength
            : Math.Max(topTextLength, bottomTextLength);

        var topShelfLength = textIndent + maxTextWidth + (ShelfLedge * scale);

        // если нижнего текста нет, то и выравнивать ничего не нужно
        if (_bottomDbText != null)
        {
            AcadUtils.WriteMessageInDebug($"top text length: {topTextLength}");
            AcadUtils.WriteMessageInDebug($"bottom text length: {bottomTextLength}");

            var diff = Math.Abs(topTextLength - bottomTextLength);
            AcadUtils.WriteMessageInDebug($"Diff: {diff}");
            if (topTextLength > bottomTextLength)
            {
                if (NoteHorizontalAlignment == TextHorizontalAlignment.Center)
                {
                    _bottomDbText.Position += diff / 2 * horV;
                }
                else if ((isLeft && NoteHorizontalAlignment == TextHorizontalAlignment.Left) ||
                         (!isLeft && NoteHorizontalAlignment == TextHorizontalAlignment.Right))
                {
                    _bottomDbText.Position += diff * horV;
                }
            }
            else
            {
                if (ValueHorizontalAlignment == TextHorizontalAlignment.Center)
                {
                    AcadUtils.WriteMessageInDebug($"Diff if center: {diff}");
                    _topDbText.Position += diff / 2 * horV;
                }
                else if ((isLeft && ValueHorizontalAlignment == TextHorizontalAlignment.Left) ||
                         (!isLeft && ValueHorizontalAlignment == TextHorizontalAlignment.Right))
                {
                    _topDbText.Position += diff * horV;
                }
            }
        }

        if (HideTextBackground)
        {
            var maskOffset = TextMaskOffset * scale;
            _topTextMask = _topDbText.GetBackgroundMask(maskOffset);
            if (_bottomDbText != null)
                _bottomTextMask = _bottomDbText.GetBackgroundMask(maskOffset);
        }

        _topShelfLine = new Line(shelfPoint, shelfPoint + (topShelfLength * horV));
    }

    private Polyline GetArrow(Point3d objectPoint, Point3d endPoint, Point3d shelfPoint, double scale)
    {
        var width = ArrowThickness * scale;
        var height = ArrowHeight * scale;
        var angle = 45.DegreeToRadian();
        var wingLength = height / Math.Sin(angle);
        var verV = (shelfPoint - endPoint).GetNormal();
        var horV = (endPoint - objectPoint).GetNormal();
        var wingProjection = wingLength * Math.Cos(angle);

        var polyline = new Polyline(3);
        var pt2 = endPoint + (width / 2 / Math.Sin(angle) * verV);
        var pt1 = pt2 - (wingProjection * horV) + (height * verV);
        var pt3 = pt1 + (wingProjection * 2 * horV);
        polyline.AddVertexAt(0, pt1.ToPoint2d(), 0.0, width, width);
        polyline.AddVertexAt(0, pt2.ToPoint2d(), 0.0, width, width);
        polyline.AddVertexAt(0, pt3.ToPoint2d(), 0.0, width, width);
        return polyline;
    }

    private string ReplaceSeparator(string numericValue)
    {
        var c = NumberSeparator == NumberSeparator.Comma ? ',' : '.';
        return numericValue.Replace(',', '.').Replace('.', c);
    }
}