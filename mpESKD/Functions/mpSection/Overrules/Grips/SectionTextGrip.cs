namespace mpESKD.Functions.mpSection.Overrules.Grips
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Section = mpSection.Section;

    /// <summary>
    /// Ручка позиции текста
    /// </summary>
    public class SectionTextGrip : SmartEntityGripData
    {
        public SectionTextGrip(Section section)
        {
            Section = section;
            GripType = GripType.Point;
            CachedAlongTopShelfTextOffset = section.AlongTopShelfTextOffset;
            CachedAcrossTopShelfTextOffset = section.AcrossTopShelfTextOffset;
            CachedAlongBottomShelfTextOffset = section.AlongBottomShelfTextOffset;
            CachedAcrossBottomShelfTextOffset = section.AcrossBottomShelfTextOffset;
        }

        /// <summary>
        /// Экземпляр класса Section
        /// </summary>
        public Section Section { get; }

        /// <summary>
        /// Имя ручки, чтобы определить от какого она текста
        /// </summary>
        public TextGripName Name { get; set; }

        public double CachedAlongTopShelfTextOffset { get; }

        public double CachedAcrossTopShelfTextOffset { get; }

        public double CachedAlongBottomShelfTextOffset { get; }

        public double CachedAcrossBottomShelfTextOffset { get; }

        public override string GetTooltip()
        {
            return Language.GetItem(Invariables.LangItem, "gp1"); // stretch
        }

        public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
        {
            try
            {
                // При удачном перемещении ручки записываем новые значения в расширенные данные
                // По этим данным я потом получаю экземпляр класса section
                if (newStatus == Status.GripEnd)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef = tr.GetObject(Section.BlockId, OpenMode.ForWrite, true, true);
                        using (var resBuf = Section.GetDataForXData())
                        {
                            blkRef.XData = resBuf;
                        }

                        tr.Commit();
                    }

                    Section.Dispose();
                }

                // При отмене перемещения возвращаем временные значения
                if (newStatus == Status.GripAbort)
                {
                    if (Name == TextGripName.TopText)
                    {
                        Section.AlongTopShelfTextOffset = CachedAlongTopShelfTextOffset;
                        Section.AcrossTopShelfTextOffset = CachedAcrossTopShelfTextOffset;
                    }

                    if (Name == TextGripName.BottomText)
                    {
                        Section.AlongBottomShelfTextOffset = CachedAlongBottomShelfTextOffset;
                        Section.AcrossBottomShelfTextOffset = CachedAcrossBottomShelfTextOffset;
                    }
                }

                base.OnGripStatusChanged(entityId, newStatus);
            }
            catch (Exception exception)
            {
                if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                    ExceptionBox.Show(exception);
            }
        }
    }
}