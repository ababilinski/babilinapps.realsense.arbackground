using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;




namespace BabilinApps.RealSense.ARBackground.Runtime
{
    /// <summary>
    /// A render feature for rendering the camera background for AR devies.
    /// </summary>
    public class RealSenseBackgroundRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        [SerializeField]
        private Settings _settings = new Settings();


        /// <summary>
        /// The scriptable render pass to be added to the renderer when the camera background is to be rendered.
        /// </summary>
        RenderCameraDepthPass _renderCameraDepthPass;


        /// <summary>
        /// The mesh for rendering the background shader.
        /// </summary>
        Mesh _backgroundMesh;

        /// <summary>
        /// Create the scriptable render pass.
        /// </summary>
        public override void Create()
        {

            _renderCameraDepthPass = new RenderCameraDepthPass("RenderCameraDepthPass") {renderPassEvent = _settings.renderEvent};


            _backgroundMesh = new Mesh();
            _backgroundMesh.vertices = new Vector3[]
            {
                new Vector3(0f, 0f, 0.1f),
                new Vector3(0f, 1f, 0.1f),
                new Vector3(1f, 1f, 0.1f),
                new Vector3(1f, 0f, 0.1f),
            };
            _backgroundMesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
            };
            _backgroundMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        }

        /// <summary>
        /// Add the background rendering pass when rendering a game camera with an enabled AR camera background component.
        /// </summary>
        /// <param name="renderer">The sriptable renderer in which to enqueue the render pass.</param>
        /// <param name="renderingData">Additional rendering data about the current state of rendering.</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {

            if (!Application.isPlaying)
            {
                return;
            }

            var currentCamera = renderingData.cameraData.camera;
            if ((currentCamera == null) || (currentCamera.cameraType != CameraType.Game))
            {
                return;
            }

            RealSenseARBackgroundURP cameraBackground = currentCamera.gameObject.GetComponent<RealSenseARBackgroundURP>();
            if ((cameraBackground == null) || (cameraBackground.BackgroundMaterial == null))
            {
                return;
            }
            
            _renderCameraDepthPass.Setup(_backgroundMesh, cameraBackground.BackgroundMaterial, renderer);
         
            renderer.EnqueuePass(_renderCameraDepthPass);

        }


        /// <summary>
        /// The custom render pass to render the camera background.
        /// </summary>
        class RenderCameraDepthPass : ScriptableRenderPass
        {
            Mesh _backgroundMesh;
            static readonly Matrix4x4 _backgroundOrthoProjection = Matrix4x4.Ortho(0f, 1f, 0f, 1f, -0.1f, 9.9f);
            private string _profilingName;
            private Material _material;
            private ScriptableRenderer _renderer;

          
            public RenderCameraDepthPass(string profilingName) : base()
            {
             
                this._profilingName = profilingName;

            }

            public void Setup(Mesh backgroundMesh, Material depthCameraMaterial, ScriptableRenderer renderer)
            {
                this._backgroundMesh = backgroundMesh;
                _renderer = renderer;
                _material = depthCameraMaterial;
               
            }


            /// <summary>
            /// Configure the render pass by configuring the render target and clear values.
            /// </summary>
            /// <param name="commandBuffer">The command buffer for configuration.</param>
            /// <param name="renderTextureDescriptor">The descriptor of the target render texture.</param>
            public override void Configure(CommandBuffer commandBuffer, RenderTextureDescriptor renderTextureDescriptor)
            {
           
               ConfigureTarget(_renderer.cameraColorTarget, _renderer.cameraDepthTarget);

               ConfigureClear(ClearFlag.Depth, Color.clear);

            }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(_profilingName);
             
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, _backgroundOrthoProjection);

                cmd.DrawMesh(_backgroundMesh, Matrix4x4.identity, _material,0,-1);
                cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix,
                                              renderingData.cameraData.camera.projectionMatrix);
                context.ExecuteCommandBuffer(cmd);
                cmd.SetInvertCulling(false);
             
                CommandBufferPool.Release(cmd);
            }

         
        }


    }
}
