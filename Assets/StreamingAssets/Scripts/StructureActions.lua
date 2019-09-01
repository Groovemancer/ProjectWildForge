--StructureActions

Enterability_Yes	 = 0
Enterability_Never	 = 1
Enterability_Soon	 = 2

function Clamp01(value)
	if (value > 1) then
		return 1
	elseif (value < 0) then
		return 0
	end
	
	return value
end

function Clamp(value, minVal, maxVal)
	if (value > maxVal) then
		return maxVal
	elseif (value < minVal) then
		return minVal
	end
	
	return value
end

-- OnUpdate_GasGenerator
-- @Params - structure:Structure
-- @Params - deltaAuts:float
function OnUpdate_GasGenerator(structure, deltaAuts)

	if (structure.Tile.Room == nil) then
		return "Structure's room was null."
	end
	
	if (structure.Tile.Room.GetGasAmount("O2") < 0.20) then
		structure.Tile.Room.ChangeGas("O2", 0.003 * deltaAuts)
	end
end

-- OnUpdate_Door
-- @Params - structure:Structure
-- @Params - deltaAuts:float
function OnUpdate_Door(structure, deltaAuts)
	if (structure.GetParameter("isOpening") >= 1) then
		structure.ChangeParameter("openness", deltaAuts)
		if (structure.GetParameter("openness") >= structure.GetParameter("doorOpenTime")) then
			structure.SetParameter("isOpening", 0)
		end
	else
		structure.ChangeParameter("openness", -deltaAuts)
	end
	
	structure.SetParameter("openness", Clamp(structure.GetParameter("openness"), 0,
		structure.GetParameter("doorOpenTime")))
	
	structure.cbOnChanged(structure)
end

-- IsEnterable_Door
-- @Params - structure:Structure
function IsEnterable_Door(structure)

	structure.SetParameter("isOpening", 1)

	if (structure.GetParameter("openness") >= structure.GetParameter("doorOpenTime")) then
		return Enterability_Yes
	end

	return Enterability_Soon
end

function Stockpile_GetItemsFromFilter()

	-- TODO: This should be reading from some kind of UI for this
	-- particular stockpile

	-- Since jobs copy arrays automatically, we could already have
	-- an Inventory[] prepared and just return that (as a sort of example filter)

	return { Inventory.__new("inv_RawStone", 50, 0) }
end

-- OnUpdate_Stockpile
-- @Params - structure:Structure
-- @Params - deltaAuts:float
function OnUpdate_Stockpile(structure, deltaAuts)
	-- We need to ensure that we have a job on the queue
    -- asking for either:
    --  (if we are empty): That ANY loose inventory be brought to us.
    --  (if we have something: Then IF we are still below the max stack size,
    --                          that more of the same should be brought to us.
	--
    -- TODO: This function doesn't need to run each update. Once we get a lot
    -- of structures in a running game, this will run a LOT more than required.
    -- Instead, it only really needs to run whenever:
    --      -- It gets created
    --      -- A good gets delivered (at which point we reset the job)
    --      -- A good gets picked up (at which point we reset the job)
    --      -- The UI's filter of allowed items gets changed

	if (structure.Tile.Inventory ~= nil and structure.Tile.Inventory.StackSize >=
		structure.Tile.Inventory.MaxStackSize) then
		structure.CancelJobs();
	end
	
	-- Maybe we already have a job queued up?
	if (structure.JobCount() > 0) then
		-- Cool, all done.
		return;
	end
	
	-- We currently are NOT full, but we don't have a job either.
	-- Two possibilities: Either we have SOME inventory, or we have NO inventory

	-- Third possibility: Something is WHACK
	if (structure.Tile.Inventory ~= null and structure.Tile.Inventory.stackSize == 0) then
		structure.CancelJobs()
		return "Stockpile has a zero-size stack. This is clearly WRONG!"
	end
	
	-- TODO: In the future, stockpiles -- rather than being a bunch of individual
	-- 1x1 tiles -- should manifest themselves as single, large objects (this
	-- would represent our first and probably only VARIABLE sized structure --
	-- at what happens if there's a "hole" in our stockpile because we have an
	-- actual structure (like a cooking station) installed in the middle of our stockpile?
	-- In any case, once we implement "mega stockpiles", then the job-creation system
	-- could be a lot smarter, in that even if the stockpile has some stuff in it, it
	-- can also still be requesting different object types in its job creation.

	itemsDesired = {}
	
	if (structure.Tile.Inventory == nil) then
		-- Debug.Log("Creating job for new stack.");
		itemsDesired = Stockpile_GetItemsFromFilter()
	else
		-- Debug.Log("Creating job for existing stack.");
		desInv = structure.Tile.Inventory.Clone()
		desInv.MaxStackSize = desInv.MaxStackSize - desInv.StackSize
		desInv.StackSize = 0

		itemsDesired = { desInv }
	end
	
	j = Job.__new(
		structure.Tile,
		nil,
		nil,
		0,
		itemsDesired,
		false
	)
	-- TODO: Later on, add stockpile priorities, so that we can take from a lower
    -- priority stockpile for a higher priority one.
	j.canTakeFromStockpile = false;

	j.RegisterJobWorkedCallback("Stockpile_JobWorked")
	structure.AddJob(j)

end

-- Stockpile_JobWorked
-- @Params - j:Job
function Stockpile_JobWorked(j)
	j.CancelJob()

	-- TODO: Change this when we figure out what we're doing for the all/any pickup job.
	for k, inv in pairs(j.inventoryRequirements) do
		if (inv.StackSize > 0) then
			World.Current.InventoryManager.PlaceInventory(j.Tile, inv)
			return
		end
	end

end

-- OnUpdate_WorkStation
-- @Params - structure:Structure
-- @Params - deltaAuts:float
function OnUpdate_WorkStation(structure, deltaAuts)
	spawnSpot = structure.GetSpawnSpotTile()
	
	if (structure.JobCount() > 0) then
		-- Check to see if the Raw Stone destination tile is full.
		if (spawnSpot.Inventory ~= nil and spawnSpot.Inventory.StackSize >= spawnSpot.Inventory.MaxStackSize) then
			-- We should stop this job, because it's impossible to make any more items.
			structure.CancelJobs()
		end

		return
	end
	
	-- If we get here, then we have no current job. Check to see if our destination is full.
	if (spawnSpot.Inventory ~= nil and spawnSpot.Inventory.StackSize >= spawnSpot.Inventory.MaxStackSize) then
		-- We are full! Don't make a job.
		return
	end
	
	-- If we get here, we need to CREATE a new job.

	jobSpot = structure.GetJobSpotTile()

	if (jobSpot.Inventory ~= nil and (jobSpot.Inventory.StackSize >= jobSpot.Inventory.MaxStackSize)) then
		-- Our drop spot is already full, so don't create a job.
		return
	end
	
	j = Job.__new(
		jobSpot,
		nil,
		nil,
		600,
		nil,
		true    -- This job repeats until the destination tile is full.
	)
	j.RegisterJobCompletedCallback("WorkStation_JobComplete")

	structure.AddJob(j)

end

-- WorkStation_JobComplete
-- @Params - j:Job
function WorkStation_JobComplete(j)
	--Debug.Log("WorkStation_JobComplete");

	World.Current.InventoryManager.PlaceInventory(j.Structure.GetSpawnSpotTile(),
		Inventory.__new("inv_RawStone", 50, 10))
end

-- OnUpdate_StoneCuttingTable
-- @Params - structure:Structure
-- @Params - deltaAuts:float
function OnUpdate_StoneCuttingTable(structure, deltaAuts)
	spawnSpot = structure.GetSpawnSpotTile()
	
	if (structure.JobCount() > 0) then
		-- Check to see if the Raw Stone destination tile is full.
		if (spawnSpot.Inventory ~= nil and spawnSpot.Inventory.StackSize >= spawnSpot.Inventory.MaxStackSize) then
			-- We should stop this job, because it's impossible to make any more items.
			structure.CancelJobs()
		end

		return
	end
	
	-- If we get here, then we have no current job. Check to see if our destination is full.
	if (spawnSpot.Inventory ~= nil and spawnSpot.Inventory.StackSize >= spawnSpot.Inventory.MaxStackSize) then
		-- We are full! Don't make a job.
		return
	end
	
	-- If we get here, we need to CREATE a new job.

	jobSpot = structure.GetJobSpotTile()

	if (jobSpot.Inventory ~= nil and (jobSpot.Inventory.StackSize >= jobSpot.Inventory.MaxStackSize)) then
		-- Our drop spot is already full, so don't create a job.
		return
	end
	
	itemsDesired = { Inventory.__new("inv_RawStone", 10, 0) }
	
	j = Job.__new(
		jobSpot,
		nil,
		nil,
		1200,
		itemsDesired,
		true    -- This job repeats until the destination tile is full.
	)
	
	j.RegisterJobCompletedCallback("StoneCuttingTable_JobComplete")

	structure.AddJob(j)
end

-- StoneCuttingTable_JobComplete
-- @Params - j:Job
function StoneCuttingTable_JobComplete(j)
	--Debug.Log("StoneCuttingTable_JobComplete");

	World.Current.InventoryManager.PlaceInventory(j.Structure.GetSpawnSpotTile(),
		Inventory.__new("inv_StoneBlock", 50, 5))
		
	j.CancelJob()
end