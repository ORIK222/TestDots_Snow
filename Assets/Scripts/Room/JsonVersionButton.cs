// using DataManager;
// using Download.Core.UI.Room;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class JsonVersionButton : MonoBehaviour
// {
//     [SerializeField] private Data.Core.DataManager _dataManager;
//     [SerializeField] private ArtworkCollectionsLoader _previewLoader;
//     [SerializeField] private CollectionsView _collectionsView;
//     [SerializeField] private TextMeshProUGUI _text;
//     [SerializeField] private WarningPanel _warningPanel;
//
//     private int _clickCount;
//     private Button _button;
//
//     private void OnEnable()
//     {
//         _button ??= GetComponent<Button>();
//         _dataManager.OnJsonLoaded.AddListener(SetJsonVersionText);
//         _button.onClick.AddListener(SetDebugRootJson);
//     }
//
//     private void SetJsonVersionText()
//     {
//         _text.text = "Json version: " + _dataManager.CollectionHolder.Version;
//     }
//
//     private void SetDebugRootJson()
//     {
//         if (++_clickCount < 3)
//             return;
//         _clickCount = 0;
//         _previewLoader.IncreaseConfigIndex();
//         GlobalControl.IsRootDebugMode = true;
//         _dataManager.GetCollectionsHolder();
//
//         if (_previewLoader.CurrentConfigIndex != 0)
//             _warningPanel.gameObject.SetActive(true);
//     }
// }
