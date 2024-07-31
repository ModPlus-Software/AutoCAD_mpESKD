using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Autodesk.AutoCAD.Geometry;
using mpESKD.Base.Abstractions;

namespace mpESKD;

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
            (new FileInfo(PathLog)).Delete();
        }
    }
}

public static class LogData
{
    public static void ToLogAnyString(this ISmartEntity smart, string str = "")
    {
        Loggerq.WriteRecord($"smart: {smart.GetType().Name}, mess: {str}");
    }

    public static void ToLogAnyStringFromPoint3dList(this ISmartEntity smart, List<Point3d> points,  string str = "")
    {
        Loggerq.WriteRecord($"\nsmart: {smart.GetType().Name}, List of 3d points:");
        Loggerq.WriteRecord("{");
        for (int i = 0; i < points.Count; i++)
        {
            Loggerq.WriteRecord($"[{i}]: {points[i].ToString()}");
        }
        Loggerq.WriteRecord("}\n");
    }

    public static void ToLogErr(this ISmartEntity smart, string className, string metodName, Exception exception)
    {
        Loggerq.WriteRecord($"smart: {smart.GetType().Name}, class: {className}, metod: {metodName} ERROR = {exception.StackTrace}");
    }
}