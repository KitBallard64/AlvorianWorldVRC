// Made with Amplify Shader Editor v1.9.5.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MoonEclipse"
{
	Properties
	{
		[NoScaleOffset][SingleLineTexture]_MoonTexBase("MoonTexBase", 2D) = "white" {}
		[NoScaleOffset][SingleLineTexture]_SunNoise("SunNoise", 2D) = "white" {}
		_Sun("Sun Transition", Range( 0 , 1)) = 0
		[HDR]_Tint("Tint", Color) = (0.1981132,0.1981132,0.1981132,0)
		[HDR]_SunColor("Sun Color", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
		_HueShift("Hue Shift", Range(0.0, 1.0)) = 0.0
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha noshadow nofog
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _MoonTexBase;
		float4 _MoonTexBase_TexelSize;
		uniform float4 _Tint;
		uniform float4 _SunColor;
		uniform sampler2D _SunNoise;
		uniform float _Sun, _HueShift;

    // Functions
    // Hue Shifts (Corrective Luminescence)
    float3 RGB_OKLAB(float3 c) // (Sourced from https://gist.github.com/Fonserbc/c6b011f9ecaf22273ed70d3f76307cc5)
        {
            c = max(c, 0.0000000001);
            float l = 0.4122214708 * c.x + 0.5363325363 * c.y + 0.0514459929 * c.z;
            float m = 0.2119034982 * c.x + 0.6806995451 * c.y + 0.1073969566 * c.z;
            float s = 0.0883024619 * c.x + 0.2817188376 * c.y + 0.6299787005 * c.z;
            
            float l_ = pow(l, 1.0 / 3.0);
            float m_ = pow(m, 1.0 / 3.0);
            float s_ = pow(s, 1.0 / 3.0);
            
            return float3(
            0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_,
            1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_,
            0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_
            );
        }

    float3 OKLAB_RGB(float3 c) // (Sourced from https://gist.github.com/Fonserbc/c6b011f9ecaf22273ed70d3f76307cc5)
        {
            float l_ = c.x + 0.3963377774 * c.y + 0.2158037573 * c.z;
            float m_ = c.x - 0.1055613458 * c.y - 0.0638541728 * c.z;
            float s_ = c.x - 0.0894841775 * c.y - 1.2914855480 * c.z;
            
            float l = l_ * l_ * l_;
            float m = m_ * m_ * m_;
            float s = s_ * s_ * s_;
            
            return float3(
            + 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
            - 1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            - 0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s
            );
        }

    float3 HUEShift(float3 RGB, float Shift)
        {
            float3 RGB_Linear = RGB_OKLAB(RGB);
            float Hue = atan2(RGB_Linear.z, RGB_Linear.y);

            Shift *= (UNITY_PI * 2);
            Hue += Shift;
            float ChromaAdjust = length(RGB_Linear.yz);

            RGB_Linear.y = cos(Hue) * ChromaAdjust;
            RGB_Linear.z = sin(Hue) * ChromaAdjust;
            
            return OKLAB_RGB(RGB_Linear);
        }

		void CalculateUVsSmooth46_g11( float2 UV, float4 TexelSize, out float2 UV0, out float2 UV1, out float2 UV2, out float2 UV3, out float2 UV4, out float2 UV5, out float2 UV6, out float2 UV7, out float2 UV8 )
		{
			{
			    float3 pos = float3( TexelSize.xy, 0 );
			    float3 neg = float3( -pos.xy, 0 );
			    UV0 = UV + neg.xy;
			    UV1 = UV + neg.zy;
			    UV2 = UV + float2( pos.x, neg.y );
			    UV3 = UV + neg.xz;
			    UV4 = UV;
			    UV5 = UV + pos.xz;
			    UV6 = UV + float2( neg.x, pos.y );
			    UV7 = UV + pos.zy;
			    UV8 = UV + pos.xy;
			    return;
			}
		}


		float3 CombineSamplesSmooth58_g11( float Strength, float S0, float S1, float S2, float S3, float S4, float S5, float S6, float S7, float S8 )
		{
			{
			    float3 normal;
			    normal.x = Strength * ( S0 - S2 + 2 * S3 - 2 * S5 + S6 - S8 );
			    normal.y = Strength * ( S0 + 2 * S1 + S2 - S6 - 2 * S7 - S8 );
			    normal.z = 1.0;
			    return normalize( normal );
			}
		}


float2 voronoihash88( float2 p )
{
	
	p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
	return frac( sin( p ) *43758.5453);
}


float voronoi88( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
{
	float2 n = floor( v );
	float2 f = frac( v );
	float F1 = 8.0;
	float F2 = 8.0; float2 mg = 0;
	for ( int j = -1; j <= 1; j++ )
	{
		for ( int i = -1; i <= 1; i++ )
	 	{
	 		float2 g = float2( i, j );
	 		float2 o = voronoihash88( n + g );
			o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
			float d = 0.5 * dot( r, r );
	 //		if( d<F1 ) {
	 //			F2 = F1;
	 			float h = smoothstep(0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness); F1 = lerp(F1, d, h) - smoothness * h * (1.0 - h);mg = g; mr = r; id = o;
	 //		} else if( d<F2 ) {
	 //			F2 = d;
	
	 //		}
	 	}
	}
	return F1;
}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float temp_output_91_0_g11 = 5.0;
			float Strength58_g11 = temp_output_91_0_g11;
			float localCalculateUVsSmooth46_g11 = ( 0.0 );
			float2 panner27 = ( -0.01 * _Time.y * float2( 1,0 ) + float2( 0,0 ));
			float2 uv_TexCoord25 = i.uv_texcoord + panner27;
			float2 temp_output_85_0_g11 = uv_TexCoord25;
			float2 UV46_g11 = temp_output_85_0_g11;
			float4 TexelSize46_g11 = _MoonTexBase_TexelSize;
			float2 UV046_g11 = float2( 0,0 );
			float2 UV146_g11 = float2( 0,0 );
			float2 UV246_g11 = float2( 0,0 );
			float2 UV346_g11 = float2( 0,0 );
			float2 UV446_g11 = float2( 0,0 );
			float2 UV546_g11 = float2( 0,0 );
			float2 UV646_g11 = float2( 0,0 );
			float2 UV746_g11 = float2( 0,0 );
			float2 UV846_g11 = float2( 0,0 );
			CalculateUVsSmooth46_g11( UV46_g11 , TexelSize46_g11 , UV046_g11 , UV146_g11 , UV246_g11 , UV346_g11 , UV446_g11 , UV546_g11 , UV646_g11 , UV746_g11 , UV846_g11 );
			float4 break140_g11 = tex2D( _MoonTexBase, UV046_g11 );
			float S058_g11 = break140_g11.r;
			float4 break142_g11 = tex2D( _MoonTexBase, UV146_g11 );
			float S158_g11 = break142_g11.r;
			float4 break146_g11 = tex2D( _MoonTexBase, UV246_g11 );
			float S258_g11 = break146_g11.r;
			float4 break148_g11 = tex2D( _MoonTexBase, UV346_g11 );
			float S358_g11 = break148_g11.r;
			float4 break150_g11 = tex2D( _MoonTexBase, UV446_g11 );
			float S458_g11 = break150_g11.r;
			float4 break152_g11 = tex2D( _MoonTexBase, UV546_g11 );
			float S558_g11 = break152_g11.r;
			float4 break154_g11 = tex2D( _MoonTexBase, UV646_g11 );
			float S658_g11 = break154_g11.r;
			float4 break156_g11 = tex2D( _MoonTexBase, UV746_g11 );
			float S758_g11 = break156_g11.r;
			float4 break158_g11 = tex2D( _MoonTexBase, UV846_g11 );
			float S858_g11 = break158_g11.r;
			float3 localCombineSamplesSmooth58_g11 = CombineSamplesSmooth58_g11( Strength58_g11 , S058_g11 , S158_g11 , S258_g11 , S358_g11 , S458_g11 , S558_g11 , S658_g11 , S758_g11 , S858_g11 );
			o.Normal = localCombineSamplesSmooth58_g11;
			o.Albedo = ( ( _Tint.rgb * tex2D( _MoonTexBase, uv_TexCoord25 ).r ) + ( _Tint.rgb * 0.025 ) );
			float2 panner78 = ( 0.05 * _Time.y * float2( 0.2,-0.5 ) + float2( 0,0 ));
			float2 uv_TexCoord79 = i.uv_texcoord * float2( 10,5 ) + panner78;
			float2 panner83 = ( -0.2 * _Time.y * float2( 0.2,0.3 ) + float2( 0,0 ));
			float2 uv_TexCoord84 = i.uv_texcoord * float2( 6,2 ) + panner83;
			float2 panner85 = ( 0.25 * _Time.y * float2( 0.1,0.1 ) + float2( 0,0 ));
			float2 uv_TexCoord86 = i.uv_texcoord * float2( 13,6 ) + panner85;
			float time88 = _Time.y;
			float2 voronoiSmoothId88 = 0;
			float voronoiSmooth88 = 0.5;
			float2 coords88 = i.uv_texcoord * 20.56;
			float2 id88 = 0;
			float2 uv88 = 0;
			float fade88 = 0.5;
			float voroi88 = 0;
			float rest88 = 0;
			for( int it88 = 0; it88 <7; it88++ ){
			voroi88 += fade88 * voronoi88( coords88, time88, id88, uv88, voronoiSmooth88,voronoiSmoothId88 );
			rest88 += fade88;
			coords88 *= 2;
			fade88 *= 0.5;
			}//Voronoi88
			voroi88 /= rest88;
			float3 lerpResult73 = lerp( float3( 0,0,0 ) , ( HUEShift(_SunColor.rgb, _HueShift) * ( tex2D( _SunNoise, uv_TexCoord79 ).r * tex2D( _SunNoise, uv_TexCoord84 ).r * tex2D( _SunNoise, uv_TexCoord86 ).r * ( saturate( ( voroi88 * 10.0 ) ) + 0.2 ) ) ) , _Sun);
			o.Emission = lerpResult73;
			o.Metallic = 1.0;
			o.Smoothness = 0.25;
			o.Alpha = 1;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19501
Node;AmplifyShaderEditor.SimpleTimeNode;89;-288,1344;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;90;-256,1440;Inherit;False;Constant;_Float3;Float 3;6;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;88;-96,1344;Inherit;True;0;0;1;0;7;False;1;False;True;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;20.56;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;91;176,1344;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;83;-768,1024;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.2,0.3;False;1;FLOAT;-0.2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;78;-768,880;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.2,-0.5;False;1;FLOAT;0.05;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;85;-672,1168;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.1,0.1;False;1;FLOAT;0.25;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;27;-320,480;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT;-0.01;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;92;320,1360;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;79;-592,880;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;10,5;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;84;-592,1024;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;6,2;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;86;-256,1152;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;13,6;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;75;-352,656;Inherit;True;Property;_SunNoise;SunNoise;1;2;[NoScaleOffset];[SingleLineTexture];Create;True;0;0;0;False;0;False;None;8eedb0cb7d087c34bab69a73edc9e164;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;25;-128,368;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;76;16,656;Inherit;True;Property;_TextureSample1;Texture Sample 1;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;80;32,864;Inherit;True;Property;_TextureSample2;Texture Sample 1;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;81;32,1072;Inherit;True;Property;_TextureSample12;Texture Sample 1;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;93;464,1376;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;1;-640,288;Inherit;True;Property;_MoonTexBase;MoonTexBase;0;2;[NoScaleOffset];[SingleLineTexture];Create;True;0;0;0;False;0;False;None;82bc08386cece8e4d8244403c28aa1af;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;2;144,288;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;71;320,16;Inherit;False;Constant;_Float2;Float 2;4;0;Create;True;0;0;0;False;0;False;0.025;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;82;389.7479,884.4026;Inherit;True;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;87;320,512;Inherit;False;Property;_SunColor;Sun Color;4;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;766.9959,142.9158,32.98086,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;63;128,-192;Inherit;False;Property;_Tint;Tint;3;1;[HDR];Create;False;0;0;0;False;0;False;0.1981132,0.1981132,0.1981132,0;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;69;592,96;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;615.4125,-91.24655;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;576,544;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;74;416,736;Inherit;False;Property;_Sun;Sun Transition;2;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureTransformNode;26;-368,368;Inherit;False;-1;False;1;0;SAMPLER2D;;False;2;FLOAT2;0;FLOAT2;1
Node;AmplifyShaderEditor.FunctionNode;62;608,256;Inherit;False;Normal From Texture;-1;;11;9728ee98a55193249b513caf9a0f1676;13,149,0,147,0,143,0,141,0,139,0,151,0,137,0,153,0,159,0,157,0,155,0,135,0,108,1;4;87;SAMPLER2D;0;False;85;FLOAT2;0,0;False;74;SAMPLERSTATE;0;False;91;FLOAT;5;False;2;FLOAT3;40;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;70;800,-96;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;73;736,528;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;64;960,640;Inherit;False;Constant;_Float0;Float 0;2;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;65;960,720;Inherit;False;Constant;_Float1;Float 1;3;0;Create;True;0;0;0;False;0;False;0.25;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1168,288;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;MoonEclipse;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;88;1;89;0
WireConnection;88;3;90;0
WireConnection;91;0;88;0
WireConnection;92;0;91;0
WireConnection;79;1;78;0
WireConnection;84;1;83;0
WireConnection;86;1;85;0
WireConnection;25;1;27;0
WireConnection;76;0;75;0
WireConnection;76;1;79;0
WireConnection;80;0;75;0
WireConnection;80;1;84;0
WireConnection;81;0;75;0
WireConnection;81;1;86;0
WireConnection;93;0;92;0
WireConnection;2;0;1;0
WireConnection;2;1;25;0
WireConnection;82;0;76;1
WireConnection;82;1;80;1
WireConnection;82;2;81;1
WireConnection;82;3;93;0
WireConnection;69;0;63;5
WireConnection;69;1;2;1
WireConnection;72;0;63;5
WireConnection;72;1;71;0
WireConnection;77;0;87;5
WireConnection;77;1;82;0
WireConnection;26;0;1;0
WireConnection;62;87;1;0
WireConnection;62;85;25;0
WireConnection;70;0;69;0
WireConnection;70;1;72;0
WireConnection;73;1;77;0
WireConnection;73;2;74;0
WireConnection;0;0;70;0
WireConnection;0;1;62;40
WireConnection;0;2;73;0
WireConnection;0;3;64;0
WireConnection;0;4;65;0
ASEEND*/
//CHKSM=E45446BC088254C1C02D605EA5BD6B57EED93411