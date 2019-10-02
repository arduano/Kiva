
float Height;
float Left;
float Right;

struct KEY
{
	float left : LEFT;
	float right : RIGHT;
	float distance : DISTANCE;
	float4 colorl: COLORL;
	float4 colore: COLORR;
	short meta : META;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

KEY VS_Note(KEY input)
{
	return input;
}

[maxvertexcount(12)]
void GS_White(point KEY input[1], inout TriangleStream<PS_IN> OutputStream)
{
	KEY k = input[0];
	PS_IN v = (PS_IN)0;
	v.pos = float4(0, 0, 0, 1);

	float left = k.left - Left;
	float right = (k.right - Left) / (Right - Left);

	float blend = k.color.w;
	float4 colorl = float4(k.colorl.xyz * blend + (1 - blend), 1);
	float4 colorr = float4(k.colorr.xyz * blend + (1 - blend), 1);
	float4 coll = colorl;
	float4 colr = colorr;

	float top = Height;
	float bottom = 0;

	left = left * 2 - 1;
	right = right * 2 - 1;
	top = top * 2 - 1;
	bottom = bottom * 2 - 1;

	v.pos.xy = float2(left, bottom);
	v.col = coll;
	OutputStream.Append(v);
	v.pos.xy = float2(left, top);
	OutputStream.Append(v);
	v.pos.xy = float2(right, top);
	v.col = colr;
	OutputStream.Append(v);
	OutputStream.RestartStrip();

	v.pos.xy = float2(right, top);
	v.col = cole;
	OutputStream.Append(v);
	v.pos.xy = float2(right, bottom);
	OutputStream.Append(v);
	v.pos.xy = float2(left, bottom);
	v.col = coll;
	OutputStream.Append(v);
	OutputStream.RestartStrip();
}

float4 PS(PS_IN input) : SV_Target
{
	return input.col;
}