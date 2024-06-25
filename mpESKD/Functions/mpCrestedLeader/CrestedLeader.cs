﻿using JetBrains.Annotations;
using TestFunctions;

namespace mpESKD.Functions.mpCrestedLeader;

using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using System.Collections.Generic;
using System.Linq;
using Base.Abstractions;
using Autodesk.AutoCAD.EditorInput;
using ModPlusAPI;
using ModPlusAPI.Windows;
using System.Windows;

/// <summary>
/// Имя
/// </summary>
[SmartEntityDisplayNameKey("h207")]
[SystemStyleDescriptionKey("h208")]
public class CrestedLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    #region Примитивы

    private readonly List<Line> _leaders = new ();

    private readonly List<Line> _leaderArrows = new ();

    private Line _shelfLine = new ();
    private Line _shelf = new ();

    /// <summary>
    /// Верхний тест
    /// </summary>
    private MText _topText;

    /// <summary>
    /// Нижний текст
    /// </summary>
    private MText _bottomText;

    /// <summary>
    ///  Маскировка верхнего текста 
    /// </summary>
    private Wipeout _topTextMask;

    /// <summary>
    /// Маскировка нижнего текста
    /// </summary>
    private Wipeout _bottomTextMask;

    private Circle _testCircle;

    private List<Circle> _testCirclesAsLeaderPoints = new ();

    private Line _tempLeader = new ();

    #endregion

    private readonly List<Point3d> _leaderPoints = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeader"/> class.
    /// </summary>
    public CrestedLeader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeader"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public CrestedLeader(ObjectId objectId)
        : base(objectId)
    {
    }

    #region Свойства

    /// <inheritdoc />
    public override string LineType { get; set; }

    /// <inheritdoc />
    public override double LineTypeScale { get; set; }

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 5.0;

    /// <summary>
    /// Состояние указания точек выносок
    /// </summary>
    [SaveToXData]
    public int CurrentJigState { get; set; } = (int)CrestedLeaderJigState.PromptInsertPoint;

    public List<Point3d> LeaderEndPointsOCS  => LeaderEndPoints.Select(x => x.TransformBy(BlockTransform.Inverse())).ToList();

    public Point3d ShelfLedgePointOCS => ShelfLedgePoint.TransformBy(BlockTransform.Inverse());

    public Point3d ShelfStartPointOCS => ShelfStartPoint.TransformBy(BlockTransform.Inverse());

    public Point3d ShelfEndPointOCS => ShelfEndPoint.TransformBy(BlockTransform.Inverse());

   
    [SaveToXData] 
    public List<Point3d> LeaderEndPoints { get; set; } = new ();

    [SaveToXData]
    public Point3d ShelfStartPoint { get; set; }

    [SaveToXData]
    public Point3d ShelfLedgePoint { get; set; }

    [SaveToXData]
    public Point3d ShelfEndPoint { get; set; }


    [SaveToXData]
    public Point3d ShelfLedgePointPreviousForGripMove { get; set; }

 
    /// <summary>
    /// Для установки новой точки вставки
    /// Точки в списке отсортированы по возрастанию X
    /// </summary>
    public List<Point3d> LeaderStartPoints
    {
        get
        {
            if (_leaders != null && _leaders.Count > 0)
            {
                return _leaders.Select(l => l.StartPoint).OrderBy(p => p.X).ToList();
            }

            return null;
        }
    }

    #endregion

    #region Свойства - геометрия

    /// <summary>
    /// Отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p61", 1.0, 0.0, 10.0, nameSymbol: "t")]
    [SaveToXData]
    public double TextIndent { get; set; } = 1.0;

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p62", 1.0, 0.0, 10.0, nameSymbol: "v")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;

    /// <summary>
    /// Выступ полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p63", 1, 0, 70, descLocalKey: "d63", nameSymbol: "n")]
    [SaveToXData]
    public int ShelfLedge { get; set; } = 1;

    /// <summary>
    /// Положение полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p78", ShelfPosition.Right)]
    [SaveToXData]
    public ShelfPosition ShelfPosition { get; set; } = ShelfPosition.Right;

    #endregion

    #region Свойства - контент

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; }

    /// <summary>
    /// Текст верхний
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p132", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    public string TopText { get; set; } = Language.GetItem("p136");

    /// <summary>
    /// Текст нижний
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 3, "p133", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    public string BottomText { get; set; } = string.Empty;

    /// <summary>
    /// Высота верхнего текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 4, "p134", 5.0, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double TopTextHeight { get; set; } = 5.0;

    /// <summary>
    /// Высота нижнего текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 5, "p135", 5.0, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double BottomTextHeight { get; set; } = 5.0;

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 6, "p85", false, descLocalKey: "d85")]
    [PropertyVisibilityDependency(new[] { nameof(TextMaskOffset) })]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 7, "p86", 0.5, 0.0, 5.0)]
    [SaveToXData]
    public double TextMaskOffset { get; set; }

    #endregion

    /// <inheritdoc/>
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>();

            if (_leaders != null)
                entities.AddRange(_leaders);

            //if (_leaderArrows != null)
            //    entities.AddRange(_leaderArrows);

            if (_shelfLine != null)
                entities.Add(_shelfLine);

            if (_shelf != null)
                entities.Add(_shelf);

            //if (_topTextMask != null) 
            //    entities.Add(_topTextMask);

            //if (_bottomTextMask != null) 
            //    entities.Add(_bottomTextMask);

            if (_topText != null)
                entities.Add(_topText);

            //if (_bottomText != null)    
            //    entities.Add(_bottomText);  

            if (_tempLeader != null)
                entities.Add(_tempLeader);

            //if (_testCircle != null)
            //entities.Add(_testCircle);

            //if (_testCirclesAsLeaderPoints != null)
            //    entities.AddRange(_testCirclesAsLeaderPoints);

            foreach (var e in entities)
                SetImmutablePropertiesToNestedEntity(e);


            return entities;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        foreach (var leaderPoint in LeaderEndPoints)
        {
            yield return leaderPoint;
        }

        //yield return InsertionPoint;
        //yield return EndPoint;
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        Loggerq.WriteRecord($"UpdateEntities start; CurrentJigState: {CurrentJigState.ToString()}");

        try
        {
            var scale = GetScale();

            if (CurrentJigState == (int)CrestedLeaderJigState.PromptInsertPoint)
            {
                 CreateTempLeader(InsertionPoint);
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptNextLeaderPoint)
            {
                 CreateTempLeader(EndPoint);
                 CreateTempLeaders();
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptShelfStartPoint)
            {
                // В режиме указания точки полки рисуется набор выносок до точки пересечения с курсором
                CreateTempLeaderLineMoved();
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptShelfIndentPoint)
            {
                // В режиме указания точки отступа полки рисуется набор выносок до точки пересечения с началом полки
                // уже нарисованы!
                // и линия от начала полки до курсора
                CreateTempShelfLine();
                CreateTextSimple(scale);
                CreateTempShelf();
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.None)
            {
                Loggerq.WriteRecord($"UpdateEntities: CrestedLeaderJigState.None");
                CreateLeaderLines();
                CreateShelf(CreateText(scale), scale);
                Loggerq.WriteRecord($"UpdateEntities: CrestedLeaderJigState.None 2");
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }

        Loggerq.WriteRecord($"UpdateEntities end");
    }

    #region Графика при отрисовке

    private void TestCreateEntities(double scale, Point3d center)
    {
        _testCircle = new Circle()
        {
            Radius = 30,
            Center = center

        };
    }

    private void CreateEntities(double scale)
    {
        CreateLeaderLines();

        /*
        var textWidth = CreateText(scale);
        CreateShelf(textWidth, scale);

        if (CurrentJigState == (int)CrestedLeaderJigState.None)
            CreateLeaderArrow(scale); 
        */
    }

    private void CreateLeaderLines()
    {
        _leaders.Clear();

        var leaderEndPointsOcs = LeaderEndPointsOCS.OrderBy(p => p.X).ToList();

        var leaderLastEndPointOcs =leaderEndPointsOcs.Last().ToPoint2d();
        var insertionPointOcs = InsertionPointOCS.ToPoint2d();

        var newLeaderVector = insertionPointOcs - leaderLastEndPointOcs;

        if (newLeaderVector.Angle.RadianToDegree() == 0 || LeaderEndPoints.Any(x => x.Equals(InsertionPoint)))
            return;

        for (int i = 0; i < leaderEndPointsOcs.Count; i++)
        {
            var intersectPoint = GetIntersectBetweenVectors(
                insertionPointOcs,
                Vector2d.XAxis,
                leaderEndPointsOcs[i].ToPoint2d(),
                newLeaderVector);

            _leaders.Add(new Line(intersectPoint.ToPoint3d(), leaderEndPointsOcs[i]));
        }

        var leaderStartPoints = _leaders?.Select(l => l.StartPoint).ToList();

        if (leaderStartPoints != null && leaderStartPoints.Count > 0)
        {
            var leaderStartPointsSort = leaderStartPoints.OrderBy(p => p.X);
            _shelfLine = new Line(leaderStartPointsSort.First(), leaderStartPointsSort.Last());
        }
    }



    private double CreateText(double scale)
    {
        if (_leaders != null && _leaderPoints.Count > 0)
        {


            _topText = new MText
            {
                Contents = TopText,
                Attachment = AttachmentPoint.MiddleCenter,
            };
            _topText.SetProperties(TextStyle, TopTextHeight * scale);

            _bottomText = new MText
            {
                Contents = BottomText,
                Attachment = AttachmentPoint.MiddleCenter,
            };
            _bottomText.SetProperties(TextStyle, BottomTextHeight * scale);

            var topTextWidth = _topText.ActualWidth;
            var bottomTextWidth = _bottomText.ActualWidth;

            var topTextHeight = _topText.ActualHeight;
            var bottomTextHeight = _bottomText.ActualHeight;

            var textWidth = topTextWidth >= bottomTextWidth ? topTextWidth : bottomTextWidth;

            var startLeaderPointsSort = _leaders.OrderBy(x => x.StartPoint.X).Select(x => x.StartPoint).ToList();

            Point3d textRegionCenterPoint;
            if (ShelfPosition == ShelfPosition.Right)
            {
                textRegionCenterPoint = startLeaderPointsSort.Last() +
                                        (Vector3d.XAxis * (((ShelfLedge + TextIndent) * scale) + (textWidth / 2)));
            }
            else
            {
                textRegionCenterPoint = startLeaderPointsSort.First() -
                                        (Vector3d.XAxis * (((ShelfLedge + TextIndent) * scale) + (textWidth / 2)));
            }

            Point3d topTextLocation = textRegionCenterPoint +
                                      (Vector3d.YAxis * ((TextVerticalOffset * scale) + (topTextHeight / 2)));
            Point3d bottomTextLocation = textRegionCenterPoint -
                                         (Vector3d.YAxis * ((TextVerticalOffset * scale) + (bottomTextHeight / 2)));

            _topText.Location = topTextLocation;
            _bottomText.Location = bottomTextLocation;

            if (HideTextBackground)
            {
                if (_topText != null)
                {
                    _topTextMask = _topText.GetBackgroundMask(TextMaskOffset * scale);
                }

                if (_bottomText != null)
                {
                    _bottomTextMask = _bottomText.GetBackgroundMask(TextMaskOffset * scale);
                }
            }

            return textWidth;
        }

        return 0;
    }

    private void CreateShelf(double textWidth, double scale)
    {
        //if (_leaders != null && _leaders.Count > 0)
        //{
        //    var startLeaderPointsSort = _leaders.OrderBy(x => x.StartPoint.X).Select(x => x.StartPoint).ToList();
        //    _shelf = new Line(startLeaderPointsSort.First(), startLeaderPointsSort.Last());

        //    var shelfLength = Vector3d.XAxis * (((ShelfLedge + (2 * TextIndent)) * scale) + textWidth);

        //    if (ShelfPosition == ShelfPosition.Right)
        //    {
        //        _shelfLine = new Line(
        //            startLeaderPointsSort.Last(),
        //            startLeaderPointsSort.Last() + shelfLength);
        //    }
        //    else
        //    {
        //        _shelfLine = new Line(
        //            startLeaderPointsSort.First(),
        //            startLeaderPointsSort.First() - shelfLength);
        //    }


        //}
    }

    private void CreateLeaderArrow(double scale)
    {
          if (CurrentJigState != (int)CrestedLeaderJigState.None)
              return;
    }
    
    #endregion

    #region Временная графика при вставке 

    /// <summary>
    /// Отрисовка выноски в режиме указания точек
    /// </summary>
    /// <param name="leaderEndPoint">Точка конца выноски</param>
    private void CreateTempLeader(Point3d leaderEndPoint)
    {
        _tempLeader = GetLeaderSimplyLine(leaderEndPoint);
        //_leaderArrows.Add(GetLeaderSimpleArrow());
    }

    private void CreateTempLeaderLineMoved()
    {
        _tempLeader = null;
        _leaders.Clear();

        bool isCorrectDistance = LeaderEndPoints.Any(x => (x - EndPoint).Length > MinDistanceBetweenPoints);

        if (isCorrectDistance)
        {
            var newLeaderVector = EndPointOCS.ToPoint2d() - LeaderEndPoints.Last().ToPoint2d();

            if (newLeaderVector.Angle.RadianToDegree() == 0 || LeaderEndPoints.Any(x => x.Equals(EndPoint)))
                return;

            for (int i = 0; i < LeaderEndPoints.Count; i++)
            {
                var intersectPoint = GetIntersectBetweenVectors(
                    EndPointOCS.ToPoint2d(),
                    Vector2d.XAxis,
                    LeaderEndPoints[i].ToPoint2d(),
                    newLeaderVector);

                _leaders.Add(new Line(intersectPoint.ToPoint3d(), LeaderEndPoints[i]));
            }

            var leaderStartPoints = _leaders?.Select(l => l.StartPoint).ToList();

            if (leaderStartPoints != null && leaderStartPoints.Count > 0)
            {
                var leaderStartPointsSort = leaderStartPoints.OrderBy(p => p.X);
                _shelfLine = new Line(leaderStartPointsSort.First(), leaderStartPointsSort.Last());
            }
        }
    }

    private void CreateTempShelfLine()
    {
        _shelfLine = null;

        if (_leaders != null && _leaders.Count > 0)
        {
            var leaderStartPoints = _leaders.Select(x => x.StartPoint).OrderBy(p => p.X).ToList();

            var leftStartPoint = leaderStartPoints.First();
            var rightStartPoint = leaderStartPoints.Last();

            var shelfLedgePoint = new Point3d(EndPointOCS.X, ShelfStartPointOCS.Y, EndPointOCS.Z);

            if (EndPoint.X > rightStartPoint.X)
            {
                // Рисуем полку вправо
                _shelfLine = new Line(leftStartPoint, shelfLedgePoint);
                ShelfPosition = ShelfPosition.Right;
            }
            else if (EndPoint.X < leftStartPoint.X)
            {
                // Влево
                _shelfLine = new Line(rightStartPoint, shelfLedgePoint);
                ShelfPosition = ShelfPosition.Left;
            }
            else
            {
                if (EndPoint.X < GeometryUtils.GetMiddlePoint3d(leftStartPoint, rightStartPoint).X)
                {
                    // если левее от середины между началами выносок
                    shelfLedgePoint = new Point3d(leftStartPoint.X - ShelfLedge, InsertionPoint.Y, InsertionPoint.Z);
                    _shelfLine = new Line(rightStartPoint, shelfLedgePoint);

                    ShelfPosition = ShelfPosition.Left;
                }
                else
                {
                    shelfLedgePoint = new Point3d(rightStartPoint.X + ShelfLedge, InsertionPoint.Y, InsertionPoint.Z);
                    _shelfLine = new Line(leftStartPoint, shelfLedgePoint);

                    ShelfPosition = ShelfPosition.Right;
                }
            }
        }
    }

    private void CreateTempShelf()
    {
        _shelf = null;

        ShelfLedgePoint = new Point3d(EndPoint.X, ShelfStartPoint.Y, ShelfStartPoint.Z);

        if (_topText != null)
        {
            var vectorShelfEndPoint = (Vector3d.XAxis * ((TextIndent * 2) + _topText.ActualWidth));

            if (ShelfPosition == ShelfPosition.Right)
            {
                ShelfEndPoint = ShelfLedgePoint + vectorShelfEndPoint;
            }
            else
            {
                ShelfEndPoint = ShelfLedgePoint - vectorShelfEndPoint;
            }

            _shelf = new Line(ShelfLedgePointOCS, ShelfEndPointOCS);

        }
    }

    private void CreateTempLeaders()
    {
        _leaders.Clear();

        foreach (var leaderPointOCS in LeaderEndPointsOCS)
        {
            _leaders.Add(GetLeaderSimplyLine(leaderPointOCS));
        }
    }

    private Line GetLeaderSimplyLine(Point3d leaderEndPoint)
    {
        var lengthLeader = 60d;
        var angleLeader = 60.DegreeToRadian();

        var leaderStartPoint = new Point3d(
            leaderEndPoint.X + lengthLeader * Math.Cos(angleLeader),
            leaderEndPoint.Y + lengthLeader * Math.Sin(angleLeader),
            leaderEndPoint.Z);

        return new Line(leaderStartPoint, leaderEndPoint)
        {
            // ColorIndex = 150
        }; 
    }

    private Line GetLeaderSimpleArrow()
    {
        return new Line();
    }

    private void CreateTextSimple(double scale)
    {
        //string text = 
        _topText = new MText
        {
            Contents = TopText,
            Attachment = AttachmentPoint.MiddleCenter,
        };
        _topText.SetProperties(TextStyle, TopTextHeight * scale);

        var topTextWidth = _topText.ActualWidth;
        var topTextHeight = _topText.ActualHeight;

        var shelfLedgePoint = new Point3d(EndPointOCS.X,  ShelfStartPointOCS.Y, ShelfStartPointOCS.Z);

        Point3d textRegionCenterPoint;
        if (ShelfPosition == ShelfPosition.Right)
        {
            textRegionCenterPoint = shelfLedgePoint + (Vector3d.XAxis * (((ShelfLedge + TextIndent) * scale) + (topTextWidth / 2)));
        }
        else
        {
            textRegionCenterPoint = shelfLedgePoint - (Vector3d.XAxis * (((ShelfLedge + TextIndent) * scale) + (topTextWidth / 2)));
        }

        _topText.Location  = textRegionCenterPoint + (Vector3d.YAxis * ((TextVerticalOffset * scale) + (topTextHeight / 2)));
    }

    #endregion

    /// <summary>
    /// Возвращает точку пересечения 2х 2D векторов
    /// </summary>
    private Point2d GetIntersectBetweenVectors(Point2d point1, Vector2d vector1, Point2d point2, Vector2d vector2)
    {
        var v1 = point1 + vector1;
        var v2 = point2 + vector2;

        // далее по уравнению прямой по двум точкам

        var x11 = point1.X;
        var y11 = point1.Y;
        var x21 = v1.X;
        var y21 = v1.Y;

        var x12 = point2.X;
        var y12 = point2.Y;
        var x22 = v2.X;
        var y22 = v2.Y;

        var a1 = (y21 - y11) / (x21 - x11);
        var a2 = (y22 - y12) / (x22 - x12);

        var b1 = ((y11 * (x21 - x11)) + (x11 * (y11 - y21))) / (x21 - x11);
        var b2 = ((y12 * (x22 - x12)) + (x12 * (y12 - y22))) / (x22 - x12);

        var x = (b1 - b2) / (a2 - a1);
        var y = (a2 * x) + b2;

        return !double.IsNaN(x) || !double.IsNaN(y) ? new Point2d(x, y) : default;
    }
}