namespace mpESKD.Base.Utils;

using System;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

/// <summary>
/// Утилиты работы с <see cref="Jig"/>
/// </summary>
public class JigUtils
{
    public class PointSampler
    {
        private static readonly Tolerance Tolerance;

        public Point3d Value { get; set; }

        static PointSampler()
        {
            Tolerance = new Tolerance(1E-6, 1E-6);
        }

        public PointSampler(Point3d value)
        {
            Value = value;
        }

        public SamplerStatus Acquire(JigPrompts prompts, string message, Action<Point3d> updater)
        {
            return Acquire(prompts, GetDefaultOptions(message), updater);
        }

        public SamplerStatus Acquire(JigPrompts prompts, string message, Point3d basePoint, Action<Point3d> updater)
        {
            return Acquire(prompts, GetDefaultOptions(message, basePoint), updater);
        }

        public SamplerStatus Acquire(JigPrompts prompts, JigPromptPointOptions options, Action<Point3d> updater)
        {
            PromptPointResult promptPointResult = prompts.AcquirePoint(options);
            if (promptPointResult.Status != PromptStatus.OK)
            {
                if (promptPointResult.Status == PromptStatus.Other)
                {
                    return SamplerStatus.OK;
                }

                return SamplerStatus.Cancel;
            }

            if (Value.IsEqualTo(promptPointResult.Value, Tolerance))
            {
                return SamplerStatus.NoChange;
            }

            var value = promptPointResult.Value;
            var point3D = value;
            Value = value;
            updater(point3D);
            return SamplerStatus.OK;
        }

        public static JigPromptPointOptions GetDefaultOptions(string message)
        {
            var jigPromptPointOption = new JigPromptPointOptions(message);
            jigPromptPointOption.UserInputControls = (UserInputControls)2272;
            return jigPromptPointOption;
        }

        public static JigPromptPointOptions GetDefaultOptions(string message, Point3d basePoint)
        {
            var jigPromptPointOption = new JigPromptPointOptions(message);
            jigPromptPointOption.BasePoint = basePoint;
            jigPromptPointOption.UseBasePoint = true;
            jigPromptPointOption.UserInputControls =
                UserInputControls.GovernedByUCSDetect |
                UserInputControls.GovernedByOrthoMode |
                UserInputControls.NoDwgLimitsChecking |
                UserInputControls.NoNegativeResponseAccepted |
                UserInputControls.Accept3dCoordinates |
                UserInputControls.AcceptOtherInputString |
                UserInputControls.UseBasePointElevation;
            return jigPromptPointOption;
        }
    }
}