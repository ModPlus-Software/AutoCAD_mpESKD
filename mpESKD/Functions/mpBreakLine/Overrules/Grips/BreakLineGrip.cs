namespace mpESKD.Functions.mpBreakLine.Overrules.Grips
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Описание ручки линии обрыва
    /// </summary>
    public class BreakLineGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BreakLineGrip"/> class.
        /// </summary>
        /// <param name="breakLine">Экземпляр класса <see cref="mpBreakLine.BreakLine"/>, связанный с этой ручкой</param>
        /// <param name="gripName">Имя ручки</param>
        public BreakLineGrip(BreakLine breakLine, BreakLineGripName gripName)
        {
            BreakLine = breakLine;
            GripName = gripName;
            GripType = GripType.Point;
        }

        /// <summary>
        /// Экземпляр класса <see cref="mpBreakLine.BreakLine"/>, связанный с этой ручкой
        /// </summary>
        public BreakLine BreakLine { get; }

        /// <summary>
        /// Имя ручки
        /// </summary>
        public BreakLineGripName GripName { get; }

        /// <inheritdoc />
        public override string GetTooltip()
        {
            switch (GripName)
            {
                case BreakLineGripName.StartGrip:
                case BreakLineGripName.EndGrip:
                {
                    return Language.GetItem(Invariables.LangItem, "gp1"); // stretch
                }

                case BreakLineGripName.MiddleGrip: return Language.GetItem(Invariables.LangItem, "gp2"); // move
            }

            return base.GetTooltip();
        }

        // Временное значение первой ручки
        private Point3d _startGripTmp;

        // временное значение последней ручки
        private Point3d _endGripTmp;

        /// <inheritdoc />
        public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
        {
            try
            {
                // При начале перемещения запоминаем первоначальное положение ручки
                // Запоминаем начальные значения
                if (newStatus == Status.GripStart)
                {
                    if (GripName == BreakLineGripName.StartGrip)
                    {
                        _startGripTmp = GripPoint;
                    }

                    if (GripName == BreakLineGripName.EndGrip)
                    {
                        _endGripTmp = GripPoint;
                    }

                    if (GripName == BreakLineGripName.MiddleGrip)
                    {
                        _startGripTmp = BreakLine.InsertionPoint;
                        _endGripTmp = BreakLine.EndPoint;
                    }
                }

                // При удачном перемещении ручки записываем новые значения в расширенные данные
                // По этим данным я потом получаю экземпляр класса BreakLine
                if (newStatus == Status.GripEnd)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef = tr.GetObject(BreakLine.BlockId, OpenMode.ForWrite, true, true);
                        using (var resBuf = BreakLine.GetDataForXData())
                        {
                            blkRef.XData = resBuf;
                        }

                        tr.Commit();
                    }

                    BreakLine.Dispose();
                }

                // При отмене перемещения возвращаем временные значения
                if (newStatus == Status.GripAbort)
                {
                    if (_startGripTmp != null & GripName == BreakLineGripName.StartGrip)
                    {
                        BreakLine.InsertionPoint = GripPoint;
                    }

                    if (GripName == BreakLineGripName.MiddleGrip & _startGripTmp != null & _endGripTmp != null)
                    {
                        BreakLine.InsertionPoint = _startGripTmp;
                        BreakLine.EndPoint = _endGripTmp;
                    }

                    if (_endGripTmp != null & GripName == BreakLineGripName.EndGrip)
                    {
                        BreakLine.EndPoint = GripPoint;
                    }
                }

                base.OnGripStatusChanged(entityId, newStatus);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
    }
}