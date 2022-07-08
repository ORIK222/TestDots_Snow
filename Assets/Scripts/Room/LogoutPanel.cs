using TMPro;using UnityEngine;


public class LogoutPanel : MonoBehaviour
{
    /*[SerializeField] private RectTransform _logoutPanel;
    [SerializeField] private TextMeshProUGUI _walletAdressText;

#if UNITY_EDITOR
    private Quaternion logoutButtonQuaternion;
    private Vector3 logoutButtonScale;
    private Button logoutButton;

    private void Start()
    {
        logoutButton = _logoutPanel.GetComponentInChildren<Button>(true);
        logoutButtonQuaternion = logoutButton.transform.localRotation;
        logoutButtonScale = logoutButton.transform.localScale;
    }

    private void Update()
    {
        if (RuntimeControl.Instance.IsVR)
        {
            logoutButton.transform.localRotation = logoutButtonQuaternion;
            logoutButton.transform.localScale = logoutButtonScale;
        }
        else
        {
            logoutButton.transform.localRotation = Quaternion.identity;
            logoutButton.transform.localScale = new Vector3(Mathf.Abs(logoutButtonScale.x), Mathf.Abs(logoutButtonScale.y), Mathf.Abs(logoutButtonScale.z));
        }
    }

#endif

    private void OnEnable()
    {
        WalletConnect.Instance.ConnectedEvent.AddListener(EnableLogoutButton);
        WalletConnect.Instance.ConnectedEventSession.AddListener(SetWalletAdress);
        WalletConnect.Instance.DisconnectedEvent.AddListener(DisableLogoutButton);
    }

    private void SetWalletAdress(WCSessionData arg0)
    {
        _walletAdressText.text = "Wallet adress: " + WalletConnect.ActiveSession.Accounts[0];
    }

    private void DisableLogoutButton(WalletConnectUnitySession arg0)
    {
        _logoutPanel.gameObject.SetActive(false);
    }

    private void EnableLogoutButton()
    {
        _logoutPanel.gameObject.SetActive(true);
    }

    public async void LogoutWallet()
    {
        Debug.Log("Logout");
        await WalletConnect.Instance.Disconnect();
    }*/
}
