using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Class)]
public class OrderActionNameAttribute : Attribute
{
    public readonly string OrderActionName;

    public OrderActionNameAttribute(string orderActionName)
    {
        this.OrderActionName = orderActionName;
    }
}