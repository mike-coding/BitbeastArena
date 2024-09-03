using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveState 
{
    public Inventory PlayerInventory = new Inventory();
    public List<BeastState> PartyMons = new List<BeastState>();
    public List<BeastState> StoredMons = new List<BeastState>();
    public int Morale = 4;

    public void SaveGame()
    {

    }

    public void LoadSave()
    {

    }
}
