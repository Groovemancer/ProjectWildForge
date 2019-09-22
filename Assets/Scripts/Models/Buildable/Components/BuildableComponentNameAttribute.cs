using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Class)]
public class BuildableComponentNameAttribute : Attribute
{
    public readonly string ComponentName;

    public BuildableComponentNameAttribute(string componentName)
    {
        this.ComponentName = componentName;
    }
}
