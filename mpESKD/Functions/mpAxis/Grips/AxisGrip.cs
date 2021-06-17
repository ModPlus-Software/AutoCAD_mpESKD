namespace mpESKD.Functions.mpAxis.Grips
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Описание ручки оси
    /// </summary>
    public class AxisGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AxisGrip"/> class.
        /// </summary>
        /// <param name="axis">Экземпляр класса <see cref="mpAxis.Axis"/>, связанный с этой ручкой</param>
        public AxisGrip(Axis axis)
        {
            Axis = axis;
            GripType = GripType.Point;
        }

        /// <summary>
        /// Экземпляр класса <see cref="mpAxis.Axis"/>, связанный с этой ручкой
        /// </summary>
        public Axis Axis { get; }

        /// <summary>
        /// Имя ручки
        /// </summary>
        public GripName GripName { get; set; }

        /// <inheritdoc />
        public override string GetTooltip()
        {
            switch (GripName)
            {
                case GripName.StartGrip:
                case GripName.EndGrip:
                case GripName.BottomMarkerGrip:
                case GripName.TopMarkerGrip:
                case GripName.BottomOrientGrip:
                case GripName.TopOrientGrip:
                {
                    return Language.GetItem("gp1"); // stretch
                }

                case GripName.MiddleGrip: return Language.GetItem("gp2"); // move
            }

            return base.GetTooltip();
        }

        // Временное значение первой ручки
        private Point3d _startGripTmp;

        // временное значение последней ручки
        private Point3d _endGripTmp;

        // other points
        private Point3d _bottomMarkerGripTmp;
        private Point3d _topMarkerGripTmp;
        private Point3d _bottomOrientGripTmp;
        private Point3d _topOrientGripTmp;

        /// <inheritdoc />
        public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
        {
            try
            {
                // Запоминаем начальные значения
                if (newStatus == Status.GripStart)
                {
                    _startGripTmp = Axis.InsertionPoint;
                    _endGripTmp = Axis.EndPoint;
                    _bottomMarkerGripTmp = Axis.BottomMarkerPoint;
                    _topMarkerGripTmp = Axis.TopMarkerPoint;
                    _bottomOrientGripTmp = Axis.BottomOrientPoint;
                    _topOrientGripTmp = Axis.TopOrientPoint;
                }

                // При удачном перемещении ручки записываем новые значения в расширенные данные
                // По этим данным я потом получаю экземпляр класса axis
                if (newStatus == Status.GripEnd)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef = tr.GetObject(Axis.BlockId, OpenMode.ForWrite, true, true);
                        using (var resBuf = Axis.GetDataForXData())
                        {
                            blkRef.XData = resBuf;
                        }

                        tr.Commit();
                    }

                    Axis.Dispose();
                }

                // При отмене перемещения возвращаем временные значения
                if (newStatus == Status.GripAbort)
                {
                    Axis.InsertionPoint = _startGripTmp;
                    Axis.EndPoint = _endGripTmp;
                    Axis.BottomMarkerPoint = _bottomMarkerGripTmp;
                    Axis.TopMarkerPoint = _topMarkerGripTmp;
                    Axis.BottomOrientPoint = _bottomOrientGripTmp;
                    Axis.TopOrientPoint = _topOrientGripTmp;
                }

                base.OnGripStatusChanged(entityId, newStatus);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
    }
}