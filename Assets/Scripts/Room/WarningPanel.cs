using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WarningPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _warningText;

    public void SetText(string text) => _warningText.text = text;
    public void Close() => gameObject.SetActive(false);
}
