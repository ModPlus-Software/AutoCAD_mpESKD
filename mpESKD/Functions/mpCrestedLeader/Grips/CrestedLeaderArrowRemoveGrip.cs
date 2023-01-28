namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Linq;

/// <summary>
/// Ручка вершин
/// </summary>
public class CrestedLeaderArrowRemoveGrip : SmartEntityGripData
{
    // Экземпляр анонимного блока
    private readonly BlockReference _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderArrowRemoveGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpChainLeader.ChainLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public CrestedLeaderArrowRemoveGrip(CrestedLeader crestedLeader, int gripIndex, BlockReference entity)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Minus;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp6"); // Удалить выноску
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (CrestedLeader)
        {
            var tempInsPoint = CrestedLeader.InsertionPoint;

            if (GripIndex == 4)
            {
                if (CrestedLeader.ArrowPoints.Count > 0)
                {
                    //var result = CrestedLeader.ArrowPoints.OrderBy(x => x).FirstOrDefault();
                    var furthiestPoints = CrestedLeader.ArrowPoints.GetFurthestPoints();
                    CrestedLeader.ArrowPoints.Remove(furthiestPoints.Item1);
                    tempInsPoint = furthiestPoints.Item1;

                    CrestedLeader.LeaderPoint = furthiestPoints.Item2;
                }
                //else
                //{
                //    var result = CrestedLeader.ArrowPoints.OrderBy(x => x).FirstOrDefault();
                //    if (result > 0)
                //    {
                //        result = CrestedLeader.ArrowPoints.OrderBy(x => x).LastOrDefault();
                //    }

                //    CrestedLeader.ArrowPoints.Remove(result);
                //    tempInsPoint = CrestedLeader.EndPoint + ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * result);

                //    if (!CrestedLeader.ArrowPoints.Any(x => x < 0))
                //    {
                //        var reversed = CrestedLeader.ArrowPoints.Select(x => -x).ToList();
                //        CrestedLeader.ArrowPoints = reversed;
                //    }
                //}
            }
            else if (CrestedLeader.ArrowPoints.Count != 0)
            {
                CrestedLeader.ArrowPoints.RemoveAt(GripIndex - 5);
            }

            CrestedLeader.InsertionPoint = tempInsPoint;
            CrestedLeader.UpdateEntities();
            CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);
                _entity.Position = tempInsPoint;
                using (var resBuf = CrestedLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }

        return ReturnValue.GetNewGripPoints;
    }
}
