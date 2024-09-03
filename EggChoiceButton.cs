using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

public class EggChoiceButton : UIButton
{
    private int selfID;
    private Image displayImage;
    private GameObject displayObject;
    private GameObject selectorIndicator;
    private BeastState babyState;
    private Sprite originalEggSprite;
    private Color originalColor;
    private Vector3 originalDisplayScale;
    private Coroutine InstanceAnimationRoutine;

    //static
    private static Dictionary<int,EggChoiceButton> AllEggChoiceControllers = new Dictionary<int, EggChoiceButton>();

    void Awake()
    {
        StoreFieldData();
        AllEggChoiceControllers[selfID]=this;
    }
    
    public override void OnEnable()
    {
        base.OnEnable();
        ResetSelf();
    }

    void ResetSelf()
    {
        DeselectSelf();
        displayImage.sprite = originalEggSprite;
        displayImage.color = originalColor;
        displayObject.transform.localScale = originalDisplayScale;
    }

    private void StoreFieldData()
    {
        //sprite info
        displayObject = transform.Find("Display").gameObject;
        displayImage = displayObject.GetComponent<Image>();
        _bg = transform.Find("BG").gameObject.GetComponent<Image>();
        originalDisplayScale = displayObject.transform.localScale;
        originalEggSprite = displayImage.sprite;
        originalColor = displayImage.color;

        selfID = int.Parse(gameObject.name[gameObject.name.Length - 1].ToString());
        selectorIndicator = gameObject.transform.Find("SelectorIndicator").gameObject;
        babyState = new BeastState();
        babyState.LoadBlueprintData(new int[] { 1, selfID - 1 });
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (selectorIndicator.activeInHierarchy) return;
        TurnOffAllSelectorIndicators();
        selectorIndicator.SetActive(true);
        InitHatchedBeastDisplay();
        GameManager.SetStartingBeast(babyState);
    }

    private void UpdateStartingBeast()
    {

    }

    private void InitHatchedBeastDisplay()
    {
        displayImage.color = Color.white;
        PerformEggCrackAnimation();
        InstanceAnimationRoutine = StartCoroutine(AnimationRoutine());
    }

    private void PerformEggCrackAnimation()
    {
        // Create particles with the new Vector3 directions
        ParticleController.CreateParticle(transform.position, 0, new Vector2(-0.75f,0), 0.0375f, originalColor, true);
        ParticleController.CreateParticle(transform.position, 1, new Vector2(0.75f, 0), 0.0375f, originalColor, true);
    }

    public static void TurnOffAllSelectorIndicators()
    {
        for (int i=1;i< AllEggChoiceControllers.Count + 1; i++) 
        {
            AllEggChoiceControllers[i].DeselectSelf();
        }
    }

    public void TurnSelectorOff() { selectorIndicator.SetActive(false); }

    public void EndAnimation() { if (InstanceAnimationRoutine !=null) StopCoroutine(InstanceAnimationRoutine); }

    public void DeselectSelf()
    {
        TurnSelectorOff();
        EndAnimation();
        displayImage.sprite = originalEggSprite;
        displayImage.color = originalColor;
        displayObject.transform.localScale = originalDisplayScale;
    }

    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        while (true)
        {
            displayImage.sprite = babyState.AnimationDictionary["F"][currentFrame];
            if (currentFrame < babyState.AnimationDictionary["F"].Count - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public static void ClearControllerDict()
    {
        AllEggChoiceControllers.Clear();
    }
}
