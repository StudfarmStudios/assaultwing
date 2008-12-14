
// bloomatic.fx
sampler samplerState; 

//Alpha value
float alpha;

float maxx;
float maxy;

//Operate on a high range (0.5 - 1.0) or the full range (0.0 - 1.0)
bool hirange;
/*
static const float gauss[25] = {
0.0029150245, 0.0130642333, 0.0215392793, 0.0130642333, 0.0029150245,
0.0130642333, 0.0585498315, 0.0965323526, 0.0585498315, 0.0130642333,
0.0215392793, 0.0965323526, 0.1591549431, 0.0965323526, 0.0215392793,
0.0130642333, 0.0585498315, 0.0965323526, 0.0585498315, 0.0130642333,
0.0029150245, 0.0130642333, 0.0215392793, 0.0130642333, 0.0029150245 };
*/
static const float gauss[9] = {
0.07, 0.13, 0.07,
0.13, 0.20, 0.13,
0.07, 0.13, 0.07 };

struct PS_INPUT 
{ 
    float2 TexCoord : TEXCOORD0; 
}; 

float4 bloomEffect(PS_INPUT Input) : COLOR0 { 
   // float4 sum = tex2D(samplerState, Input.TexCoord);
    float4 sum = (0,0,0,0);
    float4 tex;

	float pixelsizeX = 1.0 / maxx;
    float pixelsizeY = 1.0 / maxy;
    
    int i = 0;
    for (int y = -1; y < 2; y++) {
		for (int x = -1; x <2; x++) {
			float2 offset = float2(x * pixelsizeX, y * pixelsizeY);
			tex = tex2D(samplerState, Input.TexCoord + offset);
			sum += tex * gauss[i];
			i++;
		}
	}
        
    //fix alpha
    sum.a = alpha;
    
    //expand the higher range if applicable
    if (hirange) {
        sum.r = (sum.r - 0.5) * 2.0;
        sum.g = (sum.g - 0.5) * 2.0;
        sum.b = (sum.b - 0.5) * 2.0;
    }
    
    return sum;
}

technique Bloom
{
    pass P0
    {
        PixelShader = compile ps_2_0 bloomEffect();
    }
}
