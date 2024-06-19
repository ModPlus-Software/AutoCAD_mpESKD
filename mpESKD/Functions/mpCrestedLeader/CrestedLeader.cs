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
public class CrestedLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor, IJigProps
{
    #region Примитивы

    private readonly List<Line> _leaders = new ();
    private readonly List<Line> _leaderArrows = new ();

    private Line _shelf = new ();
    private Line _unionLine = new ();

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




    //[SaveToXData]
    //public bool IsFirstCreate { get; set; } = false;

    /// <summary>
    /// Состояние указания точек выносок
    /// </summary>
    [SaveToXData]
    public int CurrentJigState { get; set; } = (int)CrestedLeaderJigState.PromptInsertPoint;



    public List<Point3d> LeaderPointsOCS  => LeaderPoints.Select(x => x.TransformBy(BlockTransform.Inverse())).ToList();

    public Point3d ShelfIndentPointOCS => ShelfIndentPoint.TransformBy(BlockTransform.Inverse());

    public Point3d ShelfStartPointOCS => ShelfStartPoint.TransformBy(BlockTransform.Inverse());



    /// <summary>
    /// Точки концов выносок
    /// </summary>
    [SaveToXData] 
    public List<Point3d> LeaderPoints { get; set; } = new ();

    [SaveToXData]
    public Point3d ShelfStartPoint { get; set; }

    [SaveToXData]
    public Point3d ShelfIndentPoint { get; set; }

    [SaveToXData]
    public Point3d ShelfIndentPointTempForGripMove { get; set; }



    [SaveToXData] 
    public List<Point3d> LeaderPointsMove { get; set; } = new ();

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

            //if (_shelf != null)
            //    entities.Add(_shelf);

            //if (_unionLine != null) 
            //    entities.Add(_unionLine);

            //if (_topTextMask != null) 
            //    entities.Add(_topTextMask);

            //if (_bottomTextMask != null) 
            //    entities.Add(_bottomTextMask);

            //if (_topText != null) 
            //    entities.Add(_topText);

            //if (_bottomText != null)    
            //    entities.Add(_bottomText);  

            if (_testCircle != null)
            entities.Add(_testCircle);

            if (_testCirclesAsLeaderPoints != null)
                entities.AddRange(_testCirclesAsLeaderPoints);

            foreach (var e in entities)
                SetImmutablePropertiesToNestedEntity(e);


            return entities;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        foreach (var leaderPoint in _leaderPoints)
        {
            yield return leaderPoint;
        }
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();

            Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; LeaderPoints.Count: {LeaderPoints.Count}");
            Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; LeaderPointsOCS.Count: {LeaderPointsOCS.Count}");
            Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; LeaderPointsMove.Count: {LeaderPointsMove.Count}");
            Loggerq.WriteRecord($"--------------------------------------------------------------");

            //Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; CurrentJigState: {CurrentJigState}");
            //Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; IsFirstCreate: {IsFirstCreate}");
            Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; InsertionPoint: {InsertionPoint}");
            Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; InsertionPointOCS: {InsertionPointOCS}");
            Loggerq.WriteRecord($"--------------------------------------------------------------");

            Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; LeaderPointsMove: =>");
            var ii = 1;
            foreach (var pt in LeaderPointsMove)
            {
                Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; LeaderPointsMove[{ii}] : {pt.ToString()}");
                ii++;
            }


            if (CurrentJigState == (int)CrestedLeaderJigState.PromptInsertPoint)
            {
               // TestCreateEntities(scale, InsertionPoint);
              // CreateSimplyLeader(InsertionPoint);
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptNextLeaderPoint)
            {
                // TestCreateEntities(scale, EndPoint);
               // CreateSimplyLeader(InsertionPoint);
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptShelfStartPoint)
            {
                // CreateEntities(scale);
                TestCreateEntities(scale, EndPoint);
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptShelfIndentPoint)
            {
                TestCreateEntities(scale, EndPoint);
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.None)
            {
                _testCircle = null;
                _leaders.Clear();
                _leaderArrows.Clear();
                _testCirclesAsLeaderPoints.Clear();
                _leaderPoints.Clear();

                //List<Point3d> testCirclesPoints = new ();

                double testCircleRadius;
                //if (IsFirstCreate)
                //{
                //    Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; IsFirstCreate: TRUE");
                //    LeaderPointsMove.Clear();

                //    testCircleRadius = 30;
                //    //foreach (var leaderPointOcs in LeaderPointsOCS)
                //    //{
                //    //    var vec = leaderPointOcs - InsertionPoint;
                //    //    _leaderPoints.Add(InsertionPoint + vec);
                //    //}

                //    _leaderPoints.AddRange(LeaderPointsOCS);
                //    _leaderPoints.Add(ShelfStartPointOCS);
                //    _leaderPoints.Add(ShelfIndentPointOCS);

                //    foreach (var leaderPoint in LeaderPoints)
                //    {
                //        var vec = leaderPoint - InsertionPointOCS;
                //        LeaderPointsMove.Add(Point3d.Origin + vec); 
                //    }   
                //}
                //else
                //{
                    //Loggerq.WriteRecord($"CrectedLeader; UpdateEntities; IsFirstCreate: FALSE");

                        /*
                    foreach (var leaderPointMove in LeaderPointsMove)
                    {
                        var vectorMove = leaderPointMove - Point3d.Origin;
                        _leaderPoints.Add(InsertionPointOCS + vectorMove);

                        
                    }
                        */

                        ShelfStartPoint = InsertionPoint;

                        _leaderPoints.AddRange(LeaderPointsOCS);

                    _leaderPoints.Add(ShelfStartPointOCS);
                    _leaderPoints.Add(ShelfIndentPointOCS);
                    testCircleRadius = 10;
                //}



                foreach (var leaderPoint in _leaderPoints)
                {
                    var testCircle = new Circle()
                    {
                        Radius = testCircleRadius,
                        Center = leaderPoint
                    };

                    _testCirclesAsLeaderPoints.Add(testCircle);
                }
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

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

        var textWidth = CreateText(scale);
        CreateShelf(textWidth, scale);

        if (CurrentJigState == (int)CrestedLeaderJigState.None)
            CreateLeaderArrow(scale);
    }
    
    private void CreateLeaderLines()
    {
        var newLeaderVector = EndPointOCS.ToPoint2d() - _leaderPoints.Last().ToPoint2d();

        if (newLeaderVector.Angle.RadianToDegree() == 0 || _leaderPoints.Any(x => x.Equals(EndPoint)))
            return;

        _leaders.Clear();
        _leaderArrows.Clear();

        for (int i = 0; i < _leaderPoints.Count; i++)
        {
            var intersectPoint = GetIntersectBetweenVectors(
                EndPointOCS.ToPoint2d(),
                Vector2d.XAxis,
                _leaderPoints[i].ToPoint2d(),
                newLeaderVector);

            _leaders.Add(new Line(intersectPoint.ToPoint3d(), _leaderPoints[i]));
        }

        if (CurrentJigState != (int)CrestedLeaderJigState.None)
        {
            if (newLeaderVector.Angle.RadianToDegree() < 90 || newLeaderVector.Angle.RadianToDegree() > 270)
            {
                ShelfPosition = ShelfPosition.Right;
            }
            else
            {
                ShelfPosition = ShelfPosition.Left;
            }
        }
    }

    private double CreateText(double scale)
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
            textRegionCenterPoint = startLeaderPointsSort.Last() + (Vector3d.XAxis * (((ShelfLedge + TextIndent) * scale) + (textWidth / 2)));
        }
        else
        {
            textRegionCenterPoint = startLeaderPointsSort.First() - (Vector3d.XAxis * (((ShelfLedge + TextIndent) * scale) + (textWidth / 2)));
        }

        Point3d topTextLocation = textRegionCenterPoint + (Vector3d.YAxis * ((TextVerticalOffset * scale) + (topTextHeight / 2)));
        Point3d bottomTextLocation = textRegionCenterPoint - (Vector3d.YAxis * ((TextVerticalOffset * scale) + (bottomTextHeight / 2)));

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

    private void CreateShelf(double textWidth, double scale)
    {
        var startLeaderPointsSort = _leaders.OrderBy(x => x.StartPoint.X).Select(x => x.StartPoint).ToList();
        _unionLine = new Line(startLeaderPointsSort.First(), startLeaderPointsSort.Last());

        var shelfLenght = Vector3d.XAxis * (((ShelfLedge + (2 * TextIndent)) * scale) + textWidth);

        if (ShelfPosition == ShelfPosition.Right)
        {
            _shelf = new Line(
                startLeaderPointsSort.Last(),
                startLeaderPointsSort.Last() + shelfLenght);
        }
        else
        {
            _shelf = new Line(
                startLeaderPointsSort.First(),
                startLeaderPointsSort.First() - shelfLenght);
        }
    }

    private void CreateLeaderArrow(double scale)
    {
          if (CurrentJigState != (int)CrestedLeaderJigState.None)
              return;
    }

    #region Временная графика при вставке 

    /// <summary>
    /// Отрисовка выноски в режиме указания точек
    /// </summary>
    /// <param name="leaderEndPoint">Точка конца выноски</param>
    public void CreateSimplyLeader(Point3d leaderEndPoint)
    {
        _leaders.Add(GetLeaderSimplyLine(leaderEndPoint));
        _leaderArrows.Add(GetLeaderSimpleArrow());
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

    /// <summary>
    /// Отрисовка текущей выноски в режиме указания точек
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="pointMonitorEventArgs">pointMonitorEventArgs</param>
    public void CrestedSimplyLeaderMonitor(object sender, PointMonitorEventArgs pointMonitorEventArgs)
    {
        var cursorPoint = pointMonitorEventArgs.Context.ComputedPoint;

        pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(GetLeaderSimplyLine(cursorPoint));
        pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(GetLeaderSimpleArrow());
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