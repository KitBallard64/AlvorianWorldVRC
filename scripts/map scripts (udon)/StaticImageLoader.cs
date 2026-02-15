using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;

public class StaticImageLoader : UdonSharpBehaviour
{
    public RawImage targetRawImage;
    public VRCUrl imageUrl;

    private VRCImageDownloader imageDownloader;

    void Start()
    {
        imageDownloader = new VRCImageDownloader();
        imageDownloader.DownloadImage(imageUrl, null, (IUdonEventReceiver)this);
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        Texture2D downloadedImage = result.Result;
        targetRawImage.texture = downloadedImage;
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.LogError("Failed to load image: " + result.Error);
    }
}
