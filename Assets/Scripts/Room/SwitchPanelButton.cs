using UnityEngine;
using TMPro;

public class SwitchPanelButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    public void SetText(string text) => this.text.text = text;
}
