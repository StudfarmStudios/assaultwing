float T;
texture Texture;
float TextureWidth, TextureHeight;

sampler TextureSampler = sampler_state
{
	Texture = <Texture>;
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 TexCoord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    return input;
}

float4 PixelShaderFunction(float4 texCoord : TEXCOORD0) : COLOR0
{
	return tex2D(TextureSampler, texCoord);
}

technique Simple2DOutputTechnique
{
    pass VertexShaderPass
    {
        VertexShader = compile vs_1_1 VertexShaderFunction();
    }
    pass PixelShaderPass
    {
		StencilEnable = FALSE;
		ZEnable = FALSE;
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
    pass PixelAndVertexShaderPass
    {
		StencilEnable = FALSE;
		ZEnable = FALSE;
		PixelShader = compile ps_2_0 PixelShaderFunction();
        VertexShader = compile vs_1_1 VertexShaderFunction();
	}
}
