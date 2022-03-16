#include "./Flax/Common.hlsl"
#include "./Flax/GBufferCommon.hlsl"
#include "./Flax/GBuffer.hlsl"

META_CB_BEGIN(0, Data)
float3 containerBoundsMin;
float vpHeight;
float3 containerBoundsMax;
float vpWidth;
float vpMinDepth;
float vpMaxDepth;
float2 vpPPos;
float4x4 iWVPMatrix;
GBufferData GBuffer;

float near;
float3 shapeOffset;

float3 detailOffset;
float Time;

float cloudScale;
float densityOffset;
float densityMultiplier;
int numStepsLight;

float4 phaseParams;

float4 shapeNoiseWeights;

float timeScale;
float baseSpeed;
float detailSpeed;
float rayOffsetStrength;

float3 detailWeights;
float detailNoiseScale;

float detailNoiseWeight;
float lightAbsorptionTowardSun;
float lightAbsorptionThroughCloud;
float darknessThreshold;

float4 LightColor0;

float3 WorldSpaceLightPos0;
float Dummy0;
META_CB_END

SamplerState samplerInput;
SamplerState samplerBlueNoise;
SamplerState samplerNoiseTex;
SamplerState samplerDetailNoiseTex;

Texture2D mainTex : register(t0);
Texture2D depthTex : register(t1);
Texture2D<float4> BlueNoise : register(t2);
Texture3D<float4> NoiseTex : register(t3);
Texture3D<float4> DetailNoiseTex : register(t4);

float remap(float v, float minOld, float maxOld, float minNew, float maxNew) {
	return minNew + (v-minOld) * (maxNew - minNew) / (maxOld-minOld);
}

float LinearEyeDepth( float z , float near, float far){
    return far * near / ((near - far) * z + far);
}

bool isZero(float var) {
	return abs(var) < 1e-6f;
}

float2 squareUV(float2 uv) {
	float width = GBuffer.ScreenSize.x;
	float height = GBuffer.ScreenSize.y;
	//float minDim = min(width, height);
	float scale = 1000;
	float x = uv.x * width;
	float y = uv.y * height;
	return float2 (x/scale, y/scale);
}

float hg(float a, float g) {
	float g2 = g*g;
	return (1-g2) / (4*3.1415*pow(1+g2-2*g*(a), 1.5));
}

float phase(float a) {
	float blend = .5;
	float hgBlend = hg(a,phaseParams.x) * (1-blend) + hg(a,-phaseParams.y) * blend;
	return phaseParams.z + hgBlend*phaseParams.w;
}

// bool isZero(float var) {
// 	return abs(var) < 1.0f * (float)(pow(10,-6));
// }


float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 invRaydir) {
	// Adapted from: http://jcgt.org/published/0007/03/04/
	float3 t0 = (boundsMin - rayOrigin) / invRaydir;
	float3 t1 = (boundsMax - rayOrigin) / invRaydir;
	float3 tmin = min(t0, t1);
	float3 tmax = max(t0, t1);
	
	float dstA = max(max(tmin.x, tmin.y), tmin.z);
	float dstB = min(tmax.x, min(tmax.y, tmax.z));

	// CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
	// dstA is dst to nearest intersection, dstB dst to far intersection

	// CASE 2: ray intersects box from inside (dstA < 0 < dstB)
	// dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

	// CASE 3: ray misses box (dstA > dstB)

	float dstToBox = max(0, dstA);
	float dstInsideBox = max(0, dstB - dstToBox);
	return float2(dstToBox, dstInsideBox);
}

float3 unproject(float3 source, float4x4 ivp, float2 vpxy, float vpHeight, float vpWidth, float vpMaxDepth, float vpMinDepth) {
	float3 v;
	v.x = (source.x - vpxy.x) / vpWidth * 2.0f - 1.0f;
	v.y = -((source.y - vpxy.y) / vpHeight * 2.0f - 1.0f);
	v.z = (source.z - vpMinDepth) / (vpMaxDepth - vpMinDepth);

	float w = v.x * ivp[0][3] + v.y * ivp[1][3] + v.z * ivp[2][3] + ivp[3][3];
	float3 res = float3(v.x * ivp[0][0] + v.y * ivp[1][0] + v.z * ivp[2][0] + ivp[3][0],
						v.x * ivp[0][1] + v.y * ivp[1][1] + v.z * ivp[2][1] + ivp[3][1],
						v.x * ivp[0][2] + v.y * ivp[1][2] + v.z * ivp[2][2] + ivp[3][2]);

	if (!isZero(w)) {
		res /= w;
	}

	return res;
}

float sampleDensity(float3 position) {
    const float baseScale = 1/1000.0;

	float3 size = containerBoundsMax - containerBoundsMin;
	float3 boundsCentre = (containerBoundsMin+containerBoundsMax) * 0.5f;
	float3 uvw = (size * 0.5f + position) * baseScale * cloudScale;
	//float3 uvw = position * cloudScale * 0.001 + shapeOffset * 0.01;
	float4 shapeNoise = NoiseTex.SampleLevel(samplerNoiseTex, uvw, 0);
	float4 normalizedShapeWeights = shapeNoiseWeights / dot(shapeNoiseWeights, 1);
	float shapeFBM = dot(shapeNoise, normalizedShapeWeights);
	float baseShapeDensity = shapeFBM * densityMultiplier;
	float dens = baseShapeDensity; // * densityMultiplier;
	return dens;
}

float sampleDensity2(float3 rayPos) {
	// Constants:
	const int mipLevel = 0;
	const float baseScale = 1/1000.0;
	const float offsetSpeed = 1/100.0;

	// Calculate texture sample positions
	float time = Time * timeScale;
	float3 size = containerBoundsMax - containerBoundsMin;
	float3 boundsCentre = (containerBoundsMin+containerBoundsMax) * .5;
	float3 uvw = (size * .5 + rayPos) * baseScale * cloudScale;
	float3 shapeSamplePos = uvw + shapeOffset * offsetSpeed + float3(time,time*0.1,time*0.2) * baseSpeed;

	// Calculate falloff at along x/z edges of the cloud container
	const float containerEdgeFadeDst = 50;
	float dstFromEdgeX = min(containerEdgeFadeDst, min(rayPos.x - containerBoundsMin.x, containerBoundsMax.x - rayPos.x));
	float dstFromEdgeZ = min(containerEdgeFadeDst, min(rayPos.z - containerBoundsMin.z, containerBoundsMax.z - rayPos.z));
	float edgeWeight = min(dstFromEdgeZ,dstFromEdgeX)/containerEdgeFadeDst;
	
	// Calculate height gradient from weather map
	//float2 weatherUV = (size.xz * .5 + (rayPos.xz-boundsCentre.xz)) / max(size.x,size.z);
	//float weatherMap = WeatherMap.SampleLevel(samplerWeatherMap, weatherUV, mipLevel).x;
	float gMin = .2;
	float gMax = .7;
	float heightPercent = (rayPos.y - containerBoundsMin.y) / size.y;
	float heightGradient = saturate(remap(heightPercent, 0.0, gMin, 0, 1)) * saturate(remap(heightPercent, 1, gMax, 0, 1));
	heightGradient *= edgeWeight;

	// Calculate base shape density
	float4 shapeNoise = NoiseTex.SampleLevel(samplerNoiseTex, shapeSamplePos, mipLevel);
	float4 normalizedShapeWeights = shapeNoiseWeights / dot(shapeNoiseWeights, 1);
	float shapeFBM = dot(shapeNoise, normalizedShapeWeights) * heightGradient;
	float baseShapeDensity = shapeFBM + densityOffset * .1;

	// Save sampling from detail tex if shape density <= 0
	if (baseShapeDensity > 0) {
		// Sample detail noise
		float3 detailSamplePos = uvw*detailNoiseScale + detailOffset * offsetSpeed + float3(time*.4,-time,time*0.1)*detailSpeed;
		float4 detailNoise = DetailNoiseTex.SampleLevel(samplerDetailNoiseTex, detailSamplePos, mipLevel);
		float3 normalizedDetailWeights = detailWeights / dot(detailWeights, 1);
		float detailFBM = dot(detailNoise, normalizedDetailWeights);

		// Subtract detail noise from base shape (weighted by inverse density so that edges get eroded more than centre)
		float oneMinusShape = 1 - shapeFBM;
		float detailErodeWeight = oneMinusShape * oneMinusShape * oneMinusShape;
		float cloudDensity = baseShapeDensity - (1-detailFBM) * detailErodeWeight * detailNoiseWeight;

		return cloudDensity * densityMultiplier * 0.1;
	}
	return 0;
}

float lightmarch(float3 position) {
	float3 dirToLight = WorldSpaceLightPos0.xyz;
	float dstInsideBox = rayBoxDst(containerBoundsMin, containerBoundsMax, position, 1/dirToLight).y;
	
	float stepSize = dstInsideBox/numStepsLight;
	float totalDensity = 0;

	for (int step = 0; step < numStepsLight; step ++) {
		position += dirToLight * stepSize;
		totalDensity += max(0, sampleDensity(position) * stepSize);
	}

	float transmittance = exp(-totalDensity * lightAbsorptionTowardSun);
	return darknessThreshold + transmittance * (1-darknessThreshold);
}


META_PS(true, FEATURE_LEVEL_ES2)
float4 PS_Fullscreen(Quad_VS2PS input) : SV_Target
{
	float4 col = mainTex.Sample(samplerInput, input.TexCoord);
	// float4 coloff = mainTex.Sample(state, input.TexCoord - 0.002);

	// float c = (float)input.Position.x;

	// if(length(col - coloff) > 0.1) {
	// 	col = 0;
	// }
	// return Color + c/1000;

	float3 nearPoint = float3((float)input.Position.x, (float)input.Position.y, 0.0f);
	float3 farPoint = float3((float)input.Position.x, (float)input.Position.y, 1.0f);
	float3 rayPosition = unproject(nearPoint, iWVPMatrix, vpPPos, vpHeight, vpWidth, vpMaxDepth, vpMinDepth);
	float3 nfarPoint = unproject(farPoint, iWVPMatrix, vpPPos, vpHeight, vpWidth, vpMaxDepth, vpMinDepth);

	float3 viewVector = nfarPoint - rayPosition;
	float viewLength = length(viewVector);
	float3 rayDir = viewVector / viewLength;



	float depth = SAMPLE_RT(depthTex, input.TexCoord).r;
	float sceneDepth = length(rayDir) * LinearEyeDepth(depth, near, GBuffer.ViewFar);

	float2 rayBoxInfo = rayBoxDst(containerBoundsMin, containerBoundsMax, rayPosition, rayDir);
	float dstToBox = rayBoxInfo.x;
	float dstInsideBox = rayBoxInfo.y;


	// bool rayHitBox = dstInsideBox > 0 && dstToBox < sceneDepth;
	// if (rayHitBox) {
	//  	col = 0;
	// }
	// float dstTravelled = 0;
	// float stepSize = dstInsideBox / numStepsLight;
	// float dstLimit = min(sceneDepth - dstToBox, dstInsideBox);

	// float totalDensity = 0;
	// while (dstTravelled < dstLimit) {
	// 	float3 rayPos = rayPosition + rayDir * (dstToBox + dstTravelled);
	// 	totalDensity += sampleDensity2(rayPos) * stepSize;
	// 	dstTravelled += stepSize;
	// }
	// float transmittance = exp(-totalDensity);
	// return col * transmittance + Color.y;
	//return float4(col.x * shapeNoiseWeights.x , col.y, col.z + Color.y ,0);;
	float3 entryPoint = rayPosition + rayDir * dstToBox;

	// random starting offset (makes low-res results noisy rather than jagged/glitchy, which is nicer)
	float randomOffset = BlueNoise.SampleLevel(samplerBlueNoise, squareUV(input.TexCoord*3), 0);
	randomOffset *= rayOffsetStrength;
	
	// Phase function makes clouds brighter around sun
	float cosAngle = dot(rayDir, WorldSpaceLightPos0.xyz);
	float phaseVal = phase(cosAngle);

	float dstTravelled = randomOffset;
	float dstLimit = min(sceneDepth-dstToBox, dstInsideBox);
	
	
	
	const float stepSize = 11;

	// March through volume:
	float transmittance = 1;
	float3 lightEnergy = 0;

	while (dstTravelled < dstLimit) {
		rayPosition = entryPoint + rayDir * dstTravelled;
		float density = sampleDensity2(rayPosition);
		
		if (density > 0) {
			float lightTransmittance = lightmarch(rayPosition);
			lightEnergy += density * stepSize * transmittance * lightTransmittance * phaseVal;
			transmittance *= exp(-density * stepSize * lightAbsorptionThroughCloud);
		
			// Exit early if T is close to zero as further samples won't affect the result much
			if (transmittance < 0.01) {
				break;
			}
		}
		dstTravelled += stepSize;
	}

	// Add clouds to background
	float3 cloudCol = lightEnergy * LightColor0;
	float3 col1 = col * transmittance + cloudCol;
	return float4(col1,0);
}
