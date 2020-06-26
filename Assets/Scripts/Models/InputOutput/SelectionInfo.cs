using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SelectionInfo
{
    private List<ISelectable> stuffInTile;
    private int selectedIndex = 0;

    public SelectionInfo(Tile t)
    {
        Tile = t;

        BuildStuffInTile();
        SelectFirstStuff();
    }

    /// <summary>
    /// Returns true if the <see cref="Tile.Type"/> isn't empty or if there is stuff other than just the empty tile.
    /// </summary>
    public bool StuffInTile
    {
        get
        {
            // If we have more than just one (since Tile will always exist in the list and more than one refers to having a character/whatever also in there) then return true
            // Else only return true if TileType isn't empty since otherwise its just a vacuum space tile thingy
            return stuffInTile.Count > 1 || Tile.Type != TileTypeData.Instance.EmptyType;
        }
    }

    public Tile Tile { get; protected set; }

    public void BuildStuffInTile()
    {
        // Make sure stuffInTile is big enough to handle all the characters, plus the 3 extra values.
        stuffInTile = new List<ISelectable>();

        // Copy the character references.
        for (int i = 0; i < Tile.Actors.Count; i++)
        {
            stuffInTile.Add(Tile.Actors[i]);
        }

        // Now assign references to the other three sub-selections available.
        if (Tile.Structure != null)
        {
            stuffInTile.Add(Tile.Structure);
        }

        if (Tile.Plant != null)
        {
            stuffInTile.Add(Tile.Plant);
        }

        if (Tile.Inventory != null)
        {
            stuffInTile.Add(Tile.Inventory);
        }

        foreach (Job pendingBuildJob in Tile.PendingBuildJobs)
        {
            stuffInTile.Add(pendingBuildJob);
        }

        stuffInTile.Add(Tile);
    }

    public void SelectFirstStuff()
    {
        if (stuffInTile[selectedIndex] == null)
        {
            SelectNextStuff();
        }
    }

    public void SelectNextStuff()
    {
        do
        {
            selectedIndex = (selectedIndex + 1) % stuffInTile.Count;
        }
        while (stuffInTile[selectedIndex] == null);
    }

    public ISelectable GetSelectedStuff()
    {
        return stuffInTile[selectedIndex];
    }

    public bool IsActorSelected()
    {
        return stuffInTile[selectedIndex] is Actor;
    }
}
