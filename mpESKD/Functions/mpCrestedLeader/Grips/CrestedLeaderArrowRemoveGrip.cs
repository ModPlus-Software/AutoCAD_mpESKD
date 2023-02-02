﻿using System;
using Autodesk.AutoCAD.Geometry;
using mpESKD.Functions.mpChainLeader.Grips;

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


            if (CrestedLeader.ArrowPoints.Count > 1)
            {
                var tempLine = new Line(CrestedLeader.InsertionPoint, CrestedLeader.EndPoint);
                var mainNormal = (CrestedLeader.InsertionPoint - CrestedLeader.ArrowPoints[0]).GetNormal();
                
                // первый индекс грипа в списке начинается с 5
                if (GripIndex == 5)
                {
                    AcadUtils.WriteMessageInDebug($"надо удалять первую точки {CrestedLeader.ArrowPoints[0]}");
                    
                    CrestedLeader.ArrowPoints.Remove(CrestedLeader.ArrowPoints.FirstOrDefault());
                    
                    var firtsPoint = CrestedLeader.ArrowPoints[0];
                    var templine = new Line(firtsPoint, firtsPoint + mainNormal);
                    var pts = new Point3dCollection();

                    tempLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
                    if (pts.Count > 0)
                    {
                        tempInsPoint = pts[0];
                        CrestedLeader.InsertionPoint = tempInsPoint;
                    }
                }

                if (GripIndex == CrestedLeader.ArrowPoints.Count + 4)
                {
                    AcadUtils.WriteMessageInDebug(
                        $"надо удалять первую точки {CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1]}");

                    CrestedLeader.ArrowPoints.Remove(CrestedLeader.ArrowPoints.LastOrDefault());
                    
                    var lastPoint = CrestedLeader.ArrowPoints.LastOrDefault();
                    var templine = new Line(lastPoint, lastPoint + mainNormal);
                    var pts = new Point3dCollection();

                    tempLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
                    if (pts.Count > 0)
                    {
                        
                        CrestedLeader.EndPoint = pts[0];
                    }
                }
                else 
                {
                    CrestedLeader.ArrowPoints.RemoveAt(GripIndex - 5);
                }
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

            //else if (CrestedLeader.ArrowPoints.Count != 0)
            //{
            //    CrestedLeader.ArrowPoints.RemoveAt(GripIndex - 5);
            //}

            
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
