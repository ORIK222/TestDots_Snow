using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SnowXLogoLoader : MonoBehaviour
{
    [SerializeField]
    private Transform parentObject;

    // Start is called before the first frame update
    void Start()
    {
        var asset = Resources.Load<TextAsset>(@"SnowXLogo\SnowxLogo.glb");
        GLBLoader.LoadTiltBrush(new MemoryStream(asset.bytes), (GameObject object1) =>
        {
            object1.transform.parent = parentObject;
            object1.transform.localRotation = Quaternion.identity;
            object1.transform.localPosition = Vector3.zero;
            object1.transform.localScale = Vector3.one;
            object1.SetActive(true);
        },
        null);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
