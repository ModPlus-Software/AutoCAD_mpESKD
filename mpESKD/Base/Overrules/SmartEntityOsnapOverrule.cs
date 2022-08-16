namespace mpESKD.Base.Overrules;

using System;
using System.Diagnostics;
using Abstractions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using ModPlusAPI.Windows;
using Utils;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class SmartEntityOsnapOverrule<TEntity> : OsnapOverrule
    where TEntity : SmartEntity
{
    private readonly ISmartEntityDescriptor _descriptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartEntityOsnapOverrule{TEntity}"/> class.
    /// </summary>
    public SmartEntityOsnapOverrule()
    {
        _descriptor = TypeFactory.Instance.GetDescriptor(typeof(TEntity));
            
        // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
        SetXDataFilter(_descriptor.Name);
    }
        
    /// <inheritdoc />
    public override void GetObjectSnapPoints(Entity entity, ObjectSnapModes snapMode, IntPtr gsSelectionMark, Point3d pickPoint,
        Point3d lastPoint, Matrix3d viewTransform, Point3dCollection snapPoints, IntegerCollection geometryIds)
    {
        Debug.Print($"{_descriptor.Name} OsnapOverrule");
        if (IsApplicable(entity))
        {
            try
            {
                var smartEntity = EntityReaderService.Instance.GetFromEntity<TEntity>(entity);
                if (smartEntity != null)
                {
                    foreach (var point3d in smartEntity.GetPointsForOsnap())
                    {
                        snapPoints.Add(point3d);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        else
        {
            base.GetObjectSnapPoints(entity, snapMode, gsSelectionMark, pickPoint, lastPoint, viewTransform, snapPoints, geometryIds);
        }
    }

    /// <inheritdoc />
    public override bool IsApplicable(RXObject overruledSubject)
    {
        return ExtendedDataUtils.IsApplicable(overruledSubject, _descriptor.Name);
    }
}