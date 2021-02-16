// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpWaterProofing
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
    /// Линия гидроизоляции
    /// </summary>
    [SmartEntityDisplayNameKey("h114")]
    [SystemStyleDescriptionKey("h129")]
    public class WaterProofing : SmartEntity, ILinearEntity
    {
        #region Entities

        /// <summary>
        /// Главная полилиния объекта
        /// </summary>
        private Polyline _mainPolyline;

        /// <summary>
        /// Вторая полилиния, являющаяся смещенной копией первой
        /// </summary>
        private readonly List<Entity> _offsetPolylineEntities = new List<Entity>();

        /// <summary>
        /// Список штрихов
        /// </summary>
        private readonly List<Polyline> _strokes = new List<Polyline>();

        #endregion
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WaterProofing"/> class.
        /// </summary>
        public WaterProofing()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterProofing"/> class.
        /// </summary>
        /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        public WaterProofing(ObjectId objectId)
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

        /// <inheritdoc />
        public override double MinDistanceBetweenPoints => 20.0;

        /// <summary>
        /// Отступ первого штриха в каждом сегменте полилинии
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "p36", WaterProofingFirstStrokeOffset.ByHalfStrokeOffset, descLocalKey: "d36-1", nameSymbol: "a")]
        [SaveToXData]
        public WaterProofingFirstStrokeOffset FirstStrokeOffset { get; set; } = WaterProofingFirstStrokeOffset.ByHalfStrokeOffset;

        /// <summary>
        /// Длина штриха
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 2, "p37", 8, 1, 20, nameSymbol: "l")]
        [SaveToXData]
        public int StrokeLength { get; set; } = 8;

        /// <summary>
        /// Расстояние между штрихами
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 3, "p38", 6, 1, 20, nameSymbol: "b")]
        [SaveToXData]
        public int StrokeOffset { get; set; } = 6;

        /// <summary>
        /// Толщина линии
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 4, "p70", 2.0, 0.0, 10, nameSymbol: "t")]
        [SaveToXData]
        public double LineThickness { get; set; } = 2.0;

        /// <inheritdoc />
        [EntityProperty(PropertiesCategory.General, 4, "p35", "Continuous", descLocalKey: "d35")]
        public override string LineType { get; set; }

        /// <inheritdoc />
        [EntityProperty(PropertiesCategory.General, 5, "p6", 1.0, 0.0, 1.0000E+99, descLocalKey: "d6")]
        public override double LineTypeScale { get; set; }

        /// <inheritdoc />
        /// Не используется!
        public override string TextStyle { get; set; }

        /// <inheritdoc />
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>();
                entities.AddRange(_strokes);
                foreach (var e in entities)
                {
                    SetImmutablePropertiesToNestedEntity(e);
                }

                SetChangeablePropertiesToNestedEntity(_mainPolyline);

                entities.Add(_mainPolyline);

                foreach (var offsetPolylineEntity in _offsetPolylineEntities)
                {
                    SetChangeablePropertiesToNestedEntity(offsetPolylineEntity);
                    entities.Add(offsetPolylineEntity);
                }

                return entities;
            }
        }
        
        /// <summary>
        /// Возвращает локализованное описание для типа <see cref="WaterProofing"/>
        /// </summary>
        public static IIntellectualEntityDescriptor GetDescriptor()
        {
            return TypeFactory.Instance.GetDescriptor(typeof(WaterProofing));
        }

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
            var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);
            _mainPolyline = new Polyline(points.Count);
            for (var i = 0; i < points.Count; i++)
            {
                _mainPolyline.AddVertexAt(i, points[i], 0.0, 0.0, 0.0);
            }

            _offsetPolylineEntities.Clear();
            foreach (Entity offsetCurve in _mainPolyline.GetOffsetCurves(LineThickness * scale))
            {
                _offsetPolylineEntities.Add(offsetCurve);
            }

            // create strokes
            _strokes.Clear();
            if (_mainPolyline.Length >= MinDistanceBetweenPoints)
            {
                for (var i = 1; i < _mainPolyline.NumberOfVertices; i++)
                {
                    var segmentStartPoint = _mainPolyline.GetPoint3dAt(i - 1);
                    var segmentEndPoint = _mainPolyline.GetPoint3dAt(i);
                    Vector3d? previousSegmentVector = null;
                    Vector3d? nextSegmentVector = null;
                    if (i > 1)
                        previousSegmentVector = segmentStartPoint - _mainPolyline.GetPoint3dAt(i - 2);
                    if (i < _mainPolyline.NumberOfVertices - 1)
                        nextSegmentVector = _mainPolyline.GetPoint3dAt(i + 1) - segmentEndPoint;

                    _strokes.AddRange(CreateStrokesOnMainPolylineSegment(
                        segmentEndPoint, segmentStartPoint, scale, previousSegmentVector, nextSegmentVector));
                }
            }
        }

        private IEnumerable<Polyline> CreateStrokesOnMainPolylineSegment(
            Point3d segmentEndPoint, Point3d segmentStartPoint, double scale, Vector3d? previousSegmentVector, Vector3d? nextSegmentVector)
        {
            var strokes = new List<Polyline>();

            var lineThickness = LineThickness * scale;
            var segmentVector = segmentEndPoint - segmentStartPoint;

            var previousToCurrentCrossProductIndex = 1.0;
            if (previousSegmentVector != null)
            {
                previousToCurrentCrossProductIndex =
                    previousSegmentVector.Value.CrossProduct(segmentVector).GetNormal().Z;
            }

            var currentToNextCrossProductIndex = 1.0;
            if (nextSegmentVector != null)
            {
                currentToNextCrossProductIndex = segmentVector.CrossProduct(nextSegmentVector.Value).GetNormal().Z;
            }
            
            var angleToPreviousSegment = previousSegmentVector != null
                ? previousSegmentVector.Value.GetAngleTo(segmentVector)
                : 0.0;
            var startBackOffset = 0.0;
            if (previousToCurrentCrossProductIndex < 0 && angleToPreviousSegment > 0.0)
            {
                startBackOffset = Math.Abs(lineThickness * Math.Tan(Math.PI - (angleToPreviousSegment / 2.0)));
            }

            var angleToNextSegment = nextSegmentVector != null
                ? segmentVector.GetAngleTo(nextSegmentVector.Value)
                : 0.0;
            var endBackOffset = 0.0;
            if (currentToNextCrossProductIndex < 0 && angleToNextSegment > 0.0)
            {
                endBackOffset = Math.Abs(lineThickness * Math.Tan(Math.PI - (angleToNextSegment / 2.0)));
            }

            var segmentLength = segmentVector.Length;
            var perpendicular = segmentVector.GetPerpendicularVector().Negate();
            var distanceAtSegmentStart = _mainPolyline.GetDistAtPoint(segmentStartPoint);

            var overflowIndex = 0;

            var sumDistanceAtSegment = 0.0;
            var isSpace = true;
            var isStart = true;
            while (true)
            {
                overflowIndex++;
                double distance;
                if (isStart)
                {
                    if (FirstStrokeOffset == WaterProofingFirstStrokeOffset.ByHalfStrokeOffset)
                    {
                        distance = StrokeOffset / 2.0 * scale;
                    }
                    else if (FirstStrokeOffset == WaterProofingFirstStrokeOffset.ByStrokeOffset)
                    {
                        distance = StrokeOffset * scale;
                    }
                    else
                    {
                        distance = 0.0;
                    }

                    distance += startBackOffset;

                    isStart = false;
                }
                else
                {
                    if (isSpace)
                    {
                        distance = StrokeOffset * scale;
                    }
                    else
                    {
                        distance = StrokeLength * scale;
                    }
                }

                sumDistanceAtSegment += distance;

                if (!isSpace)
                {
                    var firstStrokePoint = _mainPolyline.GetPointAtDist(distanceAtSegmentStart + sumDistanceAtSegment - distance) +
                                           (perpendicular * lineThickness / 2.0);

                    if ((sumDistanceAtSegment - distance) < (sumDistanceAtSegment - endBackOffset))
                    {
                        // Если индекс, полученный из суммы векторов (текущий и следующий) отрицательный и последний штрих 
                        // попадает на конец сегмента полилинии, то его нужно построить так, чтобы он попал на точку не основной
                        // полилинии, а второстепенной

                        Point3d secondStrokePoint;
                        if (sumDistanceAtSegment >= segmentLength)
                        {
                            AcadUtils.WriteMessageInDebug($"segment vector: {segmentVector.GetNormal()}");
                            secondStrokePoint = 
                                segmentEndPoint - 
                                (segmentVector.GetNormal() * endBackOffset) +
                                (perpendicular * lineThickness / 2.0);
                            AcadUtils.WriteMessageInDebug($"{nameof(secondStrokePoint)}: {secondStrokePoint}");
                        }
                        else
                        {
                            secondStrokePoint =
                                _mainPolyline.GetPointAtDist(distanceAtSegmentStart + sumDistanceAtSegment) +
                                (perpendicular * lineThickness / 2.0);
                        }

                        var stroke = new Polyline(2);
                        stroke.AddVertexAt(0, firstStrokePoint.ToPoint2d(), 0.0, lineThickness,
                            lineThickness);
                        stroke.AddVertexAt(1, secondStrokePoint.ToPoint2d(), 0.0, lineThickness,
                            lineThickness);

                        SetImmutablePropertiesToNestedEntity(stroke);

                        strokes.Add(stroke);
                    }
                }

                if (sumDistanceAtSegment >= segmentLength)
                {
                    break;
                }

                if (overflowIndex >= 1000)
                {
                    break;
                }

                isSpace = !isSpace;
            }

            return strokes;
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
    }
}
