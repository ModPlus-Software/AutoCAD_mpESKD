// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpNodalLeader
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
    /// Узловая выноска
    /// </summary>
    public class NodalLeader : SmartEntity, ITextValueEntity
    {
        private readonly string _lastNodeNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodalLeader"/> class.
        /// </summary>
        public NodalLeader()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodalLeader"/> class.
        /// </summary>
        /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        public NodalLeader(ObjectId objectId)
            : base(objectId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodalLeader"/> class.
        /// </summary>
        /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
        public NodalLeader(string lastNodeNumber)
        {
            _lastNodeNumber = lastNodeNumber;
        }

        /// <summary>
        /// Точка рамки
        /// </summary>
        [SaveToXData]
        public Point3d FramePoint { get; set; }

        /// <summary>
        /// Точка рамки в внутренней системе координат блока
        /// </summary>
        public Point3d FramePointOCS => FramePoint.TransformBy(BlockTransform.Inverse());

        /// <summary>
        /// Состояние Jig при создании узловой выноски
        /// </summary>
        public NodalLeaderJigState? NodalLeaderJigState { get; set; }

        /// <inheritdoc/>
        public override double MinDistanceBetweenPoints => 1;

        /// <summary>
        /// Минимальное расстояние между рамкой и выноской
        /// </summary>
        private double MinDistanceBetweenFrameAndLeader => 1;

        /// <inheritdoc/>
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>
                {
                    _framePolyline,
                    _frameCircle,
                    _leaderLine,
                    _shelfLine,
                    _topFirstDbText,
                    _topSecondDbText,
                    _bottomDbText
                };

                foreach (var e in entities)
                {
                    SetImmutablePropertiesToNestedEntity(e);
                }

                return entities;
            }
        }

        /// <inheritdoc/>
        /// Не используется!
        public override string LineType { get; set; }

        /// <inheritdoc/>
        /// Не используется!
        public override double LineTypeScale { get; set; }

        /// <summary>
        /// Тип рамки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "p82", FrameType.Round)]
        [SaveToXData]
        public FrameType FrameType { get; set; } = FrameType.Round;

        /// <summary>
        /// Высота рамки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 2, "p76", 5.0, 1.0, nameSymbol: "fh")]
        [SaveToXData]
        public double FrameHeight
        {
            get
            {
                if (FramePoint == Point3d.Origin)
                    return 5;
                return Math.Abs(FramePoint.Y - InsertionPoint.Y) / GetFullScale();
            }

            set => FramePoint = FramePoint.Y > InsertionPoint.Y 
                ? new Point3d(FramePoint.X, InsertionPoint.Y + (value * GetFullScale()), FramePoint.Z)
                : new Point3d(FramePoint.X, InsertionPoint.Y - (value * GetFullScale()), FramePoint.Z);
        }

        /// <summary>
        /// Ширина рамки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 3, "p77", 5.0, 1.0, nameSymbol: "fw")]
        [SaveToXData]
        public double FrameWidth
        {
            get
            {
                if (FramePoint == Point3d.Origin)
                    return 5;
                return Math.Abs(FramePoint.X - InsertionPoint.X) / GetFullScale();
            }

            set =>
                FramePoint = FramePoint.X > InsertionPoint.X 
                    ? new Point3d(FramePoint.X + (value * GetFullScale()), InsertionPoint.Y, FramePoint.Z)
                    : new Point3d(FramePoint.X - (value * GetFullScale()), InsertionPoint.Y, FramePoint.Z);
        }

        /// <summary>
        /// Радиус скругления углов прямоугольной рамки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 4, "p83", 2, 1, 10, descLocalKey: "d83", nameSymbol: "r")]
        [SaveToXData]
        public int CornerRadius { get; set; } = 2;

        /// <summary>
        /// Отступ текста
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 5, "p61", 1.0, 0.0, 3.0, nameSymbol: "o")]
        [SaveToXData]
        public double TextIndent { get; set; } = 1.0;

        /// <summary>
        /// Вертикальный отступ текста
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 6, "p62", 1.0, 0.0, 3.0, nameSymbol: "v")]
        [SaveToXData]
        public double TextVerticalOffset { get; set; } = 1.0;

        /// <summary>
        /// Выступ полки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 7, "p63", 1, 0, 3, descLocalKey: "d63", nameSymbol: "l")]
        [SaveToXData]
        public int ShelfLedge { get; set; } = 1;

        /// <summary>
        /// Положение полки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 8, "p78", ShelfPosition.Right)]
        [SaveToXData]
        public ShelfPosition ShelfPosition { get; set; } = ShelfPosition.Right;

        /// <inheritdoc/>
        [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
        [SaveToXData]
        public override string TextStyle { get; set; } = "Standard";

        /// <summary>
        /// Номер узла
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 2, "p79", "", propertyScope: PropertyScope.Palette)]
        [SaveToXData]
        [ValueToSearchBy]
        public string NodeNumber { get; set; } = string.Empty;

        /// <summary>
        /// Номер листа
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 3, "p80", "", propertyScope: PropertyScope.Palette)]
        [SaveToXData]
        [ValueToSearchBy]
        public string SheetNumber { get; set; } = string.Empty;

        /// <summary>
        /// Адрес узла
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 4, "p81", "", propertyScope: PropertyScope.Palette)]
        [SaveToXData]
        [ValueToSearchBy]
        public string NodeAddress { get; set; } = string.Empty;

        /// <summary>
        /// Рамка узла при типе "Прямоугольная"
        /// </summary>
        private Polyline _framePolyline;

        /// <summary>
        /// Рамка узла при типе "Круглая"
        /// </summary>
        private Circle _frameCircle;

        /// <summary>
        /// Линия выноски
        /// </summary>
        private Line _leaderLine;

        /// <summary>
        /// Полка выноски
        /// </summary>
        private Line _shelfLine;

        /// <summary>
        /// Верхний первый текст (номер узла)
        /// </summary>
        private DBText _topFirstDbText;

        /// <summary>
        /// Верхний второй текст (номер листа)
        /// </summary>
        private DBText _topSecondDbText;

        /// <summary>
        /// Нижний текст (адрес узла)
        /// </summary>
        private DBText _bottomDbText;

        /// <inheritdoc/>
        public override IEnumerable<Point3d> GetPointsForOsnap()
        {
            yield return InsertionPoint;
            yield return FramePoint;
            yield return EndPoint;
        }

        /// <inheritdoc/>
        public override void UpdateEntities()
        {
            try
            {
                var scale = GetScale();

                //// Задание первой точки (точки вставки). Она же точка начала отсчета
                if (NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.InsertionPoint)
                {
                    var tempFramePoint = new Point3d(
                        InsertionPointOCS.X + (FrameHeight * scale),
                        InsertionPointOCS.Y + (FrameWidth * scale),
                        InsertionPointOCS.Z);
                    var tempShelfPoint = new Point3d(
                        InsertionPointOCS.X + (FrameHeight * scale * 2),
                        InsertionPointOCS.Y + (FrameWidth * scale * 2),
                        InsertionPointOCS.Z);

                    AcadUtils.WriteMessageInDebug(
                        "Create when NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.InsertionPoint");
                    
                    CreateEntities(InsertionPointOCS, tempFramePoint, tempShelfPoint, scale);
                }
                //// Задание второй точки - точки рамки. При этом в jig устанавливается EndPoint, которая по завершении
                //// будет перемещена в FramePoint
                else if (NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.ObjectPoint)
                {
                    var tempShelfPoint = new Point3d(
                        InsertionPointOCS.X + (FrameHeight * scale * 2),
                        InsertionPointOCS.Y + (FrameWidth * scale * 2),
                        InsertionPointOCS.Z);

                    AcadUtils.WriteMessageInDebug(
                        "Create when NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.ObjectPoint");
                    
                    CreateEntities(InsertionPointOCS, FramePointOCS, tempShelfPoint, scale);
                }
                //// Прочие случаи
                else
                {
                    //// Если указывается EndPoint (она же точка выноски) и расстояние до рамки меньше допустимого
                    if (IsInvalidDistanceBetweenFrameAndLeaderPoint())
                    {

                    }
                    //// Если указывается EndPoint (она же точка выноски) и расстояние до рамки допустимое
                    else if (NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.EndPoint)
                    {

                    }
                    else
                    {
                        AcadUtils.WriteMessageInDebug("Create other variant");
                        CreateEntities(InsertionPointOCS, FramePointOCS, EndPointOCS, scale);
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
            Point3d framePoint,
            Point3d leaderPoint,
            double scale)
        {

        }

        private bool IsInvalidDistanceBetweenFrameAndLeaderPoint()
        {

        }
    }
}
