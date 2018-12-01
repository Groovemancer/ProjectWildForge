using UnityEngine;
using System.Collections;

public static class StructureActions
{
    public static void Door_UpdateAction(Structure structure, float deltaAuts)
    {
        if (structure.structureParameters["isOpening"] >= 1)
        {
            Debug.Log("Door_UpdateAction: " + structure.structureParameters["openness"] + ", deltaAuts: " + deltaAuts);
            structure.structureParameters["openness"] += deltaAuts;
            if (structure.structureParameters["openness"] >= structure.structureParameters["doorOpenTime"])
            {
                structure.structureParameters["isOpening"] = 0;
            }
        }
        else
        {
            structure.structureParameters["openness"] -= deltaAuts;
        }

        structure.structureParameters["openness"] = Mathf.Clamp(structure.structureParameters["openness"], 0,
            structure.structureParameters["doorOpenTime"]);
    }

    public static Enterability Door_IsEnterable(Structure structure)
    {
        Debug.Log("Door_IsEnterable");
        structure.structureParameters["isOpening"] = 1;

        if (structure.structureParameters["openness"] >= structure.structureParameters["doorOpenTime"])
        {
            return Enterability.Yes;
        }

        return Enterability.Soon;
    }
}