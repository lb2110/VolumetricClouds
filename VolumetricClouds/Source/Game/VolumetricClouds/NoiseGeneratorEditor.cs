using System;
using System.Collections.Generic;
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.Editors;
using FlaxEngine;

namespace Game
{
    /// <summary>
    /// NoiseGeneratorEditor Script.
    /// </summary>
    [CustomEditor(typeof(NoiseGenerator))]
    public class NoiseGeneratorEditor : GenericEditor
    {
        public NoiseGenerator noisegen;

        public override void Initialize(LayoutElementsContainer layout)
        {
            noisegen = Values[0] as NoiseGenerator;

            layout.Label("NoiseGenerator Editor", TextAlignment.Center);

            base.Initialize(layout);

            layout.Space(20);
            var button_generate = layout.Button("Generate Textures", new Color(0, 122, 204));

            // Use Values[] to access the script or value being edited.
            // It is an array, because custom editors can edit multiple selected scripts simultaneously.
            button_generate.Button.Clicked += () => noisegen.GenerateTextures();
        }
    }
}
