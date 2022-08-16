namespace mpESKD.Base.Overrules;

using Abstractions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Utils;

/// <inheritdoc />
public abstract class BaseSmartEntityGripOverrule<TEntity> : GripOverrule 
    where TEntity : SmartEntity
{
    private readonly ISmartEntityDescriptor _descriptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSmartEntityGripOverrule{TEntity}"/> class.
    /// </summary>
    protected BaseSmartEntityGripOverrule()
    {
        _descriptor = TypeFactory.Instance.GetDescriptor(typeof(TEntity));
            
        // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
        SetXDataFilter(_descriptor.Name);
    }
        
    /// <inheritdoc />
    public override bool IsApplicable(RXObject overruledSubject)
    {
        return ExtendedDataUtils.IsApplicable(overruledSubject, _descriptor.Name);
    }
}