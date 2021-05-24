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

        #region fields

        
        private Point3d _tmpEndPoint;
        private double _scale;
        private List<double> _bulges;
        private Point2dCollection _pts ;

        #endregion

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
        /// Тип 
        /// </summary>
        //[EntityProperty(PropertiesCategory.Geometry, 1, "p1", FragmentMarkerType.Linear, descLocalKey: "d1")]
        //[SaveToXData]
        //public FragmentMarkerType FragmentMakerType { get; set; } = FragmentMarkerType.Linear;

        /// <summary>
        /// Радиус скругления
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "f1", 5, 0, 20, descLocalKey: "ff1", nameSymbol: "a")]
        [SaveToXData]
        public int Radius { get; set; } = 5;

        ///// <summary>Ширина ???? </summary>
        //[EntityProperty(PropertiesCategory.Geometry, 3, "p3", 5, 1, 10, descLocalKey: "d3", nameSymbol: "w")]
        //[SaveToXData]
        //public int BreakWidth { get; set; } = 5;

        ///// <summary>Длина обрыва для линейного обрыва</summary>
        //[EntityProperty(PropertiesCategory.Geometry, 4, "p4", 10, 1, 13, descLocalKey: "d4", nameSymbol: "h")]
        //[SaveToXData]
        //public int BreakHeight { get; set; } = 10;

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
                _scale = GetScale();
                if (EndPointOCS.Equals(Point3d.Origin))
                {
                    // Задание точки вставки (т.е. второй точки еще нет)
                    MakeSimplyEntity(UpdateVariant.SetInsertionPoint, _scale);
                }
                else if (length < MinDistanceBetweenPoints * _scale)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    MakeSimplyEntity(UpdateVariant.SetEndPointMinLength, _scale);
                }
                else
                {
                    // Задание второй точки
                    var pts = PointsToCreatePolyline(_scale, InsertionPointOCS, EndPointOCS);
                    FillMainPolylineWithPoints(pts, _bulges);
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
            
            
            if (variant == UpdateVariant.SetInsertionPoint)
            {
                /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
                 * Примерно аналогично созданию, только точки не создаются, а меняются
                */
                _tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);

                var pts = PointsToCreatePolyline(scale, InsertionPointOCS, _tmpEndPoint);
                FillMainPolylineWithPoints(pts, _bulges);
            }
            else if (variant == UpdateVariant.SetEndPointMinLength) //// изменение вершин полилинии
            {
                /* Изменение базовых примитивов в момент указания второй точки
                * при условии что расстояние от второй точки до первой больше минимального допустимого
                */
                 _tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);
                var pts = PointsToCreatePolyline(scale, InsertionPointOCS, _tmpEndPoint);
                FillMainPolylineWithPoints(pts, _bulges);
                EndPoint = _tmpEndPoint.TransformBy(BlockTransform);
            }
        }

        /// <summary>
        /// Получение точек для построения базовой полилинии
        /// </summary>
        private Point2dCollection PointsToCreatePolyline(
            double scale, Point3d insertionPoint, Point3d endPoint)
        {
            var length = endPoint.DistanceTo(insertionPoint);
            _bulges = new List<double>();
            _pts = new Point2dCollection();

            // точки
            //if (Overhang > 0)
            //{
            //    //pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(
            //    //    insertionPoint,
            //    //    ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(endPoint, insertionPoint, insertionPoint, Overhang / 100.0 * length),
            //    //    -Overhang / 200.0 * length));
            //    //bulges.Add(length / 5 / length / 4 * 2);
            //    pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(endPoint, insertionPoint, insertionPoint, Overhang * scale));
            //    bulges.Add(0.0);
            //}

            //// Первая точка, соответствующая ручке`
            //pts.Add(insertionPoint.ToPoint2d());
            //bulges.Add(length / 5 / length / 2 * 4);

            //// Средняя точка
            //pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length / 8));
            //bulges.Add(-length / 10 / length / 2 * 4);

            //// Конечная точка, соответствующая ручке
            //pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length));
            //bulges.Add(0);
            //if (Overhang > 0)
            //{
            //    pts.Add(ModPlus.Helpers.GeometryHelpers.GetPerpendicularPoint2d(
            //        insertionPoint,
            //        ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(insertionPoint, endPoint, endPoint, Overhang / 100.0 * length),
            //        -Overhang / 200.0 * length));
            //    bulges.Add(length / 10 / length / 4 * 2);
            //}

            //Первая точка начало дуги радиус 5

            
            // 1. от первой точки до второй проводим линию, от начала этой линии отсчитываем Radius по Х
            // 2. находим Y от Х перпендикуляр 

            double lengthRadius = Radius * _scale;

            var vector3d = endPoint - insertionPoint;
            Vector3d normal = vector3d.GetNormal();

            _pts.Add(insertionPoint.ToPoint2d());
            _bulges.Add(-0.4141);

            var vectorLength = normal * lengthRadius;

            Point3d p2_v = insertionPoint + vectorLength;
            Point3d p2 = p2_v + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            _pts.Add(p2.ToPoint2d());
            _bulges.Add(0.0);

            var p3_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) - (lengthRadius));
            Point3d p3 = p3_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            _pts.Add(p3.ToPoint2d());
            _bulges.Add(0.4141);

            var p4_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2));
            Point3d p4 = p4_t.ToPoint3d() + (vectorLength * 2).RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            _pts.Add(p4.ToPoint2d());
            _bulges.Add(0.4141);

            var p5_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) + (lengthRadius));
            Point3d p5 = p5_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            _pts.Add(p5.ToPoint2d());
            _bulges.Add(0.0);

            var p6_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, length - (lengthRadius));
            Point3d p6 = p6_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            _pts.Add(p6.ToPoint2d());
            _bulges.Add(-0.4141);

            _pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length));
            _bulges.Add(0.0);

            //if (Overhang > 0)
            //{
            //    pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length + (Overhang * scale)));
            //    bulges.Add(0.0);
            //}

            return _pts;
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

        private void SetPointsAndBulges()
        {
            //var p6_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(InsertionPoint, _tmpEndPoint, length - (len));
            //Point3d p6 = p6_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            //_pts.Add(p6.ToPoint2d());
            //_bulges.Add(-0.4141);
        }

        #endregion
    }
}