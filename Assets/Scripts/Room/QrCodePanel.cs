using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QrCodePanel : MonoBehaviour
{
    /*[SerializeField] private WalletConnectQRImage walletConnectQrImage;
    [SerializeField] private Image qrCodeImage;
    //[SerializeField] private VRKeyboard.Utils.KeyboardManager VrKeyboard;

    /// <summary>
    /// This value tells us how far from the camera the keyboard is.
    /// </summary>
    [SerializeField] private float vrKeyboardDistance = 3.76f;

    /// <summary>
    /// This value tells us how much space should the keyboard take on the screen by width. Value of 1 means the whole screen space is taken.
    /// </summary>
    [SerializeField] private float vrKeyboardWidthScale = 0.03f;

    private float scaleInitial;

    public Action OnConnected;

    private void Awake()
    {
        this.scaleInitial = VrKeyboard.transform.localScale.x;
        VrKeyboard.OnEnterClicked += () => VrKeyboard.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (WalletConnect.Instance.Connected)
        {
            ClosePanel();
        }
        else
        {
            qrCodeImage.sprite = GetSprite();
            WalletConnect.Instance.ConnectedEvent.AddListener(ClosePanel);

            // setting up the virtual keyboard to enter e-mail.
            VrKeyboard.gameObject.SetActive(true);
            UpdateVRKeyboardPosition();
            RectTransform rt = VrKeyboard.GetComponent<RectTransform>();
            Rect rect = rt.rect;
            float z = Vector3.Distance(VrKeyboard.transform.position, Camera.main.transform.position);
            Vector3 pointLow = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, z));
            Vector3 pointHight = Camera.main.ScreenToWorldPoint(new Vector3(rect.width, 0, z));
            Vector3 cameraLow = Camera.main.ViewportPointToRay(new Vector3(0, 0.5f, 0)).GetPoint(z);
            Vector3 cameraHigh = Camera.main.ViewportPointToRay(new Vector3(1, 0.5f, 0)).GetPoint(z);
            float widthCurrent = Vector3.Distance(pointLow, pointHight) / Vector3.Distance(cameraLow, cameraHigh);
            float newScale = vrKeyboardWidthScale / widthCurrent * this.scaleInitial;
            VrKeyboard.transform.localScale = new Vector3(newScale, newScale, newScale);
        }
    }

    private void UpdateVRKeyboardPosition()
    {
        // here 0.5;0.5 means center of the screen.
        var point = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)).GetPoint(vrKeyboardDistance);
        VrKeyboard.transform.position = point;
        VrKeyboard.transform.rotation = Camera.main.transform.rotation;
    }

    private void Update()
    {
        if (!WalletConnect.Instance.Connected)
        {
            // for redrawing of QR code when Oculus goes to sleep and back.
            qrCodeImage.sprite = GetSprite();
            UpdateVRKeyboardPosition();
        }
    }

    private Sprite GetSprite()
    {
        Sprite sprite = null;
        Image image = walletConnectQrImage.GetQrCode();
        if (image != null)
        {
            sprite = image.sprite;
        }

        return sprite;
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
        OnConnected?.Invoke();
    }

    private void OnDisable()
    {
        WalletConnect.Instance.ConnectedEvent.RemoveListener(ClosePanel);
    }*/
}
