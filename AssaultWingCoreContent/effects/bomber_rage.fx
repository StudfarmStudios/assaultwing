float T;
float TextureWidth;
float TextureHeight;
texture Texture;

float Intensity = 1;

sampler TextureSampler : SAMPLER0 = sampler_state
{
	Texture = <Texture>;
	MagFilter = LINEAR;
	MinFilter = LINEAR;
	MipFilter = NONE;
	AddressU = Clamp;
	AddressV = Clamp;
};

float4 PixelShaderFunction(float2 texCoord : TEXCOORD0) : COLOR0
{
	float timeIntensity = 1 + 0.1 * sin(T);
    float4 pix = tex2D(TextureSampler, texCoord);
    float distance = length(texCoord - 0.5) / 0.7;
    float pixelIntensity = Intensity * timeIntensity * distance * distance;
    float4 rage = float4(0.6, 0.1, 0.05, 1);
    return pix * (1 - pixelIntensity) + rage * pixelIntensity;
}

technique BomberRage
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
