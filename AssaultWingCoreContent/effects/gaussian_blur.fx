float T;
texture Texture;
float TextureWidth;
float TextureHeight;

sampler samplerState = sampler_state
{
	Texture = <Texture>;
	AddressU = Clamp;
	AddressV = Clamp;
};


//Operate on a high range (0.5 - 1.0) or the full range (0.0 - 1.0)
bool hirange;
/*
static const float gauss[25] = {
0.0029150245, 0.0130642333, 0.0215392793, 0.0130642333, 0.0029150245,
0.0130642333, 0.0585498315, 0.0965323526, 0.0585498315, 0.0130642333,
0.0215392793, 0.0965323526, 0.1591549431, 0.0965323526, 0.0215392793,
0.0130642333, 0.0585498315, 0.0965323526, 0.0585498315, 0.0130642333,
0.0029150245, 0.0130642333, 0.0215392793, 0.0130642333, 0.0029150245 };

static const float gauss[5] = {
0.11, 0.22, 0.34, 0.22, 0.11 };
*/
static const float gauss[7] = {
0.0588, 0.1176, 0.1765, 0.2942, 0.1765, 0.1176, 0.0588 };

struct PS_INPUT 
{ 
    float2 TexCoord : TEXCOORD0; 
}; 

float4 GaussianBlurH(PS_INPUT Input) : COLOR0 { 
   // float4 sum = tex2D(samplerState, Input.TexCoord);
    float4 sum = (0,0,0,0);
    float4 tex;

	float pixelsizeX = 1.0 / TextureWidth;
    
    int i = 0;
	for (int x = -3; x < 4; x++) {
		float2 offset = float2(x * pixelsizeX, 0);
		tex = tex2D(samplerState, Input.TexCoord + offset);
		sum += tex * gauss[i];
		i++;
	}
        
    //expand the higher range if applicable
    if (hirange) {
        sum.r = (sum.r - 0.5) * 2.0;
        sum.g = (sum.g - 0.5) * 2.0;
        sum.b = (sum.b - 0.5) * 2.0;
    }
    
    return sum;
}

float4 GaussianBlurV(PS_INPUT Input) : COLOR0 { 
   // float4 sum = tex2D(samplerState, Input.TexCoord);
    float4 sum = (0,0,0,0);
    float4 tex;

    float pixelsizeY = 1.0 / TextureHeight;
    
    int i = 0;
	for (int y = -3; y < 4; y++) {
		float2 offset = float2(0, y * pixelsizeY);
		tex = tex2D(samplerState, Input.TexCoord + offset);
		sum += tex * gauss[i];
		i++;
	}
        
    //expand the higher range if applicable
    if (hirange) {
        sum.r = (sum.r - 0.5) * 2.0;
        sum.g = (sum.g - 0.5) * 2.0;
        sum.b = (sum.b - 0.5) * 2.0;
    }
    
    return sum;
}


technique GaussianBlur
{
    pass P0
    {
        PixelShader = compile ps_2_0 GaussianBlurH();
    }
    pass P1
    {
        PixelShader = compile ps_2_0 GaussianBlurV();
    }
}
