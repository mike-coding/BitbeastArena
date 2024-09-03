using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SliderPanelStyle
{
    XP,
    Feed,
    Feed_enemy
}

public class SliderPanelController :MonoBehaviour
{
    private SliderPanelStyle _panelStyle;
    //components
    private Text _levelText;
    private Image _beastImage;
    private Slider _slider;
    private FeedButton _feedButton;

    BeastState _loadedState;
    private bool _initComplete=false;
    private int _initalXP;
    private int _initialLevel;

    private void GetComponents()
    {
        _levelText = gameObject.transform.Find("LevelDisplay/Level").gameObject.GetComponent<Text>();
        _beastImage = gameObject.transform.Find("BeastPortrait/BeastAnimation").gameObject.GetComponent<Image>();
        _slider = gameObject.transform.Find("Slider").gameObject.GetComponent<Slider>();
        _feedButton = gameObject.transform.Find("FeedButton").gameObject.GetComponent<FeedButton>();
        _initComplete = true;
    }

    public void LoadBeast(BeastState toLoad)
    {
        _loadedState = toLoad;
        if (toLoad != null)
        {
            _initalXP = toLoad.StatDict[Stat.XP];
            _initialLevel = toLoad.Level;
            _levelText.text = _loadedState.Level.ToString();
            _beastImage.enabled = true;
            StartCoroutine(AnimationRoutine());
            _slider.value = _loadedState.StatDict[Stat.XP] / _loadedState.XPToNextLevel + 0.02f; //this is XP-specific -> make generic
        }
        else
        {
            _levelText.text = "";
            _beastImage.enabled = false;
            _slider.value = 0;
            _feedButton.UpdateButtonState(ButtonState.B_State);
        }
        if (_feedButton.gameObject.activeInHierarchy) _feedButton.LoadBeast(toLoad);
    }

    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        int spriteCount = _loadedState.AnimationDictionary["F"].Count;
        while (true)
        {
            _beastImage.sprite = _loadedState.AnimationDictionary["F"][currentFrame];
            if (currentFrame < spriteCount - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.35f);
        }
    }

    private float CalculateSliderValue(int xp, int level)
    {
        float xpToNextLevel = _loadedState.XPToNextLevel; // You might need to adjust this based on the level
        return (float)xp / xpToNextLevel;
    }

    public void AnimateSliderBar()
    {
        if (!(_loadedState.StatDict[Stat.XP] != _initalXP || _initialLevel != _loadedState.Level)) return;
        int levelDifference = _loadedState.Level - _initialLevel;

        if (levelDifference > 0)
        {
            for (int i = 0; i < levelDifference; i++)
            {
                // Calculate the XP needed to fill the bar for the current level
                int xpToFill = _loadedState.XPToNextLevel - _initalXP;
                float fillDuration = xpToFill / 30f; // Assuming 20 XP per second

                // Animate to full for the current level
                StartCoroutine(FillSliderBar(_initalXP, _loadedState.XPToNextLevel, fillDuration));

                // Level up
                _initialLevel++;
                _levelText.text = _initialLevel.ToString();
                _slider.value = 0; // Reset slider for the new level
                _initalXP = 0; // Reset initial XP for calculations for the next iteration
            }
        }

        // Animate any remaining XP for the final level
        if (_loadedState.StatDict[Stat.XP] > 0)
        {
            float finalFillDuration = _loadedState.StatDict[Stat.XP] / 30f; // Adjust rate as needed
            StartCoroutine(FillSliderBar(_initalXP, _loadedState.StatDict[Stat.XP], finalFillDuration));
        }

        // Update the slider to the final value directly to ensure it matches exactly
        _slider.value = CalculateSliderValue(_loadedState.StatDict[Stat.XP], _loadedState.Level);
    }

    // Helper coroutine to animate the XP bar filling
    private IEnumerator FillSliderBar(int startXP, int endXP, float duration)
    {
        float startTime = Time.time;
        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            int currentXP = (int)Mathf.Lerp(startXP, endXP, t);
            _slider.value = CalculateSliderValue(currentXP, _initialLevel);
            yield return null;
        }
        // Ensure the bar reaches the end value
        _slider.value = CalculateSliderValue(endXP, _initialLevel);
    }

    public void ConvertToStyle(SliderPanelStyle style)
    {
        if (!_initComplete) GetComponents();
        StopAllCoroutines();
        Color HPGreen = new Color(67 / 255f, 226 / 255f, 141 / 255f, 137 / 255f);
        Color XPBlue = new Color(72 / 255f, 177 / 255f, 229 / 255f, 204 / 255f);
        Image sliderImage = _slider.gameObject.transform.Find("FilledBar").gameObject.GetComponent<Image>();

        if (style == SliderPanelStyle.XP)
        {
            _feedButton.gameObject.SetActive(false);
            sliderImage.color = XPBlue;
        }
        else
        {
            _feedButton.gameObject.SetActive(true);
            sliderImage.color = HPGreen;
        }

        _panelStyle = style;
    }
}
