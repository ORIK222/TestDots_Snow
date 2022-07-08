using DataModels;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Download.Core.UI.Room
{
    public class CollectionView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI collectionNameText;
        [SerializeField] private TextMeshProUGUI authorNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI artworksCountText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image collectionIcon;

        public void Init(Collection collection, UnityAction clickAction)
        {
            collectionNameText.text = collection.Info.Name;
            artworksCountText.text = collection.ArtWorks.Count.ToString();
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(clickAction);
            if (collection.ArtWorks.Count > 0)
            {
                var artwork = collection.ArtWorks[0];
                if (artwork.Urls != null && !string.IsNullOrEmpty(artwork.Urls.Icon))
                {
                    IconDownloaderManager.DownloadSprite(artwork.Urls.Icon, artwork.Name, OnCollectionIconDownloaded);
                }

                if (artwork.Creator != null)
                {
                    authorNameText.text = artwork.Creator.Name;
                    authorNameText.gameObject.SetActive(true);
                }
            }
        }

        private void OnCollectionIconDownloaded(Sprite sprite)
        {
            collectionIcon.sprite = sprite;
            collectionIcon.gameObject.SetActive(true);
        }
    }
}