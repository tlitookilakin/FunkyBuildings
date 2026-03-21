#if OPENGL
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
float Time;
float4 Tint;
float4 Region;
float2 Resolution;

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

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 c = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
	float2 uv = input.TextureCoordinates;

	uv.x = fmod(uv.x + Time * .05, Region.z / Resolution.x) + Region.x / Resolution.x;
	uv.y += c.r * 255 / Resolution.y;// * 255.0;
	float4 cloud = tex2D(SpriteTextureSampler, uv) * Tint;
	cloud.rgb *= c.g;
	float4 shadow = float4(0, 0, 0, (1 - c.g) * (1 - cloud.a));

	float4 result = (shadow + cloud) * c.a;
	return result;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};