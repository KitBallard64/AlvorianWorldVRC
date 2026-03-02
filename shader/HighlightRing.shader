// Made with Amplify Shader Editor v1.9.5.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "HighlightRing"
{
	Properties
	{
		[NoScaleOffset][SingleLineTexture]_Noise("Noise", 2D) = "white" {}
		[NoScaleOffset][SingleLineTexture]_Noise2("Noise 2", 2D) = "white" {}
		[NoScaleOffset][SingleLineTexture]_Texture0("Noise 3", 2D) = "white" {}
		[HDR]_RingColor("RingColor", Color) = (1,1,1,0)
		_SunRibbons("Sun Ribbon Transition %", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
		_HueShift("Hue Shift", Range(0.0, 1.0)) = 0.0
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit alpha:fade keepalpha noshadow nofog
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float4 _RingColor;
		uniform sampler2D _Noise;
		uniform sampler2D _Noise2;
		uniform sampler2D _Texture0;
		uniform float _SunRibbons, _HueShift;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}


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

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 panner15 = ( -0.05 * _Time.y * float2( 0,1 ) + float2( 0,0 ));
			float2 uv_TexCoord16 = i.uv_texcoord * float2( 2,0.05 ) + panner15;
			float2 panner4 = ( -0.01 * _Time.y * float2( 0,1 ) + float2( 0,0 ));
			float2 uv_TexCoord3 = i.uv_texcoord * float2( 1,0.05 ) + panner4;
			float saferPower40 = abs( ( ( tex2D( _Noise, uv_TexCoord16 ).r * tex2D( _Noise2, uv_TexCoord3 ).r ) * ( 1.0 - i.uv_texcoord.y ) ) );
			float temp_output_40_0 = pow( saferPower40 , 1.5 );
			float saferPower45 = abs( temp_output_40_0 );
			float saferPower10 = abs( ( ( 1.0 - i.uv_texcoord.y ) * 1.0 ) );
			float temp_output_36_0 = saturate( ( pow( saferPower45 , 0.93 ) * saturate( pow( saferPower10 , 50.0 ) ) ) );
			float temp_output_29_0 = saturate( ( ( ( temp_output_40_0 * 0.05 ) + temp_output_36_0 ) * 5.0 ) );
			float2 panner54 = ( 0.2 * _Time.y * float2( 0,-1 ) + float2( 0,0 ));
			float2 uv_TexCoord53 = i.uv_texcoord * float2( 2,0.5 ) + panner54;
			float2 panner61 = ( 0.2 * _Time.y * float2( 0,-1 ) + float2( 0,0 ));
			float2 uv_TexCoord62 = i.uv_texcoord * float2( 2,2 ) + panner61;
			float2 panner66 = ( 0.1 * _Time.y * float2( 0,-1 ) + float2( 0,0 ));
			float2 uv_TexCoord65 = i.uv_texcoord * float2( 3,0.25 ) + panner66;
			float2 uv_TexCoord58 = i.uv_texcoord * float2( 2,1.18 );
			float temp_output_50_0 = ( saturate( ( saturate( ( tex2D( _Texture0, uv_TexCoord53 ).r * tex2D( _Noise, uv_TexCoord62 ).r * tex2D( _Texture0, uv_TexCoord65 ).r * 2.15 ) ) - pow( uv_TexCoord58.y , 2.63 ) ) ) + temp_output_36_0 );
			float3 lerpResult48 = lerp( ( _RingColor.rgb * temp_output_29_0 ) , ( _RingColor.rgb * temp_output_50_0 ) , _SunRibbons);
			o.Emission = HUEShift(lerpResult48, _HueShift);
			float lerpResult47 = lerp( temp_output_29_0 , saturate( temp_output_50_0 ) , _SunRibbons);
			o.Alpha = lerpResult47;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19501
Node;AmplifyShaderEditor.PannerNode;15;-1728,64;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;-0.05;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;4;-1664,224;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;-0.01;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;16;-1552,64;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;2,0.05;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;3;-1488,224;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,0.05;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;1;-1712,-352;Inherit;True;Property;_Noise;Noise;0;2;[NoScaleOffset];[SingleLineTexture];Create;True;0;0;0;False;0;False;None;1d0f5c11648c9174581e3608216277ef;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode;19;-1712,-144;Inherit;True;Property;_Noise2;Noise 2;1;2;[NoScaleOffset];[SingleLineTexture];Create;True;0;0;0;False;0;False;None;8eedb0cb7d087c34bab69a73edc9e164;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;8;-816,400;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-1200,80;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;14;-1216,-144;Inherit;True;Property;_TextureSample1;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.OneMinusNode;13;-416,656;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-464,784;Inherit;False;Constant;_Float1;Float 1;4;0;Create;True;0;0;0;False;0;False;1;0;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-816,32;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;39;-688,256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-176,576;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;48,208;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;61;240,-256;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-1;False;1;FLOAT;0.2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;54;224,-432;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-1;False;1;FLOAT;0.2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;66;224,-832;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-1;False;1;FLOAT;0.1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PowerNode;10;48,560;Inherit;True;True;2;0;FLOAT;0;False;1;FLOAT;50;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;40;304,224;Inherit;True;True;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;62;432,-272;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;2,2;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;65;432,-832;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;3,0.25;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;53;432,-432;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;2,0.5;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;51;416,-624;Inherit;True;Property;_Texture0;Noise 3;2;2;[NoScaleOffset];[SingleLineTexture];Create;False;0;0;0;False;0;False;None;0f0786fdccf437f4e98946940385d40f;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SaturateNode;26;272,560;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;45;464,560;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;0.93;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;60;736,-416;Inherit;True;Property;_TextureSample3;Texture Sample 2;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;64;720,-848;Inherit;True;Property;_TextureSample4;Texture Sample 2;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;67;928.9769,-207.9702;Inherit;False;Constant;_Float2;Float 2;5;0;Create;True;0;0;0;False;0;False;2.15;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;52;720,-624;Inherit;True;Property;_TextureSample2;Texture Sample 2;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;43;384,448;Inherit;False;Constant;_Float0;Float 0;3;0;Create;True;0;0;0;False;0;False;0.05;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;656,544;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;63;1136.742,-477.8386;Inherit;True;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;58;1152,-656;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;2,1.18;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;720,272;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;36;864,544;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;68;1348.063,-406.5594;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;69;1397.463,-813.4594;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;2.63;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;41;1072,432;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;57;1488,-640;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;1312,432;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;59;1741.17,-487.2563;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;29;1632,352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;50;1234.31,-167.8198;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;5;784,-96;Inherit;False;Property;_RingColor;RingColor;3;1;[HDR];Create;False;0;0;0;False;0;False;1,1,1,0;588.6386,674.4012,1235.558,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;1440,-320;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;1648,16;Inherit;True;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;56;1440,-144;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;1616,-176;Inherit;False;Property;_SunRibbons;Sun Ribbon Transition %;4;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;47;1952,96;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;48;2048,-64;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2240,-80;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;HighlightRing;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;16;1;15;0
WireConnection;3;1;4;0
WireConnection;2;0;19;0
WireConnection;2;1;3;0
WireConnection;14;0;1;0
WireConnection;14;1;16;0
WireConnection;13;0;8;2
WireConnection;17;0;14;1
WireConnection;17;1;2;1
WireConnection;39;0;8;2
WireConnection;20;0;13;0
WireConnection;20;1;21;0
WireConnection;38;0;17;0
WireConnection;38;1;39;0
WireConnection;10;0;20;0
WireConnection;40;0;38;0
WireConnection;62;1;61;0
WireConnection;65;1;66;0
WireConnection;53;1;54;0
WireConnection;26;0;10;0
WireConnection;45;0;40;0
WireConnection;60;0;1;0
WireConnection;60;1;62;0
WireConnection;64;0;51;0
WireConnection;64;1;65;0
WireConnection;52;0;51;0
WireConnection;52;1;53;0
WireConnection;44;0;45;0
WireConnection;44;1;26;0
WireConnection;63;0;52;1
WireConnection;63;1;60;1
WireConnection;63;2;64;1
WireConnection;63;3;67;0
WireConnection;42;0;40;0
WireConnection;42;1;43;0
WireConnection;36;0;44;0
WireConnection;68;0;63;0
WireConnection;69;0;58;2
WireConnection;41;0;42;0
WireConnection;41;1;36;0
WireConnection;57;0;68;0
WireConnection;57;1;69;0
WireConnection;46;0;41;0
WireConnection;59;0;57;0
WireConnection;29;0;46;0
WireConnection;50;0;59;0
WireConnection;50;1;36;0
WireConnection;55;0;5;5
WireConnection;55;1;50;0
WireConnection;11;0;5;5
WireConnection;11;1;29;0
WireConnection;56;0;50;0
WireConnection;47;0;29;0
WireConnection;47;1;56;0
WireConnection;47;2;49;0
WireConnection;48;0;11;0
WireConnection;48;1;55;0
WireConnection;48;2;49;0
WireConnection;0;2;48;0
WireConnection;0;9;47;0
ASEEND*/
//CHKSM=785590B9DEAD4965E07672662F8D87F15FC03B57