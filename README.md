# VolumetricClouds for FlaxEngine
Basic VolumetricClouds shader and WorleyNoise generator for FlaxEngine

This is a port from Unity to FlaxEngine.

Original repository: https://github.com/SebLague/Clouds

I tried this for fun and to learn without prior knowledge of 3D rendering and shaders.
There are some tasks still open. I don't know if I will continue to work on this. Feel free to make it better and open a PR.

TODO:
  - Enhance performance (I am missing in-depth knowledge to do this myself). It pushes my GTX 1060 from 20% to 90%.
  - LightColor0 not correct / not the atmospheric color ("Sunrise/Sunset")
  - (Perhaps fixed: WorldSpaceLightPos0 is incorrect -> light absorption towards the sun does not work)
  - Cloud "fog" seems pixely, not smooth
  - Shader for distance clouds
  - Correct height for clouds, make it more realistic?
