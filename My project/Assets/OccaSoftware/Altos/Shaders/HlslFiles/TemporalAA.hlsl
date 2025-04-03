#ifndef TEMPORALAA_INCLUDE
#define TEMPORALAA_INCLUDE


#define _DEBUG_MOTION_VECTORS 0
#define _DEBUG_CHECKERBOARD 0

bool CheckIfValidUV(half2 UV)
{
	if (UV.x <= 0.0 || UV.x >= 1.0 || UV.y <= 0.0 || UV.y >= 1.0)
	{
		return false;
	}
	return true;
}


half random(half2 seed, half2 dotDir = half2(12.9898, 78.233))
{
	return frac(sin(dot(sin(seed), dotDir)) * 43758.5453);
}

bool GetSampleInstruction(half2 TexSize, half2 UV)
{
	half2 xy = half2(TexSize.x, TexSize.y);
	half2 modVal;
	modVal.x = fmod(floor(xy.x * UV.x), 4.0);
	modVal.y = fmod(floor(xy.y * UV.y), 4.0);
	modVal.x /= 16.0;
	modVal.y /= 4.0;
	
	half result = modVal.x + modVal.y;
	result += random(_Time.y);
	result = frac(result);
	result = (int)step(0.9375, result);
	return result >= 1 ? true:false;
}

SamplerState point_clamp_sampler;
SamplerState linear_clamp_sampler;
void TAA_float(Texture2D HistoricData, Texture2D NewFrameData, float2 UV, float2 TexSize, float BlendFactor, float2 MotionVector, out half4 MergedData, out half3 MergedDataRGB, out half MergedDataA)
{
	
	MergedData = 0;
	MergedDataRGB = 0;
	MergedDataA = 0;
	
#ifndef SHADERGRAPH_PREVIEW
	float2 texCoord = (1.0 / TexSize);
	
	
	bool doSample = GetSampleInstruction(TexSize, UV);
	
	// ProjectionParams.x < 0 on D3D, > 0 on OPENGL
	
	float4 newFrame = NewFrameData.SampleLevel(point_clamp_sampler, UV, 0);
	float2 HistUV = UV + MotionVector;
	
	bool isValidHistUV = CheckIfValidUV(HistUV);
	if (isValidHistUV)
	{
		half4 HistSample = HistoricData.SampleLevel(linear_clamp_sampler, HistUV, 0);
		
		half4 newSampleUp = NewFrameData.SampleLevel(point_clamp_sampler, UV + half2(0.0, -texCoord.y), 0);
		half4 newSampleDown = NewFrameData.SampleLevel(point_clamp_sampler, UV + half2(0.0, texCoord.y), 0);
		half4 newSampleRight = NewFrameData.SampleLevel(point_clamp_sampler, UV + half2(-texCoord.x, 0.0), 0);
		half4 newSampleLeft = NewFrameData.SampleLevel(point_clamp_sampler, UV + half2(texCoord.x, 0.0), 0);
	
		half4 newSampleUpRight = NewFrameData.SampleLevel(point_clamp_sampler, UV + half2(-texCoord.x, -texCoord.y), 0);;
		half4 newSampleUpLeft = NewFrameData.SampleLevel(point_clamp_sampler, UV + half2(texCoord.x, -texCoord.y), 0);;
		half4 newSampleDownRight = NewFrameData.SampleLevel(point_clamp_sampler, UV + half2(-texCoord.x, texCoord.y), 0);;
		half4 newSampleDownLeft = NewFrameData.SampleLevel(point_clamp_sampler, UV + half2(texCoord.x, texCoord.y), 0);;
	
		half4 minCross = min(min(min(newFrame, newSampleUp), min(newSampleDown, newSampleRight)), newSampleLeft);
		half4 maxCross = max(max(max(newFrame, newSampleUp), max(newSampleDown, newSampleRight)), newSampleLeft);
	
		half4 minBox = min(min(newSampleUpRight, newSampleUpLeft), min(newSampleDownRight, newSampleDownLeft));
		minBox = min(minBox, minCross);
	
		half4 maxBox = max(max(newSampleUpRight, newSampleUpLeft), max(newSampleDownRight, newSampleDownLeft));
		maxBox = max(maxBox, maxCross);
	
		half4 minNew = (minBox + minCross) * 0.5;
		half4 maxNew = (maxBox + maxCross) * 0.5;
	
		half4 clampedHist = clamp(HistSample, minNew, maxNew);
		HistSample = lerp(clampedHist, HistSample, 0.5);
		
		newFrame = lerp(HistSample, newFrame, BlendFactor);
	}
	
	
	#if _DEBUG_MOTION_VECTORS
	newFrame = half4(1.0, 1.0, 1.0, 0.0);
	
	if (isValidHistUV)
	{
		newFrame = half4((MotionVector).x, (MotionVector).y, 0, 0) * 10.0;
	}
	
	#endif
	
	#if _DEBUG_CHECKERBOARD
	half result = GetSampleInstruction(TexSize, UV);
	newFrame = half4(result, result, result, 0.0);
	#endif
	
	MergedData = newFrame;
	MergedDataRGB = MergedData.rgb;
	MergedDataA = MergedData.a;
#endif
}


#endif