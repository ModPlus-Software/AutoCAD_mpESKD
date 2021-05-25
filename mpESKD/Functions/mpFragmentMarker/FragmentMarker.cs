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
    /// Линия фрагмента
    /// </summary>
    [SmartEntityDisplayNameKey("h145")]
    [SystemStyleDescriptionKey("h146")]
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
        public override double MinDistanceBetweenPoints => 20;

        /// <summary>
        /// Радиус скругления
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "p83", 5, 1, 10, descLocalKey: "d83-1", nameSymbol: "r")]
        [SaveToXData]
        public int Radius { get; set; } = 5;

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
            double scale, Point3d insertionPoint, Point3d endPoint,  out List<double> bulges)
        {
            var length = endPoint.DistanceTo(insertionPoint);
            bulges = new List<double>();
            var pts = new Point2dCollection();

            // Первая точка начало дуги радиус Radius * scale
            // 1. От первой точки до второй проводим линию это будет вектор
            // 2. Чтобы получить точку от начала вектора, получаем нормаль и умножаем на нужную длину
            // 3. Поворачиваем полученный вектор на 90 градусов и отсчитываем необходимую высоту

            var lengthRadius = Radius * scale;

            var normal = (endPoint - insertionPoint).GetNormal();

            pts.Add(insertionPoint.ToPoint2d());
            bulges.Add(-0.4141);

            var vectorLength = normal * lengthRadius;

            var p2_v = insertionPoint + vectorLength;
            var p2 = p2_v + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p2.ToPoint2d());
            bulges.Add(0.0);

            var p3_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) - lengthRadius);
            var p3 = p3_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p3.ToPoint2d());
            bulges.Add(0.4141);

            var p4_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, length / 2);
            var p4 = p4_t.ToPoint3d() + (vectorLength * 2).RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p4.ToPoint2d());
            bulges.Add(0.4141);

            var p5_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) + lengthRadius);
            var p5 = p5_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p5.ToPoint2d());
            bulges.Add(0.0);

            var p6_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, length - lengthRadius);
            var p6 = p6_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p6.ToPoint2d());
            bulges.Add(-0.4141);

            pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length));
            bulges.Add(0.0);

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