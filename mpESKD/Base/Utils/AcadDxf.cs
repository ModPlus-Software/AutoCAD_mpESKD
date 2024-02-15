namespace mpESKD.Base.Utils;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using AcRx = Autodesk.AutoCAD.Runtime;
using AcEd = Autodesk.AutoCAD.EditorInput;
using AcDb = Autodesk.AutoCAD.DatabaseServices;
using AcAp = Autodesk.AutoCAD.ApplicationServices;

// https://www.caduser.ru/forum/topic35374.html
public static class AcadDxf
{
    static public List<(string, string)> DXFGet(ObjectId objectId)
    {
        if (objectId != null)
        {
            AcDb.ResultBuffer dxflist = AcadImport2.EntGet(objectId);
            if (dxflist != null)
            {
                List<(string, string)> result = new ();

                foreach (TypedValue val in dxflist)
                {
                    var dxfTuple = (val.TypeCode.ToString(), val.Value.ToString());
                    result.Add(dxfTuple);
                }

                return result;
            }

            return null;
        }

        return null;
    }

    [CommandMethod("DXFGet")]
    static public void DXFGet()
    {
        AcEd.Editor ed = AcAp.Application.DocumentManager.MdiActiveDocument.Editor;
        AcEd.PromptEntityOptions entityOpts = new AcEd.PromptEntityOptions("\nSelect entity: ");
        AcEd.PromptEntityResult rc = ed.GetEntity(entityOpts);
        if (rc.Status == AcEd.PromptStatus.OK)
        {
            AcDb.ResultBuffer dxflist = AcadImport.EntGet(rc.ObjectId);
            if (dxflist != null)
            {
                foreach (TypedValue val in dxflist)
                {
                    ed.WriteMessage("\n({0} . {1})", val.TypeCode, val.Value.ToString());
                }
            }
        }
    }
}

// Рабочий
// https://stackoverflow.com/questions/77870236/autocad-net-customization-is-there-an-entget-equivalent-for-net
internal static class AcadImport
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    [DllImport("acdb24.dll", CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "?acdbGetAdsName@@YA?AW4ErrorStatus@Acad@@AEAY01_JVAcDbObjectId@@@Z")]
    static extern AcRx.ErrorStatus acdbGetAdsName(out AdsName ename, ObjectId id);

    [System.Security.SuppressUnmanagedCodeSecurity]
    [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acdbEntGet")]
    static extern IntPtr acdbEntGet(AdsName ename);

    internal static ResultBuffer EntGet(this ObjectId id)
    {
        var errorStatus = acdbGetAdsName(out AdsName ename, id);
        if (errorStatus != AcRx.ErrorStatus.OK)
            throw new AcRx.Exception(errorStatus);
        var result = acdbEntGet(ename);
        if (result != IntPtr.Zero)
            return ResultBuffer.Create(result, true);
        return null;
    }
}

// Рабочий
// https://stackoverflow.com/questions/77870236/autocad-net-customization-is-there-an-entget-equivalent-for-net
internal static class AcadImport2
{
    [DllImport("acdb24.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?acdbGetAdsName@@YA?AW4ErrorStatus@Acad@@AEAY01_JVAcDbObjectId@@@Z")]
    extern static  ErrorStatus acdbGetAdsName(out Int64 entres, ObjectId id);

    [DllImport("acCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acdbEntGet")]
    extern static IntPtr acdbEntGet(out Int64 e);

    internal static ResultBuffer EntGet(this ObjectId id)
    {
        Int64 e = new Int64();
        IntPtr p = new IntPtr();
        if (acdbGetAdsName(out e, id) == ErrorStatus.OK)
        {
            p = acdbEntGet(out e);
            ResultBuffer rb = ResultBuffer.Create(p, true);
            return rb;
        }

        return null;
    }
}