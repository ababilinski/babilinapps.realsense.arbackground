
using System.Collections;
using Intel.RealSense;
using UnityEngine;

namespace BabilinApps.RealSense.ARBackground.Runtime
{

public class RealSenseARBackgroundURP : MonoBehaviour
{
    public Texture GetTexture => BackgroundMaterial.mainTexture;
    public RsFrameProvider Source;
    public Material BackgroundMaterial;

    private Camera _camera;
    private Intrinsics _intrinsics;
    private Vector2Int _screenSize;

    IEnumerator Start()
    {
        var currentScreenSize = new Vector2Int(Screen.width, Screen.height);
        _screenSize = currentScreenSize;
        _camera = GetComponent<Camera>();
        yield return new WaitUntil(() => Source && Source.Streaming);

        using (var profile = Source.ActiveProfile.GetStream<VideoStreamProfile>(Stream.Color))
        {
            _intrinsics = profile.GetIntrinsics();
            UpdateInGameCameraIntrinsics();
   
        }

    #region ONLY WORK IN BUILT-IN RENDER PIPELINE

        //cam = GetComponent<Camera>();

        //cam.depthTextureMode |= DepthTextureMode.Depth;
        //// Uses the same material as above to update camera's depth texture
        //// Unity will use it when calculating shadows
        //var updateCamDepthTexture = new CommandBuffer() {name = "UpdateDepthTexture"};
        //updateCamDepthTexture.Blit(BuiltinRenderTextureType.None, BuiltinRenderTextureType.CurrentActive, material);
        //updateCamDepthTexture.SetGlobalTexture("_ShadowMapTexture", Texture2D.whiteTexture);
        //updateCamDepthTexture.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute );

        //cam.AddCommandBuffer(CameraEvent.AfterDepthTexture, updateCamDepthTexture);

        //// assume single directional light
        //var light = FindObjectOfType<Light>();

        //// Copy resulting screenspace shadow map, ARBackgroundRenderer's material will multiply it over color image
        //var copyScreenSpaceShadow = new CommandBuffer {name = "CopyScreenSpaceShadow"};
        //int shadowCopyId = Shader.PropertyToID("_ShadowMapTexture");
        //copyScreenSpaceShadow.GetTemporaryRT(shadowCopyId, -1, -1, 0);
        //copyScreenSpaceShadow.CopyTexture(BuiltinRenderTextureType.CurrentActive, shadowCopyId);
        //copyScreenSpaceShadow.SetGlobalTexture(shadowCopyId, shadowCopyId);
        //light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, copyScreenSpaceShadow);
        

    #endregion

    }

    void OnEnable()
    {
        var currentScreenSize = new Vector2Int(Screen.width, Screen.height);
        _screenSize = currentScreenSize;
        
    }


    void UpdateInGameCameraIntrinsics()
    {
        var projectionMatrix = new Matrix4x4
                               {
                                   m00 = _intrinsics.fx,
                                   m11 = -_intrinsics.fy,
                                   m03 = _intrinsics.ppx / _intrinsics.width,
                                   m13 = _intrinsics.ppy / _intrinsics.height,
                                   m22 = (_camera.nearClipPlane + _camera.farClipPlane) * 0.5f,
                                   m23 = _camera.nearClipPlane * _camera.farClipPlane,
                               };
        float r = (float) _intrinsics.width / Screen.width;
        projectionMatrix = Matrix4x4.Ortho(0, Screen.width * r, Screen.height * r, 0, _camera.nearClipPlane, _camera.farClipPlane) * projectionMatrix;
        projectionMatrix.m32 = -1;
        _camera.fieldOfView = (_intrinsics.FOV[0] + _intrinsics.FOV[1]) / 2;
        _camera.projectionMatrix = projectionMatrix;
    }

    void Update()
    {

        if (_camera == null)
            return;

        var currentScreenSize = new Vector2Int(Screen.width, Screen.height);
        if (_screenSize != currentScreenSize)
        {
            _screenSize = currentScreenSize;

            UpdateInGameCameraIntrinsics();
        }

       
    }

    public Vector2Int GetImagePoint(Vector2 screenPos)
    {
        var vp = (Vector2)Camera.main.ScreenToViewportPoint(screenPos);
        vp.y = 1f - vp.y;

        float sr = (float)Screen.width / Screen.height;
        float tr = (float)_intrinsics.height / _intrinsics.width;
        float sh = sr * tr;
        vp -= 0.5f * Vector2.one;
        vp.y /= sh;
        vp += 0.5f * Vector2.one;

        return new Vector2Int((int)(vp.x * _intrinsics.width), (int)(vp.y * _intrinsics.height));
    }
}
}