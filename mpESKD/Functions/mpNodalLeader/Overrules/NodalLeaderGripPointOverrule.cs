namespace mpESKD.Functions.mpNodalLeader.Overrules
{
    using System;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Enums;
    using Base.Utils;
    using Grips;
    using ModPlusAPI.Windows;
    using Exception = Autodesk.AutoCAD.Runtime.Exception;

    /// <inheritdoc />
    public class NodalLeaderGripPointOverrule : GripOverrule
    {
        private static NodalLeaderGripPointOverrule _instance;
        private Point3d _initFramePoint;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static NodalLeaderGripPointOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new NodalLeaderGripPointOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(NodalLeader.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc />
        public override void GetGripPoints(
            Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            try
            {
                if (IsApplicable(entity))
                {
                    // Удаляю все ручки - это удалит ручку вставки блока
                    grips.Clear();

                    var nodalLeader = EntityReaderService.Instance.GetFromEntity<NodalLeader>(entity);
                    if (nodalLeader != null)
                    {
                        grips.Add(new NodalLeaderGrip(
                            nodalLeader, GripType.BasePoint, GripName.InsertionPoint, nodalLeader.InsertionPoint));
                        grips.Add(new NodalLeaderGrip(
                            nodalLeader, GripType.Point, GripName.FramePoint, nodalLeader.FramePoint));
                        grips.Add(new NodalLeaderGrip(
                            nodalLeader, GripType.Point, GripName.LeaderPoint, nodalLeader.EndPoint));
                        
                        grips.Add(new NodalLevelShelfPositionGrip(nodalLeader)
                        {
                            GripPoint = nodalLeader.EndPoint +
                                        (Vector3d.YAxis * ((nodalLeader.MainTextHeight + nodalLeader.TextVerticalOffset) * nodalLeader.GetFullScale())),
                            GripType = GripType.TwoArrowsLeftRight
                        });

                        _initFramePoint = nodalLeader.FramePoint;
                    }
                }
            }
            catch (Exception exception)
            {
                if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                    ExceptionBox.Show(exception);
            }
        }

        /// <inheritdoc />
        public override void MoveGripPointsAt(
            Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            try
            {
                if (IsApplicable(entity))
                {
                    foreach (var gripData in grips)
                    {
                        if (gripData is NodalLeaderGrip levelMarkGrip)
                        {
                            var gripPoint = levelMarkGrip.GripPoint;
                            var nodalLeader = levelMarkGrip.NodalLeader;
                            var scale = nodalLeader.GetFullScale();

                            if (levelMarkGrip.GripName == GripName.InsertionPoint)
                            {
                                ((BlockReference)entity).Position = gripPoint + offset;
                                nodalLeader.InsertionPoint = gripPoint + offset;
                                nodalLeader.FramePoint = _initFramePoint + offset;
                            }
                            else if (levelMarkGrip.GripName == GripName.FramePoint)
                            {
                                if (nodalLeader.FrameType == FrameType.Rectangular)
                                {
                                    var currentPosition = gripPoint + offset;
                                    var frameHeight = 
                                        Math.Abs(currentPosition.Y - nodalLeader.InsertionPoint.Y) / scale;
                                    var frameWidth = Math.Abs(currentPosition.X - nodalLeader.InsertionPoint.X) / scale;

                                    if (!(frameHeight <= nodalLeader.MinDistanceBetweenPoints) &&
                                        !(frameWidth <= nodalLeader.MinDistanceBetweenPoints))
                                    {
                                        nodalLeader.FramePoint = gripPoint + offset;
                                    }
                                }
                                else
                                {
                                    nodalLeader.FramePoint = gripPoint + offset;
                                }
                            }
                            else if (levelMarkGrip.GripName == GripName.LeaderPoint)
                            {
                                nodalLeader.EndPoint = gripPoint + offset;
                            }

                            // Вот тут происходит перерисовка примитивов внутри блока
                            nodalLeader.UpdateEntities();
                            nodalLeader.BlockRecord.UpdateAnonymousBlocks();
                        }
                        else
                        {
                            base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                        }
                    }
                }
                else
                {
                    base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                }
            }
            catch (Exception exception)
            {
                if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                    ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Проверка валидности примитива. Проверка происходит по наличию XData с определенным AppName
        /// </summary>
        /// <param name="overruledSubject">Instance of <see cref="RXObject"/></param>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, NodalLeader.GetDescriptor().Name);
        }
    }
}
