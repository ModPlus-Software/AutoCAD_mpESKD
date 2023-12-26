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

        private SamplerStatus Acquire(
            JigPrompts prompts, 
            JigPromptPointOptions options,
            Action<Point3d> updater)
        {
            var promptPointResult = prompts.AcquirePoint(options);
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

            Value = promptPointResult.Value;

            updater(Value);
            return SamplerStatus.OK;
        }

        private static JigPromptPointOptions GetDefaultOptions(string message)
        {
            var jigPromptPointOption = new JigPromptPointOptions(message)
            {
                UserInputControls = (UserInputControls)2272
            };
            return jigPromptPointOption;
        }

        private static JigPromptPointOptions GetDefaultOptions(string message, Point3d basePoint)
        {
            return new JigPromptPointOptions(message)
            {
                BasePoint = basePoint,
                UseBasePoint = true,
                UserInputControls = UserInputControls.GovernedByUCSDetect |
                                    UserInputControls.GovernedByOrthoMode |
                                    UserInputControls.NoDwgLimitsChecking |
                                    UserInputControls.NoNegativeResponseAccepted |
                                    UserInputControls.Accept3dCoordinates |
                                    UserInputControls.AcceptOtherInputString |
                                    UserInputControls.UseBasePointElevation
            };
        }
    }
}