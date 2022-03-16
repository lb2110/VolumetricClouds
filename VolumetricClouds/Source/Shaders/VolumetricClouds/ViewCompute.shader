#include "./Flax/Common.hlsl"

META_CB_BEGIN(0, SliceData)
float4 channelMask;
 int layer;
META_CB_END

 Texture3D<float4> volumeTexture : register(t0);
 RWTexture2D<float4> slice : register(u0);

META_CS(true, FEATURE_LEVEL_SM5)
[numthreads(8, 8, 1)]
 void CS_Slice (uint3 id : SV_DispatchThreadID)
 {
     uint3 pos = uint3(id.x,id.y,layer);
     slice[id.xy] = volumeTexture[pos] * channelMask;
 }
