namespace mpESKD.Functions.mpConcreteJoint;

using System;
using System.Collections.Generic;
using Base.Abstractions;
using ModPlusAPI;



public class ConcreteJointDescriptor : ISmartEntityDescriptor
{
    Type ISmartEntityDescriptor.EntityType => typeof(ConcreteJoint);

    string ISmartEntityDescriptor.Name => "mpConcreteJoint";

    string ISmartEntityDescriptor.LName => throw new NotImplementedException();

    string ISmartEntityDescriptor.Description => throw new NotImplementedException();

    string ISmartEntityDescriptor.FullDescription => throw new NotImplementedException();

    string ISmartEntityDescriptor.ToolTipHelpImage => throw new NotImplementedException();

    List<string> ISmartEntityDescriptor.SubFunctionsNames => throw new NotImplementedException();

    List<string> ISmartEntityDescriptor.SubFunctionsLNames => throw new NotImplementedException();

    List<string> ISmartEntityDescriptor.SubDescriptions => throw new NotImplementedException();

    List<string> ISmartEntityDescriptor.SubFullDescriptions => throw new NotImplementedException();

    List<string> ISmartEntityDescriptor.SubHelpImages => throw new NotImplementedException();
}

