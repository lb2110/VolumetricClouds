#include "./Flax/Common.hlsl"

// Constant buffers data passed from CPU to GPU
META_CB_BEGIN(0, Data)
float2 Dummy0;
float Resolution;
float Persistence;
int numCellsA;
int numCellsB;
int numCellsC;
int tile;
float4 channelMask;
bool invertNoise;
META_CB_END

RWTexture3D<float4> Output : register(u0);
RWStructuredBuffer<int> minMax : register(u1);
StructuredBuffer<float3> pointsA : register(t1);
StructuredBuffer<float3> pointsB : register(t2);
StructuredBuffer<float3> pointsC : register(t3);

static const int minMaxAccuracy = 10000000;

static const int3 offsets[] =
{
    // centre
    int3(0,0,0),
    // front face
    int3(0,0,1),
    int3(-1,1,1),
    int3(-1,0,1),
    int3(-1,-1,1),
    int3(0,1,1),
    int3(0,-1,1),
    int3(1,1,1),
    int3(1,0,1),
    int3(1,-1,1),
    // back face
    int3(0,0,-1),
    int3(-1,1,-1),
    int3(-1,0,-1),
    int3(-1,-1,-1),
    int3(0,1,-1),
    int3(0,-1,-1),
    int3(1,1,-1),
    int3(1,0,-1),
    int3(1,-1,-1),
    // ring around centre
    int3(-1,1,0),
    int3(-1,0,0),
    int3(-1,-1,0),
    int3(0,1,0),
    int3(0,-1,0),
    int3(1,1,0),
    int3(1,0,0),
    int3(1,-1,0)
};

float maxComponent(float3 vec) {
    return max(vec.x, max(vec.y, vec.z));
}

float minComponent(float3 vec) {
    return min(vec.x, min(vec.y, vec.z));
}


float worley(StructuredBuffer<float3> points, int numCells, float3 samplePos) {
    samplePos = (samplePos * tile)%1;
    int3 cellID = floor(samplePos * numCells);
    float minSqrDst = 1;

    // Loop over current cell + 26 adjacent cells to find closest point to samplePos
    for (int cellOffsetIndex = 0; cellOffsetIndex < 27; cellOffsetIndex ++) {
        int3 adjID = cellID + offsets[cellOffsetIndex];
        // Adjacent cell is outside map, so wrap around to other side to allow for seamless tiling
        if (minComponent(adjID) == -1 || maxComponent(adjID) == numCells) {
            int3 wrappedID = (adjID + numCells) % (uint3)numCells;
            int adjCellIndex = wrappedID.x + numCells * (wrappedID.y + wrappedID.z * numCells);
            float3 wrappedPoint = points[adjCellIndex];
            // Offset the wrappedPoint by all offsets to find which is closest to samplePos
            for (int wrapOffsetIndex = 0; wrapOffsetIndex < 27; wrapOffsetIndex ++) {
                float3 sampleOffset = (samplePos - (wrappedPoint + offsets[wrapOffsetIndex]));
                minSqrDst = min(minSqrDst, dot(sampleOffset, sampleOffset));
            }
        }
        // Adjacent cell is inside map, so calculate sqrDst from samplePos to cell point
        else {
            int adjCellIndex = adjID.x + numCells * (adjID.y + adjID.z * numCells);
            float3 sampleOffset = samplePos - points[adjCellIndex];
            minSqrDst = min(minSqrDst, dot(sampleOffset, sampleOffset));
        }
    }
    return sqrt(minSqrDst);
}
// Compute shader blur function for horizontal pass
META_CS(true, FEATURE_LEVEL_SM5)
[numthreads(8, 8, 8)]
void CS_Generate(uint3 id : SV_DispatchThreadID)
{
	float3 pos = id / (float)Resolution;

	float layerA = worley(pointsA,numCellsA,pos);
	float layerB = worley(pointsB,numCellsB,pos);
	float layerC = worley(pointsC,numCellsC,pos);

	float noiseSum = layerA + (layerB * Persistence) + (layerC * Persistence * Persistence);
	float maxVal = 1 + (Persistence) + (Persistence * Persistence);

	noiseSum /= maxVal;

	if (invertNoise) {
        noiseSum = 1 - noiseSum;
    }

    int val = (int)(noiseSum * minMaxAccuracy);
    InterlockedMin(minMax[0],val);
    InterlockedMax(minMax[1],val);

	// float value = 1.0f;
	// if (invertNoise == true) {
	// 	value = ((float)numCellsA / 10.0f) * channelMask.w;
	// }
	//Output[id.xyz] = float4( pos.x, pos.y * value, 0.0f, 0.0f);
	//Output[id.xy] = float4( pos.x, pos.y, 0.0f, 0.0f);
	Output[id] = Output[id] * (1-channelMask) + noiseSum * channelMask;
}

META_CS(true, FEATURE_LEVEL_SM5)
[numthreads(8, 8, 8)]
void CS_Normalize (uint3 id : SV_DispatchThreadID)
{
    float minVal = (float)minMax[0]/minMaxAccuracy;
    float maxVal = (float)minMax[1]/minMaxAccuracy;
    float4 normalizedVal = (Output[id]-minVal)/(maxVal-minVal);

    Output[id] = Output[id] * (1-channelMask) + normalizedVal * channelMask;

}