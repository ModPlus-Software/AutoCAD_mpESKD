using mpESKD.Base.Enums;
using System;
using System.Linq;
using mpESKD.Base.Utils;
using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpCrestedLeader;

internal static class ShelfActions
{
    internal static void ShelfPositionMove(ref CrestedLeader crestedLeader, Point3d newPoint)
    {
        // новое значение ShelfPosition(? , ShelfStartPoint, ShelfLedgePoint, ShelfEndPoint
        var leaderStartPointsSort = crestedLeader.LeaderStartPoints.OrderBy(p => p.X);
        var leftStartPoint = leaderStartPointsSort.First();
        var rightStartPoint = leaderStartPointsSort.Last();
        var unionLineLenght = Math.Abs(leftStartPoint.X - rightStartPoint.X);

        //var startShelfPosition = crestedLeader.ShelfPosition;

        var midUnionLinePoint = GeometryUtils.GetMiddlePoint3d(leftStartPoint, rightStartPoint);

        // Точка вставки справа
        if (crestedLeader.InsertionPoint.Equals(rightStartPoint))
        {
            // Точка вставки справа + Полка справа
            if (crestedLeader.ShelfPosition == ShelfPosition.Right)
            {
                // Направо от правой точки
                if (newPoint.X >= rightStartPoint.X)
                {
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
        
        if (newPoint.X >= midUnionLinePoint.X && crestedLeader.InsertionPoint.Equals(leftStartPoint) ||
            newPoint.X < midUnionLinePoint.X && crestedLeader.InsertionPoint.Equals(rightStartPoint))
        {
            crestedLeader.IsChangeShelfPosition = true;
        }
        else
        {
            crestedLeader.IsChangeShelfPosition = false;
        }
    }
}