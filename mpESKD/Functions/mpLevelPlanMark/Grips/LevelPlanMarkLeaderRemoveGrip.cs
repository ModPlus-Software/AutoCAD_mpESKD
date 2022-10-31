namespace mpESKD.Functions.mpLevelPlanMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка вершин
/// </summary>
public class LevelPlanMarkLeaderRemoveGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMarkLeaderRemoveGrip"/> class.
    /// </summary>
    /// <param name="levelPlanMark">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public LevelPlanMarkLeaderRemoveGrip(LevelPlanMark levelPlanMark, int gripIndex)
    {
        LevelPlanMark = levelPlanMark;
        GripIndex = gripIndex;
        GripType = GripType.Minus;
    }
    
    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public LevelPlanMark LevelPlanMark { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp6"); // Удалить выноску
    }

    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (LevelPlanMark)
        {
            LevelPlanMark.LeaderPoints.RemoveAt(GripIndex);
            LevelPlanMark.LeaderTypes.RemoveAt(GripIndex);

            LevelPlanMark.UpdateEntities();
            LevelPlanMark.BlockRecord.UpdateAnonymousBlocks();
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(LevelPlanMark.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = LevelPlanMark.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }

        return ReturnValue.GetNewGripPoints;
    }
}