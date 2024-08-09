using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using System;
using System.Runtime.InteropServices;

namespace MirroringSample
{
    // inspired by: https://adndevblog.typepad.com/autocad/2013/10/mirroring-a-dbtext-entity.html
    // https://forums.autodesk.com/t5/net/is-mirroring-an-mtext-with-c-possible/m-p/10124481#M68060
    public static class MirroringExtension
    {
        [DllImport("accore.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTextBox")]
        static extern System.IntPtr acedTextBox(IntPtr rb, double[] point1, double[] point2);

        /// <summary>
        /// Gets the WCS points of the text bounding box.
        /// </summary>
        /// <param name="dbText">Instance of DBText the method applies to.</param>
        /// <returns>The bounding box points in counterclockwise sense.</returns>
        public static Point3d[] GetTextBoxCorners(this DBText dbText)
        {
            if (dbText == null)
                throw new ArgumentNullException("dbText");

            int mirrored = dbText.IsMirroredInX ? 2 : 0;
            mirrored |= dbText.IsMirroredInY ? 4 : 0;
            var rb = new ResultBuffer(
                    new TypedValue(1, dbText.TextString),
                    new TypedValue(40, dbText.Height),
                    new TypedValue(41, dbText.WidthFactor),
                    new TypedValue(51, dbText.Oblique),
                    new TypedValue(7, dbText.TextStyleName),
                    new TypedValue(71, mirrored),
                    new TypedValue(72, (int)dbText.HorizontalMode),
                    new TypedValue(73, (int)dbText.VerticalMode));

            var point1 = new double[3];
            var point2 = new double[3];

            acedTextBox(rb.UnmanagedObject, point1, point2);

            var xform =
                Matrix3d.Displacement(dbText.Position.GetAsVector()) *
                Matrix3d.Rotation(dbText.Rotation, dbText.Normal, Point3d.Origin) *
                Matrix3d.PlaneToWorld(new Plane(Point3d.Origin, dbText.Normal));

            return new[]
            {
                new Point3d(point1).TransformBy(xform),
                new Point3d(point2[0], point1[1], 0.0).TransformBy(xform),
                new Point3d(point2).TransformBy(xform),
                new Point3d(point1[0], point2[1], 0.0).TransformBy(xform)
            };
        }

        /// <summary>
        /// Gets the WCS points of the text bounding box.
        /// </summary>
        /// <param name="mtext">Instance of DBText the method applies to.</param>
        /// <returns>The bounding box points in counterclockwise sense.</returns>
        public static Point3d[] GetMTextBoxCorners(this MText mtext)
        {
            double width = mtext.ActualWidth;
            double height = mtext.ActualHeight;
            Point3d point1, point2;
            switch (mtext.Attachment)
            {
                case AttachmentPoint.TopLeft:
                default:
                    point1 = new Point3d(0.0, -height, 0.0);
                    point2 = new Point3d(width, 0.0, 0.0);
                    break;
                case AttachmentPoint.TopCenter:
                    point1 = new Point3d(-width * 0.5, -height, 0.0);
                    point2 = new Point3d(width * 0.5, 0.0, 0.0);
                    break;
                case AttachmentPoint.TopRight:
                    point1 = new Point3d(-width, -height, 0.0);
                    point2 = new Point3d(0.0, 0.0, 0.0);
                    break;
                case AttachmentPoint.MiddleLeft:
                    point1 = new Point3d(0.0, -height * 0.5, 0.0);
                    point2 = new Point3d(width, height * 0.5, 0.0);
                    break;
                case AttachmentPoint.MiddleCenter:
                    point1 = new Point3d(-width * 0.5, -height * 0.5, 0.0);
                    point2 = new Point3d(width * 0.5, height * 0.5, 0.0);
                    break;
                case AttachmentPoint.MiddleRight:
                    point1 = new Point3d(-width, -height * 0.5, 0.0);
                    point2 = new Point3d(0.0, height * 0.5, 0.0);
                    break;
                case AttachmentPoint.BottomLeft:
                    point1 = new Point3d(0.0, 0.0, 0.0);
                    point2 = new Point3d(width, height, 0.0);
                    break;
                case AttachmentPoint.BottomCenter:
                    point1 = new Point3d(-width * 0.5, 0.0, 0.0);
                    point2 = new Point3d(width * 0.5, height, 0.0);
                    break;
                case AttachmentPoint.BottomRight:
                    point1 = new Point3d(-width, 0.0, 0.0);
                    point2 = new Point3d(0.0, height, 0.0);
                    break;
            }

            var xform =
                Matrix3d.Displacement(mtext.Location.GetAsVector()) *
                Matrix3d.Rotation(mtext.Rotation, mtext.Normal, Point3d.Origin) *
                Matrix3d.PlaneToWorld(new Plane(Point3d.Origin, mtext.Normal));

            return new[]
            {
                point1.TransformBy(xform),
                new Point3d(point2.X, point1.Y, 0.0).TransformBy(xform),
                point2.TransformBy(xform),
                new Point3d(point1.X, point2.Y, 0.0).TransformBy(xform)
            };
        }

        /// <summary>
        /// Mirrors the entity (honoring the value of MIRRTEXT system variable).
        /// </summary>
        /// <param name="source">Instance of DBText the method applies to.</param>
        /// <param name="axis">Mirror symmetry line.</param>
        /// <param name="eraseSource">Value indicating if the source object have to be erased</param>
        public static Entity Mirror(this Entity source, Line3d axis, bool eraseSource)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (axis == null)
                throw new ArgumentNullException("axis");

            var db = source.Database;
            var tr = db.TransactionManager.TopTransaction;
            if (tr == null)
                throw new Autodesk.AutoCAD.Runtime.Exception(ErrorStatus.NoActiveTransactions);

            Entity mirrored;
            if (eraseSource)
            {
                mirrored = source;
                if (!mirrored.IsWriteEnabled)
                {
                    tr.GetObject(mirrored.ObjectId, OpenMode.ForWrite);
                }
            }
            else
            {
                var ids = new ObjectIdCollection(new[] { source.ObjectId });
                var mapping = new IdMapping();
                db.DeepCloneObjects(ids, db.CurrentSpaceId, mapping, false);
                mirrored = (Entity)tr.GetObject(mapping[source.ObjectId].Value, OpenMode.ForWrite);
            }
            mirrored.TransformBy(Matrix3d.Mirroring(axis));

            // Honoring the MIRRTEXT sysvar with DBText, MText and AttributeReference.
            if ((short)Application.GetSystemVariable("MIRRTEXT") == 0)
            {
                if (mirrored is DBText)
                {
                    var pts = ((DBText)mirrored).GetTextBoxCorners();
                    var cen = new LineSegment3d(pts[0], pts[2]).MidPoint;
                    var rotAxis = Math.Abs(axis.Direction.X) < Math.Abs(axis.Direction.Y) ?
                        pts[0].GetVectorTo(pts[3]) :
                        pts[0].GetVectorTo(pts[1]);
                    mirrored.TransformBy(Matrix3d.Rotation(Math.PI, rotAxis, cen));
                }
                else if (mirrored is MText)
                {
                    var pts = ((MText)mirrored).GetMTextBoxCorners();
                    var cen = new LineSegment3d(pts[0], pts[2]).MidPoint;
                    var rotAxis = Math.Abs(axis.Direction.X) < Math.Abs(axis.Direction.Y) ?
                        pts[0].GetVectorTo(pts[3]) :
                        pts[0].GetVectorTo(pts[1]);
                    mirrored.TransformBy(Matrix3d.Rotation(Math.PI, rotAxis, cen));
                }
                else if (mirrored is BlockReference)
                {
                    foreach (ObjectId id in ((BlockReference)mirrored).AttributeCollection)
                    {
                        var attRef = (AttributeReference)tr.GetObject(id, OpenMode.ForWrite);
                        var pts = attRef.GetTextBoxCorners();
                        var cen = new LineSegment3d(pts[0], pts[2]).MidPoint;
                        var rotAxis = Math.Abs(axis.Direction.Y) > Math.Abs(axis.Direction.X) ?
                            pts[0].GetVectorTo(pts[3]) :
                            pts[0].GetVectorTo(pts[1]);
                        attRef.TransformBy(Matrix3d.Rotation(Math.PI, rotAxis, cen));
                    }
                }
            }
            return mirrored;
        }
    }
}