namespace mpESKD.Functions.mpCrestedLeader;

using System;
using mpESKD.Base;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Utils;
using ModPlusAPI;
using Autodesk.AutoCAD.ApplicationServices;

internal class CrestedLeaderEntityJig : EntityJig
{
    /// <summary>
    /// Обрабатываемый интеллектуальный примитив
    /// </summary>
    private readonly SmartEntity _smartEntity;

    private readonly Action<Point3d> _customPointAction;

    private readonly JigUtils.PointSampler _insertionPoint = new JigUtils.PointSampler(Point3d.Origin);

    private readonly JigUtils.PointSampler _nextPoint;

    private readonly JigUtils.PointSampler _customPoint;

    //private readonly JigUtils.PointSampler _donePoint;

    public CrestedLeaderEntityJig(
        SmartEntity smartEntity, 
        BlockReference blockReference, 
        Point3d startValueForNextPoint,
        Action<Point3d> customPointAction = null
        ) : base(blockReference)
    {
    }

    /// <summary>
    /// Статус
    /// </summary>
    public CrestedLeaderJigState JigState { get; set; } = CrestedLeaderJigState.PromptInsertPoint;

    /// <summary>
    /// Если значение не null, то PreviousPoint устанавливается как базовая точка
    /// при запросе второй точки. При этом JigState должен быть PromptNextLeaderPoint
    /// </summary>
    public Point3d? PreviousPoint { get; set; }

    /// <summary>
    /// Сообщение для указания первой точки (точки вставки).
    /// Сообщение по умолчанию "Укажите точку вставки:"
    /// </summary>
    public string PromptForInsertionPoint { get; set; } = Language.GetItem("msg1");

    /// <summary>
    /// Сообщение для указания следующей точки (конца выноски)
    /// Сообщение по умолчанию "Укажите конечную точку:"
    /// </summary>
    public string PromptForNextPoint { get; set; } = Language.GetItem("msg2");

    /// <summary>
    /// Сообщение для указания пользовательской точки (полки выносок)
    /// Сообщение по умолчанию "Укажите конечную точку:"
    /// </summary>
    public string PromptForCustomPoint { get; set; } = Language.GetItem("msg2");

    /// <summary>
    /// Сообщение для указания  точки отступа полки
    /// Сообщение по умолчанию "Укажите конечную точку:"
    /// </summary>
    //public string PromptForDonePoint { get; set; } = Language.GetItem("msg2");

    protected override SamplerStatus Sampler(JigPrompts prompts)
    {
        try
        {
            switch (JigState)
            {
                case CrestedLeaderJigState.PromptInsertPoint:
                    return _insertionPoint.Acquire(prompts, $"\n{PromptForInsertionPoint}", value =>
                    {
                        _smartEntity.InsertionPoint = value;
                    });

                case CrestedLeaderJigState.PromptNextLeaderPoint:
                    {
                        var basePoint = _insertionPoint.Value;
                        if (PreviousPoint.HasValue)
                        {
                            basePoint = PreviousPoint.Value;
                        }

                        return _nextPoint.Acquire(prompts, $"\n{PromptForNextPoint}", basePoint, point3d =>
                        {
                            if (PreviousPoint.HasValue)
                            {
                                var minDistance = _smartEntity.MinDistanceBetweenPoints * _smartEntity.GetFullScale();
                                if (PreviousPoint.Value.DistanceTo(point3d) < minDistance)
                                {
                                    point3d = GeometryUtils.Point3dAtDirection(PreviousPoint.Value, point3d, minDistance);
                                }
                            }

                            _smartEntity.EndPoint = point3d;
                        });
                    }

                case CrestedLeaderJigState.PromptShelfStartPoint:
                    {
                        if (_customPointAction != null)
                        {
                            var basePoint = _insertionPoint.Value;
                            if (PreviousPoint != null)
                            {
                                basePoint = PreviousPoint.Value;
                            }

                            return _customPoint.Acquire(prompts, $"\n{PromptForCustomPoint}", basePoint, point3d =>
                            {
                                if (PreviousPoint.HasValue)
                                {
                                    var minDistance = _smartEntity.MinDistanceBetweenPoints * _smartEntity.GetFullScale();
                                    if (PreviousPoint.Value.DistanceTo(point3d) < minDistance)
                                    {
                                        point3d = GeometryUtils.Point3dAtDirection(PreviousPoint.Value, point3d, minDistance);
                                    }
                                }

                                _customPointAction.Invoke(point3d);
                            });
                        }

                        throw new ArgumentException("No CustomAction");
                    }

                default:
                    return SamplerStatus.NoChange;
            }
        }
        catch
        {
            return SamplerStatus.NoChange;
        }
    }

    protected override bool Update()
    {
        try
        {
            using (AcadUtils.Document.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, true))
            {
                using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
                {
                    var obj = (BlockReference)tr.GetObject(Entity.Id, OpenMode.ForWrite, true, true);
                    obj.Position = _smartEntity.InsertionPoint;
                    obj.BlockUnit = AcadUtils.Database.Insunits;
                    tr.Commit();
                }

                _smartEntity.UpdateEntities();
                _smartEntity.BlockRecord.UpdateAnonymousBlocks();
            }

            return true;
        }
        catch
        {
            // ignored
        }

        return false;
    }
}