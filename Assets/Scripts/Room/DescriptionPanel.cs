/*
using System;
using Download.Core.Editor;
using UnityEngine;
using TMPro;
using WalletConnectSharp.Core.Models.Ethereum;
using WalletConnectSharp.Unity;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections.Generic;
using Download.Core;
using _Core.Scripts.Downolad;

public class DescriptionPanel : WalletConnectActions
{
    [SerializeField] private QrCodePanel qrCodePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI authorNameText;
    [SerializeField] private TextMeshProUGUI ownerNameText;
    [SerializeField] private TextMeshProUGUI parentCollectionNameText;
    [SerializeField] private Image creatorImage;
    [SerializeField] private Image ownerImage;
    [SerializeField] private Image parentCollectionIconImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Tex/*tMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI buyText;
    [SerializeField] private GameObject fadePanel;

    private bool isSetUp;
    private Transform target;
    private GLBData artwork;
    private string mintResults;
    private MintStatus status;

    private void Start()
    {
        qrCodePanel.OnConnected += () =>
        {
            if (status == MintStatus.Initialized)
            {
                status = MintStatus.Started;
                this.fadePanel.SetActive(false);
                Task.Run(() => ProcessMintTransaction());
            }
        };
    }

    public void ClearData()
    {
        IconDownloaderManager.Unsubscribe(OnCreatorSpriteDownloaded);
        IconDownloaderManager.Unsubscribe(OnOwnerSpriteDownloaded);
        IconDownloaderManager.Unsubscribe(OnRootCollectionSpriteDownloaded);
        creatorImage.sprite = null;
        ownerImage.sprite = null;
        parentCollectionIconImage.sprite = null;
    }

    public void SetData(GLBData artwork)
    {
        if (artwork is null)
        {
            throw new ArgumentNullException(nameof(artwork));
        }

        this.artwork = artwork;
        titleText.text = artwork.Name;
        if (string.IsNullOrEmpty(artwork.Description))
        {
            descriptionText.gameObject.SetActive(false);
        }
        else
        {
            descriptionText.text = artwork.Description;
            descriptionText.gameObject.SetActive(true);
        }

        authorNameText.text = artwork.Creator.Name;
        ownerNameText.text = artwork.Creator.Name;
        parentCollectionNameText.text = artwork.RootCollection.Info.Name;
        if (string.IsNullOrEmpty(artwork.PriceType) || Mathf.Abs(artwork.Price) < 0.0001f)
        {
            priceText.gameObject.SetActive(false);
        }
        else
        {
            priceText.text = $"{artwork.Price} {artwork.PriceType}";
            priceText.gameObject.SetActive(true);
        }

        buyText.text = $"BUY at {artwork.Price} {artwork.PriceType}";

        // set icons
        if (artwork.Creator != null && artwork.Creator.Urls != null && !string.IsNullOrEmpty(artwork.Creator.Urls.IconUrl))
        {
            string url = artwork.Creator.Urls.IconUrl;
            IconDownloaderManager.DownloadSprite(url, artwork.Creator.Name, OnCreatorSpriteDownloaded);
        }

        if (artwork.Owner != null && artwork.Owner.Urls != null && !string.IsNullOrEmpty(artwork.Owner.Urls.IconUrl))
        {
            string url = artwork.Owner.Urls.IconUrl;
            IconDownloaderManager.DownloadSprite(url, artwork.Owner.Name, OnOwnerSpriteDownloaded);
        }

        if (artwork.RootCollection != null && artwork.RootCollection.Info != null && !string.IsNullOrEmpty(artwork.RootCollection.Info.IconUrl))
        {
            string url = artwork.RootCollection.Info.IconUrl;
            IconDownloaderManager.DownloadSprite(url, artwork.RootCollection.Info.Name, OnRootCollectionSpriteDownloaded);
        }

        if (artwork.Owner != null && artwork.Owner.Name != null)
        {
            ownerNameText.text = ShortenString(artwork.Owner.Name);
        }
        else
        {
            ownerNameText.text = null;
        }    

        buyText.transform.parent.gameObject.SetActive(artwork.PurchaseData != null);
        priceText.transform.parent.gameObject.SetActive(artwork.PurchaseData != null);
        this.fadePanel.SetActive(false);
        qrCodePanel.gameObject.SetActive(false);
        buyText.transform.parent.GetComponent<Button>().interactable = artwork.PurchaseData != null;

        target = Camera.main.gameObject.transform;
        isSetUp = true;
    }

    private string ShortenString(string text, int leftSymbolsCount = 5, int rightSymbolsCount = 4)
    {
        if (text == null)
        {
            throw new ArgumentNullException(text);
        }

        if (leftSymbolsCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rightSymbolsCount), "The position cannot be less than 0");
        }

        if (rightSymbolsCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rightSymbolsCount), "The position cannot be less than 0");
        }

        string resultText = text;
        if (text.Length >= leftSymbolsCount + rightSymbolsCount)
        {
            resultText = text.Substring(0, leftSymbolsCount) + "..." + text.Substring(text.Length - rightSymbolsCount, rightSymbolsCount);
        }

        return resultText;
    }

    private void Update()
    {
        if (isSetUp && target)
        {
            transform.LookAt(target.position);
#if UNITY_EDITOR || UNITY_WEBGL
            // for properly display description panel in the play mode.
            if (!RuntimeControl.Instance.IsVR)
            {
                transform.rotation *= Quaternion.Euler(0, 180, 0);
                var scale = transform.localScale;
                if (scale.x < 0)
                {
                    scale = new Vector3(-scale.x, scale.y, scale.z);
                    transform.localScale = scale;
                }
            }
#endif
        }

        switch (status)
        {
            case MintStatus.FinishedOK:
                resultText.text = mintResults;
                buyText.transform.parent.GetComponent<Button>().interactable = false;
                artwork.PurchaseData = null;
                break;
            case MintStatus.FinishedFailure:
                Debug.LogError(mintResults);
                status = MintStatus.Initialized;
                buyText.transform.parent.GetComponent<Button>().interactable = true;
                break;
        }
    }

    public void OnClickSendMintTransaction()
    {
        if (status == MintStatus.Initialized)
        {
            if (!isSetUp)
            {
                throw new Exception("The WalletConnect API was not setup properly.");
            }

            if (artwork.PurchaseData == null)
            {
                throw new ArgumentOutOfRangeException(nameof(artwork.PurchaseData), "The artwork has no purchase data set. Please check the Json is properly configured.");
            }

            this.fadePanel.SetActive(true);
            qrCodePanel.gameObject.SetActive(true);
        }
    }


    private async void ProcessMintTransaction()
    {
        mintResults = null;
        if (WalletConnect.Instance.Connected)
        {
            var address = WalletConnect.ActiveSession.Accounts[0];
            var transaction = new TransactionData()
            {
                data = artwork.PurchaseData.Data,
                from = address,
                to = artwork.PurchaseData.To,
                value = artwork.PurchaseData.Value,
                chainId = artwork.PurchaseData.ChainId
            };

            try
            {
                mintResults = await SendTransaction(transaction);
                status = MintStatus.FinishedOK;
            }
            catch(Exception exc)
            {
                mintResults = exc.ToString();
                status = MintStatus.FinishedFailure;
            }
        }
        else
        {
            mintResults = "Not connected";
            status = MintStatus.FinishedFailure;
        }
    }

    private void OnCreatorSpriteDownloaded(Sprite sprite)
    {
        this.creatorImage.sprite = sprite;
    }

    private void OnOwnerSpriteDownloaded(Sprite sprite)
    {
        this.ownerImage.sprite = sprite;
    }

    private void OnRootCollectionSpriteDownloaded(Sprite sprite)
    {
        this.parentCollectionIconImage.sprite = sprite;
    }

    private enum MintStatus
    {
        Initialized,
        Started,
        FinishedOK,
        FinishedFailure
    }
}
#1#
*/
