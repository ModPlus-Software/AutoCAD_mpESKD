using mpESKD.Base.Utils;

namespace mpESKD;

using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Base.Abstractions;
using System;
using System.IO;

public static class Loggerq
{
    private const string PathLog = @"d:\mpCrestedLeader.log";

    public static void WriteRecord(string message)
    {
        if (!string.IsNullOrEmpty(PathLog) && !string.IsNullOrEmpty(message))
        {
            using (var logger = new StreamWriter(PathLog, true))
            {
                logger.WriteLine(DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString() + " - " +
                                 message);
            }
        }
    }

    public static void DeleteFile()
    {
        if (File.Exists(PathLog))
        {
            new FileInfo(PathLog).Delete();
        }
    }

    public static void ClearFile()
    {
        if (File.Exists(PathLog))
        {
            FileStream fs = File.Open(PathLog, FileMode.Open, FileAccess.ReadWrite);
            fs.SetLength(0);
            fs.Close();
            fs.Dispose();
        }
    }
}

public static class LogData
{
    public static void ToLogAnyString(this ISmartEntity smart, string str = "")
    {
        //Loggerq.WriteRecord($"smart: {smart.GetType().Name}, mess: {str}");
        Loggerq.WriteRecord($"{str}");
    }

    public static void ToLog(this  List<Point3d> points,  string listName = "")
    {
        Loggerq.WriteRecord($"\nList 3d points of {listName}:");
        Loggerq.WriteRecord("{");
        for (int i = 0; i < points.Count; i++)
        {
            var x = Math.Round(points[i].X, 3, MidpointRounding.AwayFromZero);
            var y = Math.Round(points[i].Y, 3, MidpointRounding.AwayFromZero);
            var z = Math.Round(points[i].Z, 3, MidpointRounding.AwayFromZero);

            Loggerq.WriteRecord($"  [{i}]: {x} , {y} , {z}");
        }
        Loggerq.WriteRecord("}\n");
    }

    public static void ToLogErr(this ISmartEntity smart, string className, string metodName, Exception exception)
    {
        Loggerq.WriteRecord($"\nERROR (!) \nclass: {className}, metod: {metodName}" +
                            $"\nTargetSite: {exception.TargetSite}" +
                            $"\nStackTrace: {exception.StackTrace}" +
                            $"\nData: {exception.Data}\n"
                            );
    }


    public static void ToLog(this Point3d point, string pointName)
    {
        var spLim = 25;
        // Console.WriteLine("{0,-20} {1,5:N1}", names[counter], hours[counter]);

        var x = Math.Round(point.X, 3, MidpointRounding.AwayFromZero);
        var y = Math.Round(point.Y, 3, MidpointRounding.AwayFromZero);

        var name = pointName + ":";
        var pt = $"{x} , {y}";

        // var str = $"{name,-50} {pt,0}";

        var sp = "";
        var spCount = spLim - name.Length;
        if (spCount > 0)
        {
            sp = new string(' ', spCount);
        }


        var str = $"{name}{sp}{pt}";
        Loggerq.WriteRecord(str);
    }

    public static void ToLog(this Point2d point, string pointName)
    {
        point.ToPoint3d().ToLog(pointName);
    }

}