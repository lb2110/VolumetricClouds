using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    /// <summary>
    /// WorleyNoiseSettings Script.
    /// </summary>
    public class WorleyNoiseSettings
    {
        [EditorDisplay("Noise Settings", "Shape Settings"),]
        public WorleyNoiseSettingsStruct[] shapeSettings = {
            new WorleyNoiseSettingsStruct()
            {
                Enable = true,
                seed = 0,
                numDivisionsA = 3,
                numDivisionsB = 7,
                numDivisionsC = 11,
                tile = 1,
                persistence = .65f,
                invert = true,
            },
            new WorleyNoiseSettingsStruct()
            {
                Enable = true,
                seed = 1,
                numDivisionsA = 9,
                numDivisionsB = 15,
                numDivisionsC = 23,
                tile = 1,
                persistence = .33f,
                invert = true,
            },
            new WorleyNoiseSettingsStruct()
            {
                Enable = true,
                seed = 2,
                numDivisionsA = 13,
                numDivisionsB = 28,
                numDivisionsC = 42,
                tile = 1,
                persistence = .58f,
                invert = true,
            },
            new WorleyNoiseSettingsStruct()
            {
                Enable = true,
                seed = 3,
                numDivisionsA = 20,
                numDivisionsB = 31,
                numDivisionsC = 45,
                tile = 1,
                persistence = .74f,
                invert = true,
            }
        };
        [EditorDisplay("Noise Settings", "Detail Settings"),]
        public WorleyNoiseSettingsStruct[] detailSettings = {
            new WorleyNoiseSettingsStruct()
            {
                Enable = true,
                seed = 0,
                numDivisionsA = 8,
                numDivisionsB = 18,
                numDivisionsC = 20,
                tile = 1,
                persistence = .76f,
                invert = true,
            },
            new WorleyNoiseSettingsStruct()
            {
                Enable = true,
                seed = 1,
                numDivisionsA = 13,
                numDivisionsB = 24,
                numDivisionsC = 28,
                tile = 1,
                persistence = .5f,
                invert = true,
            },
            new WorleyNoiseSettingsStruct()
            {
                Enable = true,
                seed = 2,
                numDivisionsA = 20,
                numDivisionsB = 28,
                numDivisionsC = 32,
                tile = 1,
                persistence = .5f,
                invert = true,
            },
            new WorleyNoiseSettingsStruct()
            {
                Enable = false,
                seed = 0,
                numDivisionsA = 5,
                numDivisionsB = 10,
                numDivisionsC = 15,
                tile = 1,
                persistence = .5f,
                invert = true,
            }
        };
    }
}
