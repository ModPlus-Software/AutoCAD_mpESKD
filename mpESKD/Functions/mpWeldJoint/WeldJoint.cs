// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpWeldJoint
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
    /// Сварной шов
    /// </summary>
    [SmartEntityDisplayNameKey("h139")]
    [SystemStyleDescriptionKey("h142")]
    public class WeldJoint : SmartEntity, ILinearEntity
    {
        private WeldJointType _weldJointType = WeldJointType.ButtFactorySolidVisible;
        
        #region Entities

        /// <summary>
        /// Полилиния для сплошных швов
        /// </summary>
        private Polyline _solidPolyline;

        /// <summary>
        /// Штрихи для создания штриховой линии прерывистого шва
        /// </summary>
        private List<Line> _intermittentStrokes = new List<Line>();

        /// <summary>
        /// Засечки или кресты
        /// </summary>
        private List<Line> _tickMarksOrCrosses = new List<Line>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="WeldJoint"/> class.
        /// </summary>
        public WeldJoint()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeldJoint"/> class.
        /// </summary>
        /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        public WeldJoint(ObjectId objectId)
            : base(objectId)
        {
        }

        /// <inheritdoc />
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
        public override double MinDistanceBetweenPoints => 2.0;

        /// <inheritdoc/>
        /// Не используется!
        public override string LineType { get; set; }

        /// <inheritdoc/>
        /// Не используется!
        public override double LineTypeScale { get; set; }

        /// <inheritdoc/>
        /// Не используется!
        public override string TextStyle { get; set; }

        /// <summary>
        /// Тип обозначения шва сварного соединения
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "p89", WeldJointType.ButtFactorySolidVisible)]
        [SaveToXData]
        public WeldJointType WeldJointType
        {
            get => _weldJointType;
            set
            {
                _weldJointType = value;
                var s = value.ToString();
                
                if (s.Contains("Factory"))
                {
                    IsVisibleTickMarkStep = true;
                    IsVisibleLargeCrossStep = false;
                    IsVisibleSmallCrossStep = false;
                    IsVisibleLargeCrossHalfHeight = false;
                    IsVisibleSmallCrossHeight = false;

                    if (s.Contains("Butt"))
                    {
                        IsVisibleTickMarkHalfHeight = true;
                        IsVisibleSmallTickMarkHeight = false;
                    }
                    //// Corner
                    else
                    {
                        IsVisibleTickMarkHalfHeight = false;
                        IsVisibleSmallTickMarkHeight = true;
                    }
                }
                //// Mounting
                else
                {
                    IsVisibleTickMarkStep = false;

                    if (s.Contains("Butt"))
                    {
                        IsVisibleLargeCrossStep = true;
                        IsVisibleSmallCrossStep = false;
                        IsVisibleLargeCrossHalfHeight = true;
                        IsVisibleSmallCrossHeight = false;
                    }
                    else
                    {
                        IsVisibleLargeCrossStep = false;
                        IsVisibleSmallCrossStep = true;
                        IsVisibleLargeCrossHalfHeight = false;
                        IsVisibleSmallCrossHeight = true;
                    }
                }

                if (s.Contains("Visible"))
                {
                    IsVisibleSeriesLength = false;
                    IsVisibleSpaceLength = false;

                    if (s.Contains("Intermittent"))
                    {
                        IsVisibleIntervalBetweenSeries = true;
                    }
                    //// Solid
                    else
                    {
                        IsVisibleIntervalBetweenSeries = false;
                    }
                }
                //// Invisible
                else
                {
                    IsVisibleSeriesLength = true;
                    IsVisibleSpaceLength = true;
                    IsVisibleIntervalBetweenSeries = false;
                }
            }
        }

        /// <summary>
        /// Шаг засечки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 2, "p90", 1.5, 1.0, 2.0, nameSymbol: "ts")]
        [SaveToXData]
        public double TickMarkStep { get; set; } = 1.5;

        /// <summary>
        /// Видимость поля <see cref="TickMarkStep"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(TickMarkStep) })]
        public bool IsVisibleTickMarkStep { get; private set; }

        /// <summary>
        /// Полувысота засечки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 3, "p91", 1.0, 1.0, 2.0, nameSymbol: "th1")]
        [SaveToXData]
        public double TickMarkHalfHeight { get; set; } = 1.0;
        
        /// <summary>
        /// Видимость поля <see cref="TickMarkHalfHeight"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(TickMarkHalfHeight) })]
        public bool IsVisibleTickMarkHalfHeight { get; private set; }

        /// <summary>
        /// Высота малой засечки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 4, "p99", 2.0, 1.0, 2.0, nameSymbol: "th2")]
        [SaveToXData]
        public double SmallTickMarkHeight { get; set; } = 2.0;
        
        /// <summary>
        /// Видимость поля <see cref="SmallTickMarkHeight"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(SmallTickMarkHeight) })]
        public bool IsVisibleSmallTickMarkHeight { get; private set; }

        /// <summary>
        /// Шаг большого креста
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 5, "p92", 4.5, 3.0, 5.0, nameSymbol: "cs1")]
        [SaveToXData]
        public double LargeCrossStep { get; set; } = 4.5;
        
        /// <summary>
        /// Видимость поля <see cref="LargeCrossStep"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(LargeCrossStep) })]
        public bool IsVisibleLargeCrossStep { get; private set; }

        /// <summary>
        /// Шаг малого креста
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 6, "p93", 3.0, 3.0, 5.0, nameSymbol: "cs2")]
        [SaveToXData]
        public double SmallCrossStep { get; set; } = 3.0;
        
        /// <summary>
        /// Видимость поля <see cref="SmallCrossStep"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(SmallCrossStep) })]
        public bool IsVisibleSmallCrossStep { get; private set; }

        /// <summary>
        /// Полувысота большого креста
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 7, "p94", 2.0, 1.0, 2.0, nameSymbol: "ch1")]
        [SaveToXData]
        public double LargeCrossHalfHeight { get; set; } = 2.0;
        
        /// <summary>
        /// Видимость поля <see cref="LargeCrossHalfHeight"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(LargeCrossHalfHeight) })]
        public bool IsVisibleLargeCrossHalfHeight { get; private set; }

        /// <summary>
        /// Высота малого креста
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 8, "p95", 2.0, 1.0, 2.0, nameSymbol: "ch2")]
        [SaveToXData]
        public double SmallCrossHeight { get; set; } = 2.0;
        
        /// <summary>
        /// Видимость поля <see cref="SmallCrossHeight"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(SmallCrossHeight) })]
        public bool IsVisibleSmallCrossHeight { get; private set; }

        /// <summary>
        /// Длина серии
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 9, "p96", 5.0, 3.0, 10.0, nameSymbol: "sl")]
        [SaveToXData]
        public double SeriesLength { get; set; } = 5.0;
        
        /// <summary>
        /// Видимость поля <see cref="SeriesLength"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(SeriesLength) })]
        public bool IsVisibleSeriesLength { get; private set; }

        /// <summary>
        /// Длина пробела
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 10, "p97", 2.0, 2.0, 3.0, nameSymbol: "s")]
        [SaveToXData]
        public double SpaceLength { get; set; } = 2.0;
        
        /// <summary>
        /// Видимость поля <see cref="SpaceLength"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(SpaceLength) })]
        public bool IsVisibleSpaceLength { get; private set; }

        /// <summary>
        /// Интервал между сериями
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 11, "p98", 3.0, 3.0, 5.0, nameSymbol: "si")]
        [SaveToXData]
        public double IntervalBetweenSeries { get; set; } = 3.0;
        
        /// <summary>
        /// Видимость поля <see cref="IntervalBetweenSeries"/> в палитре свойств
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, -1, "", "", propertyScope: PropertyScope.Hidden)]
        [PropertyVisibilityDependency(new[] { nameof(IntervalBetweenSeries) })]
        public bool IsVisibleIntervalBetweenSeries { get; private set; }

        /// <inheritdoc/>
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>
                {
                    _solidPolyline
                };

                entities.AddRange(_intermittentStrokes);
                entities.AddRange(_tickMarksOrCrosses);

                foreach (var entity in entities)
                {
                    SetImmutablePropertiesToNestedEntity(entity);
                }

                return entities;
            }
        }

        /// <summary>
        /// Возвращает локализованное описание для типа <see cref="WeldJoint"/>
        /// </summary>
        public static ISmartEntityDescriptor GetDescriptor()
        {
            return TypeFactory.Instance.GetDescriptor(typeof(WeldJoint));
        }

        /// <inheritdoc/>
        public override IEnumerable<Point3d> GetPointsForOsnap()
        {
            yield return InsertionPoint;
            yield return EndPoint;
            foreach (var middlePoint in MiddlePoints)
            {
                yield return middlePoint;
            }
        }

        /// <inheritdoc/>
        public void RebasePoints()
        {
            if (!MiddlePoints.Contains(EndPoint))
            {
                MiddlePoints.Add(EndPoint);
            }
        }

        /// <inheritdoc/>
        public override void UpdateEntities()
        {
            try
            {
                var length = EndPointOCS.DistanceTo(InsertionPointOCS);
                var scale = GetScale();
                if (EndPointOCS.Equals(Point3d.Origin))
                {
                    // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                    MakeSimplyEntity(UpdateVariant.SetInsertionPoint, scale);
                }
                else if (length < MinDistanceBetweenPoints * scale && MiddlePoints.Count == 0)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    MakeSimplyEntity(UpdateVariant.SetEndPointMinLength, scale);
                }
                else
                {
                    // Задание любой другой точки
                    CreateEntities(InsertionPointOCS, MiddlePointsOCS, EndPointOCS, scale);
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void MakeSimplyEntity(UpdateVariant variant, double scale)
        {
            if (variant == UpdateVariant.SetInsertionPoint)
            {
                /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
                 * Примерно аналогично созданию, только точки не создаются, а меняются
                */
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
            }
            else if (variant == UpdateVariant.SetEndPointMinLength)
            {
                /* Изменение базовых примитивов в момент указания второй точки
                * при условии что расстояние от второй точки до первой больше минимального допустимого
                */
                var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
                EndPoint = tmpEndPoint.TransformBy(BlockTransform);
            }
        }

        private void CreateEntities(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint, double scale)
        {
            _intermittentStrokes = new List<Line>();
            _tickMarksOrCrosses = new List<Line>();

            var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);

            if (IsVisible())
            {
                _solidPolyline = new Polyline(points.Count);
                for (var i = 0; i < points.Count; i++)
                {
                    _solidPolyline.AddVertexAt(i, points[i], 0.0, 0.0, 0.0);
                }
            }

            switch (WeldJointType)
            {
                //// ГОСТ 21.502-2016 таблица А.2 п.1
                case WeldJointType.ButtFactorySolidVisible:
                    CreateButtFactorySolidVisible(points, scale);
                    break;
                case WeldJointType.ButtMountingSolidVisible:
                    CreateButtMountingSolidVisible(points, scale);
                    break;
                case WeldJointType.ButtFactorySolidInvisible:
                    CreateButtFactorySolidInvisible(points, scale);
                    break;
                case WeldJointType.ButtMountingSolidInvisible:
                    CreateButtMountingSolidInvisible(points, scale);
                    break;
                //// ГОСТ 21.502-2016 таблица А.2 п.2
                case WeldJointType.ButtFactoryIntermittentVisible:
                    CreateButtFactoryIntermittentVisible(points, scale);
                    break;
                case WeldJointType.ButtMountingIntermittentVisible:
                    CreateButtMountingIntermittentVisible(points, scale);
                    break;
                case WeldJointType.ButtFactoryIntermittentInvisible:
                    CrateButtFactoryIntermittentInvisible(points, scale);
                    break;
                case WeldJointType.ButtMountingIntermittentInvisible:
                    CreateButtMountingIntermittentInvisible(points, scale);
                    break;
                //// ГОСТ 21.502-2016 таблица А.2 п.3
                case WeldJointType.CornerFactorySolidVisible:
                    CreateCornerFactorySolidVisible(points, scale);
                    break;
                case WeldJointType.CornerMountingSolidVisible:
                    CreateCornerMountingSolidVisible(points, scale);
                    break;
                case WeldJointType.CornerFactorySolidInvisible:
                    CreateCornerFactorySolidInvisible(points, scale);
                    break;
                case WeldJointType.CornerMountingSolidInvisible:
                    CreateCornerMountingSolidInvisible(points, scale);
                    break;
                //// ГОСТ 21.502-2016 таблица А.2 п.4
                case WeldJointType.CornerFactoryIntermittentVisible:
                    CreateCornerFactoryIntermittentVisible(points, scale);
                    break;
                case WeldJointType.CornerMountingIntermittentVisible:
                    CreateCornerMountingIntermittentVisible(points, scale);
                    break;
                case WeldJointType.CornerFactoryIntermittentInvisible:
                    CreateCornerFactoryIntermittentInvisible(points, scale);
                    break;
                case WeldJointType.CornerMountingIntermittentInvisible:
                    CreateCornerMountingIntermittentInvisible(points, scale);
                    break;
            }
        }

        private void CreateButtFactorySolidVisible(Point2dCollection points, double scale)
        {
            var tickMarkStep = TickMarkStep * scale;
            var tickMarkHalfHeight = TickMarkHalfHeight * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;

                while (true)
                {
                    var step = sumLength == 0.0 ? tickMarkStep / 2 : tickMarkStep;

                    sumLength += step;
                    if (sumLength >= segment.Length)
                        break;

                    var point = segment.GetPointAtDistance(sumLength);

                    AddTickMark(point, segment.PerpendicularVector, tickMarkHalfHeight);
                }
            });
        }

        private void CreateButtMountingSolidVisible(Point2dCollection points, double scale)
        {
            var largeCrossStep = LargeCrossStep * scale;
            var largeCrossHalfHeight = LargeCrossHalfHeight * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;

                while (true)
                {
                    var step = sumLength == 0.0 ? largeCrossStep / 2 : largeCrossStep;

                    sumLength += step;
                    if (sumLength >= segment.Length)
                        break;

                    var point = segment.GetPointAtDistance(sumLength);

                    AddLargeCross(point, segment.Direction, segment.PerpendicularVector, largeCrossHalfHeight);
                }
            });
        }

        private void CreateButtFactorySolidInvisible(Point2dCollection points, double scale)
        {
            var tickMarkStep = TickMarkStep * scale;
            var tickMarkHalfHeight = TickMarkHalfHeight * scale;
            var seriesLength = SeriesLength * scale;
            var space = SpaceLength * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    if (index % 2 == 0)
                    {
                        var pt1 = segment.GetPointAtDistance(sumLength);
                        sumLength += seriesLength;
                        var pt2 = sumLength >= segment.Length
                            ? segment.EndPoint : segment.GetPointAtDistance(sumLength);

                        var dash = new Line(pt1.ToPoint3d(), pt2.ToPoint3d());
                        _intermittentStrokes.Add(dash);

                        var dashLength = dash.Length;
                        var s = dashLength % tickMarkStep <= 0.001
                            ? (int)((dashLength - tickMarkStep) / tickMarkStep)
                            : (int)(dashLength / tickMarkStep);
                        var indent = (dashLength - (tickMarkStep * s)) / 2;

                        var dashSumLength = 0.0;
                        while (true)
                        {
                            var step = dashSumLength == 0.0 ? indent : tickMarkStep;
                            dashSumLength += step;
                            if (dashSumLength >= dashLength)
                                break;
                            var point = dash.StartPoint.ToPoint2d() + (segment.Direction * dashSumLength);

                            AddTickMark(point, segment.PerpendicularVector, tickMarkHalfHeight);
                        }
                    }
                    else
                    {
                        sumLength += space;
                    }

                    if (sumLength >= segment.Length)
                        break;

                    index++;
                }
            });
        }

        private void CreateButtMountingSolidInvisible(Point2dCollection points, double scale)
        {
            var seriesLength = SeriesLength * scale;
            var space = SpaceLength * scale;
            var largeCrossStep = LargeCrossStep * scale;
            var largeCrossHalfHeight = LargeCrossHalfHeight * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    if (index % 2 == 0)
                    {
                        var pt1 = segment.GetPointAtDistance(sumLength);
                        sumLength += seriesLength;
                        var pt2 = sumLength >= segment.Length
                            ? segment.EndPoint : segment.GetPointAtDistance(sumLength);

                        var dash = new Line(pt1.ToPoint3d(), pt2.ToPoint3d());
                        _intermittentStrokes.Add(dash);

                        var dashLength = dash.Length;
                        var s = dashLength % largeCrossStep <= 0.001
                            ? (int)((dashLength - largeCrossStep) / largeCrossStep)
                            : (int)(dashLength / largeCrossStep);
                        var indent = (dashLength - (largeCrossStep * s)) / 2;

                        var dashSumLength = 0.0;
                        while (true)
                        {
                            var step = dashSumLength == 0.0 ? indent : largeCrossStep;
                            dashSumLength += step;
                            if (dashSumLength >= dashLength)
                                break;
                            var point = dash.StartPoint.ToPoint2d() + (segment.Direction * dashSumLength);

                            AddLargeCross(point, segment.Direction, segment.PerpendicularVector, largeCrossHalfHeight);
                        }
                    }
                    else
                    {
                        sumLength += space;
                    }

                    if (sumLength >= segment.Length)
                        break;

                    index++;
                }
            });
        }

        private void CreateButtFactoryIntermittentVisible(Point2dCollection points, double scale)
        {
            var tickMarkStep = TickMarkStep * scale;
            var tickMarkHalfHeight = TickMarkHalfHeight * scale;
            var intervalBetweenSeries = IntervalBetweenSeries * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    var step = index == 0
                        ? tickMarkStep / 2
                        : index % 3 == 0 ? intervalBetweenSeries : tickMarkStep;

                    sumLength += step;
                    if (sumLength >= segment.Length)
                        break;

                    var point = segment.GetPointAtDistance(sumLength);

                    AddTickMark(point, segment.PerpendicularVector, tickMarkHalfHeight);

                    index++;
                }
            });
        }

        private void CreateButtMountingIntermittentVisible(Point2dCollection points, double scale)
        {
            var largeCrossStep = LargeCrossStep * scale;
            var largeCrossHalfHeight = LargeCrossHalfHeight * scale;

            // Чтобы интервал между сериями был по краю крестов добавлю к интервалу полу-высоту креста
            var intervalBetweenSeries = (IntervalBetweenSeries * scale) + (largeCrossHalfHeight * 2);

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    var step = index == 0
                        ? largeCrossStep / 2
                        : index % 3 == 0 ? intervalBetweenSeries : largeCrossStep;

                    sumLength += step;
                    if (sumLength >= segment.Length)
                        break;

                    var point = segment.GetPointAtDistance(sumLength);

                    AddLargeCross(point, segment.Direction, segment.PerpendicularVector, largeCrossHalfHeight);

                    index++;
                }
            });
        }

        private void CrateButtFactoryIntermittentInvisible(Point2dCollection points, double scale)
        {
            var tickMarkStep = TickMarkStep * scale;
            var tickMarkHalfHeight = TickMarkHalfHeight * scale;
            var seriesLength = SeriesLength * scale;
            var space = SpaceLength * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    if (index % 2 == 0)
                    {
                        var pt1 = segment.GetPointAtDistance(sumLength);
                        sumLength += seriesLength;
                        var pt2 = sumLength >= segment.Length
                            ? segment.EndPoint : segment.GetPointAtDistance(sumLength);

                        var dash = new Line(pt1.ToPoint3d(), pt2.ToPoint3d());
                        _intermittentStrokes.Add(dash);

                        if (index % 4 == 0)
                        {
                            var dashLength = dash.Length;
                            var s = dashLength % tickMarkStep <= 0.001
                                ? (int)((dashLength - tickMarkStep) / tickMarkStep)
                                : (int)(dashLength / tickMarkStep);
                            var indent = (dashLength - (tickMarkStep * s)) / 2;

                            var dashSumLength = 0.0;
                            while (true)
                            {
                                var step = dashSumLength == 0.0 ? indent : tickMarkStep;
                                dashSumLength += step;

                                if (dashSumLength >= dashLength)
                                    break;

                                var point = dash.StartPoint.ToPoint2d() + (segment.Direction * dashSumLength);

                                AddTickMark(point, segment.PerpendicularVector, tickMarkHalfHeight);
                            }
                        }
                    }
                    else
                    {
                        sumLength += space;
                    }

                    if (sumLength >= segment.Length)
                        break;

                    index++;
                }
            });
        }

        private void CreateButtMountingIntermittentInvisible(Point2dCollection points, double scale)
        {
            var largeCrossStep = LargeCrossStep * scale;
            var largeCrossHalfHeight = LargeCrossHalfHeight * scale;
            var seriesLength = SeriesLength * scale;
            var space = SpaceLength * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    if (index % 2 == 0)
                    {
                        var pt1 = segment.GetPointAtDistance(sumLength);
                        sumLength += seriesLength;
                        var pt2 = sumLength >= segment.Length
                            ? segment.EndPoint : segment.GetPointAtDistance(sumLength);

                        var dash = new Line(pt1.ToPoint3d(), pt2.ToPoint3d());
                        _intermittentStrokes.Add(dash);

                        if (index % 4 == 0)
                        {
                            var dashLength = dash.Length;
                            var s = dashLength % largeCrossStep <= 0.001
                                ? (int)((dashLength - largeCrossStep) / largeCrossStep)
                                : (int)(dashLength / largeCrossStep);
                            var indent = (dashLength - (largeCrossStep * s)) / 2;

                            var dashSumLength = 0.0;
                            while (true)
                            {
                                var step = dashSumLength == 0.0 ? indent : largeCrossStep;
                                dashSumLength += step;

                                if (dashSumLength >= dashLength)
                                    break;

                                var point = dash.StartPoint.ToPoint2d() + (segment.Direction * dashSumLength);

                                AddLargeCross(point, segment.Direction, segment.PerpendicularVector, largeCrossHalfHeight);
                            }
                        }
                    }
                    else
                    {
                        sumLength += space;
                    }

                    if (sumLength >= segment.Length)
                        break;

                    index++;
                }
            });
        }

        private void CreateCornerFactorySolidVisible(Point2dCollection points, double scale)
        {
            var tickMarkStep = TickMarkStep * scale;
            var smallTickMarkHeight = SmallTickMarkHeight * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;

                while (true)
                {
                    var step = sumLength == 0.0 ? tickMarkStep / 2 : tickMarkStep;
                    sumLength += step;

                    if (sumLength >= segment.Length)
                        break;

                    var point = segment.GetPointAtDistance(sumLength);

                    AddHalfTickMark(point, segment.PerpendicularVector, smallTickMarkHeight);
                }
            });
        }

        private void CreateCornerMountingSolidVisible(Point2dCollection points, double scale)
        {
            var smallCrossHeight = SmallCrossHeight * scale;
            var smallCrossStep = SmallCrossStep * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;

                while (true)
                {
                    var step = sumLength == 0.0 ? smallCrossStep / 2 : smallCrossStep;
                    sumLength += step;

                    if (sumLength >= segment.Length)
                        break;

                    var point = segment.GetPointAtDistance(sumLength);

                    AddSmallCross(point, segment.Direction, segment.PerpendicularVector, smallCrossHeight);
                }
            });
        }

        private void CreateCornerFactorySolidInvisible(Point2dCollection points, double scale)
        {
            var tickMarkStep = TickMarkStep * scale;
            var smallTickMarkHeight = SmallTickMarkHeight * scale;
            var seriesLength = SeriesLength * scale;
            var space = SpaceLength * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    if (index % 2 == 0)
                    {
                        var pt1 = segment.GetPointAtDistance(sumLength);
                        sumLength += seriesLength;
                        var pt2 = sumLength >= segment.Length
                            ? segment.EndPoint : segment.GetPointAtDistance(sumLength);

                        var dash = new Line(pt1.ToPoint3d(), pt2.ToPoint3d());
                        _intermittentStrokes.Add(dash);

                        var dashLength = dash.Length;
                        var s = dashLength % tickMarkStep <= 0.001
                            ? (int)((dashLength - tickMarkStep) / tickMarkStep)
                            : (int)(dashLength / tickMarkStep);
                        var indent = (dashLength - (tickMarkStep * s)) / 2;

                        var dashSumLength = 0.0;
                        while (true)
                        {
                            var step = dashSumLength == 0.0 ? indent : tickMarkStep;
                            dashSumLength += step;
                            if (dashSumLength >= dashLength)
                                break;
                            var point = dash.StartPoint.ToPoint2d() + (segment.Direction * dashSumLength);

                            AddHalfTickMark(point, segment.PerpendicularVector, smallTickMarkHeight);
                        }
                    }
                    else
                    {
                        sumLength += space;
                    }

                    if (sumLength >= segment.Length)
                        break;

                    index++;
                }
            });
        }

        private void CreateCornerMountingSolidInvisible(Point2dCollection points, double scale)
        {
            var smallCrossHeight = SmallCrossHeight * scale;
            var smallCrossStep = SmallCrossStep * scale;
            var seriesLength = SeriesLength * scale;
            var space = SpaceLength * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    if (index % 2 == 0)
                    {
                        var pt1 = segment.GetPointAtDistance(sumLength);
                        sumLength += seriesLength;
                        var pt2 = sumLength >= segment.Length
                            ? segment.EndPoint : segment.GetPointAtDistance(sumLength);

                        var dash = new Line(pt1.ToPoint3d(), pt2.ToPoint3d());
                        _intermittentStrokes.Add(dash);

                        var dashLength = dash.Length;
                        var s = dashLength % smallCrossStep <= 0.001
                            ? (int)((dashLength - smallCrossStep) / smallCrossStep)
                            : (int)(dashLength / smallCrossStep);
                        var indent = (dashLength - (smallCrossStep * s)) / 2;

                        var dashSumLength = 0.0;
                        while (true)
                        {
                            var step = dashSumLength == 0.0 ? indent : smallCrossStep;
                            dashSumLength += step;
                            if (dashSumLength >= dashLength)
                                break;
                            var point = dash.StartPoint.ToPoint2d() + (segment.Direction * dashSumLength);

                            AddSmallCross(point, segment.Direction, segment.PerpendicularVector, smallCrossHeight);
                        }
                    }
                    else
                    {
                        sumLength += space;
                    }

                    if (sumLength >= segment.Length)
                        break;

                    index++;
                }
            });
        }

        private void CreateCornerFactoryIntermittentVisible(Point2dCollection points, double scale)
        {
            var tickMarkStep = TickMarkStep * scale;
            var smallTickMarkHeight = SmallTickMarkHeight * scale;
            var intervalBetweenSeries = IntervalBetweenSeries * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    var step = index == 0
                        ? tickMarkStep / 2
                        : index % 3 == 0 ? intervalBetweenSeries : tickMarkStep;

                    sumLength += step;
                    if (sumLength >= segment.Length)
                        break;

                    var point = segment.GetPointAtDistance(sumLength);

                    AddHalfTickMark(point, segment.PerpendicularVector, smallTickMarkHeight);

                    index++;
                }
            });
        }

        private void CreateCornerMountingIntermittentVisible(Point2dCollection points, double scale)
        {
            var smallCrossHeight = SmallCrossHeight * scale;
            var smallCrossStep = SmallCrossStep * scale;

            // Чтобы интервал между сериями был по краю крестов добавлю к интервалу высоту малого креста
            var intervalBetweenSeries = (IntervalBetweenSeries * scale) + (smallCrossHeight * 2);

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    var step = index == 0
                        ? smallCrossStep / 2
                        : index % 3 == 0 ? intervalBetweenSeries : smallCrossStep;

                    sumLength += step;
                    if (sumLength >= segment.Length)
                        break;

                    var point = segment.GetPointAtDistance(sumLength);

                    AddSmallCross(point, segment.Direction, segment.PerpendicularVector, smallCrossHeight);

                    index++;
                }
            });
        }

        private void CreateCornerFactoryIntermittentInvisible(Point2dCollection points, double scale)
        {
            var tickMarkStep = TickMarkStep * scale;
            var smallTickMarkHeight = SmallTickMarkHeight * scale;
            var seriesLength = SeriesLength * scale;
            var space = SpaceLength * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    if (index % 2 == 0)
                    {
                        var pt1 = segment.GetPointAtDistance(sumLength);
                        sumLength += seriesLength;
                        var pt2 = sumLength >= segment.Length
                            ? segment.EndPoint : segment.GetPointAtDistance(sumLength);

                        var dash = new Line(pt1.ToPoint3d(), pt2.ToPoint3d());
                        _intermittentStrokes.Add(dash);

                        if (index % 4 == 0)
                        {
                            var dashLength = dash.Length;
                            var s = dashLength % tickMarkStep <= 0.001
                                ? (int)((dashLength - tickMarkStep) / tickMarkStep)
                                : (int)(dashLength / tickMarkStep);
                            var indent = (dashLength - (tickMarkStep * s)) / 2;

                            var dashSumLength = 0.0;
                            while (true)
                            {
                                var step = dashSumLength == 0.0 ? indent : tickMarkStep;
                                dashSumLength += step;

                                if (dashSumLength >= dashLength)
                                    break;

                                var point = dash.StartPoint.ToPoint2d() + (segment.Direction * dashSumLength);

                                AddHalfTickMark(point, segment.PerpendicularVector, smallTickMarkHeight);
                            }
                        }
                    }
                    else
                    {
                        sumLength += space;
                    }

                    if (sumLength >= segment.Length)
                        break;

                    index++;
                }
            });
        }

        private void CreateCornerMountingIntermittentInvisible(Point2dCollection points, double scale)
        {
            var smallCrossHeight = SmallCrossHeight * scale;
            var smallCrossStep = SmallCrossStep * scale;
            var seriesLength = SeriesLength * scale;
            var space = SpaceLength * scale;

            SegmentsIteration(points, segment =>
            {
                var sumLength = 0.0;
                var index = 0;

                while (true)
                {
                    if (index % 2 == 0)
                    {
                        var pt1 = segment.GetPointAtDistance(sumLength);
                        sumLength += seriesLength;
                        var pt2 = sumLength >= segment.Length
                            ? segment.EndPoint : segment.GetPointAtDistance(sumLength);

                        var dash = new Line(pt1.ToPoint3d(), pt2.ToPoint3d());
                        _intermittentStrokes.Add(dash);

                        if (index % 4 == 0)
                        {
                            var dashLength = dash.Length;
                            var s = dashLength % smallCrossStep <= 0.001
                                ? (int)((dashLength - smallCrossStep) / smallCrossStep)
                                : (int)(dashLength / smallCrossStep);
                            var indent = (dashLength - (smallCrossStep * s)) / 2;

                            var dashSumLength = 0.0;
                            while (true)
                            {
                                var step = dashSumLength == 0.0 ? indent : smallCrossStep;
                                dashSumLength += step;

                                if (dashSumLength >= dashLength)
                                    break;

                                var point = dash.StartPoint.ToPoint2d() + (segment.Direction * dashSumLength);

                                AddSmallCross(point, segment.Direction, segment.PerpendicularVector, smallCrossHeight);
                            }
                        }
                    }
                    else
                    {
                        sumLength += space;
                    }

                    if (sumLength >= segment.Length)
                        break;

                    index++;
                }
            });
        }

        private void AddTickMark(Point2d point, Vector2d perpendicularVector, double tickMarkHalfHeight)
        {
            var line = new Line(
                (point - (perpendicularVector * tickMarkHalfHeight)).ToPoint3d(),
                (point + (perpendicularVector * tickMarkHalfHeight)).ToPoint3d());
            _tickMarksOrCrosses.Add(line);
        }

        private void AddHalfTickMark(Point2d point, Vector2d perpendicularVector, double smallTickMarkHeight)
        {
            var line = new Line(
                point.ToPoint3d(),
                (point + (perpendicularVector * smallTickMarkHeight)).ToPoint3d());
            _tickMarksOrCrosses.Add(line);
        }

        private void AddLargeCross(
            Point2d point, Vector2d alongVector, Vector2d perpendicularVector, double largeCrossHalfHeight)
        {
            var rotate = 45.DegreeToRadian();
            largeCrossHalfHeight = Math.Sqrt(Math.Pow(largeCrossHalfHeight, 2) + Math.Pow(largeCrossHalfHeight, 2));

            var lineOne = new Line(
                (point - (perpendicularVector * largeCrossHalfHeight)).ToPoint3d(),
                (point + (perpendicularVector * largeCrossHalfHeight)).ToPoint3d());
            var lineTwo = new Line(
                (point - (alongVector * largeCrossHalfHeight)).ToPoint3d(),
                (point + (alongVector * largeCrossHalfHeight)).ToPoint3d());

            var matrix = Matrix3d.Rotation(rotate, Vector3d.ZAxis, point.ToPoint3d());

            lineOne.TransformBy(matrix);
            lineTwo.TransformBy(matrix);

            _tickMarksOrCrosses.Add(lineOne);
            _tickMarksOrCrosses.Add(lineTwo);
        }

        private void AddSmallCross(
            Point2d point, Vector2d alongVector, Vector2d perpendicularVector, double smallCrossHeight)
        {
            var rotate = 45.DegreeToRadian();
            var lineLength = Math.Sqrt(Math.Pow(smallCrossHeight, 2) + Math.Pow(smallCrossHeight, 2));
            point += perpendicularVector * (smallCrossHeight / 2);

            var lineOne = new Line(
                (point - (alongVector * (lineLength / 2))).ToPoint3d(),
                (point + (alongVector * (lineLength / 2))).ToPoint3d());
            var lineTwo = new Line(
                (point - (perpendicularVector * (lineLength / 2))).ToPoint3d(),
                (point + (perpendicularVector * (lineLength / 2))).ToPoint3d());

            var matrix = Matrix3d.Rotation(rotate, Vector3d.ZAxis, point.ToPoint3d());

            lineOne.TransformBy(matrix);
            lineTwo.TransformBy(matrix);

            _tickMarksOrCrosses.Add(lineOne);
            _tickMarksOrCrosses.Add(lineTwo);
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

        private bool IsVisible()
        {
            return WeldJointType.ToString().Contains("Visible");
        }

        private void SegmentsIteration(Point2dCollection points, Action<Segment> action)
        {
            for (var i = 1; i < points.Count; i++)
            {
                var segment = new Segment(points[i - 1], points[i]);

                action.Invoke(segment);
            }
        }

        /// <summary>
        /// Линейный сегмент между двумя точками
        /// </summary>
        internal class Segment
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Segment"/> class.
            /// </summary>
            /// <param name="startPoint">Начальная точка</param>
            /// <param name="endPoint">Конечная точка</param>
            public Segment(Point2d startPoint, Point2d endPoint)
            {
                StartPoint = startPoint;
                EndPoint = endPoint;

                Length = StartPoint.GetDistanceTo(EndPoint);
                Direction = (EndPoint - StartPoint).GetNormal();
                PerpendicularVector = Direction.GetPerpendicularVector();
            }

            /// <summary>
            /// Начальная точка
            /// </summary>
            public Point2d StartPoint { get; }

            /// <summary>
            /// Конечная точка
            /// </summary>
            public Point2d EndPoint { get; }

            /// <summary>
            /// Направление (единичный вектор)
            /// </summary>
            public Vector2d Direction { get; }

            /// <summary>
            /// Перпендикуляр к направлению (единичный вектор)
            /// </summary>
            public Vector2d PerpendicularVector { get; }

            /// <summary>
            /// Длина сегмента
            /// </summary>
            public double Length { get; }

            /// <summary>
            /// Возвращает точку на указанном расстоянии от начала сегмента
            /// </summary>
            /// <param name="distance">Расстояние</param>
            public Point2d GetPointAtDistance(double distance)
            {
                return StartPoint + (Direction * distance);
            }
        }
    }
}
