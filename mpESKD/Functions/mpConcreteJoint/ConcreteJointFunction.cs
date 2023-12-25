namespace mpESKD.Functions.mpConcreteJoint;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Abstractions;
using Base.Overrules;
using Base.Styles;
using Base.Utils;
using ModPlusAPI.Windows;


public class ConcreteJointFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new LinearSmartEntityGripOverrule<ConcreteJoint>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<ConcreteJoint>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<ConcreteJoint>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<ConcreteJoint>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<ConcreteJoint>();

            var groundLine = new ConcreteJoint();
            var blockReference = MainFunction.CreateBlock(groundLine);

            groundLine.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            LinearEntityUtils.InsertWithJig(groundLine, blockReference);
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
    /// Команда создания шва бетонирования
    /// </summary>
    [CommandMethod("ModPlus", "mpConcreteJoint", CommandFlags.Modal)]
    public void CreateGroundLineCommand()
    {
        SmartEntityUtils.SendStatistic<ConcreteJoint>();

        CreateConcreteJoint();
    }

    private void CreateConcreteJoint()
    {
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<ConcreteJoint>();

            var style = StyleManager.GetCurrentStyle(typeof(ConcreteJoint));
            var groundLine = new ConcreteJoint();

            var blockReference = MainFunction.CreateBlock(groundLine);
            groundLine.ApplyStyle(style, true);

            LinearEntityUtils.InsertWithJig(groundLine, blockReference);
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