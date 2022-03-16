using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    /// <summary>
    /// VolumetricCloudSettings Script.
    /// </summary>
    public class VolumetricCloudSettings
    {
        [EditorOrder(5), EditorDisplay("March settings")]
        public int numStepsLight = 8;
        [EditorOrder(6), EditorDisplay("March settings")]
        public float rayOffsetStrength = 10;
        [EditorOrder(7), EditorDisplay("Base Shape")]
        public float cloudScale = 0.5f;
        [EditorOrder(8), EditorDisplay("Base Shape")]
        public float densityMultiplier = 0.18f;
        [EditorOrder(9), EditorDisplay("Base Shape")]
        public float densityOffset = -3.39f;
        [EditorOrder(10), EditorDisplay("Base Shape")]
        public Vector3 shapeOffset = new Vector3(190.44f, 0f, 0f);
        [EditorOrder(11), EditorDisplay("Base Shape")]
        public Vector2 heightOffset;
        [EditorOrder(12), EditorDisplay("Base Shape")]
        public Vector4 shapeNoiseWeights = new Vector4(0.8f, 0.1f, 0.15f, 0f);
        [EditorOrder(13), EditorDisplay("Detail")]
        public float detailNoiseScale = 0.22f;
        [EditorOrder(14), EditorDisplay("Detail")] //TEst
        public float detailNoiseWeight = 2f;
        [EditorOrder(15), EditorDisplay("Detail")]
        public Vector3 detailNoiseWeights = new Vector3(0.7f, 0.2f, 0.5f);
        [EditorOrder(16), EditorDisplay("Detail")]
        public Vector3 detailOffset = new Vector3(51.25f, 0f, 0f);
        [EditorOrder(17), EditorDisplay("Lightning")]
        public float lightAbsorptionThroughCloud = 0.3f;
        [EditorOrder(18), EditorDisplay("Lightning")]
        public float lightAbsorptionTowardSun = 1.21f;
        [Range(0, 1), EditorOrder(19), EditorDisplay("Lightning")]
        public float darknessThreshold = 0.61f;
        [Range(0, 1), EditorOrder(20), EditorDisplay("Lightning")]
        public float forwardScattering = .811f;
        [Range(0, 1), EditorOrder(21), EditorDisplay("Lightning")]
        public float backScattering = .33f;
        [Range(0, 1), EditorOrder(22), EditorDisplay("Lightning")]
        public float baseBrightness = 0.24f;
        [Range(0, 1), EditorOrder(23), EditorDisplay("Lightning")]
        public float phaseFactor = .488f;
        [EditorOrder(26), EditorDisplay("Animation")]
        public float timeScale = 1;
        [EditorOrder(27), EditorDisplay("Animation")]
        public float baseSpeed = 0.1f;
        [EditorOrder(28), EditorDisplay("Animation")]
        public float detailSpeed = 1;
    }
}
