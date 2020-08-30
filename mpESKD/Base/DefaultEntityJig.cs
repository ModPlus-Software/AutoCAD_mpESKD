namespace mpESKD.Base
{
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Enums;
    using ModPlusAPI;
    using Utils;

    /// <summary>
    /// Реализация стандартной EntityJig
    /// </summary>
    public class DefaultEntityJig : EntityJig
    {
        /// <summary>
        /// Обрабатываемый интеллектуальный примитив
        /// </summary>
        private readonly SmartEntity _smartEntity;

        private readonly JigUtils.PointSampler _insertionPoint = new JigUtils.PointSampler(Point3d.Origin);

        private readonly JigUtils.PointSampler _nextPoint;

        /// <summary>
        /// Экземпляр <see cref="DefaultEntityJig"/>
        /// </summary>
        /// <param name="smartEntity">Экземпляр обрабатываемого интеллектуального примитива</param>
        /// <param name="blockReference">Вставка блока, представляющая обрабатываемый интеллектуальный примитив</param>
        /// <param name="startValueForNextPoint">Начальное значение для второй точки. Для примитивов, использующих
        /// конечную точку (EndPoint) влияет на отрисовку при указании первой точки</param>
        public DefaultEntityJig(
            SmartEntity smartEntity,
            BlockReference blockReference,
            Point3d startValueForNextPoint)
            : base(blockReference)
        {
            _smartEntity = smartEntity;
            _nextPoint = new JigUtils.PointSampler(startValueForNextPoint);
        }

        /// <summary>
        /// Статус
        /// </summary>
        public JigState JigState { get; set; } = JigState.PromptInsertPoint;

        /// <summary>
        /// Если значение не null, то PreviousPoint устанавливается как базовая точка
        /// при запросе второй точки. При этом JigState должен быть PromptNextPoint
        /// </summary>
        public Point3d? PreviousPoint { get; set; }

        /// <summary>
        /// Сообщение для указания первой точки (точки вставки).
        /// Сообщение по умолчанию "Укажите точку вставки:"
        /// </summary>
        public string PromptForInsertionPoint { get; set; } = Language.GetItem(Invariables.LangItem, "msg1");

        /// <summary>
        /// Сообщение для указания следующей точки.
        /// Сообщение по умолчанию "Укажите конечную точку:"
        /// </summary>
        public string PromptForNextPoint { get; set; } = Language.GetItem(Invariables.LangItem, "msg2");

        /// <inheritdoc/>
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            try
            {
                switch (JigState)
                {
                    case JigState.PromptInsertPoint:
                        return _insertionPoint.Acquire(prompts, $"\n{PromptForInsertionPoint}", value =>
                            {
                                _smartEntity.InsertionPoint = value;
                            });
                    case JigState.PromptNextPoint:
                        {
                            var basePoint = _insertionPoint.Value;
                            if (PreviousPoint != null)
                            {
                                basePoint = PreviousPoint.Value;
                            }

                            return _nextPoint.Acquire(prompts, $"\n{PromptForNextPoint}", basePoint, value =>
                            {
                                _smartEntity.EndPoint = value;
                            });
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

        /// <inheritdoc/>
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
}
