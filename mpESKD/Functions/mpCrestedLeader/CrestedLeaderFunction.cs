// todo Координаты, условия переделать с учетом поворота всего смарт объекта
// это нужно привести все .X к оси XAxis сначала ?
// где сортировка, сортировать как точки на прямой InsPt-SecondPoint слева направо

// todo Добавить  'Выбрать тип стрелки'
// todo Добавить ручку зеркалирования полки
// todo Добавить ручку выравнивания текста

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
using System.Collections.Generic;

/// <inheritdoc />
public class CrestedLeaderFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new CrestedLeaderGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<CrestedLeader>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<CrestedLeader>(), true);

        // Loggerq.DeleteFile();
        Loggerq.WriteRecord("CrestedLeaderFunction: Initialize() * * * * * * * * * * * * * * * * * * * * *");
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
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

            var crestedLeader = new CrestedLeader();
            var blockReference = MainFunction.CreateBlock(crestedLeader);
            crestedLeader.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            InsertCrestedLeaderWithJig(crestedLeader, blockReference);
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
        Loggerq.ClearFile();
        Loggerq.WriteRecord("CrestedLeaderFunction: CreateCrestedLeaderCommand() * * * * * * * * * * * * *");

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

            InsertCrestedLeaderWithJig(crestedLeader, blockReference); 
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

    private static void InsertCrestedLeaderWithJig(CrestedLeader crestedLeader, BlockReference blockReference)
    {
        List<Point3d> leaderStartPoints = new ();
        List<Point3d> leaderEndPoints = new ();
        Point3d shelfStartPoint = new ();
        Point3d shelfLedgePoint = new ();
        Point3d shelfEndPoint = new ();

        var entityJig = new DefaultEntityJig(
            crestedLeader,
            blockReference,
            new Point3d(20, 0, 0));

        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;

            // Это именно режим указания точек для смарт-объекта - не путать с режимом самого JIG
            var currentJigStateOfCrestedLeader = crestedLeader.CurrentJigState;

            if (status == PromptStatus.OK)
            {
                if (entityJig.JigState == JigState.PromptInsertPoint)
                {
                    entityJig.PreviousPoint = crestedLeader.InsertionPoint;
                    // Задан текущий режим JIG как режим NextPoint
                    entityJig.JigState = JigState.PromptNextPoint;

                    leaderEndPoints.Clear();
                    leaderEndPoints.Add(crestedLeader.InsertionPoint);
                    crestedLeader.LeaderEndPoints.Add(crestedLeader.InsertionPoint);

                    // Включение режима указания точек для смарт-объекта - указание точек выносок
                    crestedLeader.CurrentJigState = (int)CrestedLeaderJigState.PromptNextLeaderPoint;
                }
                // Если режим JIG - это NextPoint
                else if (entityJig.JigState == JigState.PromptNextPoint)
                {
                    // Если текущий режим указания точек для смарт-объекта - указание точек выносок
                    if (currentJigStateOfCrestedLeader == 2)
                    {
                        // сохранение точки выноски
                        leaderEndPoints.Add(crestedLeader.EndPoint);
                        crestedLeader.LeaderEndPoints.Add(crestedLeader.EndPoint);
                    }
                    // Если текущий режим указания точек для смарт-объекта - указание первой точки полки
                    else if (currentJigStateOfCrestedLeader == 3)
                    {
                        leaderStartPoints.Clear();
                        leaderStartPoints.AddRange(crestedLeader.LeaderStartPoints);

                        // Включение режима указания точки отступа полки как текущего
                        crestedLeader.CurrentJigState = 4; // 4

                    }
                    // Если текущий режим указания точек для смарт-объекта - указание точки отступа полки
                    else if (currentJigStateOfCrestedLeader == 4) // currentJigState == 4
                    {
                        // Отключение режима указания точек 
                        crestedLeader.CurrentJigState = 0;

                        shelfStartPoint = crestedLeader.InsertionPoint = crestedLeader.ShelfStartPoint;
                        shelfLedgePoint = crestedLeader.ShelfLedgePoint;
                        shelfEndPoint = crestedLeader.ShelfEndPoint;

                        crestedLeader.IsFirst = true;

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();

                        break;
                    }
                }
            }
            else if (status == PromptStatus.Other)
            {
                if (entityJig.JigState == JigState.PromptNextPoint)
                {
                    entityJig.PreviousPoint = crestedLeader.EndPoint;

                    // Включение режима указания первой точки полки как текущего
                    crestedLeader.CurrentJigState = 3;
                }
            }
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

                // перемещение точки вставки в точку первой точки полки
                ((BlockReference)ent).Position = crestedLeader.InsertionPoint;

                ent.XData = crestedLeader.GetDataForXData();
                tr.Commit();
            }

            crestedLeader.LeaderEndPoints.Clear();
            crestedLeader.LeaderEndPoints.AddRange(leaderEndPoints);

            crestedLeader.LeaderStartPoints.Clear();
            crestedLeader.LeaderStartPoints.AddRange(leaderStartPoints);

            crestedLeader.ShelfLedgePoint = shelfLedgePoint;
            crestedLeader.ShelfStartPoint = shelfStartPoint;
            crestedLeader.ShelfEndPoint = shelfEndPoint;

            crestedLeader.IsFirst = true;   

            crestedLeader.UpdateEntities();
            crestedLeader.BlockRecord.UpdateAnonymousBlocks();

            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(crestedLeader.BlockId, OpenMode.ForWrite, true, true);

                ent.XData = crestedLeader.GetDataForXData();
                tr.Commit();
            }
        }
    }


}