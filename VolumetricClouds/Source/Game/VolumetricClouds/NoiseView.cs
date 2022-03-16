using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    /// <summary>
    /// NoiseView Script.
    /// </summary>
    [ExecuteInEditMode]
    public class NoiseView : PostProcessEffect
    {
        [EditorDisplay(name: "View Shader")]
        public Shader SliceCompute;


        [EditorDisplay(name: "Volume Texture Layer"), EditorOrder(7)]
        public int layer = 0;

        [EditorDisplay(name: "Red Channel"), EditorOrder(3)]
        public bool rc = true;
        [EditorDisplay(name: "Green Channel"), EditorOrder(4)]
        public bool gc = true;
        [EditorDisplay(name: "Blue Channel"), EditorOrder(5)]
        public bool bc = true;
        [EditorDisplay(name: "Alpha Channel"), EditorOrder(6)]
        public bool ac = true;

        [EditorDisplay(name: "Noise Generator")]
        public Actor noiseGeneratorActor;
        [EditorOrder(2)]
        public CloudNoiseType ActiveTextureType = CloudNoiseType.Shape;

        public enum CloudNoiseType { Shape, Detail }

        private bool _doRender;
        [EditorDisplay(name: "Display"), EditorOrder(1)]
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

        private bool _isComputeSupported;

        public struct SliceData
        {
            public Vector4 channelMask;

            public int layer;       
        }
        NoiseGenerator noiseGenerator;
        GPUTexture result;

        public override bool CanRender => base.CanRender && _isComputeSupported && SliceCompute && SliceCompute.IsLoaded;

        public override void OnEnable()
        {
            _isComputeSupported = GPUDevice.Instance.Limits.HasCompute;

            noiseGenerator = noiseGeneratorActor.FindScript<NoiseGenerator>();
            if (noiseGenerator == null)
            {
                Debug.LogWarning("No NoiseGnerator attached!");
                return;
            }
        }

        public override void OnDisable()
        {
            if (_doRender)
                MainRenderTask.Instance?.CustomPostFx.Remove(this);
        }

        public override unsafe void Render(GPUContext context, ref RenderContext renderContext, GPUTexture input, GPUTexture output)
        {
            if (!_doRender)
                return;

            Vector4 ch = new Vector4(0,0,0,0);
            if (rc)
                ch.X = 1;
            if (gc)
                ch.Y = 1;
            if (bc)
                ch.Z = 1;
            if (ac)
                ch.W = 1;

            var cb = SliceCompute.GPU.GetCB(0);
            if (cb != IntPtr.Zero)
            {
                var data = new SliceData
                {
                    layer = layer,
                    channelMask = ch,
                };
                context.UpdateCB(cb, new IntPtr(&data));
            }


            context.BindCB(0, cb);

            result = new GPUTexture();
            if (ActiveTextureType == CloudNoiseType.Shape)
            {
                var desc = GPUTextureDescription.New2D((int)noiseGenerator.ShapeTexture.Height, (int)noiseGenerator.ShapeTexture.Width, PixelFormat.R8G8B8A8_UNorm, GPUTextureFlags.ShaderResource | GPUTextureFlags.UnorderedAccess | GPUTextureFlags.RenderTarget);
                if (result.Init(ref desc))
                    return;
                context.BindSR(0, noiseGenerator.ShapeTexture.ViewVolume());
            } else
            {
                var desc = GPUTextureDescription.New2D((int)noiseGenerator.DetailTexture.Height, (int)noiseGenerator.DetailTexture.Width, PixelFormat.R8G8B8A8_UNorm, GPUTextureFlags.ShaderResource | GPUTextureFlags.UnorderedAccess | GPUTextureFlags.RenderTarget);
                if (result.Init(ref desc))
                    return;
                context.BindSR(0, noiseGenerator.DetailTexture.ViewVolume());
            }
            
            
            context.BindUA(0, result.View());
            var csSlice = SliceCompute.GPU.GetCS("CS_Slice");

            if (ActiveTextureType == CloudNoiseType.Shape)
            {
                context.Dispatch(csSlice, (uint)noiseGenerator.ShapeTexture.Height / 8, (uint)noiseGenerator.ShapeTexture.Width / 8, 1);
            }
            else
            {
                context.Dispatch(csSlice, (uint)noiseGenerator.DetailTexture.Height / 8, (uint)noiseGenerator.DetailTexture.Width / 8, 1);
            }
            

            context.Draw(output, result);

            context.BindSR(0, (GPUTexture)null);
            context.BindUA(0, null);
            context.FlushState();
        }
    }
}
