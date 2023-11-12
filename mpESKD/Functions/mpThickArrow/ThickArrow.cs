﻿namespace mpESKD.Functions.mpThickArrow;

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;

/// <summary>
/// Широкая стрелка
/// </summary>
[SmartEntityDisplayNameKey("h187")]
[SystemStyleDescriptionKey("h188")]
public class ThickArrow : SmartEntity, IWithDoubleClickEditor
{
    /* не нужно - нет обозначений
     * private readonly string _lastIntegerValue = string.Empty;

       private readonly string _lastLetterValue = string.Empty;*/

    #region Entities

    /// <summary>
    /// Стрелка-полилиния
    /// </summary>
    private Polyline _shelfArrow;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ThickArrow"/> class.
    /// </summary>
    public ThickArrow() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="View"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public ThickArrow(ObjectId objectId)
        : base(objectId)
    {
    }

    /* не нужно - нет обозначений
    /// <summary>
    /// Initializes a new instance of the <see cref="View"/> class.
    /// </summary>
    /// <param name="lastIntegerValue">Числовое значение последней созданной оси</param>
    /// <param name="lastLetterValue">Буквенное значение последней созданной оси</param>
    public ThickArrow(string lastIntegerValue, string lastLetterValue)
    {
        _lastIntegerValue = lastIntegerValue;
        _lastLetterValue = lastLetterValue;
    }*/

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => 0.5;

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _shelfArrow,
            };

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <inheritdoc />
    /// В примитиве не используется!
    public override string LineType { get; set; }

    /// <inheritdoc />
    /// В примитиве не используется!
    public override double LineTypeScale { get; set; }

    /// <inheritdoc />
    /// В примитиве не используется!
    public override string TextStyle { get; set; }

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap() 
    {
        yield return InsertionPoint; // TODO: посм. итераторы
        yield return EndPoint;
        // TODO: Добавить 3ю точку?
    }

    /// <summary>
    /// UpdateEntities
    /// </summary>
    public override void UpdateEntities()
    {
        // TODO: разобрать по примеру mpView
    }

    private void CreateEntities()
    {
        // TODO: разобрать по примеру mpView
    }

}