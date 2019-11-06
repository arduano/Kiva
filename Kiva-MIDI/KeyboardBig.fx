
float Height;
float Left;
float Right;
float Aspect;
dword BarColor;

struct KEY
{
	dword colorl : COLORL;
	dword colorr : COLORR;
	float left : LEFT;
	float right : RIGHT;
	float distance : DISTANCE;
	dword meta : META;
};

struct QUAD
{
	float2 v1;
	float4 c1;
	float2 v2;
	float4 c2;
	float2 v3;
	float4 c3;
	float2 v4;
	float4 c4;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

KEY VS(KEY input)
{
	return input;
}

QUAD dim(QUAD q, float val) {
	q.c1.xyz += val;
	q.c2.xyz += val;
	q.c3.xyz += val;
	q.c4.xyz += val;
	return q;
}

void renderQuad(inout TriangleStream<PS_IN> OutputStream, QUAD quad) {
	PS_IN v = (PS_IN)0;

	v.pos = float4(quad.v1, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	v.col = quad.c1;
	OutputStream.Append(v);
	v.pos = float4(quad.v2, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	v.col = quad.c2;
	OutputStream.Append(v);
	v.pos = float4(quad.v3, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	v.col = quad.c3;
	OutputStream.Append(v);
	OutputStream.RestartStrip();

	v.pos = float4(quad.v1, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	v.col = quad.c1;
	OutputStream.Append(v);
	v.pos = float4(quad.v3, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	v.col = quad.c3;
	OutputStream.Append(v);
	v.pos = float4(quad.v4, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	v.col = quad.c4;
	OutputStream.Append(v);
	OutputStream.RestartStrip();
}

[maxvertexcount(1)]
void GS_Bar(point KEY input[1], inout TriangleStream<PS_IN> OutputStream)
{

}

[maxvertexcount(6 * 1)]
void GS_White(point KEY input[1], inout TriangleStream<PS_IN> OutputStream)
{
	KEY k = input[0];
	if (k.meta & 1) return;
	PS_IN v = (PS_IN)0;
	QUAD q = (QUAD)0;

	float height = Height;

	float left = (k.left - Left) / (Right - Left);
	float right = (k.right - Left) / (Right - Left);
	float top = height;
	float bottom = 0;

	float bez = 0.015;

	float4 colorlConv = float4((float)(k.colorl >> 24 & 0xff) / 255.0, (float)(k.colorl >> 16 & 0xff) / 255.0, (float)(k.colorl >> 8 & 0xff) / 255.0, (float)(k.colorl & 0xff) / 255.0);
	float4 colorrConv = float4((float)(k.colorr >> 24 & 0xff) / 255.0, (float)(k.colorr >> 16 & 0xff) / 255.0, (float)(k.colorr >> 8 & 0xff) / 255.0, (float)(k.colorr & 0xff) / 255.0);
	float4 colorl = float4(colorlConv.xyz * colorlConv.w + (1 - colorlConv.w), 1);
	float4 colorr = float4(colorrConv.xyz * colorrConv.w + (1 - colorrConv.w), 1);
	float4 coll = colorl;
	float4 colr = colorr;


	float4 colorL = float4(1, 0, 0, 1);
	float4 colorR = float4(1, 0, 0, 1);

	//Center
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(left, top);
	q.v2 = float2(right, top);
	q.v3 = float2(right, bottom);
	q.v4 = float2(left, bottom);
	renderQuad(OutputStream, q);
}

[maxvertexcount(6 * 1)]
void GS_Black(point KEY input[1], inout TriangleStream<PS_IN> OutputStream)
{
	KEY k = input[0];
	if (!(k.meta & 1)) return;
	PS_IN v = (PS_IN)0;
	QUAD q = (QUAD)0;

	float height = Height;

	float bez = 0.02;

	float left = (k.left - Left) / (Right - Left);
	float right = (k.right - Left) / (Right - Left);
	float top = height;
	float bottom = height * 0.35 + bez * height;

	float4 colorlConv = float4((float)(k.colorl >> 24 & 0xff) / 255.0, (float)(k.colorl >> 16 & 0xff) / 255.0, (float)(k.colorl >> 8 & 0xff) / 255.0, (float)(k.colorl & 0xff) / 255.0);
	float4 colorrConv = float4((float)(k.colorr >> 24 & 0xff) / 255.0, (float)(k.colorr >> 16 & 0xff) / 255.0, (float)(k.colorr >> 8 & 0xff) / 255.0, (float)(k.colorr & 0xff) / 255.0);
	float4 colorl = float4(colorlConv.xyz * colorlConv.w, 1);
	float4 colorr = float4(colorrConv.xyz * colorrConv.w, 1);
	float4 coll = colorl;
	float4 colr = colorr;

	//Center
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(left, top);
	q.v2 = float2(right, top);
	q.v3 = float2(right, bottom);
	q.v4 = float2(left, bottom);
	renderQuad(OutputStream, q);
}

float4 PS(PS_IN input) : SV_Target
{
	return input.col;
}