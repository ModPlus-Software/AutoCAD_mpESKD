
namespace mpESKD.Functions.mpNodalLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using mpESKD.Base.Utils;
/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class NodalFrameTypeGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodalFrameTypeGrip"/> class.
    /// </summary>
    /// <param name="nodalLeader">Экземпляр <see cref="mpNodalLeader.NodalLeader"/></param>
    public NodalFrameTypeGrip(NodalLeader nodalLeader)
    {
        NodalLeader = nodalLeader;
    }
        
    /// <summary>
    /// Экземпляр <see cref="mpNodalLeader.NodalLeader"/>
    /// </summary>
    public NodalLeader NodalLeader { get; }
        
    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("p78"); // ""; TODO изменить на "Тип рамки"
    }

    //public FrameType FrameType { get; set; } = FrameType.Round;
        
    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (NodalLeader)
        {

            NodalLeaderFrameTypeMenu nodalLeaderFrameTypeMenu = new NodalLeaderFrameTypeMenu(NodalLeader);
            nodalLeaderFrameTypeMenu.Show();


            nodalLeaderFrameTypeMenu.Loaded += NodalLeaderFrameTypeMenu_Loaded;
        }

        return ReturnValue.GetNewGripPoints;
    }

    private void NodalLeaderFrameTypeMenu_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        
    }
}