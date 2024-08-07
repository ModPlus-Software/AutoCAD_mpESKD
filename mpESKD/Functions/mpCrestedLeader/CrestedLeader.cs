using DocumentFormat.OpenXml.Vml.Spreadsheet;

#pragma warning disable SA1000
#pragma warning disable SA1129
namespace mpESKD.Functions.mpCrestedLeader;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using ModPlusAPI;

/// <summary>
/// Гребенчатая выноска
/// </summary>
[SmartEntityDisplayNameKey("h207")]
[SystemStyleDescriptionKey("h208")]
public class CrestedLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    #region Примитивы

    private readonly List<Line> _leaderLines = new ();

    //private readonly List<Line> _leaderArrows = new ();

    private Line _unionLine = new ();
    private Line _shelfLine = new ();
    private Line _shelf = new ();

    /// <summary>
    /// Верхний тест
    /// </summary>
    private MText _topText = new ();

    /// <summary>
    /// Нижний текст
    /// </summary>
    private MText _bottomText = new ();

    /// <summary>
    ///  Маскировка верхнего текста 
    /// </summary>
    private Wipeout _topTextMask = new ();

    /// <summary>
    /// Маскировка нижнего текста
    /// </summary>
    private Wipeout _bottomTextMask = new ();

    private Line _tempLeaderLine = new ();

    private readonly List<Hatch> _hatches = new();
    private readonly List<Polyline> _leaderEndLines = new();

    #endregion

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

    [SaveToXData]
    public List<Point3d> LeaderStartPoints { get; set; } = new ();

    public List<Point3d> LeaderStartPointsOCS => LeaderStartPoints.Select(x => x.TransformBy(BlockTransform.Inverse())).ToList();

    public List<Point3d> LeaderStartPointsSorted => this.OrderLeaderStartPoints();

    [SaveToXData] 
    public List<Point3d> LeaderEndPoints { get; set; } = new ();
    
    public List<Point3d> LeaderEndPointsOCS => LeaderEndPoints.Select(x => x.TransformBy(BlockTransform.Inverse())).ToList();

    // public List<Point3d> LeaderEndPointsSorted => this.OrderLeaderEndPoints();

    [SaveToXData]
    public Point3d ShelfStartPoint { get; set; }

    public Point3d ShelfStartPointOCS => ShelfStartPoint.TransformBy(BlockTransform.Inverse());

    [SaveToXData]
    public Point3d ShelfLedgePoint { get; set; }
    
    public Point3d ShelfLedgePointOCS => ShelfLedgePoint.TransformBy(BlockTransform.Inverse());

    [SaveToXData]
    public Point3d ShelfEndPoint { get; set; }
    
    public Point3d ShelfEndPointOCS => ShelfEndPoint.TransformBy(BlockTransform.Inverse());

    [SaveToXData]
    public bool IsStartPointsAssigned { get; set; } = false;

    [SaveToXData]
    public bool IsBasePointMovedByGrip { get; set; } = false;
    
    [SaveToXData]
    public bool IsBasePointMovedByOverrule { get; set; } = false;

    [SaveToXData]
    public bool IsChangeShelfPosition { get; set; } = false;

    [SaveToXData]
    public Point3d BaseLeaderEndPoint { get; set; }

    public Point3d BaseLeaderEndPointOCS => BaseLeaderEndPoint.TransformBy(BlockTransform.Inverse());

    public Point3d BaseSecondPoint => (InsertionPoint + Vector3d.XAxis).GetRotatedPointByBlock(this);

    public Vector3d BaseVectorNormal => BaseSecondPoint - InsertionPoint;

    #endregion

    #region Свойства - геометрия

    /// <summary>
    /// Отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p61", 1.0, 0.0, 10.0, nameSymbol: "t")]
    [SaveToXData]
    public double TextIndent { get; set; } = 0.0;

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
    public double ShelfLedge { get; set; } = 10;

    /// <summary>
    /// Положение полки
    /// </summary>
    [SaveToXData]
    public ShelfPosition ShelfPosition { get; set; } = ShelfPosition.Right;

    /// <summary>
    /// Размер стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p29", 5, 0.1, 10, nameSymbol: "d")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 3;

    /// <summary>
    /// Тип стрелки
    /// </summary> 
    [EntityProperty(PropertiesCategory.Geometry, 6, "gp7", LeaderEndType.ClosedArrow)]
    [SaveToXData]
    public LeaderEndType ArrowType { get; set; } = LeaderEndType.ClosedArrow;

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

    /// <summary>
    /// Выравнивание текста по горизонтали
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 8, "p73", TextHorizontalAlignment.Left, descLocalKey: "d73")]
    [SaveToXData]
    public TextHorizontalAlignment ValueHorizontalAlignment { get; set; } = TextHorizontalAlignment.Left;

    #endregion

    /// <inheritdoc/>
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>();

            if (_topTextMask != null)
                entities.Add(_topTextMask);

            if (_bottomTextMask != null) 
                entities.Add(_bottomTextMask);

            if (_leaderLines != null)
                entities.AddRange(_leaderLines);

            if (_unionLine != null)
                entities.Add(_unionLine);

            if (_shelfLine != null)
                entities.Add(_shelfLine);

            if (_shelf != null)
                entities.Add(_shelf);

            if (_topText != null)
                entities.Add(_topText);

            if (_bottomText != null)
                entities.Add(_bottomText);

            if (_tempLeaderLine != null)
                entities.Add(_tempLeaderLine);

            if (_hatches != null) 
                entities.AddRange(_hatches);

            if (_leaderEndLines != null) 
                entities.AddRange(_leaderEndLines);

            foreach (var e in entities)
                SetImmutablePropertiesToNestedEntity(e);

            return entities;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return ShelfLedgePoint;
        yield return ShelfEndPoint;

        foreach (var leaderEndPoint in LeaderEndPoints)
        {
            yield return leaderEndPoint;
        }

        foreach (var leaderEndPoint in LeaderStartPoints)
        {
            yield return leaderEndPoint;
        }
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        this.ToLogAnyString(" ");
        this.ToLogAnyString("[UpdateEntities] START");
        /*
        this.ToLogAnyString($"  IsStartPointsAssigned: {IsStartPointsAssigned.ToString()}");
        this.ToLogAnyString($"  IsMoveGripPointsAt: {IsMoveGripPointsAt.ToString()}");
        
        this.ToLogAnyString($"  Rotation: {Math.Round(Rotation, 3, MidpointRounding.AwayFromZero)} rad");
        this.ToLogAnyString($"  Rotation: {Math.Round(Rotation.RadianToDegree(), 3, MidpointRounding.AwayFromZero)} degree");
        */

        /*
        this.ToLogAnyStringFromPoint3d(InsertionPoint, "InsertionPoint");
        this.ToLogAnyStringFromPoint3d(InsertionPointOCS, "InsertionPointOCS");
        */

        /*
        BaseSecondPoint.ToLog("BaseSecondPoint");
        BaseSecondPointOCS.ToLog("BaseSecondPointOCS");

        this.ToLogAnyString("\n");
        */

        /*
        this.ToLogAnyString("Преобразование из vector3d в vector2d");
        var pt3dStart = new Point3d(100, 100, 0);
        var pt3dEnd = new Point3d(200, 200, 0);
        var vec3d = pt3dEnd - pt3dStart;
        var vec2d = vec3d.ToVector2d();

        var pt2dStart = new Point2d(pt3dStart.X, pt3dStart.Y);
        var pt2dEnd = pt2dStart + vec2d;

        this.ToLogAnyString($"Точка должна быть = (200, 200)");
        pt2dEnd.ToLog("Конец вектора 2d:");*/

        try
        {
            var scale = GetScale();

            if (CurrentJigState == (int)CrestedLeaderJigState.PromptInsertPoint)
            {
                CreateTempCurrentLeader(InsertionPoint, scale);
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptNextLeaderPoint)
            {
                CreateTempCurrentLeader(EndPoint, scale);
                CreateTempLeaders(scale);
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptShelfStartPoint)
            {
                // В режиме указания точки полки рисуется набор выносок до точки пересечения с курсором
                CreateTempLeaderLineMoved(scale); 
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptShelfIndentPoint)
            {
                CreateTempShelfLines(); 
                CreateTextSimple(scale);
                CreateTempShelf(); 
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.None)
            {
                CreateEntities(scale);

                /*
                if (IsChangeShelfPosition && IsShelfPointMovedByGrip)
                {
                    CreateEntitiesOnShelfMoved(scale);
                }
                else if (IsStartPointsAssigned)
                {
                    CreateEntitiesFirst(scale);
                }
                else
                {
                    this.ToLogAnyString("  Update Normal");
                    CreateEntities(scale);
                }*/
            }

            /*
            this.ToLogAnyString($"  Rotation: {Math.Round(Rotation, 3, MidpointRounding.AwayFromZero)} rad");
            this.ToLogAnyString($"  Rotation: {Math.Round(Rotation.RadianToDegree(), 3, MidpointRounding.AwayFromZero)} degree");
            this.ToLogAnyStringFromPoint3d(InsertionPoint, "InsertionPoint");
            this.ToLogAnyStringFromPoint3d(InsertionPointOCS, "InsertionPointOCS");
            this.ToLogAnyStringFromPoint3d(ShelfLedgePoint, "ShelfLedgePoint");
            this.ToLogAnyStringFromPoint3d(ShelfLedgePointOCS, "ShelfLedgePointOCS");
            this.ToLogAnyStringFromPoint3d(ShelfEndPoint, "ShelfEndPoint");
            this.ToLogAnyStringFromPoint3d(ShelfEndPointOCS, "ShelfEndPointOCS");
            */
        }
        catch (Exception exception)
        {
            // todo
            //ExceptionBox.Show(exception);
            this.ToLogErr("CrestedLeader", "UpdateEntities", exception);
        }

        /*
        LeaderStartPoints.ToLogAnyStringFromPoint3dList("LeaderStartPoints");
        var leaderStartPointsSort1 = LeaderStartPoints.OrderByBaseLine(LeaderStartPointsOCS);
        leaderStartPointsSort1.ToLogAnyStringFromPoint3dList("LeaderStartPoints sorted");*/

        this.ToLogAnyString($"Entities.Count: {Entities.Count()}");
        foreach (var en in Entities)
        {
            this.ToLogAnyString($"Entity: {en.GetRXClass().Name}");
        }
        this.ToLogAnyString($"_leaderEndLines.Count: {_leaderEndLines.Count()}");
        this.ToLogAnyString($"_hatches.Count: {_hatches.Count()}");

        this.ToLogAnyString("[UpdateEntities] END");
        this.ToLogAnyString(".");
    }

    private void CreateEntities(double scale)
    {
        this.ToLogAnyString("[CreateEntities] START");

        // Обнуление примитивов
        _topTextMask = null;
        _bottomTextMask = null;
        _tempLeaderLine = null;
        _leaderLines.Clear();
        _unionLine = null;
        _shelfLine = null;
        _shelf = null;
        _topText = null;
        _bottomText = null;

        // Создание выносок
        CreateLeaderLines(scale);
        
        if (_leaderLines.Count == 0)
        {
            return;
        }

        // Создание текста

        var widthWidestText = CreateText(scale);

        // Создание доп. линий и полки

        var leaderStartPointsOcsSort = LeaderStartPointsOCS.OrderBy(p => p.X).ToList();
        _unionLine = new Line(leaderStartPointsOcsSort.First(), leaderStartPointsOcsSort.Last());
        
        // ShelfStartPoint задается через обработку ручек, кроме
        if (IsBasePointMovedByOverrule)
        {
            ShelfStartPoint = this.GetShelfStartPoint();
        }

        ShelfLedgePoint = this.GetShelfLedgePoint();
        _shelfLine = new Line(ShelfStartPointOCS, ShelfLedgePointOCS);

        ShelfEndPoint = this.GetShelfEndPoint(widthWidestText);
        _shelf = new Line(ShelfLedgePointOCS, ShelfEndPointOCS);

        // Указание положения текста и создание маскировки

        var textRegionCenterPoint = GeometryUtils.GetMiddlePoint3d(ShelfLedgePointOCS, ShelfEndPointOCS);

        Vector3d movingTextPosition = new ();

        if (_bottomText != null && _topText != null && !string.IsNullOrEmpty(TopText) && !string.IsNullOrEmpty(BottomText) &&
            !_topText.ActualWidth.Equals(_bottomText.ActualWidth))
        {
            var distMove = Math.Abs(_topText.ActualWidth - _bottomText.ActualWidth);
            var textHalfMovementHorV = Vector3d.XAxis * (distMove / 2);

            movingTextPosition = EntityUtils.GetMovementPositionVector(ValueHorizontalAlignment, true, textHalfMovementHorV, ScaleFactorX);
        }

        if (_topText != null)
        {
            var yVectorToCenterTopText = Vector3d.YAxis * ((TextVerticalOffset * scale) + (_topText.ActualHeight / 2));

            _topText.Location = textRegionCenterPoint + yVectorToCenterTopText;

            if (_bottomText != null && _topText.ActualWidth < _bottomText.ActualWidth)
            {
                _topText.Location += movingTextPosition;
            }
        }

        if (_bottomText != null)
        {
            var yVectorToCenterBottomText = Vector3d.YAxis * ((TextVerticalOffset * scale) + (_bottomText.ActualHeight / 2));
           
            _bottomText.Location = textRegionCenterPoint - yVectorToCenterBottomText;

            if (_topText != null && _bottomText.ActualWidth < _topText.ActualWidth)
            {
                _bottomText.Location += movingTextPosition;
            }
        }

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

        IsStartPointsAssigned = false;
        IsBasePointMovedByOverrule = false;
       
        this.ToLogAnyString("[CreateEntities] END");
    }

    private void CreateLeaderLines(double scale)
    {
        /*
        InsertionPoint.ToLog("InsertionPoint");
        BaseLeaderEndPoint.ToLog("BaseLeaderEndPoint");
        LeaderEndPoints.ToLog("LeaderEndPoints");
        this.ToLogAnyString(" ");
        InsertionPointOCS.ToLog("InsertionPointOCS");
        BaseLeaderEndPointOCS.ToLog("BaseLeaderEndPointOCS");
        LeaderEndPointsOCS.ToLog("LeaderEndPointsOCS");

        _leaderLines.Clear();
        */

        // todo Предотвратить улет выносок в 0 и др. неприятности
        if (BaseLeaderEndPointOCS.Y.Equals(InsertionPointOCS.Y))
        {
            return;
        }

        _leaderEndLines.Clear();
        _hatches.Clear();

        // Если не нужно вычислять точки начал выносок.
        if (IsStartPointsAssigned)
        {
            for (var i = 0; i < LeaderEndPoints.Count; i++)
            {
                _leaderLines.Add(new Line(LeaderStartPointsOCS[i], LeaderEndPointsOCS[i]));
            }
        }
        else
        {
            LeaderStartPoints.Clear();

            // BaseLeaderEndPoint задается при обработке Jig и меняется в OnGripStatusChanged:

            // Для выносок, перпендикулярных к центральной линии
            if (BaseLeaderEndPoint.GetProjectPointToBaseLine(this).Equals(InsertionPoint))
            {
                for (int i = 0; i < LeaderEndPointsOCS.Count; i++)
                {
                    var leaderStartPointOcs =
                        new Point3d(LeaderEndPointsOCS[i].X, InsertionPointOCS.Y, InsertionPointOCS.Z);

                    _leaderLines.Add(new Line(leaderStartPointOcs, LeaderEndPointsOCS[i]));

                    LeaderStartPoints.Add(leaderStartPointOcs.Point3dOcsToPoint3d(this));
                }
            }
            else
            {
                var newLeaderVector = InsertionPoint.ToPoint2d() - BaseLeaderEndPoint.ToPoint2d();

                for (int i = 0; i < LeaderEndPointsOCS.Count; i++)
                {
                    var intersectPoint = Intersections.GetIntersectionBetweenVectors(
                        InsertionPoint,
                        BaseVectorNormal.ToVector2d(),
                        LeaderEndPoints[i],
                        newLeaderVector);

                    if (intersectPoint == null)
                        continue;

                    LeaderStartPoints.Add(intersectPoint.Value);

                    var leaderLine = new Line(LeaderStartPointsOCS[i], LeaderEndPointsOCS[i]);
                    _leaderLines.Add(leaderLine);
                }
            }
        }

        foreach (var leaderLine in _leaderLines)
        {
            var arrowVector = (leaderLine.EndPoint - leaderLine.StartPoint).Negate();
            CreateArrow(leaderLine.EndPoint, arrowVector, ArrowSize, scale * 0.02);
        }
    }

    private double CreateText(double scale)
    {
        double topTextWidth;
        if (!string.IsNullOrEmpty(TopText))
        {
            _topText = new MText
            {
                Contents = TopText,
                Attachment = AttachmentPoint.MiddleCenter,
            };

            _topText.SetProperties(TextStyle, TopTextHeight * scale);
            topTextWidth = _topText.ActualWidth;
        }
        else
        {
            topTextWidth = 0;
        }

        double bottomTextWidth;
        if (!string.IsNullOrEmpty(BottomText))
        {
            _bottomText = new MText
            {
                Contents = BottomText,
                Attachment = AttachmentPoint.MiddleCenter,
            };

            _bottomText.SetProperties(TextStyle, BottomTextHeight * scale);
            bottomTextWidth = _bottomText.ActualWidth;
        }
        else
        {
            bottomTextWidth = 0;
        }

        var textWidth = topTextWidth >= bottomTextWidth ? topTextWidth : bottomTextWidth;
        return textWidth;
    }
    
    #region Временная графика при вставке 

    /// <summary>
    /// Отрисовка выноски в режиме указания точек
    /// </summary>
    /// <param name="leaderEndPoint">Точка конца выноски</param>
    private void CreateTempCurrentLeader(Point3d leaderEndPoint, double scale)
    {
        _bottomTextMask = null;
        _topTextMask = null;
        _unionLine = null;
        _shelfLine = null;
        _shelf = null;
        _topText = null;
        _bottomText = null;
        
        _leaderEndLines.Clear();
        _hatches.Clear();

        _tempLeaderLine = GetLeaderSimplyLine(leaderEndPoint);

        var arrowVector = (_tempLeaderLine.EndPoint - _tempLeaderLine.StartPoint).Negate();
        CreateArrow(leaderEndPoint, arrowVector, ArrowSize, scale * 0.02);
    }
    
    private static Line GetLeaderSimplyLine(Point3d leaderEndPoint)
    {
        var lengthLeader = 60d;
        var angleLeader = 60.DegreeToRadian();

        var leaderStartPoint = new Point3d(
            leaderEndPoint.X + (lengthLeader * Math.Cos(angleLeader)),
            leaderEndPoint.Y + (lengthLeader * Math.Sin(angleLeader)),
            leaderEndPoint.Z);

        return new Line(leaderStartPoint, leaderEndPoint);
    }

    private void CreateTempLeaders(double scale)
    {
        _leaderLines.Clear();

        _leaderEndLines.Clear();
        _hatches.Clear();

        foreach (var leaderEndPointOcs in LeaderEndPointsOCS)
        {
            var leaderLine = GetLeaderSimplyLine(leaderEndPointOcs);
            _leaderLines.Add(leaderLine);

            var arrowVector = (leaderLine.EndPoint - leaderLine.StartPoint).Negate();
            CreateArrow(leaderEndPointOcs, arrowVector, ArrowSize, scale * 0.02);
        }
    }
   
    private void CreateTempLeaderLineMoved(double scale)
    {
        // EndPoint - точка курсора

        _tempLeaderLine = null;
        _leaderLines.Clear();

        _leaderEndLines.Clear();
        _hatches.Clear();

        // Проверка на приближение курсора к концам выносок
        if (LeaderEndPoints.Any(p => EndPoint.DistanceTo(p) < MinDistanceBetweenPoints))
        {
            EndPoint = EndPoint.GetNormalizedPointByDistToPointSet(LeaderEndPoints, MinDistanceBetweenPoints);
        }

        var newLeaderVector = EndPointOCS.ToPoint2d() - LeaderEndPoints.Last().ToPoint2d();

        if (newLeaderVector.Angle.RadianToDegree() == 0 || LeaderEndPoints.Any(x => x.Equals(EndPoint)))
        {
            return;
        }

        for (int i = 0; i < LeaderEndPoints.Count; i++)
        {
            var intersectPoint = Intersections.GetIntersectionBetweenVectors(
                EndPointOCS.ToPoint2d(),
                Vector2d.XAxis,
                LeaderEndPoints[i].ToPoint2d(),
                newLeaderVector);

            if (intersectPoint == null)
            {
                continue;
            }

            _leaderLines.Add(new Line(intersectPoint.Value.ToPoint3d(), LeaderEndPoints[i]));
        }

        if (_leaderLines != null)
        {
            foreach (var leaderLine in _leaderLines)
            {
                var arrowVector = (leaderLine.EndPoint - leaderLine.StartPoint).Negate();
                CreateArrow(leaderLine.EndPoint, arrowVector, ArrowSize, scale * 0.02);
            }

            LeaderStartPoints = _leaderLines.Select(l => l.StartPoint).ToList();

            if (LeaderStartPoints.Count > 1)
            {
                _shelfLine = new Line(LeaderStartPointsSorted.First(), LeaderStartPointsSorted.Last());
            }
        }
    }

    private void CreateTempShelfLines()
    {
        // EndPoint - точка курсора

        _unionLine = null;
        _shelfLine = null;

        if (LeaderStartPoints == null)
            return;

        if (LeaderStartPoints.Count > 1)
        {
            var leaderStartPointsOcsSort = LeaderStartPointsOCS.OrderBy(p => p.X);

            _unionLine = new Line(
                // ReSharper disable once PossibleMultipleEnumeration
                leaderStartPointsOcsSort.First(), 
                // ReSharper disable once PossibleMultipleEnumeration
                leaderStartPointsOcsSort.Last());

            var leaderStartPointsSort = LeaderStartPointsSorted;

            var leftStartPoint = leaderStartPointsSort.First();
            var rightStartPoint = leaderStartPointsSort.Last();

            if (EndPoint.X > rightStartPoint.X)
            {
                ShelfStartPoint = rightStartPoint;
                ShelfLedgePoint = new Point3d(EndPoint.X, ShelfStartPoint.Y, ShelfStartPoint.Z);

                ShelfPosition = ShelfPosition.Right;
            }
            else if (EndPoint.X < leftStartPoint.X)
            {
                ShelfStartPoint = leftStartPoint;
                ShelfLedgePoint = new Point3d(EndPoint.X, ShelfStartPoint.Y, ShelfStartPoint.Z);

                ShelfPosition = ShelfPosition.Left;
            }
            else
            {
                var middlePointLeaders = GeometryUtils.GetMiddlePoint3d(leftStartPoint,  rightStartPoint);

                if (EndPoint.X < middlePointLeaders.X)
                {
                    // если левее от середины между началами выносок
                    ShelfStartPoint = ShelfLedgePoint = leftStartPoint;
                    ShelfPosition = ShelfPosition.Left;
                }
                else
                {
                    ShelfStartPoint = ShelfLedgePoint = rightStartPoint;
                    ShelfPosition = ShelfPosition.Right;
                }
            }
        }
        else if (LeaderStartPoints.Count == 1)
        {
            ShelfStartPoint = LeaderStartPoints[0];

            if (EndPoint.X.Equals(ShelfStartPoint.X))
            {
                ShelfLedgePoint = ShelfStartPoint;
                ShelfPosition = ShelfPosition.Right;
            }
            else
            {
                ShelfLedgePoint = new Point3d(EndPoint.X, ShelfStartPoint.Y, ShelfStartPoint.Z);

                ShelfPosition = EndPoint.X > ShelfStartPoint.X ? ShelfPosition.Right : ShelfPosition.Left;
            }
        }

        if (!ShelfStartPoint.Equals(ShelfLedgePoint))
        {
            _shelfLine = new Line(ShelfStartPointOCS, ShelfLedgePointOCS);
            ShelfLedge = ShelfStartPoint.DistanceTo(ShelfLedgePoint);
        }
        else
        {
            ShelfLedge = 0;
        }
    }

    private void CreateTempShelf()
    {
        _shelf = null;

        if (_topText != null)
        {
            var vectorShelfEndPoint = Vector3d.XAxis * ((TextIndent * 2) + _topText.ActualWidth);

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

    private void CreateTextSimple(double scale)
    {
        _topText = new MText
        {
            Contents = TopText,
            Attachment = AttachmentPoint.MiddleCenter,
        };
        _topText.SetProperties(TextStyle, TopTextHeight * scale);

        var topTextWidth = _topText.ActualWidth;
        var topTextHeight = _topText.ActualHeight;

        Point3d textRegionCenterPoint;
        var vectorToCenterPoint = Vector3d.XAxis * (((ShelfLedge + TextIndent) * scale) + (topTextWidth / 2));

        if (ShelfPosition == ShelfPosition.Right)
        {
            textRegionCenterPoint = ShelfStartPointOCS + vectorToCenterPoint;
        }
        else
        {
            textRegionCenterPoint = ShelfStartPointOCS - vectorToCenterPoint;
        }

        _topText.Location = textRegionCenterPoint + (Vector3d.YAxis * ((TextVerticalOffset * scale) + (topTextHeight / 2)));
    }

    #endregion

    private void CreateArrow(Point3d point3d, Vector3d mainNormal, double arrowSize, double scale)
    {
        new ArrowBuilder(mainNormal, arrowSize, scale).BuildArrow(ArrowType, point3d, _hatches, _leaderEndLines);
    }
}