using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка вершин
/// </summary>
public class ChainLeaderArrowRemoveGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderArrowRemoveGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр класса <see cref="mpChainLeader.ChainLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public ChainLeaderArrowRemoveGrip(ChainLeader chainLeader, int gripIndex, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripIndex = gripIndex;
        GripType = GripType.Minus;
        Entity = entity;
    }

    public BlockReference Entity { get; }
    /// <summary>
    /// Экземпляр класса <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

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
        using (ChainLeader)
        {

            var tempInsPoint = new Point3d();
            if (ChainLeader.ArrowPoints.Count != 0)
            {
                ChainLeader.ArrowPoints.RemoveAt(GripIndex);
                var tempList = new List<double>();
                //ChainLeader.ArrowPoints.RemoveAt(GripIndex);
                tempList.AddRange(ChainLeader.ArrowPoints);
                tempList.Add(ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint));
                var q = tempList.OrderBy(x => x);
                var result = q.FirstOrDefault();
                tempInsPoint = ChainLeader.EndPoint + (ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal() * result;

                if (result > 0)
                {
                    result = q.LastOrDefault();
                    
                }
                else if (result == 0)
                {
                    tempInsPoint = ChainLeader.InsertionPoint;
                }
                else
                {
                    ChainLeader.InsertionPoint = tempInsPoint;
                }

                ChainLeader.InsertionPoint = tempInsPoint;
            }


            
            
            

            ChainLeader.UpdateEntities();
            ChainLeader.BlockRecord.UpdateAnonymousBlocks();
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(ChainLeader.BlockId, OpenMode.ForWrite, true, true);
                Entity.Position = tempInsPoint;
                using (var resBuf = ChainLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }

        return ReturnValue.GetNewGripPoints;
    }
}