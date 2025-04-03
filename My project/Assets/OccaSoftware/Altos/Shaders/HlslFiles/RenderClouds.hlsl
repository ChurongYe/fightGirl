#ifndef VOLUMETRICCLOUDS_INCLUDE
#define VOLUMETRICCLOUDS_INCLUDE


// Constant Defines
half EPSILON = 0.001;
half2 _CLOUD_WEATHERMAP_VELOCITY = half2(0.0, 0.0);
int _USE_CLOUD_WEATHERMAP_TEX = 0;
half _CLOUD_WEATHERMAP_SCALE = 0.0;
half2 _CLOUD_WEATHERMAP_VALUE_RANGE = half2(0.0, 1.0);


half EaseIn(half a)
{
	return a * a;
}

half EaseOut(half a)
{
	return 1 - EaseIn(1 - a);
}

half EaseInOut(half a)
{
	return lerp(EaseIn(a), EaseOut(a), a);
}

half random(half2 seed, half2 dotDir = half2(12.9898, 78.233))
{
	return frac(sin(dot(sin(seed), dotDir)) * 43758.5453);
}

half random(half2 seed, half min, half max)
{
	return lerp(min, max, random(seed));
}

half2 Random2(half2 seed)
{
	return half2(random(seed), random(seed, half2(34.698, 51.7348)));
}

half2 Random2(half seedX, half seedY)
{
	return Random2(half2(seedX, seedY));
}

half2 GetDir(half x, half y)
{
	return Random2(x, y) * 2.0 - 1.0;
}

half GetPerlinNoise(half2 position)
{
	half2 lowerLeft = GetDir(floor(position.x), floor(position.y));
	half2 lowerRight = GetDir(ceil(position.x), floor(position.y));
	half2 upperLeft = GetDir(floor(position.x), ceil(position.y));
	half2 upperRight = GetDir(ceil(position.x), ceil(position.y));
	
	half2 f = frac(position);
	
	lowerLeft = dot(lowerLeft, f);
	lowerRight = dot(lowerRight, f - half2(1.0, 0.0));
	upperLeft = dot(upperLeft, f - half2(0.0, 1.0));
	upperRight = dot(upperRight, f - half2(1.0, 1.0));
	
	half2 t = half2(EaseInOut(f.x), EaseInOut(f.y));
	half lowerMix = lerp(lowerLeft.x, lowerRight.x, t.x);
	half upperMix = lerp(upperLeft.x, upperRight.x, t.x);
	return saturate(lerp(lowerMix, upperMix, t.y) + 0.5);
}

void GradientPerlinNoise_half(half2 position, out half value)
{
	value = GetPerlinNoise(position);
}

half4 BilinearSample(Texture2D Tex, SamplerState Sampler, half2 UV, half2 Scale, half RTScale)
{
	Scale *= RTScale;
	half2 st = UV * Scale - 0.5;
	half2 iuv = floor(st);
	half2 fuv = frac(st);
	
	half4 a = Tex.SampleLevel(Sampler, (iuv + half2(0.5, 0.5)) / Scale, 0);
	half4 b = Tex.SampleLevel(Sampler, (iuv + half2(1.5, 0.5)) / Scale, 0);
	half4 c = Tex.SampleLevel(Sampler, (iuv + half2(0.5, 1.5)) / Scale, 0);
	half4 d = Tex.SampleLevel(Sampler, (iuv + half2(1.5, 1.5)) / Scale, 0);
	
	return lerp(lerp(a, b, fuv.x), lerp(c, d, fuv.x), fuv.y);
}

// w0, w1, w2, and w3 are the four cubic B-spline basis functions
half w0(half a)
{
	return (1.0 / 6.0) * (a * (a * (-a + 3.0) - 3.0) + 1.0);
}

half w1(half a)
{
	return (1.0 / 6.0) * (a * a * (3.0 * a - 6.0) + 4.0);
}

half w2(half a)
{
	return (1.0 / 6.0) * (a * (a * (-3.0 * a + 3.0) + 3.0) + 1.0);
}

half w3(half a)
{
	return (1.0 / 6.0) * (a * a * a);
}

// g0 and g1 are the two amplitude functions
half g0(half a)
{
	return w0(a) + w1(a);
}

half g1(half a)
{
	return w2(a) + w3(a);
}

// h0 and h1 are the two offset functions
half h0(half a)
{
	return -1.0 + w1(a) / (w0(a) + w1(a));
}

half h1(half a)
{
	return 1.0 + w3(a) / (w2(a) + w3(a));
}

// Bicubic Reference: https://www.shadertoy.com/view/4df3Dn
half4 BicubicSample(Texture2D Tex, SamplerState Sampler, half2 UV, half2 Scale, half RTScale)
{
	Scale *= RTScale;
	half2 st = UV * Scale + 0.5;
	half2 invScale = 1.0 / Scale;
	half2 iuv = floor(st);
	half2 fuv = frac(st);
	
	half g0x = g0(fuv.x);
	half g1x = g1(fuv.x);
	half h0x = h0(fuv.x);
	half h1x = h1(fuv.x);
	half h0y = h0(fuv.y);
	half h1y = h1(fuv.y);
	
	half2 p0 = (half2(iuv.x + h0x, iuv.y + h0y) - 0.5) * invScale;
	half2 p1 = (half2(iuv.x + h1x, iuv.y + h0y) - 0.5) * invScale;
	half2 p2 = (half2(iuv.x + h0x, iuv.y + h1y) - 0.5) * invScale;
	half2 p3 = (half2(iuv.x + h1x, iuv.y + h1y) - 0.5) * invScale;
	
	return g0(fuv.y) * (g0x * Tex.SampleLevel(Sampler, p0, 0) +
                        g1x * Tex.SampleLevel(Sampler, p1, 0)) +
           g1(fuv.y) * (g0x * Tex.SampleLevel(Sampler, p2, 0) +
                        g1x * Tex.SampleLevel(Sampler, p3, 0));
}

void UpsampleTexture_half(Texture2D Tex, SamplerState Sampler, half2 UV, half2 Scale, half RTScale, out half4 Value, out half3 RGB, out half Alpha)
{
#ifdef SHADERGRAPH_PREVIEW
	Value = 0;
	RGB = 0;
	Alpha = 0;
#else
	Value = BicubicSample(Tex, Sampler, UV, Scale, RTScale);
	RGB = Value.rgb;
	Alpha = Value.a;
#endif
}

void screenToViewVector_half(half2 UV, out half3 viewVector)
{
	viewVector = half3(0.0, 0.0, 0.0);
#ifndef SHADERGRAPH_PREVIEW
	float3 viewDirectionTemp = mul(unity_CameraInvProjection, float4(UV * 2 - 1, 0.0, -1));
	viewVector = mul(unity_CameraToWorld, viewDirectionTemp);
#endif
}

half3 Luminance(half3 color, half amount)
{
	half luminance = (0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b);
	return lerp(luminance, color, amount);
}

void TAA_float(Texture2D AveragedPastFrames, Texture2D CurrentFrame, float2 inputUV, SamplerState samplerState, float blend, out float4 ColorOut)
{
#if defined(SHADERGRAPH_PREVIEW)
ColorOut = float4(0,0,0,0);
#else
	ColorOut = blend * CurrentFrame.SampleLevel(samplerState, inputUV, 0) + (1.0 - blend) * AveragedPastFrames.SampleLevel(samplerState, inputUV, 0);
#endif	
}

half HeightInAtmos(half3 rayOrigin, half3 rayPos, half atmosThickness, half planetRadius, half atmosHeight)
{
	half heightInAtmos = distance(rayPos, half3(rayOrigin.x, -planetRadius, rayOrigin.z)) - (planetRadius + atmosHeight);
	heightInAtmos /= atmosThickness;
	return saturate(heightInAtmos);
}

bool CheckIfInsideSDFSphere(half3 pointToCheck, half3 spherePosition, half sphereRadius)
{
	half test = length(pointToCheck - spherePosition) - sphereRadius;
	if (test > 0)
		return false;
	
	return true;
}

struct IntersectData
{
	bool hit;
	bool inside;
	half frontfaceDistance;
	half backfaceDistance;
	half output;
};

// http://kylehalladay.com/blog/tutorial/math/2013/12/24/Ray-Sphere-Intersection.html
// https://stackoverflow.com/questions/6533856/ray-sphere-intersection
// https://www.cs.colostate.edu/~cs410/yr2017fa/more_progress/pdfs/cs410_F17_Lecture10_Ray_Sphere.pdf
IntersectData RaySphereIntersect(half3 rayOrigin, half3 rayDir, half sphereRad, half3 spherePosition)
{
	IntersectData intersectionData;
	intersectionData.hit = false;
	intersectionData.inside = false;
	intersectionData.frontfaceDistance = 0.0;
	intersectionData.backfaceDistance = 0.0;
	
	half3 sphereCenter = spherePosition;
	half a = dot(rayDir, rayDir);
	half b = 2.0 * dot(rayDir, rayOrigin - sphereCenter);
	half c = dot(sphereCenter, sphereCenter) + dot(rayOrigin, rayOrigin) - 2.0 * dot(sphereCenter, rayOrigin) - sphereRad * sphereRad;
	half determinant = (b * b) - (4.0 * a * c);
	
	if (determinant < 0.0)
		return intersectionData;
	
	
	intersectionData.hit = true;
	determinant = sqrt(determinant);
	half point1 = (-b - determinant) / (2.0 * a);
	point1 = max(point1, 0);
	half point2 = (-b + determinant) / (2.0 * a);
	point2 = max(point2, 0);
	
	intersectionData.frontfaceDistance = min(point1, point2);
	intersectionData.backfaceDistance = max(point1, point2);
	
	return intersectionData;
}

struct AtmosHitData
{
	bool didHit;
	bool doubleIntersection;
	half nearDist;
	half nearDist2;
	half farDist;
	half farDist2;
	half position;
	bool doHighAltMarchAtFar;
	bool doHighAltMarchAtNear;
};



AtmosHitData AtmosphereIntersection(half3 rayOrigin, half3 rayDir, half atmosHeight, half planetRadius, half atmosThickness, half maxDist)
{
	half3 sphereCenter = half3(rayOrigin.x, -planetRadius, rayOrigin.z);
	half innerRad = planetRadius + atmosHeight;
	half outerRad = planetRadius + atmosHeight + atmosThickness;
	
	IntersectData innerData = RaySphereIntersect(rayOrigin, rayDir, innerRad, sphereCenter);
	IntersectData outerData = RaySphereIntersect(rayOrigin, rayDir, outerRad, sphereCenter);
	AtmosHitData hitData;
	hitData.didHit = false;
	hitData.doubleIntersection = false;
	
	hitData.doHighAltMarchAtFar = true;
	hitData.doHighAltMarchAtNear = false;
	
	
	bool insideInner = CheckIfInsideSDFSphere(rayOrigin, sphereCenter, innerRad);
	bool insideOuter = CheckIfInsideSDFSphere(rayOrigin, sphereCenter, outerRad);
	
	
	half nearIntersectDistance = 0.0;
	half farIntersectDistance = 0.0;
	half nearIntersectDistance2 = 0.0;
	half farIntersectDistance2 = 0.0;
	
	//Case 1
	if (insideInner && insideOuter)
	{
		nearIntersectDistance = innerData.backfaceDistance;
		farIntersectDistance = outerData.backfaceDistance;
		hitData.position = 0;
	}
	
	// Case 2
	if (!insideInner && insideOuter)
	{
		nearIntersectDistance = 0;
		farIntersectDistance = min(outerData.backfaceDistance, maxDist);
		
		// InnerData.frontFaceDistance > 0 when the ray intersects with the inner sphere.
		if (innerData.frontfaceDistance > 0.0)
		{
			farIntersectDistance = min(innerData.frontfaceDistance, maxDist);
			hitData.doHighAltMarchAtFar = false;
			
			if (innerData.backfaceDistance < maxDist)
			{
				nearIntersectDistance2 = innerData.backfaceDistance;
				farIntersectDistance2 = min(outerData.backfaceDistance, maxDist);
			}
		}
		
		hitData.position = 1;
	}
	
	bool lookingAboveClouds = false;
	// Case 3
	if (!insideInner && !insideOuter)
	{
		if (outerData.frontfaceDistance <= 0.0)
			lookingAboveClouds = true;
		
		nearIntersectDistance = outerData.frontfaceDistance;
		farIntersectDistance = min(outerData.backfaceDistance, maxDist);
		hitData.doHighAltMarchAtNear = true;
		hitData.doHighAltMarchAtFar = false;
		// InnerData.frontFaceDistance > 0 when the ray intersects with the inner sphere.
		if (innerData.frontfaceDistance > 0.0)
		{
			farIntersectDistance = min(innerData.frontfaceDistance, maxDist);
			if (innerData.backfaceDistance < maxDist)
			{
				nearIntersectDistance2 = innerData.backfaceDistance;
				farIntersectDistance2 = min(outerData.backfaceDistance, maxDist);
			}
		}
		
		hitData.position = 2;
	}
	
	hitData.nearDist = nearIntersectDistance;
	hitData.nearDist2 = nearIntersectDistance2;
	hitData.farDist = farIntersectDistance;
	hitData.farDist2 = farIntersectDistance2;
	
	if (hitData.nearDist < maxDist)
		hitData.didHit = true;
	
	if (hitData.nearDist2 > 0.0)
		hitData.doubleIntersection = true;
	
	if (lookingAboveClouds)
		hitData.didHit = false;
	
	if (hitData.farDist > maxDist)
		hitData.doHighAltMarchAtFar = false;
	
	return hitData;
}


half InverseLerp(half a, half b, half v)
{
	return (v - a) / (b - a);
}

half Remap(half iMin, half iMax, half oMin, half oMax, half v)
{
	half t = InverseLerp(iMin, iMax, v);
	return lerp(oMin, oMax, t);
}

struct AtmosphereData
{
	// Pre-Defined
	int atmosThickness;
	int atmosHeight;
	int cloudFadeDistance;
	half distantCoverageAmount;
	half distantCoverageDepth;
};


struct StaticMaterialData
{
	SamplerState fogSampler;
	
	half3 rayOrigin;
	half3 sunPos;
	
	half cloudiness;
	half alphaAccumulation;
	half4 baseRGBAInf;
	half3 extinction;
	half3 highAltExtinction;
	half HG;
	
	half multipleScatteringAmpGain;
	half multipleScatteringDensityGain;
	int multipleScatteringOctaves;
	
	Texture3D baseTexture;
	half3 baseScale;
	half3 baseTimescale;
	
	Texture2D curlNoise;
	half curlScale;
	half curlStrength;
	half curlTimescale;
	half curlAdjustmentBase;
	
	Texture3D detail1Texture;
	half3 detail1Scale;
	half detail1Strength;
	half3 detail1Timescale;
	bool detail1Invert;
	half2 detail1HeightRemap;
	half4 detail1RGBAInf;
	
	Texture3D detail2Texture;
	half3 detail2Scale;
	half detail2Strength;
	half3 detail2Timescale;
	bool detail2Invert;
	half2 detail2HeightRemap;
	half4 detail2RGBAInf;
	
	
	Texture2D highAltTex1;
	Texture2D highAltTex2;
	Texture2D highAltTex3;
	half highAltitudeAlphaAccumulation;
	half2 highAltOffset1;
	half2 highAltOffset2;
	half2 highAltOffset3;
	half2 highAltScale1;
	half2 highAltScale2;
	half2 highAltScale3;
	half highAltitudeCloudiness;
	half highAltInfluence1;
	half highAltInfluence2;
	half highAltInfluence3;
	
	int lightingDistance;
	int planetRadius;
	
	half heightDensityInfluence;
	half cloudinessDensityInfluence;
	
	Texture2D weathermapTex;
	SamplerState weathermapSampler;
};

struct RayData
{
	half3 rayPosition;
	half3 rayDirection;
	half relativeDepth;
	half rayDepth;
	half meanStepSize;
	half noiseAdjustment;
};

half GetDistantCoverageMap(half cloudiness, RayData rayData, AtmosphereData atmosData)
{
	if (rayData.rayDepth > atmosData.distantCoverageDepth)
	{
		half t = saturate(Remap(atmosData.distantCoverageDepth, atmosData.distantCoverageDepth + 2000.0, 0.0, 1.0, rayData.rayDepth));
		cloudiness = saturate(lerp(cloudiness, atmosData.distantCoverageAmount, t));
	}
	
	return cloudiness;
}


half GetCloudShape2D(StaticMaterialData materialData, RayData rayData, AtmosphereData atmosData, int mip)
{
	if (materialData.highAltitudeCloudiness <= 0)
		return 0;
	
	half2 uv = (rayData.rayPosition.xz * 0.00001);
	half timeOffset = _Time.y * 0.0001;
	half2 uv1 = (uv + timeOffset * materialData.highAltOffset1) * materialData.highAltScale1;
	half2 uv2 = (uv + timeOffset * materialData.highAltOffset2) * materialData.highAltScale2;
	half2 uv3 = (uv + timeOffset * materialData.highAltOffset3) * materialData.highAltScale3;
	
	half val1;
	half val2;
	half val3;
	
	val1 = materialData.highAltTex1.SampleLevel(materialData.fogSampler, uv1, mip).r;
	val2 = materialData.highAltTex2.SampleLevel(materialData.fogSampler, uv2, mip).r;
	val3 = materialData.highAltTex3.SampleLevel(materialData.fogSampler, uv3, mip).r;
	
	half val = (val1 * materialData.highAltInfluence1 + val2 * materialData.highAltInfluence2 + val3 * materialData.highAltInfluence3);
	
	val = saturate(Remap(1.0 - materialData.highAltitudeCloudiness, 1.0, 0.0, 1.0, val));
	val *= materialData.highAltitudeCloudiness;
	
	return val * val;
}


half2 GetWeathermapUV(half3 rayPosition, half3 rayOrigin, bool doFloatingOrigin)
{
	half2 UV = rayPosition.xz;
	if (doFloatingOrigin)
		UV -= rayOrigin.xz;
	
	UV *= 0.0001;
	UV += _CLOUD_WEATHERMAP_VELOCITY * _Time.y * 0.01;
	
	return UV;
}

half GetCloudShapeVolumetric(StaticMaterialData materialData, RayData rayData, AtmosphereData atmosData, int mip)
{
	
	materialData.cloudiness = GetDistantCoverageMap(materialData.cloudiness, rayData, atmosData);
	
	// Early Exit on Cloudiness
	if (materialData.cloudiness <= EPSILON)
		return 0;
	
	// Set up
	half heightPercent = HeightInAtmos(materialData.rayOrigin, rayData.rayPosition, atmosData.atmosThickness, materialData.planetRadius, atmosData.atmosHeight);
	
	if (heightPercent > (1.0 - EPSILON) || heightPercent < EPSILON)
		return 0;
	
	half threshold = 1.0 - materialData.cloudiness;
	half3 uvw = rayData.rayPosition * 0.000005;
	
	half weathermapSample = 0;
	half2 weathermapUV;
	
	UNITY_BRANCH
	if (_USE_CLOUD_WEATHERMAP_TEX)
	{
		weathermapUV = GetWeathermapUV(rayData.rayPosition, materialData.rayOrigin, true);
		weathermapSample = materialData.weathermapTex.SampleLevel(materialData.weathermapSampler, weathermapUV, mip).r;
	}
	else
	{
		weathermapUV = GetWeathermapUV(rayData.rayPosition, materialData.rayOrigin, false);
		weathermapUV *= _CLOUD_WEATHERMAP_SCALE;
		
		weathermapSample = GetPerlinNoise(weathermapUV);
		weathermapSample = saturate(Remap(_CLOUD_WEATHERMAP_VALUE_RANGE.x, _CLOUD_WEATHERMAP_VALUE_RANGE.y, 0.0, 1.0, weathermapSample));
	}
	
	weathermapSample *= materialData.cloudiness;
	
	half roundingFromBelow = 0.17;
	// Rounding
	half roundingAtBottom = saturate(Remap(0.0, roundingFromBelow, 0.0, 1.0, heightPercent));
	half roundingAtTop = saturate(1.0 - heightPercent);
	half rounding = roundingAtBottom * (roundingAtTop * roundingAtTop);
	weathermapSample *= rounding;
	
	if (weathermapSample <= EPSILON)
		return 0;
	
	// Sample Curl
	half2 curlOffset = (materialData.curlTimescale * _Time.y) * 0.0001;
	half2 curlUV = materialData.curlScale * (uvw.xz + curlOffset);
	half3 curlSample = materialData.curlNoise.SampleLevel(materialData.fogSampler, curlUV, mip).rgb;
	curlSample = (curlSample - 0.5) * 2.0;
	curlSample *= materialData.curlStrength;
	
	// Sample Base
	half3 heightOffset = (heightPercent * heightPercent * materialData.baseTimescale) * 0.0001;
	half3 baseOffset = (_Time.y * materialData.baseTimescale) * 0.0001;
	half3 baseUVW = materialData.baseScale * (uvw + baseOffset + heightOffset);
	baseUVW += curlSample * materialData.curlAdjustmentBase * 0.01;
	
	half4 baseSample = materialData.baseTexture.SampleLevel(materialData.fogSampler, baseUVW, mip).rgba;
	half baseVal = (baseSample.r * materialData.baseRGBAInf.r + baseSample.g * materialData.baseRGBAInf.g + baseSample.b * materialData.baseRGBAInf.b + baseSample.a * materialData.baseRGBAInf.a);
	half value = 1.0;
	
	baseVal = lerp(baseVal, 1.0, materialData.cloudiness);
	value = saturate(Remap(1.0 - weathermapSample, 1.0, 0.0, 1.0, baseVal));
	
	
	if (value <= EPSILON)
		return 0;
	
	
	// Sample Detail 1
	if (materialData.detail1Strength > EPSILON)
	{
		half3 detail1Offset = (materialData.detail1Timescale * _Time.y) * 0.0001;
		half3 detail1UVW = materialData.detail1Scale * (uvw + detail1Offset);
		detail1UVW += curlSample;
	
		half4 detail1Sample = materialData.detail1Texture.SampleLevel(materialData.fogSampler, detail1UVW, mip).rgba;
		half detail1Value = (detail1Sample.r * materialData.detail1RGBAInf.r + detail1Sample.g * materialData.detail1RGBAInf.g + detail1Sample.b * materialData.detail1RGBAInf.b + detail1Sample.a * materialData.detail1RGBAInf.a);
		
		detail1Value = (1.0 - detail1Value);
		detail1Value = saturate(Remap(1.0 - materialData.detail1Strength, 1.0, 0.0, 1.0, detail1Value));
		detail1Value *= saturate(Remap(materialData.detail1HeightRemap.x, materialData.detail1HeightRemap.y, 0.0, 1.0, heightPercent));
		value = saturate(Remap(detail1Value, 1.0, 0.0, 1.0, value));
	}
	
	
	// Early Exit on Detail
	if (value <= EPSILON)
		return 0;
	
	
	// Sample Detail 2
	if (materialData.detail2Strength > EPSILON)
	{
		half3 detail2Offset = (materialData.detail2Timescale * _Time.y) * 0.0001;
		half3 detail2UVW = materialData.detail2Scale * (uvw + detail2Offset);
		detail2UVW += curlSample;
		half4 detail2Sample = materialData.detail2Texture.SampleLevel(materialData.fogSampler, detail2UVW, mip).rgba;
		half detail2Value = (detail2Sample.r * materialData.detail2RGBAInf.r + detail2Sample.g * materialData.detail2RGBAInf.g + detail2Sample.b * materialData.detail2RGBAInf.b + detail2Sample.a * materialData.detail2RGBAInf.a);
		
		detail2Value = (1.0 - detail2Value);
		detail2Value = saturate(Remap(1.0 - materialData.detail2Strength, 1.0, 0.0, 1.0, detail2Value));
		detail2Value *= saturate(Remap(materialData.detail2HeightRemap.x, materialData.detail2HeightRemap.y, 0.0, 1.0, heightPercent));
		value = saturate(Remap(detail2Value, 1.0, 0.0, 1.0, value));
	}
	
	
	if (value <= EPSILON)
		return 0;

	//value *= value;
	value *= saturate(Remap(0.0, materialData.heightDensityInfluence, 0.0, 1.0, heightPercent));
	value *= lerp(1.0, materialData.cloudiness, materialData.cloudinessDensityInfluence);
	
	if (value <= EPSILON)
		return 0;
	
	return value;
}

half BeerLambert(half absorptionCoefficient, half stepSize, half density)
{
	return exp(-absorptionCoefficient * stepSize * density);
}

half HenyeyGreenstein(half cos_angle, half eccentricity)
{
	half e2 = eccentricity * eccentricity;
	float f = abs((1.0 + e2 - 2.0 * eccentricity * cos_angle));
	return ((1.0 - e2) / pow(f, 1.5)) / 4.0 * 3.1416;
}

struct OSLightingData
{
	half3 baseLighting;
	half3 outScatterLighting;
};

OSLightingData GetLightingDataVolumetric(StaticMaterialData materialData, RayData rayData, AtmosphereData atmosData, int mip)
{
	RayData cachedRayData = rayData;
	// Set Up
	half3 cachedRayOrigin = rayData.rayPosition;
	
	half lightSampleDistribution = 2.0; // Parameterize
	int sampleCount = 4; // Parameterize
	
	half3 spherePosition = half3(materialData.rayOrigin.x, -materialData.planetRadius, materialData.rayOrigin.z);
	IntersectData intersectData = RaySphereIntersect(rayData.rayPosition, materialData.sunPos, materialData.planetRadius + atmosData.atmosHeight + atmosData.atmosThickness, spherePosition);
	half lightingDistanceToSample = min(intersectData.backfaceDistance, materialData.lightingDistance);
	
	half totalDensity = 0.0;
	half currentStepSize = 0.0;
	half cloudDensity = 0.0;
	half loddedD = GetCloudShapeVolumetric(materialData, rayData, atmosData, mip + 2);
	
	for (int i = 1; i <= sampleCount; i++)
	{
		// Step the ray forward
		half lightSample = half(i) / half(sampleCount + 1);
		lightSample = pow(lightSample, lightSampleDistribution);
		
		half totalDistance = lightSample * lightingDistanceToSample;
		currentStepSize = totalDistance - currentStepSize;
		rayData.rayPosition = cachedRayOrigin + (materialData.sunPos * totalDistance);
		
		// Sample the cloud density to determine the lighting influence on this point.
		cloudDensity = GetCloudShapeVolumetric(materialData, rayData, atmosData, mip + floor((i - 1) * 0.5));
		totalDensity += cloudDensity * currentStepSize;
	}
	
	half amp = 1.0;
	OSLightingData osLightingData;
	osLightingData.baseLighting = cloudDensity;
	osLightingData.baseLighting = exp(-totalDensity * materialData.extinction);
	osLightingData.baseLighting += saturate((1.0 - loddedD) * 0.03);
	osLightingData.outScatterLighting = 0.0;
	
	for (int octaveCounter = 1; octaveCounter < materialData.multipleScatteringOctaves; octaveCounter++)
	{
		amp *= materialData.multipleScatteringAmpGain;
		totalDensity *= materialData.multipleScatteringDensityGain;
		osLightingData.outScatterLighting += exp(-totalDensity * materialData.extinction) * amp;
	}
	
	return osLightingData;
}


void SampleClouds_half(SamplerState CloudSampler, half3 RayOrigin, half3 RayDir, half3 SunPos, Texture3D BaseTexture3D, Texture3D DetailTexture3D, half AlphaAccumulation, half Cloudiness, half3 SunColor, half3 AmbientColor, half BlueNoise, half NumSteps, half CloudLayerHeight, half CloudLayerThickness, half CloudFadeDistance, half3 BaseLayerScale, half BlueNoiseStrength, half Detail1Strength, half3 BaseTimescale, half3 Detail1Timescale, half3 Detail1Scale, half FogPower, half SceneDepthEye, half3 VolumetricsFogColor, half SceneDepth01, half LightingDistance, half PlanetRadius, Texture2D CurlNoise, half CurlScale, half CurlStrength, half CurlTimescale, half CurlAdjustmentBase, half SunIntensity, Texture3D DetailTexture3D2, half3 Detail2Scale, half3 Detail2Timescale, half Detail2Intensity, half AmbientExposure, half DistantCoverageDepth, half DistantCoverageAmount, half2 Detail1HeightRemap, bool Detail1Invert, half2 Detail2HeightRemap, bool Detail2Invert, half HeightDensityInfluence, half CloudinessDensityInfluence, Texture2D HighAltitudeCloudsTexture1, Texture2D HighAltitudeCloudsTexture2, Texture2D HighAltitudeCloudsTexture3, half2 HighAltOffset1, half2 HighAltOffset2, half2 HighAltOffset3, half2 HighAltScale1, half2 HighAltScale2, half2 HighAltScale3, half HighAltCloudiness, half HighAltInfluence1, half HighAltInfluence2, half HighAltInfluence3, half4 BaseRGBAInfluence, half4 Detail1RGBAInfluence, half4 Detail2RGBAInfluence, half HighAltitudeAlphaAccumulation, bool RenderLocal, half MultipleScatteringAmpGain, half MultipleScatteringDensityGain, int MultipleScatteringOctaves, half HGForward, half HGBack, half HGBlend, half HGStrength, Texture2D WeathermapTex, SamplerState WeathermapSampler, out half4 cloudData, out half3 cloudColor, out half alpha)
{
	/*
	Optimizations:
	1. Sample low freq noise at low lod level, once value > 0, go get the detailed noise and sample it until you get <= 0 continuously for 10 samples, then return back to low lod.
	2. Incremental mip for lighting -> done
	3. Add mip sampler levels to improve performance based on (1) current alpha and (2) distance
    4. Early Exit if Ray is Pointed Towards the surface of the planet. -> Tested, need 2 samples to provide smooth gradient + limited use cases. -> not doing
	5. Reprojection
	6. Convert some xyz depth tests to xz depth tests where logical
	7. Once TAA implemented, implement UV point jitter for temporal multisampling
	8. Give option of ultra low res buffer where Depth Cull
	*/
	
	
#ifdef SHADERGRAPH_PREVIEW
	alpha = 1.0;
	cloudColor = 0.0;
	cloudData = half4(cloudColor, 1.0);
#else
	alpha = 1.0;
	cloudColor = 0.0;
	
	// Material Data Setup
	StaticMaterialData materialData;
	
	materialData.fogSampler = CloudSampler;
	
	materialData.rayOrigin = RayOrigin;
	materialData.sunPos = SunPos;
	
	materialData.cloudiness = Remap(0.0, 1.0, 0.0, 0.9, Cloudiness);
	materialData.alphaAccumulation = max(AlphaAccumulation * 0.001, 0);
	materialData.baseRGBAInf = BaseRGBAInfluence;
	
	materialData.multipleScatteringAmpGain = max(MultipleScatteringAmpGain, 0.0);
	materialData.multipleScatteringDensityGain = max(MultipleScatteringDensityGain, 0.0);
	materialData.multipleScatteringOctaves = max(MultipleScatteringOctaves, 1);
	
	materialData.baseTexture = BaseTexture3D;
	materialData.baseScale = BaseLayerScale;
	materialData.baseTimescale = BaseTimescale;
	
	materialData.curlNoise = CurlNoise;
	
	materialData.detail1Texture = DetailTexture3D;
	materialData.detail1Scale = Detail1Scale;
	materialData.detail1Strength = Detail1Strength;
	materialData.detail1Timescale = Detail1Timescale;
	materialData.detail1Invert = Detail1Invert;
	materialData.detail1HeightRemap = Detail1HeightRemap;
	materialData.detail1RGBAInf = Detail1RGBAInfluence;
	
	materialData.detail2Texture = DetailTexture3D2;
	materialData.detail2Scale = Detail2Scale;
	materialData.detail2Strength = Detail2Intensity;
	materialData.detail2Timescale = Detail2Timescale;
	materialData.detail2HeightRemap = Detail2HeightRemap;
	materialData.detail2Invert = Detail2Invert;
	materialData.detail2RGBAInf = Detail2RGBAInfluence;
	
	materialData.lightingDistance = LightingDistance;
	materialData.planetRadius = PlanetRadius * 1000.0;
	
	materialData.curlScale = CurlScale;
	materialData.curlStrength = CurlStrength;
	materialData.curlTimescale = CurlTimescale;
	materialData.curlAdjustmentBase = CurlAdjustmentBase;
	
	materialData.heightDensityInfluence = HeightDensityInfluence;
	materialData.cloudinessDensityInfluence = CloudinessDensityInfluence;
	
	materialData.highAltTex1 = HighAltitudeCloudsTexture1;
	materialData.highAltTex2 = HighAltitudeCloudsTexture2;
	materialData.highAltTex3 = HighAltitudeCloudsTexture3;
	materialData.highAltitudeAlphaAccumulation = HighAltitudeAlphaAccumulation;
	materialData.highAltitudeCloudiness = HighAltCloudiness;
	materialData.highAltOffset1 = HighAltOffset1;
	materialData.highAltOffset2 = HighAltOffset2;
	materialData.highAltOffset3 = HighAltOffset3;
	materialData.highAltScale1 = HighAltScale1;
	materialData.highAltScale2 = HighAltScale2;
	materialData.highAltScale3 = HighAltScale3;
	materialData.highAltInfluence1 = HighAltInfluence1;
	materialData.highAltInfluence2 = HighAltInfluence2;
	materialData.highAltInfluence3 = HighAltInfluence3;
	
	materialData.weathermapTex = WeathermapTex;
	materialData.weathermapSampler = WeathermapSampler;
	
	// Cloud Parameter Setup
	AtmosphereData atmosData;
	atmosData.atmosThickness = CloudLayerThickness * 1000;
	atmosData.atmosHeight = CloudLayerHeight * 1000;
	atmosData.cloudFadeDistance = CloudFadeDistance * 1000;
	atmosData.distantCoverageDepth = DistantCoverageDepth * 1000;
	atmosData.distantCoverageAmount = Remap(0.0, 1.0, 0.0, 0.9, DistantCoverageAmount);
	
	// Lighting Parameter Setup
	RayDir = normalize(RayDir);
	AtmosHitData hitData = AtmosphereIntersection(RayOrigin, RayDir, atmosData.atmosHeight, materialData.planetRadius, atmosData.atmosThickness, atmosData.cloudFadeDistance);;
	
	bool doSampleClouds = false;
	if (hitData.didHit && hitData.nearDist < atmosData.cloudFadeDistance)
		doSampleClouds = true;
	
	
	half ambientLightColorVal = 0.0;
	
	if (doSampleClouds)
	{
		// INITIAL SETUP
		half depth = hitData.nearDist;
		half maxDepth = min(hitData.farDist, atmosData.cloudFadeDistance);
		half totalDepth = maxDepth - depth;
		
		/*
		if (totalDepth > 12000.0)
		{
			totalDepth = 12000.0;
			hitData.doHighAltMarchAtNear = false;
			hitData.doHighAltMarchAtFar = false;
		}
		*/		

		half samplingStartDepth = depth;
		half invNumSteps = 1.0 / NumSteps;
		
		
		half gapDepth = 0;
		bool accountedForDoubleIntersect = true;
		
		if (hitData.doubleIntersection)
		{
			totalDepth = totalDepth + (hitData.farDist2 - hitData.nearDist2);
			gapDepth = hitData.nearDist2 - hitData.farDist;
			accountedForDoubleIntersect = false;
		}
		
		RayData rayData;
		rayData.rayDirection = RayDir;
		rayData.meanStepSize = totalDepth * invNumSteps;
		
		// SAMPLING
		half valueAtPoint = 0;
		half3 lightEnergy = 0.0;
		rayData.noiseAdjustment = Remap(0.0, 1.0, -min(rayData.meanStepSize, abs(depth)), rayData.meanStepSize, BlueNoise) * BlueNoiseStrength;
		depth += rayData.noiseAdjustment;
		
		
		half fogFactor = samplingStartDepth;
		half ambientEnergy = 0.0;
		int mip = 0;
		
		// Physically realistic HGForward Value is 0.6. HGBack is a purely artistic factor.
		half cos_angle = dot(normalize(materialData.sunPos), normalize(RayDir));
		HGForward = HenyeyGreenstein(cos_angle, HGForward);
		HGBack = HenyeyGreenstein(cos_angle, HGBack);
		half HG = max(HGForward, HGBack);
		HG = lerp(1.0, HG, saturate(HGStrength));
		materialData.HG = HG;
		
		// Scattering Values (source: https://journals.ametsoc.org/view/journals/bams/79/5/1520-0477_1998_079_0831_opoaac_2_0_co_2.xml):
		// Cumulus: 50 - 120
		// Stratus: 40 - 60
		// Cirrus: 0.1 - 0.7
		
		// Wavelength-specific Scattering Distribution for Cloudy medium : https://www.patarnott.com/satsens/pdf/opticalPropertiesCloudsReview.pdf
		
		// Albedo of cloudy material is near to 1. Given that extinction coefficient is calculated as absorption + scattering, when absorption = 0 then extinction = scattering.
		half3 scattering = max(materialData.alphaAccumulation, 0.000000001) * half3(1.0, 0.964, 0.92);
		half3 highAltScattering = max(materialData.highAltitudeAlphaAccumulation, 0.000000001) * half3(1.0, 0.964, 0.92);
		half3 absorption = 0.0;
		half3 extinction = scattering + absorption;
		materialData.extinction = extinction;
		materialData.highAltExtinction = highAltScattering;
		half baseEnergy = 0.0;
		
		half priorTotalDist = 0.0;
		half priorSample = 0.0;
		for (int i = 1; i <= int(NumSteps); i++)
		{
			// Sample Cloud Shape
			rayData.rayDepth = depth;
			rayData.rayPosition = RayOrigin + (RayDir * rayData.rayDepth);
			
			if (rayData.rayPosition.y < 0.0)
				break;
			
			if (RenderLocal && rayData.rayDepth > SceneDepthEye && SceneDepth01 < 1.0)
				break;
			
			
			if (alpha < 0.3 && rayData.rayDepth > 10000)
				mip = 1;
			
			
			if (alpha < 0.15 && rayData.rayDepth > 10000)
				mip = 2;
			
			valueAtPoint = GetCloudShapeVolumetric(materialData, rayData, atmosData, mip);
			
			// If the cloud exists at this point, sample the lighting
			if (valueAtPoint > 0.00)
			{
				half priorAlpha = alpha;
				half3 sampleExtinction = materialData.extinction * valueAtPoint;
				
				half transmittance = exp(-sampleExtinction * rayData.meanStepSize).r;
				alpha *= transmittance;
				half3 invSampleExtinction = 1.0 / sampleExtinction;
				
				// Height
				half heightVal = HeightInAtmos(materialData.rayOrigin, rayData.rayPosition, atmosData.atmosThickness, materialData.planetRadius, atmosData.atmosHeight);
				
				half3 scatterVal = scattering * valueAtPoint;
				
				if (SunIntensity > 0.001)
				{
					// In-Scattering
					half inScattering = valueAtPoint;
					inScattering *= 8.0;
					inScattering = min(1.0, inScattering);
					inScattering = inScattering * inScattering;
					half heightInScattering = saturate(Remap(0.0, 1.0, 0.1, 0.5, heightVal));
					half inScatterAdjustment = 0.0;
					inScattering = inScatterAdjustment + lerp(inScattering, 1.0, heightInScattering);
					inScattering = 1.0;
				
					// Direct
					OSLightingData osLightingData = GetLightingDataVolumetric(materialData, rayData, atmosData, mip);
					half3 lightData = ((osLightingData.baseLighting * HG) + osLightingData.outScatterLighting) * scatterVal * inScattering;
					half3 intScatter = (lightData - (lightData * transmittance)) * invSampleExtinction;
					lightEnergy += intScatter * priorAlpha;
				}
				
				//Ambient
				half invHeightVal = 1.0 - heightVal;
				half intAmbient = scatterVal.r * Remap(0.0, 1.0, 0.4, 1.0, 1.0 - (invHeightVal * invHeightVal));
				intAmbient = (intAmbient - (intAmbient * transmittance)) * invSampleExtinction.r;
				ambientEnergy += intAmbient * priorAlpha;
				
				// Fog
				half energData = (scatterVal.r - (scatterVal.r * transmittance)) * invSampleExtinction.r;
				baseEnergy += energData * priorAlpha;
				
				if (samplingStartDepth <= 0.0)
				{
					samplingStartDepth = rayData.rayDepth;
				}
				else
				{
					samplingStartDepth += (rayData.rayDepth - samplingStartDepth) * energData * priorAlpha;
				}
			}
			
			if ((i == int(NumSteps) && hitData.doHighAltMarchAtFar) || (i == 1 && hitData.doHighAltMarchAtNear))
			{
				half highAltDepth = hitData.farDist;
				if (hitData.doHighAltMarchAtNear)
				{
					highAltDepth = hitData.nearDist;
				}
				rayData.rayPosition = RayOrigin + (RayDir * (highAltDepth));
				
				valueAtPoint = GetCloudShape2D(materialData, rayData, atmosData, 0);
				if (valueAtPoint > 0.00)
				{
					half priorAlpha = alpha;
					
					half3 sampleExtinction = materialData.highAltExtinction * valueAtPoint;
				
					half transmittance = exp(-sampleExtinction.r);
					alpha *= transmittance;
					half3 invSampleExtinction = 1.0 / sampleExtinction;
					
					RayData tempRayData = rayData;
					tempRayData.rayPosition = tempRayData.rayPosition + SunPos * 200.0;
					half3 scatterVal = highAltScattering * valueAtPoint;
					half lightSample = GetCloudShape2D(materialData, tempRayData, atmosData, 0) * 0.1;
					half3 lighting = exp(-lightSample * materialData.highAltExtinction);
					half3 lightData = lighting * scatterVal * HG;
					half3 intScatter = (lightData - (lightData * transmittance)) * invSampleExtinction;
					lightEnergy += intScatter * priorAlpha;

					//Ambient
					half intAmbient = scatterVal.r;
					intAmbient = (intAmbient - (intAmbient * transmittance)) * invSampleExtinction.r;
					ambientEnergy += intAmbient * priorAlpha;
				
					// Fog
					half energData = (scatterVal.r - (scatterVal.r * transmittance)) * invSampleExtinction.r;
					baseEnergy += energData * priorAlpha;
					
					samplingStartDepth += (highAltDepth - samplingStartDepth) * energData * priorAlpha;
				}
			}
			
			if (alpha <= 0.01)
			{
				break;
			}
			
			depth += rayData.meanStepSize;
			
			// Handle Double Intersect if needed.
			if (depth > hitData.farDist && !accountedForDoubleIntersect)
			{
				depth = hitData.nearDist2;
				accountedForDoubleIntersect = true;
			}
		}
		
		if (alpha < 1.0)
		{
			half fogval = saturate(samplingStartDepth / atmosData.cloudFadeDistance);
			fogval = pow(fogval, FogPower);
			half invFogVal = 1.0 - fogval;
			
			cloudColor = 0.0;
			cloudColor += lightEnergy * SunColor * SunIntensity * invFogVal;
			
			// Ambient Lighting
			half fogAmbientBlend = 0.5; // Parameterize
			half ambientSaturation = 0.75; // Parameterize
			AmbientColor = lerp(AmbientColor, VolumetricsFogColor, fogAmbientBlend);
			AmbientColor = Luminance(AmbientColor, ambientSaturation);
			cloudColor += ambientEnergy * AmbientColor * AmbientExposure * invFogVal;
			
			
			// Depth Fog
			cloudColor += VolumetricsFogColor * fogval * baseEnergy;
		}
		
	}
	cloudData = half4(cloudColor, alpha);
	
	
#endif
}

#endif