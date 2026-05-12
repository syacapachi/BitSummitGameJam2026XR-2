// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
//Unityのシェーダーメニューに出す名前
Shader "Skybox/Horizontal Skybox Add Texture"
{
    //インスペクター上で設定できる項目
    Properties
    {
        //内部変数名(ここの下にある) ("表示名", 型) = 初期値
        _Color1 ("Top Color", Color) = (1, 1, 1, 0)
        _Color2 ("Horizon Color", Color) = (1, 1, 1, 0)
        _Color3 ("Bottom Color", Color) = (1, 1, 1, 0)
        _Exponent1 ("Exponent Factor for Top Half", Float) = 1.0
        _Exponent2 ("Exponent Factor for Bottom Half", Float) = 1.0
        _Intensity ("Intensity Amplifier", Float) = 1.0
        _MainTex ("Sky Texture", 2D) = "white" {}
        _Rotate("Texture Rotation", Range(0,1)) = 1
        [MaterialToggle] _TextureClamp("Is Clamp", Float) = 0
        _TextureMinY ("Texture Min Y", Range(-1,1)) = -0.2
        _TextureMaxY ("Texture Max Y", Range(-1,1)) = 1.0
        _TextureStrength ("Texture Strength", Range(0,1)) = 1
        // Added: 太陽の見た目を制御するためのプロパティ
        _SunColor ("Sun Color", Color) = (1.0, 0.92, 0.75, 1.0)
        _SunTime24h ("Sun Time (24h)", Range(0,24)) = 12
        _SunIntensity ("Sun Intensity", Range(0,8)) = 1.5
        _SunAngularRadius ("Sun Angular Radius", Range(0.001,0.2)) = 0.04
        _SunElevationAmplitude ("Sun Elevation Amplitude", Range(0,1)) = 0.85
    }

    CGINCLUDE//ここから GPUコード共通部 メンバー変数、メソッドの宣言

    //  UnityObjectToWorldNormal ヘルパー関数を含むファイルを使う
    #include "UnityCG.cginc"

    //後で使われる。頂点シェーダーを計算するときに引数として使う変数の構造体
    struct appdata
    {
        
        //型 変数名 : GPUに送るメタデータ
        //型とメタデータが違うのは、精度を設定できるようにするため
        //例 
        //half4 pos : POSITION; -> 16bitの位置情報
        //float4 pos : POSITION -> 32bitの位置情報
        //Meshの頂点座標(x,y,z,scale)
        float4 position : POSITION;
        //テクスチャの方向ベクトル(x,y,z)->UVベクトルで検索
        float3 texcoord : TEXCOORD0;
        //インスタンスIDを頂点情報として受け取る
        //1回の描画処理で同じメッシュを大量に描画できる。
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    //後で使われる。
    struct v2f
    {
        //画面座標
        float4 position : SV_POSITION;
        //テクスチャの方向ベクトル
        float3 texcoord : TEXCOORD0;
        //VR用,どっちの目に描画するか
        UNITY_VERTEX_OUTPUT_STEREO
    };

    //変数
    //half4 -> RGBA(各16bit)つまり、色を表せる。
    //half  ->float16
    //float ->float32 
    half4 _Color1;
    half4 _Color2;
    half4 _Color3;
    half _Intensity;
    half _Exponent1;
    half _Exponent2;
    half4 _MainTex_ST;//これだけUnityで設定できない。
    sampler2D _MainTex;
    half _TextureClamp;
    half _Rotate;
    half _TextureMinY;
    half _TextureMaxY;
    half _TextureStrength;
    // Added: 太陽描画用の内部変数
    half4 _SunColor;
    half _SunTime24h;
    half _SunIntensity;
    half _SunAngularRadius;
    half _SunElevationAmplitude;

    //共通関数
    //モデルの各頂点(vert)に対して実行される。
    //画面のピクセルを返す
    v2f vert (appdata v)
    {
        //宣言
        v2f o;
        // インスタンスIDを元に位置やスケールを反映
        UNITY_SETUP_INSTANCE_ID(v);
        //VR用、どちらかの目に描画するかを計算＆UNITY_VERTEX_OUTPUT_STEREOに反映
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        //UnityObjectの座標を画面の座標に変換
        o.position = UnityObjectToClipPos (v.position);
        //テクスチャのUV座標を渡す。
        o.texcoord = v.texcoord;
        return o;
    }
    //画面のピクセル単位(flag)で実行される。
    //ピクセルの色を決定する。
    half4 frag (v2f i) : COLOR
    {
        //方向ベクトルを正規化。(-1 <= x,y,z, <= 1)
        float3 dir = normalize(i.texcoord);

        // ===== グラデーション =====
        float p = dir.y;
        //空の上側である割合
        float p1 = 1.0f - pow (min (1.0f, 1.0f - p), _Exponent1);
        //空の下側である割合
        float p3 = 1.0f - pow (min (1.0f, 1.0f + p), _Exponent2);
        //空の中間である割合
        float p2 = 1.0f - p1 - p3;
        //色を混ぜ合わせる
        half4 gradient =
        (_Color1 * p1 +
         _Color2 * p2 +
         _Color3 * p3);

        // =========     X回転     ==============
        float rad = (2.0 * UNITY_PI) * _Rotate;

        float2 rotatedXZ;

        rotatedXZ.x =
            dir.x * cos(rad) -
            dir.z * sin(rad);

        rotatedXZ.y =
             dir.x * sin(rad) +
             dir.z * cos(rad);

         // Y範囲制限
         float rawY =
            (dir.y - _TextureMinY)
            / (_TextureMaxY - _TextureMinY);

        float mask =
            step(0.0, rawY) *
            step(rawY, 1.0);

         // ===== 球面UV(Textureのどこを描画するか) =====
        float2 uv;
        //座標を角度に変換
        // -π <= atan2(y,x) <= π
        uv.x = atan2(rotatedXZ.x, rotatedXZ.y) / (2.0 * UNITY_PI) + 0.5;
        uv.y = saturate(rawY);

        // ===== Texture =====
        half4 tex = tex2D(_MainTex, uv);

        // =====================================
        // Clamp切り替え
        // =====================================

        //GPUのIfは、処理を止めるため(基本的にコードを全て実行しかできない、偽の場合は他のGPUが終わるまで待機する)なるべく使わない。
        //処理を工夫してみよう。
        float finalMask =
            lerp(mask, 1.0, _TextureClamp);

        // Added: 24時間ベースの時刻から太陽方向を作り、空にディスクを重ねる
        float sunAngle = ((_SunTime24h - 6.0) / 24.0) * (2.0 * UNITY_PI);
        float sunHeight = sin(sunAngle) * _SunElevationAmplitude;
        float sunHorizontal = sqrt(saturate(1.0 - sunHeight * sunHeight));
        float3 sunDir = normalize(float3(cos(sunAngle) * sunHorizontal, sunHeight, sin(sunAngle) * sunHorizontal));

        float sunAlignment = dot(dir, sunDir);
        float sunDisc = smoothstep(cos(_SunAngularRadius * 1.35), cos(_SunAngularRadius), sunAlignment);
        float sunGlow = smoothstep(cos(_SunAngularRadius * 4.0), cos(_SunAngularRadius * 1.2), sunAlignment);
        float sunVisibility = saturate(sunDir.y * 4.0 + 0.2);
        half3 sunColor = _SunColor.rgb * ((sunDisc * _SunIntensity) + (sunGlow * _SunIntensity * 0.2)) * sunVisibility;

        // ===== 合成 =====
        half4 finalColor = gradient + tex * _TextureStrength * finalMask;
        // Added: 太陽色を最終色に加算して描画する
        finalColor.rgb += sunColor;
        
        return finalColor * _Intensity;
    }

    ENDCG //ここまで GPUコード共通部
    
    //GPUの処理部(性能ごとに分けられる)
    SubShader
    {
        //描画順制御
        //最初に描画される。
        Tags { "RenderType"="Background" "Queue"="Background" }
        // SubShader 全体に適用される ShaderLab コマンドをここに記述
        Pass //ここから 1回の描画処理で行われる処理
        {
            ZWrite Off//深度バッファ書き込みOFF。
            Cull Off//裏面描画ON。
            Fog { Mode Off }//Fog無効。

            CGPROGRAM //ここからGPU実行コード
            //計算精度を犠牲にして処理速度を最速にする設定。
            #pragma fragmentoption ARB_precision_hint_fastest
            // "vert" 関数を頂点シェーダーとして扱う
            #pragma vertex vert
            // "frag" 関数をピクセル (フラグメント) シェーダーとして扱う
            #pragma fragment frag
            ENDCG
        }
    }
}
