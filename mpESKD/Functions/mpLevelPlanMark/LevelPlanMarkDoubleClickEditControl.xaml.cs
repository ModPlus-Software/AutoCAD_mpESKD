﻿namespace mpESKD.Functions.mpLevelPlanMark;

using System;
using Base.Abstractions;
using mpESKD.Base.Utils;

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
        Resources.SetModPlusResources();
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(LevelPlanMark);
        
    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not LevelPlanMark levelPlanMark)
            throw new ArgumentException("Wrong type of entity");

        _levelPlanMark = levelPlanMark;
        
        LevelNumBox.Value = Math.Round(_levelPlanMark.PlanMark, _levelPlanMark.Accuracy);
        
        LevelNumBox.Focus();
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _levelPlanMark.PlanMark = LevelNumBox.Value!.Value;
    }
}