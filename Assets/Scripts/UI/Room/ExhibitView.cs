using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ExhibitView : MayInitialize
{
    [SerializeField] private Image modelPreviewImage;
    [SerializeField] private Text stateText;

    private Sprite defaultSprite;
    private Vector2 anchorMin;
    private Vector2 anchorMax;
    private Vector2 sizeDelta;
    private RectTransform rectState;
    private TextAnchor anchor;
    private int fontSize;
    private Color defaultColor;

    protected override void OnInitialize()
    {
        rectState = this.stateText.GetComponent<RectTransform>();
        anchorMin = rectState.anchorMin;
        anchorMax = rectState.anchorMax;
        sizeDelta = rectState.sizeDelta;
        anchor = this.stateText.alignment;
        this.fontSize = this.stateText.fontSize;
        defaultSprite = modelPreviewImage.sprite;
        defaultColor = modelPreviewImage.color;
    }


    public void SetPreviewImage(Sprite previewSprite) => modelPreviewImage.sprite = previewSprite;
    public void SetLoadModelState(string loadStateText)
    {
        stateText.text = loadStateText;
        this.stateText.alignment = anchor;
        this.stateText.fontSize = this.fontSize;
        rectState.anchorMin = anchorMin;
        rectState.anchorMax = anchorMax;
        rectState.sizeDelta = sizeDelta;
    }

    public void SetLoadModelErrorState(string errorText)
    {
        stateText.text = errorText;
        this.stateText.alignment = TextAnchor.UpperCenter;
        this.stateText.fontSize = 30;
        rectState.anchorMin = Vector2.zero;
        rectState.anchorMax = Vector2.one;
        rectState.sizeDelta = Vector2.zero;
    }

    public void DisablePreview(Color? backColor = null)
    {
        if (backColor == null)
        {
            modelPreviewImage.enabled = false;
        }
        else
        {
            modelPreviewImage.enabled = true;
            modelPreviewImage.sprite = null;
            modelPreviewImage.color = backColor.Value;
        }
    }

    public void EnabledPreviewImage()
    {
        modelPreviewImage.enabled = true;
        modelPreviewImage.sprite = defaultSprite;
        modelPreviewImage.color = defaultColor;
    }
}
