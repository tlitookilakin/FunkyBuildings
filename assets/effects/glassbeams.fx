#if OPENGL
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
float2 Offset;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float hardBeam(float v, float period, float choke)
{
	return smoothstep(period - choke, period, abs(fmod(v, period)));
}

float softBeam(float v, float period, float width, float expand)
{
	return smoothstep((period - width) / 2, (period - expand) / 2, abs(abs(fmod(v, period)) - period / 2));
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 c = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
	float f = input.Position.x + input.Position.y;
	float o = (Offset.x + Offset.y) / 4;

	float a = 0.0;
	a += hardBeam(f - o * .25, 80.0, 16.0) * .75;
	a += softBeam(f - o * .2, 48.0, 16.0, 0.0) * .75;
	a += hardBeam(f - o * .3, 64.0, 48.0) * .75;
	a += softBeam(f - o * .15, 32.0, 24.0, 0.0) * .6;

	c *= clamp(a, 0.0, 1.0);
	c *= .5;
	return c;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};