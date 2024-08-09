using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Navigation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using mpESKD;
using mpESKD.Base;
using mpESKD.Base.Attributes;
using mpESKD.Base.Enums;
using mpESKD.Base.Utils;
using mpESKD.Functions.mpCrestedLeader;

namespace TestFunctions
{
    public interface IJigProps
    {
        public int CurrentJigState { get; set; }

        public void CrestedSimplyLeaderMonitor(object sender, PointMonitorEventArgs pointMonitorEventArgs);

        public void CreateSimplyLeader(Point3d jig1InsertionPoint);


        public List<Point3d> LeaderPointsOCS { get;}

        public Point3d ShelfIndentPointOCS { get; }

        public Point3d ShelfStartPointOCS { get; }

        public List<Point3d> LeaderPoints { get; set; }

        public Point3d ShelfStartPoint { get; set; }

        public Point3d ShelfIndentPoint { get; set; }

        public Point3d ShelfIndentPointPreviousForGripMove { get; set; }

        public List<Point3d> LeaderPointsPreviousForGripMove { get; set; }
    }

    public static class TestJig
    {
        public static void InsertTestWithJig<T>(T entity, BlockReference blockReference)
            where T : SmartEntity, IJigProps
        {
            Loggerq.WriteRecord("*");
            Loggerq.WriteRecord("**");
            Loggerq.WriteRecord($"TestJig; InsertTestWithJig; start");


            if (entity is SmartEntity)
            {
                //var stateThisJig = 1;
                Point3d? jig1InsertionPoint = null;
                List<Point3d> jig2NextPoints = new();
                Point3d? jig1ShelfStartPoint = null;
                Point3d? jig1ShelfEndPoint = null;

                var entityJig1 = new DefaultEntityJig(
                    entity,
                    blockReference,
                    new Point3d(20, 0, 0));

                do
                {
                    var status1 = AcadUtils.Editor.Drag(entityJig1).Status;

                    // Это именно режим указания точек для смарт-объекта - не путать с режимом самого JIG
                    var currentJigState = entity.CurrentJigState;

                    if (status1 == PromptStatus.OK)
                    {
                        Loggerq.WriteRecord($"TestJig; InsertTestWithJig; CLICK !!!");
                        //Loggerq.WriteRecord($"TestJig; InsertTestWithJig; entityJig1.PromptStatus==OK: {status1.ToString()}");
                        //Loggerq.WriteRecord($"TestJig; InsertTestWithJig; entityJig1 {GetJigState(entityJig1)}");

                        if (entityJig1.JigState == JigState.PromptInsertPoint)
                        {
                            jig1InsertionPoint = entity.InsertionPoint;

                            // задан текущий режим JIG как режим NextPoint
                            entityJig1.JigState = JigState.PromptNextPoint;
                            entity.CurrentJigState = 2; // 2
                        }
                        // если режим JIG - это NextPoint
                        else if (entityJig1.JigState == JigState.PromptNextPoint)
                        {
                            // если текущий режим указания точек для смарт-объекта  - указание точек выносок
                            if (currentJigState == 2)
                            {
                                // сохранение точки выноски
                                jig2NextPoints.Add(entity.EndPoint);
                            }
                            // если текущий режим указания точек для смарт-объекта  - указание первой точки полки
                            else if (currentJigState == 3)
                            {
                                // сохранение точки начала полки
                                jig1ShelfStartPoint = entity.EndPoint;
                                // включение режима указания точки отступа полки как текущего
                                entity.CurrentJigState = 4; // 4
                            }
                            // если текущий режим указания точек для смарт-объекта  -  указание точки отступа полки
                            else // currentJigState == 4
                            {
                                // сохранение точки отступа полки
                                jig1ShelfEndPoint = entity.EndPoint;
                                break;
                            }
                        }
                    }
                    else if (status1 == PromptStatus.Other)
                    {
                        if (entityJig1.JigState == JigState.PromptNextPoint)
                        {
                            // включение режима указания первой точки полки как текущего
                            entity.CurrentJigState = (int)CrestedLeaderJigState.PromptShelfStartPoint;
                        }
                    }
                    else
                    {
                        Loggerq.WriteRecord(
                            $"TestJig; InsertTestWithJig; entityJig1.PromptStatus!=OK: {status1.ToString()}");
                        Loggerq.WriteRecord($"TestJig; InsertTestWithJig; entityJig1 {GetJigState(entityJig1)}");

                        EntityUtils.Erase(blockReference.Id);
                        break;
                    }
                } while (true);


                if (!entity.BlockId.IsErased)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                    {
                        var ent = tr.GetObject(entity.BlockId, OpenMode.ForWrite, true, true);

                        ent.XData = entity.GetDataForXData();
                        tr.Commit();
                    }
                }


                //Loggerq.WriteRecord($"TestJig; InsertTestWithJig; {jig1Point1.Value}");

                if (jig1InsertionPoint != null)
                    Loggerq.WriteRecord(
                        $"TestJig; InsertTestWithJig; (1) jig1InsertionPoint: {jig1InsertionPoint.Value.ToString()}");
                else
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig; jig1InsertionPoint: NULL");


                Loggerq.WriteRecord($"TestJig; InsertTestWithJig; (2) jig2NextPoints:");
                var i = 1;
                foreach (var jig2NextPoint in jig2NextPoints)
                {
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig; jig2NextPoint[{i}]: {jig2NextPoint.ToString()}");
                    i++;
                }

                if (jig1ShelfStartPoint != null)
                    Loggerq.WriteRecord(
                        $"TestJig; InsertTestWithJig; (3) jig1ShelfStartPoint: {jig1ShelfStartPoint.Value.ToString()}");
                else
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig; (3) jig1ShelfStartPoint: NULL");

                if (jig1ShelfEndPoint != null)
                    Loggerq.WriteRecord(
                        $"TestJig; InsertTestWithJig; (4) jig1ShelfEndPoint: {jig1ShelfEndPoint.Value.ToString()}");
                else
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig; (4) jig1ShelfEndPoint: NULL");
            }


            Loggerq.WriteRecord($"TestJig; InsertTestWithJig; end");
        }

        public static void InsertTestWithJig2<T>(T entity, BlockReference blockReference)
            where T : SmartEntity, IJigProps
        {
            Loggerq.WriteRecord("*");
            Loggerq.WriteRecord("**");
            Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; start");

            if (entity is SmartEntity)
            {
                List<Point3d> jig1LeaderPoints = new();
                Point3d jig1ShelfIndentPoint = new();

                var entityJig1 = new DefaultEntityJig(
                    entity,
                    blockReference,
                    new Point3d(20, 0, 0));

                do
                {
                    var status1 = AcadUtils.Editor.Drag(entityJig1).Status;

                    // Это именно режим указания точек для смарт-объекта - не путать с режимом самого JIG
                    var currentJigState = entity.CurrentJigState;

                    if (status1 == PromptStatus.OK)
                    {
                        //Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; CLICK !!!");
                        //Loggerq.WriteRecord($"TestJig; InsertTestWithJig; entityJig1.PromptStatus==OK: {status1.ToString()}");
                        //Loggerq.WriteRecord($"TestJig; InsertTestWithJig; entityJig1 {GetJigState(entityJig1)}");

                        if (entityJig1.JigState == JigState.PromptInsertPoint)
                        {
                            // задан текущий режим JIG как режим NextPoint
                            entityJig1.JigState = JigState.PromptNextPoint;
                            entity.CreateSimplyLeader(entity.InsertionPoint);

                            jig1LeaderPoints.Add(entity.InsertionPoint);

                            entity.CurrentJigState = 2; // 2
                        }
                        // если режим JIG - это NextPoint
                        else if (entityJig1.JigState == JigState.PromptNextPoint)
                        {
                            // если текущий режим указания точек для смарт-объекта  - указание точек выносок
                            if (currentJigState == 2)
                            {
                                // сохранение точки выноски
                                jig1LeaderPoints.Add(entity.EndPoint);

                                entity.CreateSimplyLeader(entity.EndPoint);
                            }
                            // если текущий режим указания точек для смарт-объекта  - указание первой точки полки
                            else if (currentJigState == 3)
                            {
                                // сохранение точки начала полки
                                entity.ShelfStartPoint = entity.EndPoint;

                                // включение режима указания точки отступа полки как текущего
                                entity.CurrentJigState = 4; // 4
                            }
                            // если текущий режим указания точек для смарт-объекта  -  указание точки отступа полки
                            else if (currentJigState == 4) // currentJigState == 4
                            {
                                Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; currentJigState == 4)");
                                // сохранение точки отступа полки
                                jig1ShelfIndentPoint = entity.EndPoint;

                                // отключение режима указания точек 
                                entity.CurrentJigState = 0;
                                entity.InsertionPoint = entity.ShelfStartPoint;

                                entity.UpdateEntities();
                                entity.BlockRecord.UpdateAnonymousBlocks();

                                break;
                            }
                        }
                    }
                    else if (status1 == PromptStatus.Other)
                    {
                        if (entityJig1.JigState == JigState.PromptNextPoint)
                        {
                            // включение режима указания первой точки полки как текущего
                            entity.CurrentJigState = 3; // (int)CrestedLeaderJigState.PromptShelfStartPoint;
                        }
                    }
                    else
                    {
                        EntityUtils.Erase(blockReference.Id);
                        break;
                    }
                } while (true);


                if (!entity.BlockId.IsErased)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                    {
                        var ent = tr.GetObject(entity.BlockId, OpenMode.ForWrite, true, true);

                        // перемещение точки вставки в точку первой точки полки
                        ((BlockReference)ent).Position = entity.InsertionPoint;

                        ent.XData = entity.GetDataForXData();
                        tr.Commit();
                    }

                    // Точки выносок
                    entity.LeaderPoints.AddRange(jig1LeaderPoints);
                    // Точка отступа полки
                    entity.ShelfIndentPoint = jig1ShelfIndentPoint;

                    // Точки выносок для их прорисовки при смещения при перетаскивании за базовую точку 
                    // в GripPointOverrule MoveGripPointsAt :
                    /*
                      if (moveGrip.GripIndex == 0)
                        {
                            moveGrip.NewPoint = moveGrip.GripPoint + offset;

                            crestedLeader.ShelfLedgePoint = crestedLeader.ShelfLedgePointPreviousForGripMove + offset;

                            crestedLeader.LeaderEndPoints = crestedLeader
                                .LeaderPointsPreviousForGripMove.Select(x => x + offset)
                                .ToList();

                            var pos = moveGrip.GripPoint + offset;
                            ((BlockReference)entity).Position = pos;
                            crestedLeader.InsertionPoint = pos;
                        }

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                    */
                    // в Grip добавить  свойство 'public Point3d NewPoint { get; set; }'
                    // в Grip в OnGripStatusChanged добавить
                    /*
                        if (newStatus == Status.GripEnd)
                            {
                                var offset = NewPoint - _gripTmp;
                                CrestedLeader.ShelfLedgePointPreviousForGripMove += offset;

                                CrestedLeader.LeaderPointsPreviousForGripMove = CrestedLeader.LeaderPointsPreviousForGripMove
                                    .Select(x => x + offset)
                                    .ToList();

                                CrestedLeader.UpdateEntities();
                                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

                                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                                {
                                    var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);
                                    using (var resBuf = CrestedLeader.GetDataForXData())
                                    {
                                        blkRef.XData = resBuf;
                                    }

                                    tr.Commit();
                                }

                                CrestedLeader.Dispose();
                            }
                    entity.LeaderPointsPreviousForGripMove.AddRange(jig1LeaderPoints);
                    */
                    // Точка отступа полки для ее прорисовки при смещения при перетаскивании за базовую точку 
                    entity.ShelfIndentPointPreviousForGripMove = jig1ShelfIndentPoint;

                    entity.UpdateEntities();
                    entity.BlockRecord.UpdateAnonymousBlocks();

                    using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                    {
                        var ent = tr.GetObject(entity.BlockId, OpenMode.ForWrite, true, true);

                        ent.XData = entity.GetDataForXData();
                        tr.Commit();
                    }
                }

                #region LOG

                /*
                //Loggerq.WriteRecord($"TestJig; InsertTestWithJig; {jig1Point1.Value}");

                if (jig1InsertionPoint != null)
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; (1) jig1InsertionPoint: {jig1InsertionPoint.Value.ToString()}");
                else
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; jig1InsertionPoint: NULL");


                Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; (2) jig1LeaderPointsOcs:");
                var i = 1;
                foreach (var jig1LeaderPointOcs in jig1LeaderPointsOcs)
                {
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; jig1LeaderPoint[{i}]: {jig1LeaderPointOcs.ToString()}");
                    i++;
                }

                if (jig1ShelfStartPoint != null)
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; (3) jig1ShelfStartPoint: {jig1ShelfStartPoint.Value.ToString()}");
                else
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; (3) jig1ShelfStartPoint: NULL");

                if (jig1ShelfEndPoint != null)
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; (4) jig1ShelfEndPoint: {jig1ShelfEndPoint.Value.ToString()}");
                else
                    Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; (4) jig1ShelfEndPoint: NULL");
                */

                #endregion
            }

            Loggerq.WriteRecord($"TestJig; InsertTestWithJig2; end");
        }

        private static string GetJigState(this DefaultEntityJig jig)
        {
            string result;

            var state = jig.JigState;
            var stateString = string.Empty;

            switch (state)
            {
                case JigState.PromptInsertPoint:
                    stateString = "PromptInsertPoint";
                    break;
                case JigState.PromptNextPoint:
                    stateString = "PromptNextLeaderPoint";
                    break;
                case JigState.CustomPoint:
                    stateString = "PromptShelfStartPoint";
                    break;
            }

            var prevPoint = jig.PreviousPoint;
            var prevPointString = "NULL";

            if (prevPoint != null)
            {
                prevPointString = $"({prevPoint.Value.X} {prevPoint.Value.Y})";
            }

            result = $"[State: {stateString}; PreviousPoint: {prevPointString}]";

            return result;
        }
    }
}