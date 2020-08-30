namespace mpESKD.Functions.mpSection.Overrules.Grips
{
    using System.Linq;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Base;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;
    using Section = mpSection.Section;

    /// <summary>
    /// Ручка удаления вершины
    /// </summary>
    public class SectionRemoveVertexGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SectionRemoveVertexGrip"/> class.
        /// </summary>
        /// <param name="section">Экземпляр класса <see cref="mpSection.Section"/></param>
        /// <param name="index">Индекс ручки</param>
        public SectionRemoveVertexGrip(Section section, int index)
        {
            Section = section;
            GripIndex = index;
            GripType = GripType.Minus;
        }

        /// <summary>
        /// Экземпляр класса <see cref="mpSection.Section"/>
        /// </summary>
        public Section Section { get; }

        /// <summary>
        /// Индекс ручки
        /// </summary>
        public int GripIndex { get; }

        /// <inheritdoc />
        public override string GetTooltip()
        {
            return Language.GetItem(Invariables.LangItem, "gp3"); // "Удалить вершину";
        }

        /// <inheritdoc />
        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            using (Section)
            {
                Point3d? newInsertionPoint = null;

                if (GripIndex == 0)
                {
                    Section.InsertionPoint = Section.MiddlePoints[0];
                    newInsertionPoint = Section.MiddlePoints[0];
                    Section.MiddlePoints.RemoveAt(0);
                }
                else if (GripIndex == Section.MiddlePoints.Count + 1)
                {
                    Section.EndPoint = Section.MiddlePoints.Last();
                    Section.MiddlePoints.RemoveAt(Section.MiddlePoints.Count - 1);
                }
                else
                {
                    Section.MiddlePoints.RemoveAt(GripIndex - 1);
                }

                Section.UpdateEntities();
                Section.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(Section.BlockId, OpenMode.ForWrite, true, true);
                    if (newInsertionPoint.HasValue)
                    {
                        ((BlockReference)blkRef).Position = newInsertionPoint.Value;
                    }

                    using (var resBuf = Section.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }

            return ReturnValue.GetNewGripPoints;
        }
    }
}