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
    public static void ToLogAnyString(this ISmartEntity smart, string str)
    {
        Loggerq.WriteRecord($"smart: {smart.GetType().Name}, str: {str}");
    }

    public static void ToLogErr(this ISmartEntity smart, string className, string metodName, Exception exception)
    {
        Loggerq.WriteRecord($"smart: {smart.GetType().Name}, class: {className}, metod: {metodName} ERROR = {exception.StackTrace}");
    }
}