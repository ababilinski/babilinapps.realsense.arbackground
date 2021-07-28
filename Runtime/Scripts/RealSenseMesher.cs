using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Intel.RealSense;
//Credit: https://github.com/sugi-cho/RealSense-Touch
namespace BabilinApps.RealSense.ARBackground.Runtime
{
    public class RealSenseMesher : MonoBehaviour
    {
        public RsFrameProvider pointSource;
        FrameQueue frameQueue;

        Vector3[] vertices;
        Mesh mesh;

        private Renderer myRenderer;
        private MeshFilter myMeshFilter;

        // Start is called before the first frame update
        void Start()
        {

            myRenderer = GetComponent<Renderer>();
            myMeshFilter = GetComponent<MeshFilter>();
            pointSource.OnStart += OnStart;
            pointSource.OnStop += OnStop;
        }


        private void OnDestroy()
        {
            frameQueue?.Dispose();
        }

        void OnStart(PipelineProfile obj)
        {
            frameQueue = new FrameQueue();

            using (var depth = obj.Streams.FirstOrDefault(s => s.Stream == Stream.Depth && s.Format == Format.Z16)?.As<VideoStreamProfile>())
            {
                if (depth != null)
                {
                    CreateResources(depth.Width, depth.Height);
                }
            }

            pointSource.OnNewSample += OnNewSample;
        }

        private void OnNewSample(Frame frame)
        {
            if (frameQueue == null)
            {
                return;
            }

            try
            {
                if (frame.IsComposite)
                {
                    using (var fs = frame.As<FrameSet>())
                    {
                        using (var points = fs.FirstOrDefault<Points>(Stream.Depth, Format.Xyz32f))
                        {
                            if (points != null)
                            {
                                frameQueue.Enqueue(points);
                            }
                        }

                        return;
                    }
                }

                if (frame.Is(Extension.Points))
                {
                    frameQueue.Enqueue(frame);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void OnStop()
        {
            pointSource.OnNewSample -= OnNewSample;
            if (frameQueue != null)
                frameQueue.Dispose();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (frameQueue != null)
            {
                if (frameQueue.PollForFrame<Points>(out Points points))
                    using (points)
                    {
                        if (points.Count != mesh.vertexCount)
                        {
                            using (var videoStreamProfile = points.GetProfile<VideoStreamProfile>())
                            {
                                CreateResources(videoStreamProfile.Width, videoStreamProfile.Height);
                            }
                        }

                        if (points.VertexData != IntPtr.Zero)
                        {
                            points.CopyVertices(vertices);

                            mesh.vertices = vertices;
                            mesh.UploadMeshData(false);
                        }
                    }
            }
        }

        void CreateResources(int width, int height)
        {
            vertices = new Vector3[width * height];

            var indices = new int[(width - 1) * (height - 1) * 6];

            for (var y = 0; y < height - 1; y++)
            for (var x = 0; x < width - 1; x++)
            {
                var idx = (y * (width - 1) + x) * 6;
                var i0 = y * width + x;
                var i1 = i0 + 1;
                var i2 = i0 + width;
                var i3 = i1 + width;

                indices[idx + 0] = i0;
                indices[idx + 1] = i2;
                indices[idx + 2] = i3;

                indices[idx + 3] = i0;
                indices[idx + 4] = i3;
                indices[idx + 5] = i1;
            }

            if (mesh != null)
                mesh.Clear();
            else
                mesh = new Mesh() {indexFormat = IndexFormat.UInt32,};
            mesh.MarkDynamic();
            mesh.vertices = vertices;

            var uvs = new Vector2[width * height];
            Array.Clear(uvs, 0, uvs.Length);
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    uvs[i + j * width].x = i / (float) width;
                    uvs[i + j * width].y = j / (float) height;
                }
            }

            mesh.uv = uvs;

            mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

            myMeshFilter.sharedMesh = mesh;

            var mpb = new MaterialPropertyBlock();
            myRenderer.SetPropertyBlock(mpb);
        }
    }
}