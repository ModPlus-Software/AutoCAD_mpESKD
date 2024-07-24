using Autodesk.AutoCAD.Colors;

namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using mpESKD.Functions.mpCrestedLeader.Grips;
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


public delegate void ShelfPosChangeDelegate(ObjectId entityId, GripData.Status newStatus);


/// <summary>
/// Имя
/// </summary>
[SmartEntityDisplayNameKey("h207")]
[SystemStyleDescriptionKey("h208")]
public class CrestedLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    public event ShelfPosChangeDelegate ShelfPosChangeEvent;

    #region Примитивы

    private readonly List<Line> _leaders = new ();
    private readonly List<Line> _leaderArrows = new ();

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

    private Line _tempLeader = new ();
    //private Xline _firstLeader = new();

    #endregion

    private readonly List<Point3d> _leaderPoints = new ();

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
    public List<Point3d> LeaderEndPoints { get; set; } = new ();
    
    public List<Point3d> LeaderEndPointsOCS  => LeaderEndPoints.Select(x => x.TransformBy(BlockTransform.Inverse())).ToList();

    [SaveToXData]
    public Point3d ShelfStartPoint { get; set; }

    public Point3d ShelfStartPointOCS => ShelfStartPoint.TransformBy(BlockTransform.Inverse());

    [SaveToXData]
    public Point3d ShelfLedgePoint { get; set; }
    
    public Point3d ShelfLedgePointOCS => ShelfLedgePoint.TransformBy(BlockTransform.Inverse());

    [SaveToXData]
    public Point3d ShelfEndPoint { get; set; }
    
    public Point3d ShelfEndPointOCS => ShelfEndPoint.TransformBy(BlockTransform.Inverse());

    public Point3d TempNewPoint { get; set; }
    public Point3d TempNewPointOCS => TempNewPoint.TransformBy(BlockTransform.Inverse());

    [SaveToXData]
    public bool IsFirst { get; set; } = false;

    [SaveToXData]
    public bool IsBasePointMovedByGrip { get; set; } = false;

    [SaveToXData]
    public bool IsBasePointMovedByOverrule { get; set; } = false;

    [SaveToXData]
    public ShelfPosition PrevShelfPosition { get; set; }

    [SaveToXData]
    /// <summary>
    /// Для установки новой точки вставки
    /// Точки в списке отсортированы по возрастанию X
    /// </summary>
    public List<Point3d> LeaderStartPoints { get; set; } = new ();

    public List<Point3d> LeaderStartPointsOCS => LeaderStartPoints.Select(x => x.TransformBy(BlockTransform.Inverse())).ToList();

    //public CrestedLeaderGrip CrestedLeaderGrip { get; set; } = null;
    //public ObjectId ObjectIdForGrip { get; set; }
    //public GripData.Status GripDataStatus { get; set; }

    [SaveToXData]
    public bool IsChangeShelfPosition { get; set; } = false;

    /*
    [SaveToXData]
    public Point3d BoundStartPoint { get; set; }
    public Point3d BoundStartPointOCS => BoundStartPoint.TransformBy(BlockTransform.Inverse());
    */

    [SaveToXData]
    public Point3d BoundEndPoint { get; set; }
    public Point3d BoundEndPointOCS => BoundEndPoint.TransformBy(BlockTransform.Inverse());

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
    //[EntityProperty(PropertiesCategory.Geometry, 4, "p78", ShelfPosition.Right)]
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

            //if (_firstLeader != null)
            //    entities.Add(_firstLeader);

            if (_leaders != null)
                entities.AddRange(_leaders);

            //if (_leaderArrows != null)
            //    entities.AddRange(_leaderArrows);

            if (_unionLine != null)
                entities.Add(_unionLine);

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

            if (_bottomText != null)
                entities.Add(_bottomText);

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
        foreach (var leaderEndPoint in LeaderEndPoints)
        {
            yield return leaderEndPoint;
        }

        //foreach (var leaderStartPoint in LeaderStartPoints)
        //{
        //    yield return leaderStartPoint;
        //}

        //yield return InsertionPoint;
        //yield return EndPoint;
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        try
        {
            Loggerq.WriteRecord($"CrestedLeader: UpdateEntities() => IsFirst: {IsFirst.ToString()}");

            /*
            Loggerq.WriteRecord("CrestedLeader: UpdateEntities() => *\n**");
            Loggerq.WriteRecord("CrestedLeader: UpdateEntities() => START");
            Loggerq.WriteRecord($"CrestedLeader: UpdateEntities() =>        CurrentJigState: {CurrentJigState.ToString()}");
            Loggerq.WriteRecord($"CrestedLeader: UpdateEntities() =>        ShelfLedge: {ShelfLedge.ToString()}");
            Loggerq.WriteRecord($"CrestedLeader: UpdateEntities() =>        ShelfPosition: {ShelfPosition.ToString()}");
            */
            var scale = GetScale();

            if (CurrentJigState == (int)CrestedLeaderJigState.PromptInsertPoint)
            {
                CreateTempCurrentLeader(InsertionPoint);
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptNextLeaderPoint) // 2
            {
                CreateTempCurrentLeader(EndPoint);
                CreateTempLeaders();
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptShelfStartPoint) // 3
            {
                // В режиме указания точки полки рисуется набор выносок до точки пересечения с курсором
                CreateTempLeaderLineMoved(); // получен LeaderStartPoints, в jig запомнили
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.PromptShelfIndentPoint) // 4
            {
                // В режиме указания точки отступа полки рисуется набор выносок до точки пересечения с началом полки
                // уже нарисованы!
                // и линия от начала полки до курсора
                CreateTempShelfLine(); // создаются линия между нач.выносок, линия к полке, заданы ShelfStartPoint, ShelfLedgePoint, ShelfLedge
                CreateTextSimple(scale); //  создается текст
                CreateTempShelf(); // создается полка выноски, задается ShelfEndPoint, 
            }
            else if (CurrentJigState == (int)CrestedLeaderJigState.None)
            {
                if (!PrevShelfPosition.Equals(ShelfPosition))
                {
                   // Loggerq.WriteRecord("CrestedLeader: UpdateEntities() =>      (!PrevShelfPosition.Equals(ShelfPosition))");

                    var leaderStartPointsSort = LeaderStartPoints.OrderBy(p => p.X);

                    var vectorToShelfLedgePoint = Vector3d.XAxis * ShelfLedge;

                    var widthText = CreateText(scale);
                    var vectorToShelfEndPoint = Vector3d.XAxis * (widthText + (TextIndent * scale));

                    if (ShelfPosition == ShelfPosition.Right)
                    {
                       // Loggerq.WriteRecord("CrestedLeader: UpdateEntities() =>         ShelfPosition == ShelfPosition.Right");

                        ShelfStartPoint = leaderStartPointsSort.Last();

                        ShelfLedgePoint = ShelfStartPoint + vectorToShelfLedgePoint;
                        ShelfEndPoint = ShelfLedgePoint + vectorToShelfEndPoint;
                    }
                    else
                    {
                       // Loggerq.WriteRecord("CrestedLeader: UpdateEntities() =>         ShelfPosition == ShelfPosition.Left");

                        ShelfStartPoint = leaderStartPointsSort.First();

                        ShelfLedgePoint = ShelfStartPoint - vectorToShelfLedgePoint;
                        ShelfEndPoint = ShelfLedgePoint - vectorToShelfEndPoint;
                    }

                    PrevShelfPosition = ShelfPosition;

                    _unionLine = null;
                    _shelfLine = null;
                    _shelf = null;
                    _tempLeader = null;
                    _leaders.Clear();



                    for (var i = 0; i < LeaderEndPoints.Count; i++)
                    {
                        _leaders.Add(new Line(LeaderStartPointsOCS[i], LeaderEndPointsOCS[i]));
                    }

                    var leaderBound = _leaders.First(l => l.StartPoint.Equals(InsertionPoint));
                   // BoundStartPoint = leaderBound.StartPoint;
                    BoundEndPoint = leaderBound.EndPoint;

                    var leaderStartPointsOcsSort = LeaderStartPointsOCS.OrderBy(p => p.X).ToList();
                    _unionLine = new Line(leaderStartPointsOcsSort.First(), leaderStartPointsOcsSort.Last());

                    _shelfLine = new Line(ShelfStartPointOCS, ShelfLedgePointOCS);
                    _shelf = new Line(ShelfLedgePointOCS, ShelfEndPointOCS);

                    var textRegionCenterPoint = GeometryUtils.GetMiddlePoint3d(ShelfLedgePointOCS, ShelfEndPointOCS);

                    if (_topText != null)
                    {
                        var yVectorToCenterTopText = Vector3d.YAxis * ((TextVerticalOffset * scale) + (_topText.ActualHeight / 2));
                        _topText.Location = textRegionCenterPoint + yVectorToCenterTopText;
                    }

                    if (_bottomText != null)
                    {
                        var yVectorToCenterBottomText = Vector3d.YAxis * ((TextVerticalOffset * scale) + (_bottomText.ActualHeight / 2));
                        _bottomText.Location = textRegionCenterPoint - yVectorToCenterBottomText;
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

                    /*
                    if (this.CrestedLeaderGrip != null)
                    {
                        Loggerq.WriteRecord("CrestedLeader: UpdateEntities() => CrestedLeaderGrip != null");
                        this.CrestedLeaderGrip.OnGripStatusChanged(this.ObjectIdForGrip, this.GripDataStatus);
                    }
                    else
                    {
                        Loggerq.WriteRecord("CrestedLeader: UpdateEntities() => CrestedLeaderGrip = null");
                    }
                    */

                    IsChangeShelfPosition = true;

                    //InsertionPoint = ShelfPosition == ShelfPosition.Right
                    //    ? leaderStartPointsSort.Last()
                    //    : leaderStartPointsSort.First();

                    //ShelfPosChangeEvent?.Invoke(this.BlockId, GripData.Status.GripEnd);

                    // IsFirst = true;
                    return;
                }

                if (IsFirst)
                {
                   // Loggerq.WriteRecord("CrestedLeader: UpdateEntities() =>         IsFirst = true");

                    _unionLine = null;
                    _shelfLine = null;
                    _shelf = null;
                    _tempLeader = null;
                    _leaders.Clear();

                    for (var i = 0; i < LeaderEndPoints.Count; i++)
                    {
                        _leaders.Add(new Line(LeaderStartPointsOCS[i], LeaderEndPointsOCS[i]));
                    }

                    //var leadersSort = _leaders.OrderBy(l => l.StartPoint.X);
                    //var leaderBound = ShelfPosition == ShelfPosition.Right ? 

                    //var leaderBound = _leaders.First(l => l.StartPoint.Equals(InsertionPoint));
                    //BoundStartPoint = leaderBound.StartPoint;
                    //BoundEndPoint = leaderBound.EndPoint;

                    var leaderStartPointsOcsSort = LeaderStartPointsOCS.OrderBy(p => p.X).ToList();
                    _unionLine = new Line(leaderStartPointsOcsSort.First(), leaderStartPointsOcsSort.Last());

                    _shelfLine = new Line(ShelfStartPointOCS, ShelfLedgePointOCS);
                    _shelf = new Line(ShelfLedgePointOCS, ShelfEndPointOCS);

                    _ = CreateText(scale);
                    var textRegionCenterPoint = GeometryUtils.GetMiddlePoint3d(ShelfLedgePointOCS, ShelfEndPointOCS);

                    if (_topText != null)
                    {
                        var yVectorToCenterTopText =
                            Vector3d.YAxis * ((TextVerticalOffset * scale) + (_topText.ActualHeight / 2));
                        _topText.Location = textRegionCenterPoint + yVectorToCenterTopText;
                    }

                    _bottomText = null;
                    var leaderBound = _leaders.First(l => l.StartPoint.Equals(InsertionPoint));
                    //BoundStartPoint = leaderBound.StartPoint;
                    BoundEndPoint = leaderBound.EndPoint;

                    Loggerq.WriteRecord("CrestedLeader: UpdateEntities() =>         IsFirst = true");
                    Loggerq.WriteRecord($"CrestedLeader: UpdateEntities() =>         IsFirst = true: InsertionPoint:{InsertionPoint.ToString()}");
                    Loggerq.WriteRecord($"CrestedLeader: UpdateEntities() =>         IsFirst = true: InsertionPointOCS:{InsertionPointOCS.ToString()}");
                    Loggerq.WriteRecord($"CrestedLeader: UpdateEntities() =>         IsFirst = true: leaderBound.StartPoint:{leaderBound.StartPoint.ToString()}");
                    Loggerq.WriteRecord($"CrestedLeader: UpdateEntities() =>         IsFirst = true: leaderBound.EndPoint:{leaderBound.EndPoint.ToString()}");

                    IsFirst = false;
                }
                else
                {
                   // Loggerq.WriteRecord("CrestedLeader: UpdateEntities() =>         IsFirst = false, Run CreateEntities()");
                    CreateEntities(scale);
                }
            }

           // Loggerq.WriteRecord("CrestedLeader: UpdateEntities() => END");
        }
        catch (Exception exception)
        {
            // todo
            // ExceptionBox.Show(exception);

            Loggerq.WriteRecord("CrestedLeader: UpdateEntities() =>  ERROR");
        }
    }

    private void CreateEntities(double scale)
    {
        
        Loggerq.WriteRecord("CrestedLeader: CreateEntities() => START");
        Loggerq.WriteRecord($"  CrestedLeader: CreateEntities() =>      IsBasePointMovedByGrip: {IsBasePointMovedByGrip.ToString()}");
        Loggerq.WriteRecord($"  CrestedLeader: CreateEntities() =>      IsBasePointMovedByOverrule: {IsBasePointMovedByOverrule.ToString()}");
        Loggerq.WriteRecord($"  CrestedLeader: CreateEntities() =>      InsertionPoint: {InsertionPoint.ToString()}");

        _tempLeader = null;
        _unionLine = null;
        _shelfLine = null;
        _shelf = null;

        ShelfStartPoint = InsertionPoint;


        // Перетаскивание
        if (!IsBasePointMovedByGrip && IsBasePointMovedByOverrule)
        {

           if (!CreateLeaderLines(true))
            {
                return;
            }

            var leadersStartPointsSort = LeaderStartPoints.OrderBy(p => p.X).ToList();

            ShelfStartPoint = ShelfPosition == ShelfPosition.Right
                ? leadersStartPointsSort.Last()
                : leadersStartPointsSort.First();

            IsBasePointMovedByOverrule = false;
        }
        // Перетаскивание выполнено
        else if (IsBasePointMovedByGrip && !IsBasePointMovedByOverrule)
        {
            Loggerq.WriteRecord("CrestedLeader: CreateEntities() =>          Move done: start");
            _leaders.Clear();

            for (var i = 0; i < LeaderEndPoints.Count; i++)
            {
                _leaders.Add(new Line(LeaderStartPointsOCS[i], LeaderEndPointsOCS[i]));
            }

            //Loggerq.WriteRecord("CrestedLeader: CreateEntities() =>          Move done: 1");

            Loggerq.WriteRecord($"CrestedLeader: CreateEntities() =>          Move done: _leaders.Count:{_leaders.Count}");
            if (_leaders.Count > 0)
            {
                for (var index = 0; index < _leaders.Count; index++)
                {
                    var leader = _leaders[index];
                    Loggerq.WriteRecord($"CrestedLeader: CreateEntities() =>          " +
                                        $"Move done: ({index}) {leader.StartPoint.ToString()},{leader.EndPoint.ToString()}");
                }
            }
            
            Loggerq.WriteRecord($"CrestedLeader: CreateEntities() =>          Move done: InsertionPointOCS:{InsertionPointOCS}");
            Loggerq.WriteRecord($"CrestedLeader: CreateEntities() =>          Move done: InsertionPoint:{InsertionPoint}");

            var leaderBound = _leaders.First(l => l.StartPoint.Equals(InsertionPointOCS));
            
            BoundEndPoint =InsertionPoint + (leaderBound.EndPoint - InsertionPointOCS);

            Loggerq.WriteRecord($"CrestedLeader: CreateEntities() =>          Move done: BoundEndPoint:{BoundEndPoint}");

            IsBasePointMovedByGrip = false;

            Loggerq.WriteRecord("CrestedLeader: CreateEntities() =>          Move done: end");
        }
        // Не перетаскивание
        else if (!IsBasePointMovedByGrip && !IsBasePointMovedByOverrule)
        {
            if (!CreateLeaderLines())
            {
                return; 
            }
        }

        if (_leaders.Count < 1)
            return;

        var leaderStartPointsOcsSort = LeaderStartPointsOCS.OrderBy(p => p.X);
        _unionLine = new Line(leaderStartPointsOcsSort.First(), leaderStartPointsOcsSort.Last());

        // Ширина самого широкого текста (верхнего или нижнего)
        var textWidth = CreateText(scale);

        var vectorToShelfLedge = Vector3d.XAxis * ShelfLedge;

        ShelfLedgePoint = ShelfPosition == ShelfPosition.Right
            ? ShelfStartPoint + vectorToShelfLedge
            : ShelfStartPoint - vectorToShelfLedge;

        _shelfLine = new Line(ShelfStartPointOCS, ShelfLedgePointOCS);

        var vectorToShelfEndpoint = Vector3d.XAxis * ((TextIndent * scale) + textWidth);

        ShelfEndPoint = ShelfPosition == ShelfPosition.Right
            ? ShelfLedgePoint + vectorToShelfEndpoint
            : ShelfLedgePoint - vectorToShelfEndpoint;

        _shelf = new Line(ShelfLedgePointOCS, ShelfEndPointOCS);

        var textRegionCenterPoint = GeometryUtils.GetMiddlePoint3d(ShelfLedgePointOCS, ShelfEndPointOCS);

        if (_topText != null)
        {
            var yVectorToCenterTopText = Vector3d.YAxis * ((TextVerticalOffset * scale) + (_topText.ActualHeight / 2));
            _topText.Location = textRegionCenterPoint + yVectorToCenterTopText;
        }

        if (_bottomText != null)
        {
            var yVectorToCenterBottomText = Vector3d.YAxis * ((TextVerticalOffset * scale) + (_bottomText.ActualHeight / 2));
            _bottomText.Location = textRegionCenterPoint - yVectorToCenterBottomText;
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

       // Loggerq.WriteRecord("CrestedLeader: CreateEntities() => END");
    }

    private bool CreateLeaderLines(bool IsOverruleMoving = false)
    {
        Loggerq.WriteRecord("CrestedLeader: CreateLeaderLines() => START");
        //Loggerq.WriteRecord($"CrestedLeader: CreateLeaderLines() =>      IsOverruleMoving: {IsOverruleMoving}");
        Loggerq.WriteRecord($"CrestedLeader: CreateLeaderLines() =>      InsertionPoint: {InsertionPoint.ToString()}");
        Loggerq.WriteRecord($"CrestedLeader: CreateLeaderLines() =>      InsertionPointOCS: {InsertionPointOCS.ToString()}");
        Loggerq.WriteRecord($"CrestedLeader: CreateLeaderLines() =>      BoundEndPoint: {BoundEndPoint.ToString()}");
        Loggerq.WriteRecord($"CrestedLeader: CreateLeaderLines() =>      BoundEndPointOCS: {BoundEndPointOCS.ToString()}");

        /*
        // отсортировать _leaders по StartPoint.Х  
        var leadersSort = _leaders.OrderBy(l => l.StartPoint.X);
        Loggerq.WriteRecord($"CrestedLeader: CreateLeaderLines() =>      leadersSort!");

        // Получить крайнюю линию
        var boundLine = ShelfPosition == ShelfPosition.Right ? leadersSort.Last() : leadersSort.First();
        Loggerq.WriteRecord($"CrestedLeader: CreateLeaderLines() =>      boundLine!");
        */


        _leaders.Clear();
        LeaderStartPoints.Clear();

        if (LeaderEndPoints.Any(x => x.Equals(InsertionPoint)) || 
            BoundEndPoint.Equals(InsertionPoint))
            return false;

        /*
        var leaderEndPointsOcsSort = LeaderEndPointsOCS.OrderBy(p => p.X).ToList();

        var leaderEndPointNearestToInsPt = ShelfPosition == ShelfPosition.Right
            ? leaderEndPointsOcsSort.Last().ToPoint2d()
            : leaderEndPointsOcsSort.First().ToPoint2d();

        var newLeaderVector = IsOverruleMoving
        ? TempNewPointOCS.ToPoint2d() - leaderEndPointNearestToInsPt
        : InsertionPointOCS.ToPoint2d() - leaderEndPointNearestToInsPt;
        */

        //var newLeaderVector = InsertionPointOCS.ToPoint2d() - boundLine.EndPoint.ToPoint2d();
        // var newLeaderVector = InsertionPointOCS.ToPoint2d() - BoundEndPoint.ToPoint2d();


        /*
        if (IsOverruleMoving)
        {
            if (GetBoundEndPoint(InsertionPoint, LeaderEndPoints, ShelfPosition) is { } boundEndPoint)
            {
                BoundEndPoint = boundEndPoint;

            }
            else
            {
                return false;
            }

        }*/

        if (BoundEndPointOCS.X.Equals(InsertionPointOCS.X))
        {
            for (int i = 0; i < LeaderEndPointsOCS.Count; i++)
            {
                var intersectPointOcs = new Point2d(LeaderEndPointsOCS[i].X, InsertionPointOCS.Y);
                var intersectPointOcs3d = intersectPointOcs.ToPoint3d();

                _leaders.Add(new Line(intersectPointOcs3d, LeaderEndPointsOCS[i]));

                LeaderStartPoints.Add(InsertionPoint + (intersectPointOcs3d - InsertionPointOCS));
            }
        }
        else
        {
            var newLeaderVector = InsertionPointOCS.ToPoint2d() - BoundEndPointOCS.ToPoint2d();

            for (int i = 0; i < LeaderEndPointsOCS.Count; i++)
            {
                var intersectPointOcs = GetIntersectBetweenVectors(
                    InsertionPointOCS.ToPoint2d(),
                    Vector2d.XAxis,
                    LeaderEndPointsOCS[i].ToPoint2d(),
                    newLeaderVector);


                if (intersectPointOcs == null)
                    continue;

                var intersectPointOcs3d = intersectPointOcs.Value.ToPoint3d();

                _leaders.Add(new Line(intersectPointOcs3d, LeaderEndPointsOCS[i]));

                LeaderStartPoints.Add(InsertionPoint + (intersectPointOcs3d - InsertionPointOCS));
            }
        }

        /*
               _firstLeader = new Line(InsertionPointOCS, leaderEndPointNearestToInsPt.ToPoint3d())
               {
                   ColorIndex = 150,
                   Thickness = 20,
                   LineWeight = (LineWeight)20,
               };*/

        //_firstLeader = new Xline()
        //{
        //    BasePoint =     EndPointOCS,
        //    SecondPoint = leaderEndPointNearestToInsPt.ToPoint3d()
        //};

        //var leaderBound = _leaders.First(l => l.StartPoint.Equals(InsertionPoint));
        //BoundStartPoint = leaderBound.StartPoint;
        //BoundEndPoint = leaderBound.EndPoint;

        return true;
    }

    private double CreateText(double scale)
    {
        _topText = new MText
        {
            Contents = TopText,
            Attachment = AttachmentPoint.MiddleCenter,
        };

        _topText.SetProperties(TextStyle, TopTextHeight * scale);
        var topTextWidth = _topText.ActualWidth;

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
    private void CreateTempCurrentLeader(Point3d leaderEndPoint)
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

                if (intersectPoint == null)
                    continue;

                _leaders.Add(new Line(intersectPoint.Value.ToPoint3d(), LeaderEndPoints[i]));
            }

            var leaderStartPoints = _leaders?.Select(l => l.StartPoint).ToList();

            if (leaderStartPoints != null && leaderStartPoints.Count > 0)
            {
                var leaderStartPointsSort = leaderStartPoints.OrderBy(p => p.X).ToList();
                _shelfLine = new Line(leaderStartPointsSort.First(), leaderStartPointsSort.Last());
                
                LeaderStartPoints = leaderStartPoints;

               
            }
        }
    }

    private void CreateTempShelfLine()
    
    {
        _unionLine = null;
        _shelfLine = null;

        if (LeaderStartPoints == null)
            return;

        if (LeaderStartPoints.Count > 1)
        {
            var leaderStartPointsOcsSort = LeaderStartPointsOCS.OrderBy(p => p.X);
            _unionLine = new Line(leaderStartPointsOcsSort.First(), leaderStartPointsOcsSort.Last());

            var leaderStartPointsSort = LeaderStartPoints.OrderBy(p => p.X);

            var leftStartPoint = leaderStartPointsSort.First();
            var rightStartPoint = leaderStartPointsSort.Last();

            // линия между началами выносок

            if (EndPoint.X > rightStartPoint.X)
            {
                ShelfStartPoint = rightStartPoint;
                ShelfLedgePoint = new Point3d(EndPoint.X, ShelfStartPoint.Y, ShelfStartPoint.Z);

                // Рисуем линию к полке вправо
                ShelfPosition = ShelfPosition.Right;
            }
            else if (EndPoint.X < leftStartPoint.X)
            {
                ShelfStartPoint = leftStartPoint;
                ShelfLedgePoint = new Point3d(EndPoint.X, ShelfStartPoint.Y, ShelfStartPoint.Z);
                // Влево
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

                if (EndPoint.X > ShelfStartPoint.X)
                {
                    // Рисуем линию к полке вправо
                    ShelfPosition = ShelfPosition.Right;
                }
                else
                {
                    // Влево
                    ShelfPosition = ShelfPosition.Left;
                }
            }

        }

        PrevShelfPosition = ShelfPosition;

        if (!ShelfStartPoint.Equals(ShelfLedgePoint))
        {
            _shelfLine = new Line(ShelfStartPointOCS, ShelfLedgePointOCS);
            ShelfLedge = (ShelfLedgePoint - ShelfStartPoint).Length;
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
        _topText = new MText
        {
            Contents = TopText,
            Attachment = AttachmentPoint.MiddleCenter,
        };
        _topText.SetProperties(TextStyle, TopTextHeight * scale);

        var topTextWidth = _topText.ActualWidth;
        var topTextHeight = _topText.ActualHeight;

        Point3d textRegionCenterPoint;
        var vectorToCenterPoint = (Vector3d.XAxis * (((ShelfLedge + TextIndent) * scale) + (topTextWidth / 2)));

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

    /// <summary>
    /// Возвращает точку пересечения 2х 2D векторов
    /// </summary>
    private Point2d? GetIntersectBetweenVectors(Point2d point1, Vector2d vector1, Point2d point2, Vector2d vector2)
    {
        if (point1.Equals(point2))
            return null;

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

    private Point3d? GetBoundEndPoint(Point3d insPoint, List<Point3d> leaderEndPoints, ShelfPosition shelfPosition)
    {
         // для каждой leaderEndPoint получим список точек пересечения с осью Х
         //var intersectByLeaderEndPoint = leaderEndPoints.Select(p =>  GetIntersectBetweenVectors(
         //    insPoint.ToPoint2d(),
         //    Vector2d.XAxis, 
         //    p.ToPoint2d(),
         //    InsertionPoint.ToPoint2d() - 
         //);

        if (leaderEndPoints.Any(p => p.Equals(insPoint)))
        {
            return null;
        }

        // Если есть выноски сверху и и снизу полки - разделим их
        var leaderEndPointsUp = new List<Point3d>();
        var leaderEndPointsBottom = new List<Point3d>();

        foreach (var leaderEndPoint in leaderEndPoints)
        {
            if (leaderEndPoint.Y > insPoint.Y)
            {
                leaderEndPointsUp.Add(leaderEndPoint);
            }
            else
            {
                leaderEndPointsBottom.Add(leaderEndPoint);
            }
        }

        var lUp = leaderEndPoints.Where(p => p.Y > insPoint.Y).ToList();
        var lDown = leaderEndPoints.Where(p => p.Y < insPoint.Y).ToList();




        for (int i = 0; i < leaderEndPoints.Count; i++)
         {
             var boundEndPointX = leaderEndPoints[i].ToPoint2d();
             
            var vector = insPoint.ToPoint2d() - boundEndPointX;

            var insects = leaderEndPoints.Select(p =>
                GetIntersectBetweenVectors(
                    insPoint.ToPoint2d(),
                        Vector2d.XAxis,
                        p.ToPoint2d(),
                    vector)
                ).ToList();

            if ((insects.All(p => p.Value.X <= insPoint.X) && shelfPosition == ShelfPosition.Right) ||
                (insects.All(p => p.Value.X >= insPoint.X) && shelfPosition == ShelfPosition.Left))
            {
                return boundEndPointX.ToPoint3d();
            }
         }

         return null;
    }

}