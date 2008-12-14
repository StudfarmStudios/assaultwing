
// bloomatic.fx
sampler samplerState; 

//Width to sample from
float mag;

//Alpha value
float alpha;

//Operate on a high range (0.5 - 1.0) or the full range (0.0 - 1.0)
bool hirange;

const float2 offsets[12] = {
   -0.326212, -0.405805,
   -0.840144, -0.073580,
   -0.695914,  0.457137,
   -0.203345,  0.620716,
    0.962340, -0.194983,
    0.473434, -0.480026,
    0.519456,  0.767022,
    0.185461, -0.893124,
    0.507431,  0.064425,
    0.896420,  0.412458,
   -0.321940, -0.932615,
   -0.791559, -0.597705,
};

struct PS_INPUT 
{ 
    float2 TexCoord : TEXCOORD0; 
}; 

float4 bloomEffect(PS_INPUT Input) : COLOR0 { 
    float4 sum = tex2D(samplerState, Input.TexCoord);
    float4 tex;
    
    //accumulate the color values from 12 neighboring pixels
    for(int i = 0; i < 12; i++){
        tex = tex2D(samplerState, Input.TexCoord + mag * offsets[i]);
        
        sum += tex;
    }
    //average the sum
    sum /= 13;
    
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
