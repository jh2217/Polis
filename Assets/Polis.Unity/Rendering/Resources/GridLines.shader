// Line shader for the terrain grid. Fades the lines out as the camera zooms out, measured in screen pixels
// per tile, so the fade is the same in the Game view and the Scene view (each renders with its own camera).
// Orthographic cameras only: unity_OrthoParams.y is half the visible height in world units, and tiles are 1 unit wide.
// Lives in a Resources folder so Shader.Find("Polis/GridLines") still works in builds.
Shader "Polis/GridLines"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.1, 0.1, 1)
        _FadeOutPx ("Invisible at or below (pixels per tile)", Float) = 8
        _FadeInPx ("Fully visible at or above (pixels per tile)", Float) = 32
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "GridLines"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _FadeOutPx;
                float _FadeInPx;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fade : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                float pixelsPerTile = _ScreenParams.y / (2.0 * unity_OrthoParams.y);
                output.fade = saturate((pixelsPerTile - _FadeOutPx) / max(_FadeInPx - _FadeOutPx, 0.0001));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(_Color.rgb, _Color.a * input.fade);
            }
            ENDHLSL
        }
    }
}
