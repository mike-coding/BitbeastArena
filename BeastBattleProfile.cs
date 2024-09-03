using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeastBattleProfile 
{
    public bool IsEmpty { get { return (BeastObject==null && Controller==null && State==null); } }
    public bool IsPreset { get { return (State!=null && (BeastObject==null || Controller==null)); } }
    public bool IsLoaded { get { return (State!=null && BeastObject!=null && Controller!=null); } }
    public bool IsAlive { get { return State != null && State.StatDict[Stat.CurrentHP] > 0; } }

    public GameObject BeastObject;
    public BeastController Controller;
    public BeastState State;

    public void Preset(BeastState toLoad) { State = toLoad; }

    public void FinishLoad(GameObject toLoadObject, BeastController toLoadController)
    {
        BeastObject = toLoadObject;
        Controller = toLoadController;
    }

    public void Clear()
    {
        State = null;
        Controller = null;
        BeastObject = null;
    }
}
