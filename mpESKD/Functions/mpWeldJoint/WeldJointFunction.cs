namespace mpESKD.Functions.mpWeldJoint;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Abstractions;
using Base.Overrules;
using Base.Styles;
using Base.Utils;
using ModPlusAPI.Windows;

/// <inheritdoc />
public class WeldJointFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new LinearSmartEntityGripOverrule<WeldJoint>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<WeldJoint>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<WeldJoint>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<WeldJoint>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<WeldJoint>();

            var waterProofing = new WeldJoint();
            var blockReference = MainFunction.CreateBlock(waterProofing);

            waterProofing.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            LinearEntityUtils.InsertWithJig(waterProofing, blockReference);
        }
        catch (System.Exception exception)
        {
            ExceptionBox.Show(exception);
        }
        finally
        {
            Overrule.Overruling = true;
        }
    }

    /// <summary>
    /// Команда создания линии грунта
    /// </summary>
    [CommandMethod("ModPlus", "mpWeldJoint", CommandFlags.Modal)]
    public void CreateWeldJointCommand()
    {
        SmartEntityUtils.SendStatistic<WeldJoint>();
            
        CreateWeldJoint();
    }

    /// <summary>
    /// Команда создания линия грунта из полилинии
    /// </summary>
    [CommandMethod("ModPlus", "mpWeldJointFromPolyline", CommandFlags.Modal)]
    public void CreateWeldJointFromPolylineCommand()
    {
        SmartEntityUtils.SendStatistic<WeldJoint>();
            
        /* Регистрация ЕСКД приложения должна запускаться при запуске
         * функции, т.к. регистрация происходит в текущем документе
         * При инициализации плагина регистрации нет!
         */
        ExtendedDataUtils.AddRegAppTableRecord<WeldJoint>();
            
        LinearEntityUtils.CreateFromPolyline<WeldJoint>();
    }

    private void CreateWeldJoint()
    {
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<WeldJoint>();

            var style = StyleManager.GetCurrentStyle(typeof(WeldJoint));
            var waterProofing = new WeldJoint();

            var blockReference = MainFunction.CreateBlock(waterProofing);
            waterProofing.ApplyStyle(style, true);

            LinearEntityUtils.InsertWithJig(waterProofing, blockReference);
        }
        catch (System.Exception exception)
        {
            ExceptionBox.Show(exception);
        }
        finally
        {
            Overrule.Overruling = true;
        }
    }
}