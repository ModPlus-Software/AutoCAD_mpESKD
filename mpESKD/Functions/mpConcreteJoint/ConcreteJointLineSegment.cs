﻿namespace mpESKD.Functions.mpConcreteJoint;

using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

/// <summary>
/// Сегмент линии шва бетонирования
/// </summary>
internal struct ConcreteJointLineSegment
{
    /// <summary>
    /// Сегмент шва
    /// </summary>
    /// <param name="polylines">Полилинии, из которых состоит сегмент</param>
    /// <param name="endPointLineBreak">Точка, в которой прерывается линия шва в конце сегмента</param>
    /// <param name="remnantAtEnd">Длина неполного излома в конце сегмента, вдоль центральной линии</param>
    /// <param name="startBasePoint">Первая точка сегмента на центральной линии</param>
    internal ConcreteJointLineSegment(List<Polyline> polylines, Point2d endPointLineBreak, double remnantAtEnd, Point2d startBasePoint)
    {
        Polylines = polylines;
        EndPointLineBreak = endPointLineBreak;
        RemnantAtEnd = remnantAtEnd;
        StartBasePoint = startBasePoint;
    }

    /// <summary>
    /// Полилинии, из которых состоит сегмент
    /// </summary>
    internal List<Polyline> Polylines { get; }

    /// <summary>
    /// Точка, в которой прерывается линия шва в конце сегмента
    /// </summary>
    internal Point2d EndPointLineBreak { get; set; }

    /// <summary>
    /// Длина неполного излома в конце сегмента, вдоль центральной линии
    /// </summary>
    internal double RemnantAtEnd { get; set; }

    /// <summary>
    /// Первая точка сегмента на центральной линии 
    /// </summary>
    internal Point2d StartBasePoint { get; }

}