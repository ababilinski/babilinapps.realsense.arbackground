Shader "Custom/URP/RealSensePointCloudShadowReceiver"
{  
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0.35,0.4,0.45,1.0)
    }
    SubShader
    {
       Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent-1"
        }
    

        Pass
        { 
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend DstColor Zero, Zero One

            Cull Back
            ZTest LEqual
            ZWrite Off
            HLSLPROGRAM
      
            #pragma vertex vert
		//#pragma geometry geom
            #pragma fragment frag

		
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
 
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
          

            CBUFFER_START(UnityPerMaterial)
            float4 _ShadowColor;
            CBUFFER_END
            struct GeomData
{
    float4 positionCS         : SV_POSITION;
    float3 positionWS         : TEXCOORD0;  
    float3 normalWS         : TEXCOORD1; 
    float4 tangentWS         : TEXCOORD2; 
    float3 viewDirectionWS     : TEXCOORD3; 
    float2 lightmapUV         : TEXCOORD4; 
    float3 sh                 : TEXCOORD5; 
    float4 fogFactorAndVertexLight : TEXCOORD6; 
    float4 shadowCoord         : TEXCOORD7;
};
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float3 positionWS               : TEXCOORD0;
                float fogCoord                  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            struct v2f
            {
                float3 wPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Varyings vert (Attributes input)
            {
				 Varyings output = (Varyings)0;
				
              
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
               
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
               // output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
          
 
                return output;
            }

			[maxvertexcount(3)]
			void geom(triangle GeomData input[3], inout TriangleStream<GeomData> triStream)
				{
				Varyings o;
				half3 w0 = input[0].positionWS;
				half3 w1 = input[1].positionWS;
				half3 w2 = input[2].positionWS;

				half l = dot(w0 - w1, w0 - w1) + dot(w1 - w2, w1 - w2) + dot(w2 - w0, w2 - w0);
				if (l < .03) {
					triStream.Append(input[0]);
					triStream.Append(input[1]);
					triStream.Append(input[2]);
                
				}
               
			}

           
            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
 
                half4 color = _ShadowColor;
 
            #ifdef _MAIN_LIGHT_SHADOWS
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = input.positionWS;
 
                float4 shadowCoord = GetShadowCoord(vertexInput);
                half shadowAttenutation = MainLightRealtimeShadow(shadowCoord);
                color = lerp(half4(1,1,1,1), color, (1.0 - shadowAttenutation) * _ShadowColor.a);
            #endif
                color.rgb = MixFogColor(color.rgb, half3(1,1,1), input.fogCoord);
                return color;
            }
          ENDHLSL
        }
    }
}
