#if OPENGL
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
float Time;
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

float2 random2(float2 p)
{
	return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
}

float voroni(float2 pos, float2 grid, float2 offset)
{	
	// Scale
	pos *= grid;

	// Offset
	pos += fmod(offset, grid);

	// Tile the space
	float2 i_st = floor(pos);
	float2 f_st = frac(pos);

	float m_dist = 1.0; // minimum distance

	for (int y = -1; y <= 1; y++)
	{
		for (int x = -1; x <= 1; x++)
		{
			// Neighbor place in the grid
			float2 neighbor = float2(float(x), float(y));

			// Random position from current + neighbor place in the grid
			float2 pnt = random2(fmod(i_st + neighbor, grid));

			// Animate the point
			pnt = 0.5 + 0.5 * sin(Time * 2.0 + 6.2831 * pnt);

			// Vector between the pixel and the point
			float2 diff = neighbor + pnt
			-f_st;

			// Distance to the point
			float dist = length(diff);

			// Keep the closer distance
			m_dist = min(m_dist, dist);
		}
	}
	
	//m_dist += step(.95, f_st.x) + step(.95, f_st.y);
	
	return m_dist * m_dist * .75;
}

// - u_time * .3 + sin(st.x * 5. + u_time) * .1;

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 c = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
	float2 uv = input.Position.xy / Resolution;

	//c = input.Color;

	float a = 0;
	//sin(input.Position.x / 8 + Time * 5.0) * 0.1, Time * 2.0
	a += voroni(uv, floor(Resolution / 16.0), float2(0.0, Time));
	a += voroni(uv, floor(Resolution / 12.0), float2(Time * .2, Time * 1.2));
	a = clamp(a, 0.0, 1.0);
	c *= a;

	return c;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};