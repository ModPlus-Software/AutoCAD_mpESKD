namespace mpESKD.Base.Overrules;

using System.Diagnostics;
using Abstractions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ModPlusAPI.Windows;
using Utils;

/// <inheritdoc />
public class SmartEntityObjectOverrule<TEntity> : ObjectOverrule
    where TEntity : SmartEntity
{
    private readonly ISmartEntityDescriptor _descriptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartEntityObjectOverrule{TEntity}"/> class.
    /// </summary>
    public SmartEntityObjectOverrule()
    {
        _descriptor = TypeFactory.Instance.GetDescriptor(typeof(TEntity));
            
        // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
        SetXDataFilter(_descriptor.Name);
    }

    /// <inheritdoc/>
    public override void Close(DBObject dbObject)
    {
        Debug.Print($"{_descriptor.Name} ObjectOverrule");
            
        if (IsApplicable(dbObject))
        {
            try
            {
                if (AcadUtils.Document == null)
                    return;

                if (dbObject == null)
                    return;

                if ((dbObject.IsNewObject && dbObject.Database == AcadUtils.Database) ||
                    (dbObject.IsUndoing && dbObject.IsModifiedXData)) //// ||
                    ////(dbObject.IsModified && AcadUtils.Database.TransactionManager.TopTransaction == null))
                {
                    var smartEntity = EntityReaderService.Instance.GetFromEntity<TEntity>(dbObject);
                    if (smartEntity == null)
                        return;

                    smartEntity.UpdateEntities();
                    smartEntity.GetBlockTableRecordForUndo((BlockReference)dbObject)?.UpdateAnonymousBlocks();
                }
            }
            catch (Exception exception)
            {
                if (exception.Message != "eWrongDatabase")
                    throw;
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        base.Close(dbObject);
    }

    /// <inheritdoc />
    public override bool IsApplicable(RXObject overruledSubject)
    {
        return ExtendedDataUtils.IsApplicable(overruledSubject, _descriptor.Name, true);
    }
}