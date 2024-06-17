namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Abstractions;
using Base.Overrules;
using Base.Styles;
using Base.Utils;
using ModPlusAPI.Windows;
using Base.Enums;

/// <inheritdoc />
public class CrestedLeaderFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new CrestedLeaderGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<CrestedLeader>(),
            true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<CrestedLeader>(),
            true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        Loggerq.WriteRecord($"CrestedLeaderFunction; CreateAnalog; START)");

        SmartEntityUtils.SendStatistic<CrestedLeader>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<CrestedLeader>();

            var crestedLeader = new CrestedLeader();
            var blockReference = MainFunction.CreateBlock(crestedLeader);
            crestedLeader.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            // todo Тест
            TestFunctions.TestJig.InsertTestWithJig2(crestedLeader, blockReference);
            // InsertCrestedLeaderWithJig(crestedLeader, blockReference);
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
    /// Команда создания гребенчатой выноски
    /// </summary>
    [CommandMethod("ModPlus", "mpCrestedLeader", CommandFlags.Modal)]
    public void CreateCrestedLeaderCommand()
    {
        Loggerq.DeleteFile();
        Loggerq.WriteRecord($" * "); Loggerq.WriteRecord($" * "); Loggerq.WriteRecord($" * ");
        Loggerq.WriteRecord($" *-*-*-*-*-*-*- M  O  D   P  L  U  S -*-*-*-*-*-*-*-*-*");
        Loggerq.WriteRecord($" * ");

        CreateCrestedLeader();
    }

    private static void CreateCrestedLeader()
    {
        SmartEntityUtils.SendStatistic<CrestedLeader>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<CrestedLeader>();

            var style = StyleManager.GetCurrentStyle(typeof(CrestedLeader));
            var crestedLeader = new CrestedLeader();

            var blockReference = MainFunction.CreateBlock(crestedLeader);
            crestedLeader.ApplyStyle(style, true);

            // todo Тестировние Jig
            TestFunctions.TestJig.InsertTestWithJig2(crestedLeader, blockReference);
            // InsertCrestedLeaderWithJig(crestedLeader, blockReference); 
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

    /*
    private static void InsertCrestedLeaderWithJig(CrestedLeader crestedLeader, BlockReference blockReference)
    {
        var entityJig = new DefaultEntityJig(
            crestedLeader,
            blockReference,
            new Point3d(20, 0, 0),
            (point) => crestedLeader.EndPoint = point);

        AcadUtils.Editor.TurnForcedPickOn();
        AcadUtils.Editor.PointMonitor += crestedLeader.CrestedSimplyLeaderMonitor;
        
        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;

            if (status == PromptStatus.OK)
            {
                if (entityJig.JigState == JigState.PromptInsertPoint)
                {
                    entityJig.JigState = JigState.PromptNextPoint;
                    crestedLeader.CurrentJigState = (int)CrestedLeaderJigState.PromptNextLeaderPoint;

                    entityJig.PreviousPoint = crestedLeader.InsertionPoint;
                    crestedLeader.CreateSimplyLeader(crestedLeader.InsertionPoint);
                }
                // Если текущий режим - указание точек выносок  и выполнен клик мышкой
                else if (entityJig.JigState == JigState.PromptNextPoint)
                {
                    var toEndPointVector = crestedLeader.EndPoint - crestedLeader.InsertionPoint;
                    crestedLeader.LeaderPointsOcs.Add(Point3d.Origin + toEndPointVector);

                    crestedLeader.CreateSimplyLeader(crestedLeader.EndPoint);
                }
                else
                {
                    // Выполняется Action (где задается EndPoint) и выход
                    crestedLeader.CurrentJigState = (int)CrestedLeaderJigState.None;
                    break;
                }
            }
            // Нажат Enter
            else if (status == PromptStatus.Other)
            {
                // Проверяем, что мы в режиме указания точек выносок
                if (entityJig.JigState == JigState.PromptNextPoint)
                {
                    entityJig.JigState = JigState.CustomPoint;
                    crestedLeader.CurrentJigState = (int)CrestedLeaderJigState.PromptShelfStartPoint;

                    AcadUtils.Editor.TurnForcedPickOff();
                    AcadUtils.Editor.PointMonitor -= crestedLeader.CrestedSimplyLeaderMonitor;
                }
            }
            // Нажат Cancel или что-то  другое (но не Enter)
            else 
            {
                EntityUtils.Erase(blockReference.Id);
                break;
            }
        } while (true);

        if (!crestedLeader.BlockId.IsErased)
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(crestedLeader.BlockId, OpenMode.ForWrite, true, true);

                ent.XData = crestedLeader.GetDataForXData();
                tr.Commit();
            }
        }
        else
        {
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.Editor.PointMonitor -= crestedLeader.CrestedSimplyLeaderMonitor;
            
        }
    }

    */
}