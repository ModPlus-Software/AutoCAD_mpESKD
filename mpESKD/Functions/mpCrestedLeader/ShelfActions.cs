using mpESKD.Base.Enums;
using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using mpESKD.Base.Utils;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;

namespace mpESKD.Functions.mpCrestedLeader;

internal static class ShelfActions
{
    internal static void ShelfPositionMove(ref CrestedLeader crestedLeader, Point3d newPoint)
    {
        // новое значение ShelfPosition(? , ShelfStartPoint, ShelfLedgePoint, ShelfEndPoint
        /*
        var leaderStartPointsSort = crestedLeader.LeaderStartPoints.OrderBy(p => p.X);

        var leftStartPoint = leaderStartPointsSort.First();
        var rightStartPoint = leaderStartPointsSort.Last();
        */

        // Точка проекции newPoint на центральную линию, приведенную в координаты блока
        //newPoint = newPoint.Point3dToPoint3dOcs(crestedLeader);

        var setShelfPosition = crestedLeader.ShelfPosition;

        newPoint = newPoint.GetProjectPointToBaseLine(crestedLeader).Point3dToPoint3dOcs(crestedLeader);

        var leaderStartPointsOcsSort = crestedLeader.LeaderStartPointsOCS.OrderBy(p => p.X);

        // Крайняя слева точка начала выносок в координатах блока
        var leftStartPoint = leaderStartPointsOcsSort.First();
            //crestedLeader.LeaderStartPointsOCS.OrderBy(p => p.X);
            //.Point3dToPoint3dOcs(crestedLeader);

        // Крайняя справа точка начала выносок в координатах блока
        var rightStartPoint = leaderStartPointsOcsSort.Last();
            //crestedLeader.LeaderStartPointsSorted.Last()
            //.Point3dToPoint3dOcs(crestedLeader);

        // Средняя точка начала выносок в координатах блока
        var midUnionLinePoint = GeometryUtils.GetMiddlePoint3d(leftStartPoint, rightStartPoint);
            //.Point3dToPoint3dOcs(crestedLeader);

        var unionLineLenght = Math.Abs(leftStartPoint.X - rightStartPoint.X);

        // Точка вставки справа
        if (crestedLeader.InsertionPointOCS.Equals(rightStartPoint))
        {
            // Точка вставки справа + Полка справа
            if (crestedLeader.ShelfPosition == ShelfPosition.Right)
            {
                // Направо от правой точки
                if (newPoint.X >= rightStartPoint.X)
                {
                    // ShelfLedge = [точка проекции newPoint на BaseLine].DistanceTo(rightStartPoint)
                    // GetProjectPointToBaseLine

                    crestedLeader.ShelfLedge = Math.Abs(newPoint.X - rightStartPoint.X);
                }
                // Между средней точкой и правой точкой
                else if (newPoint.X >= midUnionLinePoint.X && newPoint.X <= rightStartPoint.X)
                {
                    crestedLeader.ShelfLedge = 0;
                }
                // Между левой точкой и средней точкой
                else if (newPoint.X >= leftStartPoint.X && newPoint.X < midUnionLinePoint.X)
                {
                    crestedLeader.ShelfPosition = ShelfPosition.Left;
                    crestedLeader.ShelfLedge = unionLineLenght;
                }
                // Налево от левой точки
                // newPoint.X < leftStartPoint.X
                else
                {
                    crestedLeader.ShelfPosition = ShelfPosition.Left;
                    crestedLeader.ShelfLedge = Math.Abs(newPoint.X - rightStartPoint.X);
                }
            }
            // Точка вставки справа + Полка слева
            else if (crestedLeader.ShelfPosition == ShelfPosition.Left)
            {
                // Направо от правой точки
                if (newPoint.X >= rightStartPoint.X)
                {
                    crestedLeader.ShelfPosition = ShelfPosition.Right;
                    crestedLeader.ShelfLedge = Math.Abs(newPoint.X - rightStartPoint.X);
                }
                // Между средней точкой и правой точкой
                else if (newPoint.X >= midUnionLinePoint.X && newPoint.X <= rightStartPoint.X)
                {
                    crestedLeader.ShelfPosition = ShelfPosition.Right;
                    crestedLeader.ShelfLedge = 0;
                }
                // Между левой точкой и средней точкой
                else if (newPoint.X >= leftStartPoint.X && newPoint.X < midUnionLinePoint.X)
                {
                    crestedLeader.ShelfLedge = unionLineLenght;
                }
                // Налево от левой точки
                // newPoint.X < leftStartPoint.X
                else
                {
                    crestedLeader.ShelfLedge = Math.Abs(newPoint.X - rightStartPoint.X);
                }
            }
        }
        // Точка вставки слева
        else
        {
            // Точка вставки слева + Полка справа
            if (crestedLeader.ShelfPosition == ShelfPosition.Right)
            {
                // Направо от правой точки
                if (newPoint.X >= rightStartPoint.X)
                {
                    crestedLeader.ShelfLedge = Math.Abs(newPoint.X - leftStartPoint.X);
                }
                // Между средней точкой и правой точкой
                else if (newPoint.X >= midUnionLinePoint.X && newPoint.X <= rightStartPoint.X)
                {
                    crestedLeader.ShelfLedge = unionLineLenght;
                }
                // Между левой точкой и средней точкой
                else if (newPoint.X >= leftStartPoint.X && newPoint.X < midUnionLinePoint.X)
                {
                    crestedLeader.ShelfPosition = ShelfPosition.Left;
                    crestedLeader.ShelfLedge = 0;
                }
                // Налево от левой точки
                // newPoint.X < leftStartPoint.X
                else
                {
                    crestedLeader.ShelfPosition = ShelfPosition.Left;
                    crestedLeader.ShelfLedge = Math.Abs(newPoint.X - leftStartPoint.X);
                }
            }
            // Точка вставки слева + Полка слева
            else if (crestedLeader.ShelfPosition == ShelfPosition.Left)
            {
                // Направо от правой точки
                if (newPoint.X >= rightStartPoint.X)
                {
                    crestedLeader.ShelfPosition = ShelfPosition.Right;
                    crestedLeader.ShelfLedge = Math.Abs(newPoint.X - leftStartPoint.X);
                }
                // Между средней точкой и правой точкой
                else if (newPoint.X >= midUnionLinePoint.X && newPoint.X <= rightStartPoint.X)
                {
                    crestedLeader.ShelfPosition = ShelfPosition.Right;
                    crestedLeader.ShelfLedge = unionLineLenght;
                }
                // Между левой точкой и средней точкой
                else if (newPoint.X >= leftStartPoint.X && newPoint.X < midUnionLinePoint.X)
                {
                    crestedLeader.ShelfLedge = 0;
                }
                // Налево от левой точки
                // newPoint.X < leftStartPoint.X
                else
                {
                    crestedLeader.ShelfLedge = Math.Abs(newPoint.X - leftStartPoint.X);
                }
            }
        }
        
        
        if (newPoint.X >= midUnionLinePoint.X && crestedLeader.InsertionPointOCS.Equals(leftStartPoint) ||
            newPoint.X < midUnionLinePoint.X && crestedLeader.InsertionPointOCS.Equals(rightStartPoint))
        {
            crestedLeader.IsChangeShelfPosition = true;
        }
        else
        {
            crestedLeader.IsChangeShelfPosition = false;
        }

        /*
        if (crestedLeader.ShelfPosition != setShelfPosition)
        {
            crestedLeader.IsChangeShelfPosition = true;
        }
        else
        {
            crestedLeader.IsChangeShelfPosition = false;
        }*/

        crestedLeader.ToLogAnyString($"IsChangeShelfPosition: {crestedLeader.IsChangeShelfPosition}");
    }
}