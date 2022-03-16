using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FlaxEngine;

namespace Game
{
    /// <summary>
    /// NoiseGenerator Script.
    /// </summary>
    [ExecuteInEditMode]
    public class NoiseGenerator : Script
    {

        [EditorOrder(1), EditorDisplay(name: "Live Rendering"), ReadOnly]
        public bool LiveRender = false; //LiveRender to working
        [EditorOrder(2), EditorDisplay(name: "Compute Shader")]
        public Shader noiseCompute;
        [EditorOrder(3), EditorDisplay(name: "Shape Resolution")]
        public float shapeResolution = 136.0f;
        [EditorDisplay(name: "Detail Resolution"), EditorOrder(4)]
        public float detailResolution = 32.0f;
        [EditorDisplay(name: "Settings"), EditorOrder(5)]
        public JsonAsset JSON_settings;
        [HideInEditor]
        public WorleyNoiseSettings settings;

        [HideInEditor]
        public GPUTexture ShapeTexture;
        [HideInEditor]
        public GPUTexture DetailTexture;

        private bool _isComputeSupported;

        List<GPUBuffer> buffersToRelease = new List<GPUBuffer>();

        [StructLayout(LayoutKind.Sequential)]
        private struct Data
        {
            public Vector2 Dummy0;
            public float Resolution;
            public float Persistence;
            public int numCellsA;
            public int numCellsB;
            public int numCellsC;
            public int tile;
            public Vector4 channelMask;
            public bool invertNoise;
        }

        public void GenerateTextures()
        {
            _isComputeSupported = GPUDevice.Instance.Limits.HasCompute;

            if (JSON_settings)
            {
                settings = (WorleyNoiseSettings)JSON_settings.CreateInstance();
            }
            else
            {
                Debug.LogError("[NoiseGenerator] No settings file provided");
                return;
            }

            ShapeTexture = new GPUTexture();
            var desc1 = GPUTextureDescription.New3D((int)shapeResolution, (int)shapeResolution, (int)shapeResolution, PixelFormat.R16G16B16A16_UNorm, GPUTextureFlags.ShaderResource | GPUTextureFlags.UnorderedAccess | GPUTextureFlags.RenderTarget);
            if (ShapeTexture.Init(ref desc1))
                return;

            DetailTexture = new GPUTexture();
            var desc2 = GPUTextureDescription.New3D((int)detailResolution, (int)detailResolution, (int)detailResolution, PixelFormat.R16G16B16A16_UNorm, GPUTextureFlags.ShaderResource | GPUTextureFlags.UnorderedAccess | GPUTextureFlags.RenderTarget);
            if (DetailTexture.Init(ref desc2))
                return;

            var rt = new RenderTask();
            rt.Render += _rt_Render;
        }

        private void _rt_Render(RenderTask rt, GPUContext context)
        {
            if (LiveRender) //LiveRender to working
            {
                if (JSON_settings)
                {
                    settings = (WorleyNoiseSettings)JSON_settings.CreateInstance();
                } else
                {
                    Debug.LogError("[NoiseGenerator] No settings file provided");
                    return;
                }
            }
            //ShapeTexture
            //Red
            if (settings.shapeSettings[0].Enable)
                _Generate(context, ShapeTexture, settings.shapeSettings[0], new Vector4(1, 0, 0, 0));
            //Green
            if (settings.shapeSettings[1].Enable)
                _Generate(context, ShapeTexture, settings.shapeSettings[1], new Vector4(0, 1, 0, 0));
            //Blue
            if (settings.shapeSettings[2].Enable)
                _Generate(context, ShapeTexture, settings.shapeSettings[2], new Vector4(0, 0, 1, 0));
            //Alpha
            if (settings.shapeSettings[3].Enable)
                _Generate(context, ShapeTexture, settings.shapeSettings[3], new Vector4(0, 0, 0, 1));

            //DetailTexture
            //Red
            if (settings.detailSettings[0].Enable)
                _Generate(context, DetailTexture, settings.detailSettings[0], new Vector4(1, 0, 0, 0));
            //Green
            if (settings.detailSettings[1].Enable)
                _Generate(context, DetailTexture, settings.detailSettings[1], new Vector4(0, 1, 0, 0));
            //Blue
            if (settings.detailSettings[2].Enable)
                _Generate(context, DetailTexture, settings.detailSettings[2], new Vector4(0, 0, 1, 0));
            //Alpha
            if (settings.detailSettings[3].Enable)
                _Generate(context, DetailTexture, settings.detailSettings[3], new Vector4(0, 0, 0, 1));

            Debug.Log("[NoiseGenerator] Noise textures generated!");

            foreach (var buffer in buffersToRelease)
            {
                buffer.ReleaseGPU();
            }

            if (!LiveRender)
            {
                rt.Render -= _rt_Render;
            }
        }

        private unsafe bool _Generate(GPUContext context, GPUTexture texture, WorleyNoiseSettingsStruct settings, Vector4 channelMask)
        {
            GPUBuffer pointsA = _UpdateWorley(settings.numDivisionsA);
            GPUBuffer pointsB = _UpdateWorley(settings.numDivisionsB);
            GPUBuffer pointsC = _UpdateWorley(settings.numDivisionsC);

            var minMax = _CreateMinMaxBuffer(new int[] { int.MaxValue, 0 }, sizeof(int));

            context.BindSR(0, (GPUTexture)null);
            context.BindUA(0, null);
            context.FlushState();
            context.ResetCB();

            var cb = noiseCompute.GPU.GetCB(0);
            if (cb != IntPtr.Zero)
            {
                var data = new Data
                {
                    Resolution = texture.Width,
                    Persistence = settings.persistence,
                    numCellsA = settings.numDivisionsA,
                    numCellsB = settings.numDivisionsB,
                    numCellsC = settings.numDivisionsC,
                    tile = settings.tile,
                    channelMask = channelMask,
                    invertNoise = settings.invert,
                };
                context.UpdateCB(cb, new IntPtr(&data));
            }


            context.BindCB(0, cb);
            context.BindUA(0, texture.ViewVolume());
            context.BindUA(1, minMax.View());
            context.BindSR(1, pointsA.View());
            context.BindSR(2, pointsB.View());
            context.BindSR(3, pointsC.View());
            var csBlurH = noiseCompute.GPU.GetCS("CS_Generate");
            int groupCountX = texture.Width / 8;
            int groupCountY = texture.Height / 8;
            context.Dispatch(csBlurH, (uint)groupCountX, (uint)groupCountY, (uint)texture.Depth / 8);

            var csNorm = noiseCompute.GPU.GetCS("CS_Normalize");
            context.Dispatch(csNorm, (uint)groupCountX, (uint)groupCountY, (uint)texture.Depth / 8);

            context.BindSR(0, (GPUTexture)null);
            context.BindUA(0, null);
            context.FlushState();

            return true;
        }

        private GPUBuffer _UpdateWorley(int div)
        {
            var prng = new System.Random(0);
            return _CreateWorleyPointsBuffer(prng, div);
        }
        private GPUBuffer _CreateWorleyPointsBuffer(System.Random prng, int numCellsPerAxis)
        {
            var points = new Vector3[numCellsPerAxis * numCellsPerAxis * numCellsPerAxis];
            float cellSize = 1f / numCellsPerAxis;

            for (int x = 0; x < numCellsPerAxis; x++)
            {
                for (int y = 0; y < numCellsPerAxis; y++)
                {
                    for (int z = 0; z < numCellsPerAxis; z++)
                    {
                        float randomX = (float)prng.NextDouble();
                        float randomY = (float)prng.NextDouble();
                        float randomZ = (float)prng.NextDouble();
                        Vector3 randomOffset = new Vector3(randomX, randomY, randomZ) * cellSize;
                        Vector3 cellCorner = new Vector3(x, y, z) * cellSize;

                        int index = x + numCellsPerAxis * (y + z * numCellsPerAxis);
                        points[index] = cellCorner + randomOffset;
                    }
                }
            }
            return _CreatePointsBuffer(points, sizeof(float) * 3);
        }

        private unsafe GPUBuffer _CreatePointsBuffer(Vector3[] data, int stride)
        {
            var buffer = new GPUBuffer();
            fixed (Vector3* ptr = data)
            {
                var desc = GPUBufferDescription.Structured(data.Length, stride, false);
                desc.InitData = new IntPtr(ptr);
                buffer.Init(ref desc);
            }
            buffersToRelease.Add(buffer);
            return buffer;
        }

        private unsafe GPUBuffer _CreateMinMaxBuffer(int[] data, int stride)
        {
            var buffer = new GPUBuffer();
            fixed (int* ptr = data)
            {
                var desc = GPUBufferDescription.Structured(data.Length, stride, true);
                desc.InitData = new IntPtr(ptr);
                buffer.Init(ref desc);
            }
            buffersToRelease.Add(buffer);
            return buffer;
        }


        /// <inheritdoc/>
        public override void OnEnable()
        {
            GenerateTextures();
            // Here you can add code that needs to be called when script is enabled (eg. register for events)
        }
    }
}
