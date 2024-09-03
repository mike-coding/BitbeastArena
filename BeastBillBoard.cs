using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BeastBillBoardType
{
    Ensnare,
    Burn,
    Stun,
    Poison,
    Stench,
    StatBar,
    DamageParticle
}

public class BeastBillBoard : MonoBehaviour
{
    //positioning
    public float zRotationOffset;

    //general
    private BeastController _attachedController;
    public BeastBillBoardType Type;

    //animation / Sprite
    public float AnimationSpeed = 0.35f;

    public List<Sprite> Sprites = new List<Sprite>();

    //components
    private SpriteRenderer _sp;

    public void Init(BeastController parent)
    {
        _attachedController = parent;
        _sp = GetComponent<SpriteRenderer>();

        switch (Type)
        {
            case BeastBillBoardType.Stench:
                SetPosition(0.15f);
                StartCoroutine(LifeTimeRoutine(Ability.Dex["Stench"].CoolDownTime));
                StartCoroutine(AnimationRoutine());
                break;
            case BeastBillBoardType.Burn:
                SetPosition(0.2f);
                StartCoroutine(AnimationRoutine());
                break;
            case BeastBillBoardType.Poison:
                SetPosition(0.2f);
                StartCoroutine(AnimationRoutine());
                break;
            case BeastBillBoardType.Stun:
                SetPosition(0.2f);
                StartCoroutine(AnimationRoutine());
                //do stuff
                break;
            case BeastBillBoardType.Ensnare:
                SetPosition(-0.09f,-0.05f,false);
                //for now, do nothing
                break;
            case BeastBillBoardType.StatBar:
                SetPosition(0.32f);
                break;
            case BeastBillBoardType.DamageParticle:
                break;
        }
    }

    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        int spriteCount = Sprites.Count;
        while (true)
        {
            _sp.sprite = Sprites[currentFrame];
            if (currentFrame < spriteCount - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(AnimationSpeed);
        }
    }

    private void SetPosition(float Yoffset=0,float Zoffset=0, bool fromTop=true)
    {
        float Ypos = Yoffset;
        if (fromTop) Ypos = _attachedController.CurrentState.YoffsetToTop + Yoffset; //YoffsetToTop is 0?
        transform.localPosition = new Vector3(0, Ypos, Zoffset);
        transform.localRotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, zRotationOffset);
    }

    private IEnumerator LifeTimeRoutine(float duration)
    {
        float runTime = 0;
        while (runTime < duration)
        {
            yield return null;
            runTime += Time.deltaTime;
        }
        DestroySelf(true);
    }

    public void DestroySelf(bool selfTriggered=false)
    {
        //remove self from parent _billboards
        if (selfTriggered) _attachedController.ModifyBillBoards(this, Mod.Subtraction);
        else
        {
            if (Type != BeastBillBoardType.Stench) GameObject.Destroy(gameObject);
            else GameObject.Destroy(gameObject.transform.parent.gameObject);
        }
    }
}
