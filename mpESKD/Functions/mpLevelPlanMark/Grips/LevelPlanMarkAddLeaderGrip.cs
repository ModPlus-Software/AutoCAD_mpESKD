using DocumentFormat.OpenXml.EMMA;
using mpESKD.Base.Abstractions;

namespace mpESKD.Functions.mpLevelPlanMark;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Windows;
using System.Windows.Controls;
using Base.Enums;


/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class LevelPlanMarkAddLeaderGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMarkAddLeaderGrip"/> class.
    /// </summary>
    /// <param name="levelPlanMark">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    public LevelPlanMarkAddLeaderGrip(LevelPlanMark levelPlanMark) //TODO
    {
        LevelPlanMark = levelPlanMark;
        GripType = GripType.Plus;
    }

    /// <summary>
    /// Экземпляр <see cref="mpNodalLeader.NodalLeader"/>
    /// </summary>
    public LevelPlanMark LevelPlanMark { get; }

    /// <summary>
    /// Новое значение точки вершины
    /// </summary>
    public Point3d NewPoint { get; set; }

    // Временное значение ручки
    private Point3d _gripTmp;


    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Тип рамки
        return Language.GetItem("gp4"); // TODO localization
    }

    ///// <inheritdoc />
    //public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    //{
    //    using (LevelPlanMark)
    //    {
    //        AcadUtils.WriteMessageInDebug($"plus ");
    //    }

    //    return ReturnValue.GetNewGripPoints;
    //}

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripStart)
        {
            AcadUtils.Editor.TurnForcedPickOn();
            AcadUtils.WriteMessageInDebug($"plus {Status.GripStart} - {LevelPlanMark.InsertionPointOCS}");
            //AcadUtils.Editor.PointMonitor += AddNewVertex_EdOnPointMonitor;
            
        }

        var leaderPointsCount = LevelPlanMark.LeaderPoints.Count;
        if (newStatus == Status.GripEnd)
        {
            int i= 0;
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.WriteMessageInDebug($"plus {Status.GripEnd} - {LevelPlanMark.InsertionPoint}" );
            //AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
            using (LevelPlanMark)
            {
                Point3d? newInsertionPoint = new Point3d(5,5,0);
                
                //if (leaderPointsCount == 0)
                //{
                //    leaderPointsCount = -1;
                //}

                LevelPlanMark.LeaderPoints.Insert(leaderPointsCount, NewPoint);
                
                foreach (var leaderPoint in LevelPlanMark.LeaderPoints)
                {
                    AcadUtils.WriteMessageInDebug($"leaderpoints {i}-{leaderPoint} ");
                    i++;
                }

                //AcadUtils.WriteMessageInDebug($"{}");

                LevelPlanMark.UpdateEntities();
                LevelPlanMark.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(LevelPlanMark.BlockId, OpenMode.ForWrite, true, true);
                    if (newInsertionPoint.HasValue)
                    {
                        ((BlockReference)blkRef).Position = newInsertionPoint.Value;
                    }

                    using (var resBuf = LevelPlanMark.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }
        }

        if (newStatus == Status.GripAbort)
        {
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.WriteMessageInDebug($"plus {Status.GripAbort}");
            
            //AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}