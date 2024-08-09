#pragma warning disable SA1515
#pragma warning disable SA1513
namespace mpESKD.Functions.mpCrestedLeader;

using System;
using System.Linq;
using Base.Utils;
using Base.Enums;
using Autodesk.AutoCAD.Geometry;

/// <summary>
/// Изменение положения полки текста
/// </summary>
internal static class ShelfActions
{
    /// <summary>
    /// Изменяет положение полки текста при перетаскивании ручки 
    /// </summary>
    /// <param name="crestedLeader">Экземпляр объекта <see cref="CrestedLeader"></see></param>
    /// <param name="newPoint">Точка курсора</param>
    internal static void ShelfPositionMove(ref CrestedLeader crestedLeader, Point3d newPoint)
    {
        if (crestedLeader.LeaderStartPoints.Count == 0)
        {
            return;
        }

        // Точка проекции newPoint на центральную линию, приведенная к координатам блока
        newPoint = newPoint.GetProjectPointToBaseLine(crestedLeader).Point3dToPoint3dOcs(crestedLeader);

        var leaderStartPointsOcsSort = crestedLeader.LeaderStartPointsOCS.OrderBy(p => p.X);

        // Крайняя слева точка начала выносок в координатах блока
        // ReSharper disable once PossibleMultipleEnumeration
        var leftStartPoint = leaderStartPointsOcsSort.First();

        // Крайняя справа точка начала выносок в координатах блока
        // ReSharper disable once PossibleMultipleEnumeration
        var rightStartPoint = leaderStartPointsOcsSort.Last();

        // Средняя точка начала выносок в координатах блока
        var midUnionLinePoint = GeometryUtils.GetMiddlePoint3d(leftStartPoint, rightStartPoint);

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
        
        if ((newPoint.X >= midUnionLinePoint.X && crestedLeader.InsertionPointOCS.Equals(leftStartPoint)) ||
            (newPoint.X < midUnionLinePoint.X && crestedLeader.InsertionPointOCS.Equals(rightStartPoint)))
        {
            crestedLeader.IsChangeShelfPosition = true;
        }
        else
        {
            crestedLeader.IsChangeShelfPosition = false;
        }
    }
}