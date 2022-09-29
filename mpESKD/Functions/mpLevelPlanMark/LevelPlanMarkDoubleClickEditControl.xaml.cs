namespace mpESKD.Functions.mpLevelPlanMark;

using System;
using Base.Abstractions;

/// <summary>
/// Логика взаимодействия для LevelPlanMarkDoubleClickEditControl.xaml
/// </summary>
public partial class LevelPlanMarkDoubleClickEditControl : IDoubleClickEditControl
{
    private LevelPlanMark _levelPlanMark;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMarkDoubleClickEditControl"/> class.
    /// </summary>
    public LevelPlanMarkDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(LevelPlanMark);
        
    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (!(smartEntity is LevelPlanMark levelPlanMark))
            throw new ArgumentException("Wrong type of entity");

        _levelPlanMark = levelPlanMark;
            
        TbValue.Text = _levelPlanMark.PlanMark;

        TbValue.Focus();
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _levelPlanMark.PlanMark = TbValue.Text;
    }
}