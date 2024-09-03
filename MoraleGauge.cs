using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoraleGauge : MonoBehaviour
{
    private Image[] _moraleBlocks = new Image[3];
    private bool _initComplete=false;

    private void OnEnable()
    {
        if (!_initComplete) GetReferences();
        UpdateBlockStates();
    }

    private void GetReferences()
    {
        for (int i =0; i<3; i++)
        {
            _moraleBlocks[i] = transform.Find($"LevelFills/Level {i+1}").gameObject.GetComponent<Image>();
        }
        _initComplete= true;
    }

    private void UpdateBlockStates()
    {
        for (int i = 0; i < 3; i++) if (i + 1 > GameManager.Morale) DarkenImage(_moraleBlocks[i]);
    }

    private void DarkenImage(Image inputImage)
    {
        Color originalColor = inputImage.color;
        float newAlpha = 50 / 255f;
        Color newColor = new Color(originalColor.r,originalColor.g,originalColor.b,newAlpha);
        inputImage.color = newColor;
    }
}
