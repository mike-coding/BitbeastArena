using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatBar : MonoBehaviour
{
    GameObject HPBarObject;
    GameObject ShieldBarObject;
    GameObject LevelObject;
    Slider HPBar;
    Slider ShieldBar;
    Text Level;
    BeastBillBoard BillBoardController;
    BeastController AttachedBeastController;


    public void Init(bool isEnemy = false)
    {
        HPBarObject = transform.Find("HPBar").gameObject;
        HPBar = HPBarObject.GetComponent<Slider>();
        ShieldBarObject = transform.Find("ShieldBar").gameObject;
        ShieldBar = ShieldBarObject.GetComponent<Slider>();
        LevelObject = transform.Find("LevelHolder/Level").gameObject;
        Level = LevelObject.GetComponent<Text>();
        BillBoardController = transform.parent.GetComponent<BeastBillBoard>();
        AttachedBeastController = transform.parent.parent.GetComponent<BeastController>();
        if (isEnemy)
        {
            Image BarImage = HPBarObject.transform.Find("FillRect").gameObject.GetComponent<Image>();
            BarImage.color = new Color(255 / 255f, 147 / 255f, 108 / 255f, 137 / 255f);
        }
    }

    public void UpdateToController(BeastController controller)
    {
        BeastState profile = controller.CurrentState;
        ShieldBarObject.SetActive(controller.Shield.Sum> 0);
        HPBar.value = (float)profile.StatDict[Stat.CurrentHP] / profile.StatDict[Stat.MaxHP];
        ShieldBar.value = (float)controller.Shield.Sum / profile.StatDict[Stat.MaxHP]; 
        Level.text = profile.Level.ToString();
        BillBoardController.Init(AttachedBeastController);
    }
}
