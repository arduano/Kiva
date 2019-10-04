
float Height;
float Left;
float Right;
float Aspect;

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

[maxvertexcount(6)]
void GS_White(point KEY input[1], inout TriangleStream<PS_IN> OutputStream)
{
	KEY k = input[0];
	if (k.meta & 1) return;
	PS_IN v = (PS_IN)0;
	QUAD q = (QUAD)0;

	float dist = k.distance * Height * 0.08;

	float left = (k.left - Left) / (Right - Left);
	float right = (k.right - Left) / (Right - Left);

	float4 colorlConv = float4((float)(k.colorl >> 24 & 0xff) / 255.0, (float)(k.colorl >> 16 & 0xff) / 255.0, (float)(k.colorl >> 8 & 0xff) / 255.0, (float)(k.colorl & 0xff) / 255.0);
	float4 colorrConv = float4((float)(k.colorr >> 24 & 0xff) / 255.0, (float)(k.colorr >> 16 & 0xff) / 255.0, (float)(k.colorr >> 8 & 0xff) / 255.0, (float)(k.colorr & 0xff) / 255.0);
	float4 colorl = float4(colorlConv.xyz * colorlConv.w + (1 - colorlConv.w), 1);
	float4 colorr = float4(colorrConv.xyz * colorrConv.w + (1 - colorrConv.w), 1);
	float4 coll = colorl;
	float4 colr = colorr;

	float top = Height + Height * 0.08;
	float bottom = 0;

	q.c1 = coll;
	q.c2 = coll;
	q.c3 = colr;
	q.c4 = colr;
	q.c1.xyz *= 0.8;
	q.c4.xyz *= 0.8;
	q.v1 = float2(left, top - dist);
	q.v2 = float2(right, top - dist);
	q.v3 = float2(right, bottom - dist);
	q.v4 = float2(left, bottom - dist);
	renderQuad(OutputStream, q);
}

[maxvertexcount(18)]
void GS_Black(point KEY input[1], inout TriangleStream<PS_IN> OutputStream)
{
	KEY k = input[0];
	if (!(k.meta & 1)) return;
	PS_IN v = (PS_IN)0;
	QUAD q = (QUAD)0;

	float dist = k.distance * Height * 0.08;

	float left = (k.left - Left) / (Right - Left);
	float right = (k.right - Left) / (Right - Left);

	float4 colorlConv = float4((float)(k.colorl >> 24 & 0xff) / 255.0, (float)(k.colorl >> 16 & 0xff) / 255.0, (float)(k.colorl >> 8 & 0xff) / 255.0, (float)(k.colorl & 0xff) / 255.0);
	float4 colorrConv = float4((float)(k.colorr >> 24 & 0xff) / 255.0, (float)(k.colorr >> 16 & 0xff) / 255.0, (float)(k.colorr >> 8 & 0xff) / 255.0, (float)(k.colorr & 0xff) / 255.0);
	float4 colorl = float4(colorlConv.xyz * colorlConv.w, 1);
	float4 colorr = float4(colorrConv.xyz * colorrConv.w, 1);
	float4 coll = colorl;
	float4 colr = colorr;

	float top = Height + Height * 0.08;
	float bottom = Height * 0.5;

	q.c1 = coll;
	q.c2 = coll;
	q.c3 = colr;
	q.c4 = colr;
	q.c1.xyz = 1 - (1 - q.c1.xyz) * 0.8;
	q.c4.xyz = 1 - (1 - q.c4.xyz) * 0.8;
	q.v1 = float2(left, top - dist);
	q.v2 = float2(right, top - dist);
	q.v3 = float2(right, bottom - dist);
	q.v4 = float2(left, bottom - dist);
	renderQuad(OutputStream, q);

	q.c1 = coll;
	q.c2 = coll;
	q.c3 = coll;
	q.c4 = coll;
	q.c1.xyz = 1 - (1 - q.c1.xyz) * 0.8;
	q.c4.xyz = 1 - (1 - q.c4.xyz) * 0.8;
	q.c2.xyz = 1 - (1 - q.c2.xyz) * 0.8;
	q.v1 = float2(left + 0.04 * Height * Aspect, top + 0.07 * Height - dist);
	q.v2 = float2(right - 0.04 * Height * Aspect, top + 0.07 * Height - dist);
	q.v3 = float2(right, top - dist);
	q.v4 = float2(left, top - dist);
	renderQuad(OutputStream, q);

	q.c1 = colr;
	q.c2 = colr;
	q.c3 = colr;
	q.c4 = colr;
	q.c1.xyz = 1 - (1 - q.c1.xyz) * 0.8;
	q.v1 = float2(left, bottom - dist);
	q.v2 = float2(right, bottom - dist);
	q.v3 = float2(right - 0.04 * Height * Aspect, bottom - 0.07 * Height - dist);
	q.v4 = float2(left + 0.04 * Height * Aspect, bottom - 0.07 * Height - dist);
	renderQuad(OutputStream, q);
}

float4 PS(PS_IN input) : SV_Target
{
	return input.col;
}