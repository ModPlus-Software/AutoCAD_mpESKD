// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpFragmentMarker
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
    /// Линия обрыва
    /// </summary>
    [SmartEntityDisplayNameKey("h48")]
    [SystemStyleDescriptionKey("h53")]
    public class FragmentMarker : SmartEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FragmentMarker"/> class.
        /// </summary>
        public FragmentMarker()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FragmentMarker"/> class.
        /// </summary>
        /// <param name="blockId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        public FragmentMarker(ObjectId blockId)
            : base(blockId)
        {
        }

        /// <summary>
        /// Возвращает локализованное описание для типа <see cref="FragmentMarker"/>
        /// </summary>
        public static IIntellectualEntityDescriptor GetDescriptor()
        {
            return TypeFactory.Instance.GetDescriptor(typeof(FragmentMarker));
        }

        #region Properties

        /// <inheritdoc />
        /// В примитиве не используется!
        public override string LineType { get; set; }

        /// <inheritdoc />
        /// В примитиве не используется!
        public override double LineTypeScale { get; set; }

        /// <inheritdoc />
        /// В примитиве не используется!
        public override string TextStyle { get; set; }
        
        /// <inheritdoc />
        public override double MinDistanceBetweenPoints
        {
            get
            {
                switch (FragmentMakerType)
                {
                    case FragmentMarkerType.Linear:
                        return 15.0;
                    case FragmentMarkerType.Curvilinear:
                    case FragmentMarkerType.Cylindrical:
                        return 1.0;
                    default:
                        return 15.0;
                }
            }
        }

        /// <summary>
        /// Тип линии обрыва: линейный, криволинейный, цилиндрический
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "p1", FragmentMarkerType.Linear, descLocalKey: "d1")]
        [SaveToXData]
        public FragmentMarkerType FragmentMakerType { get; set; } = FragmentMarkerType.Linear;

        /// <summary>
        /// Выступ линии обрыва за границы "обрываемого" объекта
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 2, "p2", 2, 0, 10, descLocalKey: "d2", nameSymbol: "a")]
        [SaveToXData]
        public int Overhang { get; set; } = 2;

        /// <summary>Ширина Обрыва для линейного обрыва</summary>
        [EntityProperty(PropertiesCategory.Geometry, 3, "p3", 5, 1, 10, descLocalKey: "d3", nameSymbol: "w")]
        [SaveToXData]
        public int BreakWidth { get; set; } = 5;

        /// <summary>Длина обрыва для линейного обрыва</summary>
        [EntityProperty(PropertiesCategory.Geometry, 4, "p4", 10, 1, 13, descLocalKey: "d4", nameSymbol: "h")]
        [SaveToXData]
        public int BreakHeight { get; set; } = 10;

        #endregion

        #region Geometry

        /// <summary>Средняя точка. Нужна для перемещения  примитива</summary>
        public Point3d MiddlePoint => new Point3d(
            (InsertionPoint.X + EndPoint.X) / 2,
            (InsertionPoint.Y + EndPoint.Y) / 2,
            (InsertionPoint.Z + EndPoint.Z) / 2);
        
        /// <summary>
        /// Главная полилиния примитива
        /// </summary>
        private Polyline _mainPolyline;

        /// <inheritdoc />
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity> { _mainPolyline };
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
            yield return EndPoint;
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
                    var pts = PointsToCreatePolyline(scale, InsertionPointOCS, EndPointOCS, out List<double> bulges);
                    FillMainPolylineWithPoints(pts, bulges);
                }
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
            List<double> bulges;
            if (variant == UpdateVariant.SetInsertionPoint)
            {
                /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
                 * Примерно аналогично созданию, только точки не создаются, а меняются
                */
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);

                var pts = PointsToCreatePolyline(scale, InsertionPointOCS, tmpEndPoint, out bulges);
                FillMainPolylineWithPoints(pts, bulges);
            }
            else if (variant == UpdateVariant.SetEndPointMinLength) //// изменение вершин полилинии
            {
                /* Изменение базовых примитивов в момент указания второй точки
                * при условии что расстояние от второй точки до первой больше минимального допустимого
                */
                var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);
                var pts = PointsToCreatePolyline(scale, InsertionPointOCS, tmpEndPoint, out bulges);
                FillMainPolylineWithPoints(pts, bulges);
                EndPoint = tmpEndPoint.TransformBy(BlockTransform);
            }
        }

        /// <summary>
        /// Получение точек для построения базовой полилинии
        /// </summary>
        private Point2dCollection PointsToCreatePolyline(
            double scale, Point3d insertionPoint, Point3d endPoint, out List<double> bulges)
        {
            var length = endPoint.DistanceTo(insertionPoint);
            bulges = new List<double>();
            var pts = new Point2dCollection();
            if (FragmentMakerType == FragmentMarkerType.Linear)
            {
                // точки
                if (Overhang > 0)
                {
                    pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(endPoint, insertionPoint, insertionPoint, Overhang * scale));
                    bulges.Add(0.0);
                }

                // Первая точка, соответствующая ручке
                pts.Add(insertionPoint.ToPoint2d());
                bulges.Add(0.0);
                pts.Add(ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) - (BreakWidth / 2.0 * scale)));
                bulges.Add(0.0);
                pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(
                    insertionPoint,
                    ModPlus.Helpers.GeometryHelpers.ConvertPoint2DToPoint3D(ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) - (BreakWidth / 4.0 * scale))),
                    BreakHeight / 2.0 * scale));
                bulges.Add(0.0);
                pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(insertionPoint, ModPlus.Helpers.GeometryHelpers.ConvertPoint2DToPoint3D(
                    ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) + (BreakWidth / 4.0 * scale))), -BreakHeight / 2.0 * scale));
                bulges.Add(0.0);
                pts.Add(ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) + (BreakWidth / 2.0 * scale)));
                bulges.Add(0.0);

                // Конечная точка, соответствующая ручке
                pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length));
                bulges.Add(0.0);
                if (Overhang > 0)
                {
                    pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length + (Overhang * scale)));
                    bulges.Add(0.0);
                }
            }

            if (FragmentMakerType == FragmentMarkerType.Curvilinear)
            {
                if (Overhang > 0)
                {
                    pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(
                        insertionPoint,
                        ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(endPoint, insertionPoint, insertionPoint, Overhang / 100.0 * length),
                        -Overhang / 200.0 * length));
                    bulges.Add(length / 10 / length / 4 * 2);
                }

                // Первая точка, соответствующая ручке
                pts.Add(insertionPoint.ToPoint2d());
                bulges.Add(length / 10 / length / 2 * 4);

                // Средняя точка
                pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length / 2));
                bulges.Add(-length / 10 / length / 2 * 4);

                // Конечная точка, соответствующая ручке
                pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length));
                bulges.Add(0);
                if (Overhang > 0)
                {
                    pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(
                        insertionPoint,
                        ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(insertionPoint, endPoint, endPoint, Overhang / 100.0 * length),
                        -Overhang / 200.0 * length));
                    bulges.Add(length / 10 / length / 4 * 2);
                }
            }

            if (FragmentMakerType == FragmentMarkerType.Cylindrical)
            {
                // first
                pts.Add(insertionPoint.ToPoint2d());
                bulges.Add(-0.392699081698724);
                pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(
                    insertionPoint,
                    ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(insertionPoint, endPoint, insertionPoint, length / 10.0),
                    length / 10));
                bulges.Add(-length / 10 / length / 2 * 3);

                // center
                pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length / 2));
                bulges.Add(length / 10 / length / 2 * 3);
                pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(
                    insertionPoint,
                    ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(insertionPoint, endPoint, insertionPoint, length - (length / 10.0)),
                    -length / 10));
                bulges.Add(0.392699081698724);

                // endpoint
                pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length));
                bulges.Add(0.392699081698724);
                pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(
                    insertionPoint,
                    ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(insertionPoint, endPoint, insertionPoint, length - (length / 10.0)),
                    length / 10));
                bulges.Add(length / 10 / length / 2 * 3);
                pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length / 2));
                bulges.Add(0.0);
            }

            return pts;
        }

        /// <summary>Изменение точек полилинии</summary>
        /// <param name="points">Коллекция 2Д точек</param>
        /// <param name="bulges">Список выпуклостей</param>
        private void FillMainPolylineWithPoints(Point2dCollection points, IList<double> bulges)
        {
            _mainPolyline = new Polyline(points.Count);
            SetImmutablePropertiesToNestedEntity(_mainPolyline);
            for (var i = 0; i < points.Count; i++)
            {
                _mainPolyline.AddVertexAt(i, points[i], bulges[i], 0.0, 0.0);
            }
        }

        #endregion
    }
}