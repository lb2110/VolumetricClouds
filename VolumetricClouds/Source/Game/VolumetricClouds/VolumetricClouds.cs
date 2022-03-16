using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FlaxEngine;

namespace Game
{
    /// <summary>
    /// VolumetricClouds Script.
    /// </summary>
    [ExecuteInEditMode]
    public class VolumetricClouds : PostProcessEffect
    {
        private GPUPipelineState _psFullscreen;
        private Shader _shader;
        private NoiseGenerator noiseGen;

        private JsonAsset _JSON_Settings;
        [EditorOrder(8), EditorDisplay(name: "Settings")]
        public JsonAsset JSON_Settings
        {
            get => _JSON_Settings;
            set
            {
                if (_JSON_Settings != value)
                {
                    _JSON_Settings = value;
                    UpdateSettings();
                }
            }
        }
        private VolumetricCloudSettings Settings;

        [EditorOrder(2), EditorDisplay(name: "Camera")]
        public Camera cam;
        [EditorOrder(3), EditorDisplay(name: "Clouds Container")]
        public Actor container;
        [EditorOrder(4), EditorDisplay(name: "Sun")]
        public Actor sun;
        [EditorOrder(6), EditorDisplay(name: "Blue Noise")]
        public Texture BlueNoise;
        [EditorOrder(5), EditorDisplay(name: "Noise Generator")]
        public Actor noiseGenerator;
        [EditorOrder(7), EditorDisplay(name: "Cloud Shader")]
        public Shader Shader
        {
            get => _shader;
            set
            {
                if (_shader != value)
                {
                    _shader = value;
                    ReleaseShader();
                }
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Data
        {
            public Vector3 containerBoundsMin;
            public float vpHeight;

            public Vector3 containerBoundsMax;
            public float vpWidth;

            public float vpMinDepth;
            public float vpMaxDepth;
            public Vector2 vpPPos;

            public Matrix iWVPMatrix;

            public GBufferData GBuffer;

            public float near;
            public Vector3 shapeOffset;

            public Vector3 detailOffset;
            public float Time;

            public float cloudScale;
            public float densityOffset;
            public float densityMultiplier;
            public int numStepsLight;

            public Vector4 phaseParams;

            public Vector4 shapeNoiseWeights;

            public float timeScale;
            public float baseSpeed;
            public float detailSpeed;
            public float rayOffsetStrength;

            public Vector3 detailWeights;
            public float detailNoiseScale;

            public float detailNoiseWeight;
            public float lightAbsorptionTowardSun;
            public float lightAbsorptionThroughCloud;
            public float darknessThreshold;

            public Color LightColor0;

            public Vector3 WorldSpaceLightPos0;
            public float Dummy0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GBufferData
        {
            public Vector4 ViewInfo;
            public Vector4 ScreenSize;
            public Vector3 ViewPos;
            public float ViewFar;
            public Matrix InvViewMatrix;
            public Matrix InvProjectionMatrix;
        }


        private bool _doRender = false;
        [EditorOrder(1), EditorDisplay(name: "Enable")]
        public bool doRender
        {
            get => _doRender;
            set
            {
                _doRender = value;
                if (value == true)
                {
                    MainRenderTask.Instance.CustomPostFx.Add(this);
                }
                else
                {
                    MainRenderTask.Instance.CustomPostFx.Remove(this);
                }
            }
        }

        public override void OnEnable()
        {
#if FLAX_EDITOR
        // Register for asset reloading event and dispose resources that use shader
        Content.AssetReloading += OnAssetReloading;
#endif
            noiseGen = noiseGenerator.GetScript<NoiseGenerator>();
            if (!noiseGen)
                Debug.LogError("[VolumetricClouds] NoiseGenerator not specified");

            UpdateSettings();
        }

        public void UpdateSettings()
        {
            if (_JSON_Settings)
            {
                Settings = (VolumetricCloudSettings)_JSON_Settings.CreateInstance();
                Debug.Log("[VolumetricClouds] Settings updated");
            }
            else
            {
                Debug.LogError("[VolumetricClouds] No settings file provided");
                return;
            }
        }

#if FLAX_EDITOR
    private void OnAssetReloading(Asset asset)
    {
        // Shader will be hot-reloaded
        if (asset == Shader)
            ReleaseShader();
    }
#endif

        public override void OnDisable()
        {
            // Remember to unregister from events and release created resources (it's gamedev, not webdev)
            if (_doRender)
                MainRenderTask.Instance.CustomPostFx.Remove(this);
#if FLAX_EDITOR
        Content.AssetReloading -= OnAssetReloading;
#endif
            ReleaseShader();
        }

        private void ReleaseShader()
        {
            // Release resources using shader
            Destroy(ref _psFullscreen);
        }

        public override bool CanRender => base.CanRender && Shader && Shader.IsLoaded;

        public override unsafe void Render(GPUContext context, ref RenderContext renderContext, GPUTexture input, GPUTexture output)
        {
            // Only render this if doRender is enabled
            if (!doRender)
            {
                return;
            }

            if (Settings == null)
            {
                Debug.LogError("[VolumetricClouds] NoiseGenerator not specified");
                return;
            }

            //Clouds container bounds
            Vector3 containerBoundsMin = container.Position - container.LocalScale / 2;
            Vector3 containerBoundsMax = container.Position + container.LocalScale / 2;

            //Calculations to display cloud container on screen
            var viewProjection = Matrix.Multiply(Matrix.Identity, renderContext.View.View);
            Matrix wvpMatrix = Matrix.Multiply(viewProjection, renderContext.View.Projection);
            Matrix.Transpose(ref wvpMatrix, out var ViewProjectionMatrix);
            ViewProjectionMatrix.Invert();

            if (!_psFullscreen)
            {
                _psFullscreen = new GPUPipelineState();
                var desc = GPUPipelineState.Description.DefaultFullscreenTriangle;
                desc.PS = Shader.GPU.GetPS("PS_Fullscreen");
                _psFullscreen.Init(ref desc);
            }

            // Set constant buffer data (memory copy is used under the hood to copy raw data from CPU to GPU memory)
            var cb = Shader.GPU.GetCB(0);
            if (cb != IntPtr.Zero)
            {

                var data = new Data
                {
                    containerBoundsMax = containerBoundsMax,
                    containerBoundsMin = containerBoundsMin,
                    vpHeight = cam.Viewport.Height,
                    vpWidth = cam.Viewport.Width,
                    vpMaxDepth = cam.Viewport.MaxDepth,
                    vpMinDepth = cam.Viewport.MinDepth,
                    vpPPos = new Vector2(cam.Viewport.X, cam.Viewport.Y),
                    iWVPMatrix = ViewProjectionMatrix,
                    near = renderContext.View.Near,
                    shapeOffset = Settings.shapeOffset,
                    detailOffset = Settings.detailOffset,
                    Time = Time.GameTime,
                    cloudScale = Settings.cloudScale,
                    densityMultiplier = Settings.densityMultiplier,
                    densityOffset = Settings.densityOffset,
                    numStepsLight = Settings.numStepsLight,
                    phaseParams = new Vector4(Settings.forwardScattering, Settings.backScattering, Settings.baseBrightness, Settings.phaseFactor),
                    timeScale = Settings.timeScale,
                    baseSpeed = Settings.baseSpeed,
                    detailSpeed = Settings.detailSpeed,
                    rayOffsetStrength = Settings.rayOffsetStrength,
                    detailWeights = Settings.detailNoiseWeights,
                    detailNoiseScale = Settings.detailNoiseScale,
                    detailNoiseWeight = Settings.detailNoiseWeight,
                    lightAbsorptionTowardSun = Settings.lightAbsorptionTowardSun,
                    lightAbsorptionThroughCloud = Settings.lightAbsorptionThroughCloud,
                    darknessThreshold = Settings.darknessThreshold,
                    LightColor0 = sun.As<Light>().Color, //Is this correct? 
                    WorldSpaceLightPos0 = sun.Direction, //Is this correct? Unity discribes _WorldSpaceLightPos0 as "Directional lights: (world space direction, 0)."
                    shapeNoiseWeights = Settings.shapeNoiseWeights,
                };
                var gbufferdata = new GBufferData
                {
                    ViewInfo = renderContext.View.ViewInfo,
                    ViewFar = renderContext.View.Far,
                    ViewPos = renderContext.View.Position,
                    InvViewMatrix = renderContext.View.IV,
                    InvProjectionMatrix = renderContext.View.IP,
                    ScreenSize = renderContext.View.ScreenSize
                };
                data.GBuffer = gbufferdata;

                context.UpdateCB(cb, new IntPtr(&data));
            }

            // Draw fullscreen triangle using custom Pixel Shader
            context.BindCB(0, cb);
            context.BindSR(0, input);
            context.BindSR(1, renderContext.Buffers.DepthBuffer);
            context.BindSR(2, BlueNoise.Texture);
            context.BindSR(3, noiseGen.ShapeTexture.ViewVolume());
            context.BindSR(4, noiseGen.DetailTexture.ViewVolume());
            context.SetState(_psFullscreen);
            context.SetRenderTarget(output.View());
            context.DrawFullscreenTriangle();
        }
    }
}
