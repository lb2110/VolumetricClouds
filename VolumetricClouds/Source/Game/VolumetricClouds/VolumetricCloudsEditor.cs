using System;
using System.Collections.Generic;
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.Editors;
using FlaxEngine;

namespace Game
{
    /// <summary>
    /// VolumetricCloudsEditor Script.
    /// </summary>
    [CustomEditor(typeof(VolumetricClouds))]
    public class VolumetricCloudsEditor : GenericEditor
    {
        public VolumetricClouds clouds;

        public override void Initialize(LayoutElementsContainer layout)
        {
            clouds = Values[0] as VolumetricClouds;

            layout.Label("VolumetricClouds Editor", TextAlignment.Center);

            base.Initialize(layout);

            layout.Space(20);
            var button_generate = layout.Button("Reload Settings", new Color(0, 122, 204));

            // Use Values[] to access the script or value being edited.
            // It is an array, because custom editors can edit multiple selected scripts simultaneously.
            button_generate.Button.Clicked += () => clouds.UpdateSettings();
        }
    }
}
