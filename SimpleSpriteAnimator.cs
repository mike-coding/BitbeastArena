using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSpriteAnimator : MonoBehaviour
{
    public bool FixFacingUpwards;
    public bool RandomRotation;
    public bool MatchParentY;
    public float AnimationSpeed= 0.35f;
    public List<Sprite> Sprites = new List<Sprite>();

    private SpriteRenderer _sp;
    private float zRotation;

    // Start is called before the first frame update
    void Awake()
    {
        _sp = GetComponent<SpriteRenderer>();
        if (RandomRotation) RandomlyRotate();
        else zRotation = transform.rotation.z;
        if (Sprites.Count>0) StartCoroutine(AnimationRoutine());
        if (FixFacingUpwards) StartCoroutine(FaceUpwardsRoutine());
        if (MatchParentY) StartCoroutine(MatchParentYRoutine());
    }

    private void RandomlyRotate()
    {
        List<float> rotationAngles = new List<float> { 0f, 90f, 180f, 270f };
        float selectedAngle = rotationAngles[Random.Range(0, rotationAngles.Count)];
        zRotation = selectedAngle;
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

    private IEnumerator FaceUpwardsRoutine()
    {
        while (true)
        {
            //transform.LookAt(Vector3.up);
            transform.rotation = Quaternion.Euler(0, 0, zRotation);
            yield return null;
        }
    }

    private IEnumerator MatchParentYRoutine()
    {
        while (true)
        {
            if (transform.position.y != transform.parent.position.y) transform.position = new Vector3(transform.position.x, transform.parent.position.y-0.025f, transform.position.z);
            yield return null;
        }
    }
}
