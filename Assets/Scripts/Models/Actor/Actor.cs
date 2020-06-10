using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ProjectWildForge.Pathfinding;
using System.Linq;
using MoonSharp.Interpreter;

public enum ActorJobPriority
{
    Urgent,
    High,
    Medium,
    Low,
}

[MoonSharpUserData]
public class Actor : IXmlSerializable, ISelectable, IUpdatable
{
    /// Unique ID of the actor
    public readonly int Id;

    /// What Id we currently are sitting at
    private static int currentId = 0;

    public Bounds Bounds
    {
        get
        {
            return new Bounds(
                new Vector3(X - 1, Y - 1, 0),
                new Vector3(1, 1));
        }
    }

    private Tile currTile;
    public Tile CurrTile
    {
        get
        {
            return currTile;
        }
        set
        {
            if (currTile != null)
            {
                currTile.Actors.Remove(this);
            }
            currTile = value;
            currTile.Actors.Add(this);

            TileOffset = Vector3.zero;
        }
    }

    // If we aren't moving, then destTile = CurrTile
    Tile _destTile;
    Tile destTile
    {
        get { return _destTile; }
        set
        {
            if (_destTile != value)
            {
                _destTile = value;
            }
        }
    }
    Tile nextTile;

    /// Tile offset for animation
    public Vector3 TileOffset { get; set; }

    /// <summary>
    /// Returns a float representing the Character's X position, which can
    /// be part-way between two tiles during movement.
    /// </summary>
    public float X
    {
        get
        {
            return CurrTile.X + TileOffset.x;
        }
    }

    /// <summary>
    /// Returns a float representing the Character's Y position, which can
    /// be part-way between two tiles during movement.
    /// </summary>
    public float Y
    {
        get
        {
            return CurrTile.Y + TileOffset.y;
        }
    }

    /// <summary>
    /// Returns a float representing the Character's Z position, which can
    /// be part-way between two tiles during movement.
    /// </summary>
    public float Z
    {
        get
        {
            return CurrTile.Z + TileOffset.z;
        }
    }

    public List<Tile> Path { get; set; }

    // Amount of action points required to move 1 tile.
    private float movementCost = 25f;
    private float baseMovementCost = 25f;

    /// Tiles per second.
    public float MovementCost
    {
        get
        {
            return movementCost;
        }
    }

    /// Our job, if any.
    public Job MyJob
    {
        get
        {
            JobState jobState = FindInitiatingState() as JobState;
            if (jobState != null)
            {
                return jobState.Job;
            }

            return null;
        }
    }

    // The current state
    private State state;

    // List of global states that always run
    private List<State> globalStates;

    // Queue of states that aren't important enough to interrupt, but should run soon
    private Queue<State> stateQueue;

    private float workCost = 25f;   // Amount of action points required to do any amount of work
    private float workRate;   // Amount of work completed per DoWork attempt
    private float baseWorkRate = 50f;   // Amount of work completed per DoWork attempt

    public float WorkCost
    {
        get { return workCost; }
    }

    public float WorkRate
    {
        get { return workRate; }
    }

    private float baseExperienceRate = 1f; // Rate at which we gain experience for skills
    private float experienceRate; // Rate at which we gain experience for skills

    public float ExperienceRate
    {
        get { return experienceRate; }
    }

    public float ActionPoints { get; set; }
    public bool Acted { get; set; }

    /// A callback to trigger when actor information changes (notably, the position).
    public event Action<Actor> OnActorChanged;

    Job myJob;

    public string Name { get; protected set; }

    // Priorities for jobs for the actor
    public Dictionary<JobCategory, ActorJobPriority> Priorities { get; private set; }

    public Race Race { get; protected set; }

    public string SpriteName { get; set; }

    public bool IsFemale { get; set; }

    // The item we are carrying (not gear/equipment)
    public Inventory Inventory { get; set; }

    /// Stats, for actor.
    public Dictionary<string, Stat> Stats { get; protected set; }

    /// Skills, for actor
    public Dictionary<string, Skill> Skills { get; protected set; }
    public bool IsSelected { get; set; }

    public Actor()
    {
        // Use only for serialization
        InitializeActorValues();
        Id = currentId++;
        IsFemale = false;
    }

    public Actor(Tile tile, string name, string race, bool isFemale, string spriteName = null)
    {
        CurrTile = destTile = tile;
        Name = name;
        Race = PrototypeManager.Race.Get(race);
        IsFemale = isFemale;
        InitializeActorValues();

        if (string.IsNullOrEmpty(spriteName))
        {
            if (IsFemale)
            {
                SpriteName = RandomUtils.ObjectFromList(Race.FemaleSprites, string.Empty);
            }
            else
            {
                SpriteName = RandomUtils.ObjectFromList(Race.MaleSprites, string.Empty);
            }
        }
        else
        {
            SpriteName = spriteName;
        }

        stateQueue = new Queue<State>();
        globalStates = new List<State>
        {
            // TODO Add NeedState
            //new NeedState(this);
        };
        Id = currentId++;
    }

    private State FindInitiatingState()
    {
        if (state == null)
        {
            return null;
        }

        State rootState = state;
        while (rootState.NextState != null)
        {
            rootState = rootState.NextState;
        }

        return rootState;
    }

    private void InitializeActorValues()
    {
        InitStats();
        UseStats();

        InitSkills();
        UseSkills();
        LoadPriorities();
    }

    private void LoadPriorities()
    {
        Priorities = new Dictionary<JobCategory, ActorJobPriority>();

        foreach (JobCategory category in PrototypeManager.JobCategory.Values)
        {
            Priorities[category] = ActorJobPriority.High; // TODO: Set these in a meaningful way and store them!
        }
    }

    public string GetName()
    {
        return Name;
    }

    private void InitStats()
    {
        Stats = new Dictionary<string, Stat>(PrototypeManager.Stat.Count);
        for (int i = 0; i < PrototypeManager.Stat.Count; i++)
        {
            Stat prototypeStat = PrototypeManager.Stat.Values[i];
            Stat newStat = prototypeStat.Clone();

            Stat raceStat = Race.StatModifiers.Find(stat => stat.Type == newStat.Type);
            int raceValue = (raceStat != null) ? raceStat.Value : 0;

            // Gets a random value within the min and max range of the stat.
            // TODO: Should there be any bias or any other algorithm applied here to make stats more interesting?
            newStat.Value = Math.Max(RandomUtils.DiceRoll(3, 8, raceValue), 1);
            Stats.Add(newStat.Type, newStat);
        }

        DebugUtils.LogChannel("Actor", string.Format("{0}'s Stats: {1}", Name, string.Join(", ", Stats.Select(s => s.Value.ToString()))));

        DebugUtils.LogChannel("Actor", "Initialized " + Stats.Count + " Stats.");
    }

    private void UseStats()
    {
        try
        {
            movementCost = baseMovementCost - (0.3f * baseMovementCost * ((float)Stats["Agility"].Value - 10) / 10); // +/- 30%
            workRate = baseWorkRate + (0.5f * baseWorkRate * ((float)Stats["Dexterity"].Value - 10) / 10); // +/- 50%
            experienceRate = baseExperienceRate + (0.3f * baseExperienceRate * ((float)Stats["Intellect"].Value - 10) / 10); // +/- 30%

            DebugUtils.LogChannel("Actor", string.Format("{0}'s movementCost: {1}, workRate: {2}, experienceRate: {3}", Name, movementCost, workRate, experienceRate));
        }
        catch (KeyNotFoundException)
        {
            DebugUtils.LogError("Stat keys not found. If not testing, this is really bad!");
        }
    }

    private void InitSkills()
    {
        Skills = new Dictionary<string, Skill>(PrototypeManager.Skill.Count);
        for (int i = 0; i < PrototypeManager.Skill.Count; i++)
        {
            Skill prototypeSkill = PrototypeManager.Skill.Values[i];
            Skill newSkill = prototypeSkill.Clone();

            // TODO: Add Skill modifiers to Races
            Skill raceSkill = Race.SkillModifiers.Find(skill => skill.Type == newSkill.Type);
            int raceValue = (raceSkill != null) ? raceSkill.Value : 0;

            // Gets a random value within the min and max range of the skill.
            // TODO: Should there be any bias or any other algorithm applied here to make skills more interesting?
            newSkill.Value = Math.Max(RandomUtils.DiceRoll(3, 8, raceValue, 0), 0);
            Skills.Add(newSkill.Type, newSkill);
        }

        DebugUtils.LogChannel("Actor", string.Format("{0}'s Skills: {1}", Name, string.Join(", ", Skills.Select(s => s.Value.ToString()))));

        DebugUtils.LogChannel("Actor", "Initialized " + Skills.Count + " Skills.");
    }

    private void UseSkills()
    {
        try
        {
            // Maybe modify some stuff from skills? Or would we rather just use skill by itself?
        }
        catch (KeyNotFoundException)
        {
            DebugUtils.LogError("Skill keys not found. If not testing, this is really bad!");
        }
    }

    public void GainSkillExperience(string skillType, float experience)
    {
        Skill skill;
        if (Skills.TryGetValue(skillType, out skill))
        {
            skill.GainExperience(experience * ExperienceRate);
            DebugUtils.LogChannel("Actor", string.Format("{0}'s {1} Gained {2} experience!", Name, skillType, (experience * ExperienceRate)));
        }
    }

    // AUTs are "Arbitrary Unit of Time", e.g. 100 AUT/s means every 1 second 100 AUTs pass
    public void EveryFrameUpdate(float deltaAuts)
    {
        Acted = false;
        ActionPoints += deltaAuts;

        // Run all the global states first so that they can interrupt or queue up new states
        foreach (State globalState in globalStates)
        {
            globalState.Update(deltaAuts);
        }

        // We finished the last state
        if (state == null)
        {
            if (stateQueue.Count > 0)
            {
                SetState(stateQueue.Dequeue());
            }
            else
            {
                Job job = World.Current.JobManager.GetJob(this);
                if (job != null)
                {
                    SetState(new JobState(this, job));
                }
                else
                {
                    // TODO: Lack of job states should be more interesting. Maybe go to the pub and have a pint?
                    SetState(new IdleState(this));
                }
            }
        }

        state.Update(deltaAuts);

        //Animation.Update(deltaAuts);

        if (Acted == false)
            ActionPoints -= deltaAuts;

        if (OnActorChanged != null)
        {
            OnActorChanged(this);
        }
    }

    public void FixedFrequencyUpdate(float deltaAuts)
    {
        throw new InvalidOperationException("Not supported by this class");
    }

    public ActorJobPriority GetPriority(JobCategory category)
    {
        return Priorities[category];
    }

    public void SetPriority(JobCategory category, ActorJobPriority priority)
    {
        Priorities[category] = priority;
    }

    public List<JobCategory> CategoriesOfPriority(ActorJobPriority priority)
    {
        List<JobCategory> ret = new List<JobCategory>();
        foreach (KeyValuePair<JobCategory, ActorJobPriority> row in Priorities)
        {
            if (row.Value == priority)
            {
                ret.Add(row.Key);
            }
        }

        return ret;
    }

    #region State

    public void PrioritizeJob(Job job)
    {
        if (state != null)
        {
            state.Interrupt();
        }

        SetState(new JobState(this, job));
    }

    /// <summary>
    /// Stops the current state. Makes the character halt what is going on and start looking for something new to do, might be the same thing.
    /// </summary>
    public void InterruptState()
    {
        if (state != null)
        {
            state.Interrupt();

            // We can't use SetState(null), because it runs Exit on the state and we don't want to run both Interrupt and Exit.
            state = null;
        }
    }

    /// <summary>
    /// Removes all the queued up states.
    /// </summary>
    public void ClearStateQueue()
    {
        // If we interrupt, we get rid of the queue as well.
        while (stateQueue.Count > 0)
        {
            State queuedState = stateQueue.Dequeue();
            queuedState.Interrupt();
        }
    }

    public void QueueState(State newState)
    {
        stateQueue.Enqueue(newState);
    }

    public void SetState(State newState)
    {
        if (state != null)
        {
            state.Exit();
        }

        state = newState;

        if (state != null)
        {
            state.Enter();
        }
    }

    #endregion

    public void RegisterOnChangedCallback(Action<Actor> cb)
    {
        OnActorChanged += cb;
    }

    public void UnregisterOnChangedCallback(Action<Actor> cb)
    {
        OnActorChanged -= cb;
    }

    void OnJobStopped(Job j)
    {
        // Job completed (if non-repating) or was cancelled.

        j.UnregisterJobStoppedCallback(OnJobStopped);

        if (j != myJob)
        {
            Debug.LogError("Actor being told about job that isn't there's. You forgot to unregister something.");
            return;
        }

        myJob = null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                     SAVING & LOADING
    /// 
    ////////////////////////////////////////////////////////////////////////////////////////////////

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", CurrTile.X.ToString());
        writer.WriteAttributeString("Y", CurrTile.Y.ToString());
        writer.WriteAttributeString("Z", CurrTile.Z.ToString());

        writer.WriteAttributeString("Race", Race.Type);
        writer.WriteAttributeString("Name", Name);
        writer.WriteAttributeString("IsFemale", IsFemale.ToString());
        writer.WriteAttributeString("SpriteName", SpriteName);

        SaveStats(writer);
        SaveSkills(writer);
    }

    public void ReadXml(XmlReader reader)
    {
        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Stats":
                    DebugUtils.LogChannel("Actor", "ReadXml Stats!");
                    LoadStats(reader);
                    break;
                case "Skills":
                    DebugUtils.LogChannel("Actor", "ReadXml Skills!");
                    LoadSkills(reader);
                    break;
            }
        }
    }

    private void SaveStats(XmlWriter writer)
    {
        writer.WriteStartElement("Stats");
        foreach (Stat stat in Stats.Values)
        {
            string statType = stat.Type;
            int statVal = stat.Value;

            writer.WriteStartElement("Stat");
            writer.WriteAttributeString("Type", statType);
            writer.WriteAttributeString("Value", statVal.ToString());
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private void SaveSkills(XmlWriter writer)
    {
        writer.WriteStartElement("Skills");
        foreach (Skill skill in Skills.Values)
        {
            string skillType = skill.Type;
            int skillVal = skill.Value;
            float skillExp = skill.Experience;

            writer.WriteStartElement("Skill");
            writer.WriteAttributeString("Type", skillType);
            writer.WriteAttributeString("Value", skillVal.ToString());
            writer.WriteAttributeString("Exp", skillExp.ToString());
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private void LoadStats(XmlReader reader)
    {
        Stats = new Dictionary<string, Stat>(PrototypeManager.Stat.Count);
        if (reader.ReadToDescendant("Stat"))
        {
            do
            {
                string statType = reader.GetAttribute("Type");
                int statVal = int.Parse(reader.GetAttribute("Value"));

                Stat prototypeStat = PrototypeManager.Stat.Get(statType);
                Stat newStat = prototypeStat.Clone();

                newStat.Value = statVal;
                Stats.Add(newStat.Type, newStat);
            } while (reader.ReadToNextSibling("Stat"));
        }

        DebugUtils.LogChannel("Actor", string.Format("{0}'s Stats: {1}", Name, string.Join(", ", Stats.Select(s => s.Value.ToString()))));
        UseStats();
    }

    private void LoadSkills(XmlReader reader)
    {
        Skills = new Dictionary<string, Skill>(PrototypeManager.Skill.Count);
        if (reader.ReadToDescendant("Skill"))
        {
            do
            {
                string skillType = reader.GetAttribute("Type");
                int skillVal = int.Parse(reader.GetAttribute("Value"));
                float skillExp = float.Parse(reader.GetAttribute("Exp"));

                Skill prototypeSkill = PrototypeManager.Skill.Get(skillType);
                Skill newSkill = prototypeSkill.Clone();

                newSkill.Value = skillVal;
                newSkill.Experience = skillExp;
                Skills.Add(newSkill.Type, newSkill);
            } while (reader.ReadToNextSibling("Skill"));
        }

        DebugUtils.LogChannel("Actor", string.Format("{0}'s Skills: {1}", Name, string.Join(", ", Skills.Select(s => s.Value.ToString()))));
        UseSkills();
    }

    public string GetDescription()
    {
        string strDesc = IsFemale ? "actor_desc_gender_female" : "actor_desc_gender_male";
        return string.Format(StringUtils.GetLocalizedTextFiltered("comment#" + strDesc), StringUtils.GetLocalizedTextFiltered("comment#" + Race.Name));
    }

    public string GetJobDescription()
    {
        if (MyJob == null)
        {
            return "job_no_job_desc";
        }

        return MyJob.Description;
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        // TODO: Implement health
        //yield return health.TextForSelectionPanel();

        // TODO: Implement Needs
        //foreach (Need n in Needs)
        //{
        //    yield return StringUtils.GetLocalizedTextFiltered("comment#" + n.Name) + ": " + n.DisplayAmount;
        //}

        foreach (Stat stat in Stats.Values)
        {

            yield return StringUtils.GetLocalizedTextFiltered("comment#" + stat.Name) + ": " + stat.Value;
        }
    }
}