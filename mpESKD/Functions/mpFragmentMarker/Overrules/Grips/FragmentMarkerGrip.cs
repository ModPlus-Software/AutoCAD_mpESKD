namespace mpESKD.Functions.mpFragmentMarker.Overrules.Grips
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
    public class FragmentMarkerGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FragmentMarkerGrip"/> class.
        /// </summary>
        /// <param name="fragmentMarker">Экземпляр класса <see cref="mpFragmentMarker.FragmentMarker"/>, связанный с этой ручкой</param>
        /// <param name="gripName">Имя ручки</param>
        public FragmentMarkerGrip(FragmentMarker fragmentMarker, GripName gripName)
        {
            FragmentMarker = fragmentMarker;
            GripName = gripName;
            GripType = GripType.Point;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodalLeaderGrip"/> class.
        /// </summary>
        /// <param name="fragmentMarker">Экземпляр <see cref="mpNodalLeader.NodalLeader"/></param>
        /// <param name="gripType">Вид ручки</param>
        /// <param name="gripName">Имя ручки</param>
        /// <param name="gripPoint">Точка ручки</param>
        //public FragmentMarkerGrip(            FragmentMarker fragmentMarker,
        //    GripType gripType,
        //    GripName gripName,
        //    Point3d gripPoint)
        //{
        //    FragmentMarker = fragmentMarker;
        //    GripName = gripName;
        //    GripType = gripType;
        //    GripPoint = gripPoint;
        //}

        /// <summary>
        /// Экземпляр класса <see cref="mpFragmentMarker.FragmentMarker"/>, связанный с этой ручкой
        /// </summary>
        public FragmentMarker FragmentMarker { get; }

        /// <summary>
        /// Имя ручки
        /// </summary>
        public GripName GripName { get; }

        /// <inheritdoc />
        public override string GetTooltip()
        {
            switch (GripName)
            {
                case GripName.StartGrip:
                case GripName.EndGrip:
                {
                    return Language.GetItem("gp1"); // stretch
                }

                case GripName.LeaderGrip: return Language.GetItem("gp2"); // move
            }

            return base.GetTooltip();
        }

        // Временное значение первой ручки
        private Point3d _startGripTmp;

        // временное значение последней ручки
        private Point3d _endGripTmp;

        // временное значение последней ручки
        private Point3d _leaderGripTmp;

        /// <inheritdoc />
        public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
        {
            try
            {
                // При начале перемещения запоминаем первоначальное положение ручки
                // Запоминаем начальные значения
                if (newStatus == Status.GripStart)
                {
                    if (GripName == GripName.StartGrip)
                    {
                        _startGripTmp = GripPoint;
                    }

                    if (GripName == GripName.EndGrip)
                    {
                        _endGripTmp = GripPoint;
                    }

                    //if (GripName == GripName.MiddleGrip)
                    //{
                    //    _startGripTmp = FragmentMarker.InsertionPoint;
                    //    _endGripTmp = FragmentMarker.EndPoint;
                    //}

                    if (GripName == GripName.LeaderGrip)
                    {
                        _leaderGripTmp = FragmentMarker.LeaderPoint;
                    }
                }

                // При удачном перемещении ручки записываем новые значения в расширенные данные
                // По этим данным я потом получаю экземпляр класса FragmentMarker
                if (newStatus == Status.GripEnd)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef = tr.GetObject(FragmentMarker.BlockId, OpenMode.ForWrite, true, true);
                        using (var resBuf = FragmentMarker.GetDataForXData())
                        {
                            blkRef.XData = resBuf;
                        }

                        tr.Commit();
                    }

                    FragmentMarker.Dispose();
                }

                // При отмене перемещения возвращаем временные значения
                if (newStatus == Status.GripAbort)
                {
                    if (_startGripTmp != null & GripName == GripName.StartGrip)
                    {
                        FragmentMarker.InsertionPoint = GripPoint;
                    }

                    //if (GripName == GripName.MiddleGrip & _startGripTmp != null & _endGripTmp != null)
                    //{
                    //    FragmentMarker.InsertionPoint = _startGripTmp;
                    //    FragmentMarker.EndPoint = _endGripTmp;
                    //}

                    if (_endGripTmp != null & GripName == GripName.EndGrip)
                    {
                        FragmentMarker.EndPoint = GripPoint;
                    }

                    if (_leaderGripTmp != null & GripName == GripName.LeaderGrip)
                    {
                        FragmentMarker.LeaderPoint = GripPoint;
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