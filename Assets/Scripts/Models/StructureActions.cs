using UnityEngine;
using System.Collections;

public static class StructureActions
{
    public static void Door_UpdateAction(Structure structure, float deltaAuts)
    {
        if (structure.GetParameter("isOpening") >= 1)
        {
            //Debug.Log("Door_UpdateAction: " + structure.GetParameter("openness") + ", deltaAuts: " + deltaAuts);
            structure.ChangeParameter("openness", deltaAuts);
            if (structure.GetParameter("openness") >= structure.GetParameter("doorOpenTime"))
            {
                structure.SetParameter("isOpening", 0);
            }
        }
        else
        {
            structure.ChangeParameter("openness", -deltaAuts);
        }

        structure.SetParameter("openness", Mathf.Clamp(structure.GetParameter("openness"), 0,
            structure.GetParameter("doorOpenTime")));

        structure.cbOnChanged(structure);
    }

    public static Enterability Door_IsEnterable(Structure structure)
    {
        //Debug.Log("Door_IsEnterable");
        structure.SetParameter("isOpening", 1);

        if (structure.GetParameter("openness") >= structure.GetParameter("doorOpenTime"))
        {
            return Enterability.Yes;
        }

        return Enterability.Soon;
    }

    public static void JobComplete_StructureBuilding(Job theJob)
    {
        WorldController.Instance.World.PlaceStructure(theJob.jobObjectType, theJob.Tile);
        theJob.Tile.PendingStructureJob = null;
    }
}