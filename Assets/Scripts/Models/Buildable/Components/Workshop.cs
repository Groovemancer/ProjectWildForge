using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

[Serializable]
[BuildableComponentName("Workshop")]
public class Workshop : BuildableComponent
{
    public Workshop()
    {
    }

    private Workshop(Workshop other) : base(other)
    {
        ParamsDefinitions = other.ParamsDefinitions;
        PossibleProductions = other.PossibleProductions;
        RunConditions = other.RunConditions;
        HaulConditions = other.HaulConditions;
        Efficiency = other.Efficiency;
    }

    [XmlElement("ParameterDefinitions")]
    public WorkShopParameterDefinitions ParamsDefinitions { get; set; }

    public Parameter CurrentProcessingTime
    {
        get
        {
            return StructureParams[ParamsDefinitions.CurrentProcessingTime.ParameterName];
        }
    }

    public Parameter MaxProcessingTime
    {
        get
        {
            return StructureParams[ParamsDefinitions.MaxProcessingTime.ParameterName];
        }
    }

    /// <summary>
    /// Means that there is input being processed (input was consumed and is inside machine).
    /// </summary>
    public Parameter InputProcessed
    {
        get
        {
            return StructureParams[ParamsDefinitions.InputProcessed.ParameterName];
        }
    }

    public Parameter IsRunning
    {
        get
        {
            return StructureParams[ParamsDefinitions.IsRunning.ParameterName];
        }

        set
        {
            StructureParams[ParamsDefinitions.IsRunning.ParameterName] = value;
        }
    }

    public Parameter CurrentProductionChainName
    {
        get
        {
            return StructureParams[ParamsDefinitions.CurrentProductionChainName.ParameterName];
        }
    }

    public Parameter InputHaulingJobsCount
    {
        get
        {
            return StructureParams[ParamsDefinitions.InputHaulingJobsCount.ParameterName];
        }
    }

    public Parameter HasAllNeededInputInventory
    {
        get
        {
            return StructureParams[ParamsDefinitions.HasAllNeededInputInventory.ParameterName];
        }
    }

    [XmlElement("ProductionChain")]
    public List<ProductionChain> PossibleProductions { get; set; }

    [XmlElement("RunConditions")]
    public Conditions RunConditions { get; set; }

    [XmlElement("HaulConditions")]
    public Conditions HaulConditions { get; set; }

    [XmlElement("Efficiency")]
    public SourceDataInfo Efficiency { get; set; }

    public override bool RequiresSlowUpdate
    {
        get
        {
            return true;
        }
    }

    // TODO: Implement ContextMenu
    //private List<ComponentContextMenu> WorkshopMenuActions { get; set; }

    public override BuildableComponent Clone()
    {
        return new Workshop(this);
    }

    public override IEnumerable<string> GetDescription()
    {
        if (PossibleProductions.Count > 1)
        {
            StringBuilder sb = new StringBuilder();
            string prodChain = CurrentProductionChainName.ToString();
            sb.AppendLine(!string.IsNullOrEmpty(prodChain) ? string.Format("Production: {0}", prodChain) : "No selected production");
            float curProcessingTime = CurrentProcessingTime.ToFloat();
            if (!ParentStructure.HasCustomProgressReport && curProcessingTime > 0f)
            {
                float perc = 0f;
                float maxProcessingTime = MaxProcessingTime.ToFloat();
                if (maxProcessingTime != 0f)
                {
                    perc = curProcessingTime * 100f / maxProcessingTime;
                    if (perc > 100f)
                    {
                        perc = 100f;
                    }
                }

                sb.AppendLine(string.Format("Progress: {0:0}%", perc));
            }

            int numHaulingJobs = InputHaulingJobsCount.ToInt();
            if (numHaulingJobs > 0)
            {
                sb.AppendLine(string.Format("Hauling jobs: {0}", numHaulingJobs));
            }

            yield return sb.ToString();
        }
        else
        {
            yield return null;
        }
    }

    public override bool IsValid()
    {
        return true;
    }

    public override bool CanFunction()
    {
        bool canWork = false;
        string curSetupChainName = CurrentProductionChainName.ToString();

        if (!string.IsNullOrEmpty(curSetupChainName))
        {
            canWork = true;
            ProductionChain prodChain = GetProductionChainByName(curSetupChainName);
            //// create possible jobs for factory(hauling input)
            bool areAllHaulParamReqsFulfilled = true;
            if (HaulConditions != null)
            {
                areAllHaulParamReqsFulfilled = AreParameterConditionsFulfilled(RunConditions.ParamConditions);
            }

            if (areAllHaulParamReqsFulfilled)
            {
                HaulingJobForInputs(prodChain);
                canWork = true;
            }

            componentRequirements = Requirements.None;
        }
        else
        {
            componentRequirements = Requirements.Production;
        }

        return canWork;
    }

    public override void FixedFrequencyUpdate(float deltaTime)
    {
        // if there is enough input, do the processing and store item to output
        // - remove items from input
        // - add param to reflect factory can provide output (has output inside)
        //   - as output will be produced after time, it is possible that output spot can be ocupied meanwhile
        // - process for specified time
        // - if output slot is free, provide output (if not, keep output 'inside' factory)
        if (ParentStructure.IsBeingDestroyed)
        {
            return;
        }

        if (RunConditions != null && AreParameterConditionsFulfilled(RunConditions.ParamConditions) == false)
        {
            IsRunning.SetValue(false);
            return;
        }

        float efficiency = 1f;
        if (Efficiency != null)
        {
            efficiency = RetrieveFloatFor(Efficiency, ParentStructure);
        }

        string curSetupChainName = CurrentProductionChainName.ToString();

        if (!string.IsNullOrEmpty(curSetupChainName))
        {
            ProductionChain prodChain = GetProductionChainByName(curSetupChainName);

            // if there is no processing in progress
            if (InputProcessed.ToInt() == 0)
            {
                // check input slots for input inventory
                List<KeyValuePair<Tile, int>> flaggedForTaking = CheckForInventoryAtInput(prodChain);

                // if all the input requirements are ok, you can start processing:
                if (flaggedForTaking.Count == prodChain.Input.Count)
                {
                    List<TileObjectTypeAmount> outPlacement = CheckForInventoryAtOutput(prodChain);
                    if (outPlacement.Count == prodChain.Output.Count)
                    {
                        //// consume input inventory
                        //ConsumeInventories(flaggedForTaking);

                        InputProcessed.SetValue(1); // check if it can be bool
                        IsRunning.SetValue(true);

                        DebugUtils.Log("================ Processed ================");

                        Job job = new Job(
                                     ParentStructure.Jobs.WorkSpotTile,
                                     null,
                                     ProcessingComplete,
                                     prodChain.ProcessingTime,
                                     null,
                                     Job.JobPriority.High,
                                     "workshop",
                                     prodChain.WorkSkill,
                                     false,
                                     false,
                                     false);

                        job.OnJobWorked += ProcessingProgress;
                        ParentStructure.Jobs.Add(job);

                        // reset processing timer and set max time for processing for this prod. chain
                        CurrentProcessingTime.SetValue(0f);
                        MaxProcessingTime.SetValue(prodChain.ProcessingTime);
                        HasAllNeededInputInventory.SetValue(true);
                    }
                }
                else
                {
                    HasAllNeededInputInventory.SetValue(false);
                }
            }
        }
    }

    public void ProcessingProgress(Job job)
    {
        IsRunning.SetValue(true);
    }

    public void ProcessingComplete(Job job)
    {
        string curSetupChainName = CurrentProductionChainName.ToString();

        if (!string.IsNullOrEmpty(curSetupChainName))
        {

            ProductionChain prodChain = GetProductionChainByName(curSetupChainName);

            // check input slots for input inventory
            List<KeyValuePair<Tile, int>> flaggedForTaking = CheckForInventoryAtInput(prodChain);

            // consume input inventory
            ConsumeInventories(flaggedForTaking);

            List<TileObjectTypeAmount> outPlacement = CheckForInventoryAtOutput(prodChain);

            // if output placement was found for all products, place them
            if (//outPlacement.Count == 0 ||
                outPlacement.Count == prodChain.Output.Count)
            {
                PlaceInventories(outPlacement);
                //// processing done, can fetch input for another processing
                InputProcessed.SetValue(0);
                IsRunning.SetValue(false);
                CurrentProcessingTime.SetValue(0f);
            }
        }
    }

    // TODO: Implement Context Menu
    //public override List<ContextMenuAction> GetContextMenu()
    //{
    //    if (WorkshopMenuActions != null)
    //    {
    //        return WorkshopMenuActions.Select(x => CreateComponentContextMenuItem(x)).ToList();
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}

    protected override void Initialize()
    {
        if (ParamsDefinitions == null)
        {
            // don't need definition for all furniture, just use defaults
            ParamsDefinitions = new WorkShopParameterDefinitions();
        }

        // check if context menu is needed
        if (PossibleProductions.Count > 1)
        {
            componentRequirements = Requirements.Production;

            // TODO: Implement Context Menu
            //WorkshopMenuActions = new List<ComponentContextMenu>();

            CurrentProductionChainName.SetValue(null);
            foreach (ProductionChain chain in PossibleProductions)
            {
                string prodChainName = chain.Name;
                // TODO: Implement Context Menu
                //WorkshopMenuActions.Add(new ComponentContextMenu()
                //{
                //    Name = prodChainName,
                //    Function = ChangeCurrentProductionChain
                //});
            }
        }
        else
        {
            if (PossibleProductions.Count == 1)
            {
                CurrentProductionChainName.SetValue(PossibleProductions[0].Name);
            }
            else
            {
                DebugUtils.LogWarningChannel(ComponentLogChannel, string.Format("Structure {0} is marked as factory, but has no production chain", ParentStructure.Type));
            }
        }

        // add dynamic params here
        CurrentProcessingTime.SetValue(0);
        MaxProcessingTime.SetValue(0);
        InputProcessed.SetValue(0);

        ParentStructure.Removed += WorkshopRemoved;
    }

    private void PlaceInventories(List<TileObjectTypeAmount> outPlacement)
    {
        foreach (TileObjectTypeAmount outPlace in outPlacement)
        {
            if (outPlace.IsEmpty)
            {
                World.Current.InventoryManager.PlaceInventory(outPlace.Tile, new Inventory(outPlace.ObjectType, outPlace.Amount));
            }
            else
            {
                outPlace.Tile.Inventory.StackSize += outPlace.Amount;
            }
        }
    }

    private void ConsumeInventories(List<KeyValuePair<Tile, int>> flaggedForTaking)
    {
        foreach (KeyValuePair<Tile, int> toConsume in flaggedForTaking)
        {
            World.Current.InventoryManager.ConsumeInventory(toConsume.Key, toConsume.Value);
        }
    }

    private void PlaceInventoryToWorkshopInput(Job job)
    {
        job.CancelJob();
        foreach (Inventory heldInventory in job.DeliveredItems.Values)
        {
            if (heldInventory.StackSize > 0)
            {
                World.Current.InventoryManager.PlaceInventory(job.Tile, heldInventory);
                job.Tile.Inventory.Locked = true;
            }
        }
    }

    private void UnlockInventoryAtInput(Structure structure)
    {
        // go though all productions and unlock the inputs
        foreach (ProductionChain prodChain in PossibleProductions)
        {
            foreach (Item inputItem in prodChain.Input)
            {
                // check input slots for req. item:
                Tile tile = World.Current.GetTileAt(
                    structure.Tile.X + inputItem.SlotPosX,
                    structure.Tile.Y + inputItem.SlotPosY,
                    structure.Tile.Z);

                if (tile.Inventory != null && tile.Inventory.Locked)
                {
                    tile.Inventory.Locked = false;
                    DebugUtils.LogChannel(ComponentLogChannel, string.Format("Inventory {0} at tile {1} is unlocked", tile.Inventory, tile));
                }
            }
        }
    }

    private void WorkshopRemoved(Structure structure)
    {
        // unlock all inventories at input if there is something left
        UnlockInventoryAtInput(ParentStructure);
    }

    private void ChangeCurrentProductionChain(Structure structure, string newProductionChainName)
    {
        string oldProductionChainName = structure.Parameters[ParamsDefinitions.CurrentProductionChainName.ParameterName].Value;
        bool isProcessing = structure.Parameters[ParamsDefinitions.InputProcessed.ParameterName].ToInt() > 0;

        // if selected production really changes and nothing is being processed now
        if (isProcessing || newProductionChainName.Equals(oldProductionChainName))
        {
            return;
        }

        structure.Jobs.CancelAll();
        structure.Parameters[ParamsDefinitions.CurrentProductionChainName.ParameterName].SetValue(newProductionChainName);

        // unlock all inventories at input if there is something left
        UnlockInventoryAtInput(structure);
    }

    private void HaulingJobForInputs(ProductionChain prodChain)
    {
        int numHaulingJobs = 0;
        bool isProcessing = InputProcessed.ToInt() > 0;
        //// for all inputs in production chain
        foreach (Item reqInputItem in prodChain.Input)
        {
            if (isProcessing && !reqInputItem.HasHopper)
            {
                continue;
            }

            //// if there is no hauling job for input object type, create one
            Job structJob;
            string requiredType = reqInputItem.ObjectType;
            bool existingHaulingJob = ParentStructure.Jobs.HasJobWithPredicate(x => x.RequestedItems.ContainsKey(requiredType), out structJob);

            if (existingHaulingJob)
            {
                numHaulingJobs++;
            }
            else
            {
                Tile inTile = World.Current.GetTileAt(
                                  ParentStructure.Tile.X + reqInputItem.SlotPosX,
                                  ParentStructure.Tile.Y + reqInputItem.SlotPosY,
                                  ParentStructure.Tile.Z);

                // create job for desired input resource
                string desiredInv = reqInputItem.ObjectType;
                int desiredAmount = reqInputItem.Amount;

                if (reqInputItem.HasHopper)
                {
                    desiredAmount = PrototypeManager.Inventory.Get(desiredInv).MaxStackSize;
                }

                if (inTile.Inventory != null && inTile.Inventory.Type == reqInputItem.ObjectType &&
                    inTile.Inventory.StackSize <= desiredAmount)
                {
                    desiredAmount = desiredAmount - inTile.Inventory.StackSize;
                }

                if (desiredAmount > 0)
                {
                    Job job = new Job(
                                 inTile,
                                 null,  // beware: passed jobObjectType is expected Furniture only !!
                                 null,
                                 40,
                                 new RequestedItem[] { new RequestedItem(desiredInv, desiredAmount, desiredAmount) },
                                 Job.JobPriority.Medium,
                                 "hauling",
                                 "Hauling",
                                 false,
                                 false,
                                 false);

                    job.Description = string.Format("Hauling '{0}' to '{1}'", desiredInv, ParentStructure.GetName());
                    job.OnJobWorked += PlaceInventoryToWorkshopInput;
                    ParentStructure.Jobs.Add(job);
                    numHaulingJobs++;
                }
            }
        }

        InputHaulingJobsCount.SetValue(numHaulingJobs);
    }

    private List<TileObjectTypeAmount> CheckForInventoryAtOutput(ProductionChain prodChain)
    {
        List<TileObjectTypeAmount> outPlacement = new List<TileObjectTypeAmount>();

        // processing is done, try to spit the output
        // check if output can be placed in world
        if (prodChain != null && prodChain.Output != null)
        {
            foreach (Item outObjType in prodChain.Output)
            {
                int amount = outObjType.Amount;

                // check ouput slots for products:
                Tile outputTile = World.Current.GetTileAt(
                    ParentStructure.Tile.X + outObjType.SlotPosX,
                    ParentStructure.Tile.Y + outObjType.SlotPosY,
                    ParentStructure.Tile.Z);

                bool tileHasOtherStructure = outputTile.Structure != null && outputTile.Structure != ParentStructure;

                if (!tileHasOtherStructure &&
                    (outputTile.Inventory == null ||
                    (outputTile.Inventory.Type == outObjType.ObjectType && outputTile.Inventory.StackSize + amount <= outputTile.Inventory.MaxStackSize)))
                {
                    // out product can be placed here
                    outPlacement.Add(new TileObjectTypeAmount()
                    {
                        Tile = outputTile,
                        IsEmpty = outputTile.Inventory == null,
                        ObjectType = outObjType.ObjectType,
                        Amount = outObjType.Amount
                    });
                }
            }
        }

        return outPlacement;
    }

    private List<KeyValuePair<Tile, int>> CheckForInventoryAtInput(ProductionChain prodChain)
    {
        List<KeyValuePair<Tile, int>> flaggedForTaking = new List<KeyValuePair<Tile, int>>();
        foreach (Item reqInputItem in prodChain.Input)
        {
            // check input slots for req. item:
            Tile tile = World.Current.GetTileAt(
                ParentStructure.Tile.X + reqInputItem.SlotPosX,
                ParentStructure.Tile.Y + reqInputItem.SlotPosY,
                ParentStructure.Tile.Z);

            if (tile.Inventory != null && tile.Inventory.Type == reqInputItem.ObjectType
                && tile.Inventory.StackSize >= reqInputItem.Amount)
            {
                flaggedForTaking.Add(new KeyValuePair<Tile, int>(tile, reqInputItem.Amount));
            }
        }

        return flaggedForTaking;
    }

    private ProductionChain GetProductionChainByName(string productionChainName)
    {
        return PossibleProductions.FirstOrDefault(chain => chain.Name.Equals(productionChainName));
    }

    [Serializable]
    public class Item
    {
        [XmlAttribute("ObjectType")]
        public string ObjectType { get; set; }

        [XmlAttribute("Amount")]
        public int Amount { get; set; }

        [XmlAttribute("SlotPosX")]
        public int SlotPosX { get; set; }

        [XmlAttribute("SlotPosY")]
        public int SlotPosY { get; set; }

        [XmlAttribute("HasHopper")]
        public bool HasHopper { get; set; }
    }

    [Serializable]
    public class ProductionChain
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("ProcessingTime")]
        public float ProcessingTime { get; set; }

        [XmlAttribute("Skill")]
        public string WorkSkill { get; set; }

        [XmlArray("Input")]
        public List<Item> Input { get; set; }

        [XmlArray("Output")]
        public List<Item> Output { get; set; }
    }

    [Serializable]
    public class WorkShopParameterDefinitions
    {
        // constants for parameters
        public const string CurProcessingTimeParamName = "cur_processing_time";
        public const string MaxProcessingTimeParamName = "max_processing_time";
        public const string CurProcessedInvParamName = "cur_processed_inv";
        public const string CurIsRunningParamName = "workshop_is_running";
        public const string CurProductionChainParamName = "cur_production_chain";
        public const string InputHaulingJobsCountParamName = "input_hauling_job_count";
        public const string HasAllNeededInputInventoryParamName = "has_all_needed_input_inv";

        public WorkShopParameterDefinitions()
        {
            // default values if not defined from outside
            CurrentProcessingTime = new ParameterDefinition(CurProcessingTimeParamName);
            MaxProcessingTime = new ParameterDefinition(MaxProcessingTimeParamName);
            InputProcessed = new ParameterDefinition(CurProcessedInvParamName);
            IsRunning = new ParameterDefinition(CurIsRunningParamName);
            CurrentProductionChainName = new ParameterDefinition(CurProductionChainParamName);
            InputHaulingJobsCount = new ParameterDefinition(InputHaulingJobsCountParamName);
            HasAllNeededInputInventory = new ParameterDefinition(HasAllNeededInputInventoryParamName);
        }

        [XmlElement("CurrentProcessingTime")]
        public ParameterDefinition CurrentProcessingTime { get; set; }

        [XmlElement("MaxProcessingTime")]
        public ParameterDefinition MaxProcessingTime { get; set; }

        [XmlElement("InputProcessed")]
        public ParameterDefinition InputProcessed { get; set; }

        [XmlElement("IsRunning")]
        public ParameterDefinition IsRunning { get; set; }

        [XmlElement("CurrentProductionChainName")]
        public ParameterDefinition CurrentProductionChainName { get; set; }

        [XmlElement("InputHaulingJobsCount")]
        public ParameterDefinition InputHaulingJobsCount { get; set; }

        [XmlElement("HasAllNeededInputInventory")]
        public ParameterDefinition HasAllNeededInputInventory { get; set; }
    }

    private class TileObjectTypeAmount
    {
        public Tile Tile { get; set; }

        public bool IsEmpty { get; set; }

        public string ObjectType { get; set; }

        public int Amount { get; set; }
    }
}