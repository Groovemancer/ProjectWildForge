--StructureActions

Enterability_Yes	 = 0
Enterability_Never	 = 1
Enterability_Soon	 = 2

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
	if (structure.Parameters["isOpening"].ToFloat() >= 1) then
		structure.Parameters["openness"].ChangeFloatValue(deltaAuts)
		if (structure.Parameters["openness"].ToFloat() >= structure.Parameters["doorOpenTime"].ToFloat()) then
			structure.Parameters["isOpening"].SetValue(0)
		end
	else
		structure.Parameters["openness"].ChangeFloatValue(-deltaAuts)
	end
	
	structure.Parameters["openness"].SetValue(ModUtils.Clamp(structure.Parameters["openness"].ToFloat(), 0,
		structure.Parameters["doorOpenTime"].ToFloat()))
		
	if (structure.VerticalDoor == true) then
		structure.SetAnimationState("vertical")
	else
		structure.SetAnimationState("horizontal")
	end
	
	structure.SetAnimationProgressValue(structure.Parameters["openness"].ToFloat(), 1)
end

-- IsEnterable_Door
-- @Params - structure:Structure
function IsEnterable_Door(structure)

	structure.Parameters["isOpening"].SetValue(1)

	if (structure.Parameters["openness"].ToFloat() >= structure.Parameters["doorOpenTime"].ToFloat()) then
		return Enterability_Yes
	end

	return Enterability_Soon
end

function Door_GetSpriteName(structure)
	if (structure.VerticalDoor) then
	    return structure.Type .. "_Vertical_0"
	else
	    return structure.Type .. "_Horizontal_0"
	end
end

-- IsEnterable_Door
-- @Params - structure:Structure
function Stockpile_GetItemsFromFilter(structure)

	-- TODO: This should be reading from some kind of UI for this
	-- particular stockpile

	-- Since jobs copy arrays automatically, we could already have
	-- an Inventory[] prepared and just return that (as a sort of example filter)

	--return { Inventory.__new("inv_RawStone", 0, 50) }
	return structure.AcceptsForStorage()
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
		structure.Jobs.CancelAll();
		return "Stockpile is Full!"
	end
	
	-- Maybe we already have a job queued up?
	if (structure.Jobs.Count > 0) then
		-- Cool, all done.
		return;
	end
	
	-- We currently are NOT full, but we don't have a job either.
	-- Two possibilities: Either we have SOME inventory, or we have NO inventory

	-- Third possibility: Something is WHACK
	if (structure.Tile.Inventory ~= null and structure.Tile.Inventory.stackSize == 0) then
		structure.CancelAll()
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

	
	local itemsDesired = {}

	if( structure.Tile.Inventory == nil ) then
		--ModUtils.ULog("Creating job for new stack.")
		itemsDesired = Stockpile_GetItemsFromFilter( structure )
	else
		--ModUtils.ULog("Creating job for existing stack.")
		local inventory = structure.Tile.Inventory
		local item = RequestedItem.__new(inventory.Type, 1, inventory.MaxStackSize - inventory.StackSize)
        itemsDesired = { item }
    end
	
	local job = Job.__new(
		structure.Tile,
		nil,
		nil,
		0,
		itemsDesired,
		Job.JobPriority.Low,
		"hauling",
		"Hauling",
		false
	)
	job.Description = "job_stockpile_moving_desc"
	
	ModUtils.ULog("OnUpdate_Stockpile Job Created")
	
	-- TODO: Later on, add stockpile priorities, so that we can take from a lower
    -- priority stockpile for a higher priority one.
	job.canTakeFromStockpile = false;
	job.AcceptsAny = true

	job.RegisterJobWorkedCallback("Stockpile_JobWorked")
	structure.Jobs.Add(job)

end

-- Stockpile_JobWorked
-- @Params - job:Job
function Stockpile_JobWorked(job)
    job.CancelJob()

    -- TODO: Change this when we figure out what we're doing for the all/any pickup job.
    --values = job.GetInventoryRequirementValues();
    for k, inv in pairs(job.DeliveredItems) do
        if(inv.StackSize > 0) then
            World.Current.InventoryManager.PlaceInventory(job.tile, inv)
            return -- There should be no way that we ever end up with more than on inventory requirement with StackSize > 0
        end
    end
end

-- OnUpdate_WorkStation
-- @Params - structure:Structure
-- @Params - deltaAuts:float
function OnUpdate_WorkStation(structure, deltaAuts)
	local outputSpot = structure.Jobs.OutputSpotTile
	
	if (structure.Jobs.Count > 0) then
		-- Check to see if the Raw Stone destination tile is full.
		if (outputSpot.Inventory ~= nil and outputSpot.Inventory.StackSize >= outputSpot.Inventory.MaxStackSize) then
			-- We should stop this job, because it's impossible to make any more items.
			structure.Jobs.CancelAll()
		end

		return
	end
	
	-- If we get here, then we have no current job. Check to see if our destination is full.
	if (outputSpot.Inventory ~= nil and outputSpot.Inventory.StackSize >= outputSpot.Inventory.MaxStackSize) then
		-- We are full! Don't make a job.
		return
	end
	
	-- If we get here, we need to CREATE a new job.

	local jobSpot = structure.Jobs.WorkSpotTile

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
		Job.JobPriority.Medium,
		"workshop",
		"Masonry",
		true    -- This job repeats until the destination tile is full.
	)
	j.RegisterJobCompletedCallback("WorkStation_JobComplete")

	structure.Jobs.Add(j)

end

-- WorkStation_JobComplete
-- @Params - j:Job
function WorkStation_JobComplete(j)
	--Debug.Log("WorkStation_JobComplete");

	World.Current.InventoryManager.PlaceInventory(j.Buildable.Jobs.OutputSpotTile,
		Inventory.__new("inv_RawStone", 10, 50))
end

-- OnUpdate_StoneCuttingTable
-- @Params - structure:Structure
-- @Params - deltaAuts:float
function OnUpdate_StoneCuttingTable(structure, deltaAuts)
	ModUtils.ULog("OnUpdate_StoneCuttingTable 1")
	local jobSpot = structure.Jobs.WorkSpotTile
	local inputSpot = structure.Jobs.InputSpotTile
	local outputSpot = structure.Jobs.OutputSpotTile
	
	--if (structure.Jobs.Count > 0) then
	--	-- Check to see if the Raw Stone destination tile is full.
	--	if (outputSpot.Inventory ~= nil and outputSpot.Inventory.StackSize >= outputSpot.Inventory.MaxStackSize) then
	--		-- We should stop this job, because it's impossible to make any more items.
	--		structure.Jobs.CancelAll()
	--		ModUtils.ULog("OnUpdate_StoneCuttingTable 2")
	--	end
	--
	--	ModUtils.ULog("OnUpdate_StoneCuttingTable 3")
	--	return
	--end
	
	-- If we get here, then we have no current job. Check to see if our destination is full.
	if (outputSpot.Inventory ~= nil and outputSpot.Inventory.StackSize >= outputSpot.Inventory.MaxStackSize) then
	--	-- We are full! Don't make a job.
	--	ModUtils.ULog("OnUpdate_StoneCuttingTable 4")
		return
	end
	
	if (inputSpot.Inventory ~= nil and inputSpot.Inventory.Type == "inv_RawStone" and
		inputSpot.Inventory.StackSize >= 10) then
		j = Job.__new(
			jobSpot,
			nil,
			nil,
			1200,
			nil,
			Job.JobPriority.Medium,
			"workshop",
			"Masonry",
			false
		)
		
		j.RegisterJobCompletedCallback("StoneCuttingTable_JobComplete")
		
		structure.Jobs.Add(j)
		
		if (inputSpot.Inventory.StackSize <= 0) then
			inputSpot.Inventory = nil
		end
		
        --furniture.Parameters["smelttime"].ChangeFloatValue(deltaTime)
        --if (furniture.Parameters["smelttime"].ToFloat() >= furniture.Parameters["smelttime_required"].ToFloat()) then
        --    furniture.Parameters["smelttime"].SetValue(0)
		--
        --    if (outputSpot.Inventory == nil) then
        --        World.Current.InventoryManager.PlaceInventory(outputSpot, Inventory.__new("Steel Plate", 5))
        --        inputSpot.Inventory.StackSize = inputSpot.Inventory.StackSize - 5
		--
        --    elseif (outputSpot.Inventory.StackSize <= outputSpot.Inventory.MaxStackSize - 5) then
        --        outputSpot.Inventory.StackSize = outputSpot.Inventory.StackSize + 5
        --        inputSpot.Inventory.StackSize = inputSpot.Inventory.StackSize - 5
        --    end
		--
        --    if (inputSpot.Inventory.StackSize <= 0) then
        --        inputSpot.Inventory = nil
        --    end
        --end
		ModUtils.ULog("OnUpdate_StoneCuttingTable 5")
    end
	
	
	if (inputSpot.Inventory ~= nil and inputSpot.Inventory.StackSize == inputSpot.Inventory.MaxStackSize) then
        -- We have the max amount of resources, cancel the job.
        -- This check exists mainly, because the job completed callback doesn't
        -- seem to be reliable.
        --structure.Jobs.CancelAll()
		ModUtils.ULog("OnUpdate_StoneCuttingTable 6")
        return
    end

    --if (structure.Jobs.Count > 0) then
	--	ModUtils.ULog("OnUpdate_StoneCuttingTable 7")
    --    return
    --end
	
	-- Create job depending on the already available stack size.
    local desiredStackSize = 50
    if(inputSpot.Inventory ~= nil and inputSpot.Inventory.StackSize < inputSpot.Inventory.MaxStackSize) then
        desiredStackSize = inputSpot.Inventory.MaxStackSize - inputSpot.Inventory.StackSize
		ModUtils.ULog("OnUpdate_StoneCuttingTable 8")
    end
	
	ModUtils.ULog("Desired StackSize: " .. desiredStackSize )
	
	local itemsDesired = { RequestedItem.__new("inv_RawStone", desiredStackSize) }
	
	j = Job.__new(
		inputSpot,
		nil,
		nil,
		0,
		itemsDesired,
		Job.JobPriority.Medium,
		"hauling",
		"Hauling",
		false
	)  
	
	j.RegisterJobWorkedCallback("StoneCuttingTable_JobWorked")

	structure.Jobs.Add(j)
	ModUtils.ULog("OnUpdate_StoneCuttingTable 9")
end

function StoneCuttingTable_JobWorked(job)
    job.CancelJob()
    local inputSpot = job.Tile.Structure.Jobs.InputSpotTile
    for k, inv in pairs(job.DeliveredItems) do
        if(inv ~= nil and inv.StackSize > 0) then
            World.Current.InventoryManager.PlaceInventory(inputSpot, inv)
            inputSpot.Inventory.Locked = true
            return
        end
    end
end

-- StoneCuttingTable_JobComplete
-- @Params - job:Job
function StoneCuttingTable_JobComplete(job)
	ModUtils.ULog("StoneCuttingTable_JobComplete")
	
	job.CancelJob()
	
	local inputSpot = job.Tile.Structure.Jobs.InputSpotTile
	--if (inputSpot.Inventory ~= nil) then
		inputSpot.Inventory.StackSize = inputSpot.Inventory.StackSize - 10
		
		if (inputSpot.Inventory.StackSize <= 0) then
			inputSpot.Inventory = nil
		end

		World.Current.InventoryManager.PlaceInventory(job.Tile.Structure.Jobs.OutputSpotTile,
			Inventory.__new("inv_StoneBlock", 5, 50))
	--end
end

return "Lua Script Parsed!"