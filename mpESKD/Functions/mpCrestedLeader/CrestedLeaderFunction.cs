using System.Linq;

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
using CSharpFunctionalExtensions;
using System.Collections.Generic;

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
        List<Point3d> leaderPoints = new ();
        Point3d shelfLedgePoint = new ();

        var entityJig = new DefaultEntityJig(
            crestedLeader,
            blockReference,
            new Point3d(20, 0, 0));

        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;

            // Это именно режим указания точек для смарт-объекта - не путать с режимом самого JIG
            var currentJigStateOfCrestedLeader = crestedLeader.CurrentJigState;

            /*
            Loggerq.WriteRecord($"currentJigState :{currentJigStateOfCrestedLeader}");

            if (currentJigStateOfCrestedLeader == (int)CrestedLeaderJigState.PromptInsertPoint ||
                currentJigStateOfCrestedLeader == (int)CrestedLeaderJigState.PromptNextLeaderPoint)
            {
                crestedLeader.CreateSimplyLeader(crestedLeader.InsertionPoint);
            }
            else
            {
                crestedLeader.CreateSimplyLeader(crestedLeader.EndPoint);
            }*/

            if (status == PromptStatus.OK)
            {
                if (entityJig.JigState == JigState.PromptInsertPoint)
                {

                    entityJig.PreviousPoint = crestedLeader.InsertionPoint;
                    // Задан текущий режим JIG как режим NextPoint
                    entityJig.JigState = JigState.PromptNextPoint;

                    leaderPoints.Add(crestedLeader.InsertionPoint);
                    crestedLeader.LeaderEndPoints.Add(crestedLeader.InsertionPoint);

                    // Включение режима указания точек для смарт-объекта - указание точек выносок
                    crestedLeader.CurrentJigState = (int)CrestedLeaderJigState.PromptNextLeaderPoint;

                    // Отрисовка выноски при клике в режиме PromptNextLeaderPoint // todo
                    //crestedLeader.CreateTempLeaders();
                }
                // Если режим JIG - это NextPoint
                else if (entityJig.JigState == JigState.PromptNextPoint)
                {
                    // Если текущий режим указания точек для смарт-объекта - указание точек выносок
                    if (currentJigStateOfCrestedLeader == 2)
                    {
                        // crestedLeader.CreateSimplyLeader(crestedLeader.EndPoint);

                        // сохранение точки выноски
                        leaderPoints.Add(crestedLeader.EndPoint);
                        crestedLeader.LeaderEndPoints.Add(crestedLeader.EndPoint);


                    }
                    // Если текущий режим указания точек для смарт-объекта - указание первой точки полки
                    else if (currentJigStateOfCrestedLeader == 3)
                    {
                        // crestedLeader.CreateSimplyLeader(crestedLeader.EndPoint);

                        // Сохранение точки начала полки
                        crestedLeader.ShelfStartPoint = crestedLeader.EndPoint;

                        // Включение режима указания точки отступа полки как текущего
                        crestedLeader.CurrentJigState = 4; // 4

                    }
                    // Если текущий режим указания точек для смарт-объекта - указание точки отступа полки
                    else if (currentJigStateOfCrestedLeader == 4) // currentJigState == 4
                    {
                        // crestedLeader.CreateSimplyLeader(crestedLeader.EndPoint);

                        // Сохранение точки отступа полки
                        shelfLedgePoint = crestedLeader.EndPoint;
                        //crestedLeader.ShelfLedgePoint = shelfLedgePoint;

                        // Отключение режима указания точек 
                        crestedLeader.CurrentJigState = 0;

                        // Новая точка вставки (точка начала крайней справа выноски)
                        if (crestedLeader.LeaderStartPoints != null)
                        {
                            crestedLeader.InsertionPoint = crestedLeader.LeaderStartPoints.Last();
                        }

                        crestedLeader.ShelfLedge = (crestedLeader.EndPoint - crestedLeader.ShelfStartPoint).Length;

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
            crestedLeader.LeaderEndPoints.AddRange(leaderPoints);
            crestedLeader.ShelfLedgePoint = shelfLedgePoint;
            crestedLeader.ShelfStartPoint = crestedLeader.InsertionPoint;

            //crestedLeader.LeaderPointsPreviousForGripMove.AddRange(jig1LeaderPoints);
            crestedLeader.ShelfLedgePointPreviousForGripMove = shelfLedgePoint;

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