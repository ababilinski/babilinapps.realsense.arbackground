

/*
 Please Read the Unity Decompiled library for Licensing. 

  This script is the AR Background Renderer script from Unity 2019.4.x. 
  It was only used for reference and to check if it works with Unity 2020.
  Unity 2020.3.x still supports this script as long as you use the Built-in Render Pipeline. 
*/
#if UNITY_2020 || UNITY_2020_1_OR_NEWER

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.XR.ARBackgroundRenderer.Obsolete
{
  

  /// <summary>
  ///   <para>Class used to override a camera's default background rendering path to instead render a given Texture and/or Material. This will typically be used with images from the color camera for rendering the AR background on mobile devices.</para>
  /// </summary>

  public enum ARRenderMode
  {
    /// <summary>
    ///   <para>The standard background is rendered. (Skybox, Solid Color, etc.)</para>
    /// </summary>
    StandardBackground,

    /// <summary>
    ///   <para>The material associated with XR.ARBackgroundRenderer is being rendered as the background.</para>
    /// </summary>
    MaterialAsBackground,
  }
  public class ARBackgroundRenderer
  {
    protected Camera m_Camera = (Camera) null;
    protected Material m_BackgroundMaterial = (Material) null;
    protected Texture m_BackgroundTexture = (Texture) null;
    private ARRenderMode m_RenderMode = ARRenderMode.StandardBackground;
    private CommandBuffer m_CommandBuffer = (CommandBuffer) null;
    private CameraClearFlags m_CameraClearFlags = CameraClearFlags.Skybox;

    public event Action backgroundRendererChanged = null;

    /// <summary>
    ///   <para>The Material used for AR rendering.</para>
    /// </summary>
    public Material backgroundMaterial
    {
      get => this.m_BackgroundMaterial;
      set
      {
        if ((UnityEngine.Object) this.m_BackgroundMaterial == (UnityEngine.Object) value)
          return;
        this.RemoveCommandBuffersIfNeeded();
        this.m_BackgroundMaterial = value;
        if (this.backgroundRendererChanged != null)
          this.backgroundRendererChanged();
        this.ReapplyCommandBuffersIfNeeded();
      }
    }

    /// <summary>
    ///   <para>An optional Texture used for AR rendering. If this property is not set then the texture set in XR.ARBackgroundRenderer._backgroundMaterial as "_MainTex" is used.</para>
    /// </summary>
    public Texture backgroundTexture
    {
      get => this.m_BackgroundTexture;
      set
      {
        if ((bool) (UnityEngine.Object) (this.m_BackgroundTexture = value))
          return;
        this.RemoveCommandBuffersIfNeeded();
        this.m_BackgroundTexture = value;
        if (this.backgroundRendererChanged != null)
          this.backgroundRendererChanged();
        this.ReapplyCommandBuffersIfNeeded();
      }
    }

    /// <summary>
    ///   <para>An optional Camera whose background rendering will be overridden by this class. If this property is not set then the main Camera in the Scene is used.</para>
    /// </summary>
    public Camera camera
    {
      get => (UnityEngine.Object) this.m_Camera != (UnityEngine.Object) null ? this.m_Camera : Camera.main;
      set
      {
        if ((UnityEngine.Object) this.m_Camera == (UnityEngine.Object) value)
          return;
        this.RemoveCommandBuffersIfNeeded();
        this.m_Camera = value;
        if (this.backgroundRendererChanged != null)
          this.backgroundRendererChanged();
        this.ReapplyCommandBuffersIfNeeded();
      }
    }

    /// <summary>
    ///   <para>When set to XR.ARRenderMode.StandardBackground (default) the camera is not overridden to display the background image. Setting this property to XR.ARRenderMode.MaterialAsBackground will render the texture specified by XR.ARBackgroundRenderer._backgroundMaterial and or XR.ARBackgroundRenderer._backgroundTexture as the background.</para>
    /// </summary>
    public ARRenderMode mode
    {
      get => this.m_RenderMode;
      set
      {
        if (value == this.m_RenderMode)
          return;
        this.m_RenderMode = value;
        switch (this.m_RenderMode)
        {
          case ARRenderMode.StandardBackground:
            this.DisableARBackgroundRendering();
            break;
          case ARRenderMode.MaterialAsBackground:
            this.EnableARBackgroundRendering();
            break;
          default:
            throw new Exception("Unhandled render mode.");
        }
        if (this.backgroundRendererChanged == null)
          return;
        this.backgroundRendererChanged();
      }
    }

    protected bool EnableARBackgroundRendering()
    {
      if ((UnityEngine.Object) this.m_BackgroundMaterial == (UnityEngine.Object) null)
        return false;
      Camera camera = !((UnityEngine.Object) this.m_Camera != (UnityEngine.Object) null) ? Camera.main : this.m_Camera;
      if ((UnityEngine.Object) camera == (UnityEngine.Object) null)
        return false;
      this.m_CameraClearFlags = camera.clearFlags;
      camera.clearFlags = CameraClearFlags.Depth;
      this.m_CommandBuffer = new CommandBuffer();
      Texture source = this.m_BackgroundTexture;
      if ((UnityEngine.Object) source == (UnityEngine.Object) null && this.m_BackgroundMaterial.HasProperty("_MainTex"))
        source = this.m_BackgroundMaterial.GetTexture("_MainTex");
      this.m_CommandBuffer.Blit(source, (RenderTargetIdentifier) BuiltinRenderTextureType.CameraTarget, this.m_BackgroundMaterial);
      camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, this.m_CommandBuffer);
      camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, this.m_CommandBuffer);
      return true;
    }

    /// <summary>
    ///   <para>Disables AR background rendering. This method is called internally but can be overridden by users who wish to subclass XR.ARBackgroundRenderer to customize handling of AR background rendering.</para>
    /// </summary>
    protected void DisableARBackgroundRendering()
    {
      if (this.m_CommandBuffer == null)
        return;
      Camera camera = this.m_Camera ?? Camera.main;
      if ((UnityEngine.Object) camera == (UnityEngine.Object) null)
        return;
      camera.clearFlags = this.m_CameraClearFlags;
      camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, this.m_CommandBuffer);
      camera.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, this.m_CommandBuffer);
    }

    private bool ReapplyCommandBuffersIfNeeded()
    {
      if (this.m_RenderMode != ARRenderMode.MaterialAsBackground)
        return false;
      this.EnableARBackgroundRendering();
      return true;
    }

    private bool RemoveCommandBuffersIfNeeded()
    {
      if (this.m_RenderMode != ARRenderMode.MaterialAsBackground)
        return false;
      this.DisableARBackgroundRendering();
      return true;
    }
  }

}
#endif