using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    /// <summary>
    /// WorleyNoiseSettings Script.
    /// </summary>

        /// <inheritdoc/>
        public struct WorleyNoiseSettingsStruct
        {
            [EditorDisplay(name: "Seed")]
            public int seed;
            [Range(1, 50), EditorDisplay(name: "Divisions A")]
            public int numDivisionsA;
            [Range(1, 50), EditorDisplay(name: "Divisions B")]
            public int numDivisionsB;
            [Range(1, 50), EditorDisplay(name: "Divisions C")]
            public int numDivisionsC;

            [EditorDisplay(name: "Persistence")]
            public float persistence;
            [EditorDisplay(name: "Tile")]
            public int tile;
            [EditorDisplay(name: "Invert Texture")]
            public bool invert;
            [EditorDisplay(name: "Enable"), EditorOrder(1)]
            public bool Enable;
        }
}
