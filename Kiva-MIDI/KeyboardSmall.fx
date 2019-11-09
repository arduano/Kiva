
float Height;
float Left;
float Right;
float Aspect;
dword BarColor;
int ScreenWidth;
int ScreenHeight;

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

QUAD dim(QUAD q, float val){
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

[maxvertexcount(6 * 5)]
void GS_White(point KEY input[1], inout TriangleStream<PS_IN> OutputStream)
{
	KEY k = input[0];
	if (k.meta & 1) return;
	PS_IN v = (PS_IN)0;
	QUAD q = (QUAD)0;

	float dist = k.distance * Height * 0.08;

	float left = (k.left - Left) / (Right - Left);
	float right = (k.right - Left) / (Right - Left);
	float top = Height + Height * 0.08 - dist;
	float bottom = 0 - dist;

	float bez = 0.07;

	float ileft = left + bez * Height * Aspect;
	float iright = right - bez * Height * Aspect;
	float itop = top - bez * Height;
	float ibottom = bottom + bez * Height;

	float4 colorlConv = float4((float)(k.colorl >> 24 & 0xff) / 255.0, (float)(k.colorl >> 16 & 0xff) / 255.0, (float)(k.colorl >> 8 & 0xff) / 255.0, (float)(k.colorl & 0xff) / 255.0);
	float4 colorrConv = float4((float)(k.colorr >> 24 & 0xff) / 255.0, (float)(k.colorr >> 16 & 0xff) / 255.0, (float)(k.colorr >> 8 & 0xff) / 255.0, (float)(k.colorr & 0xff) / 255.0);
	float4 colorl = float4(colorlConv.xyz * colorlConv.w + (1 - colorlConv.w), 1);
	float4 colorr = float4(colorrConv.xyz * colorrConv.w + (1 - colorrConv.w), 1);
	float4 coll = colorl;
	float4 colr = colorr;

	//Center
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(ileft, itop);
	q.v2 = float2(iright, itop);
	q.v3 = float2(iright, ibottom);
	q.v4 = float2(ileft, ibottom);
	renderQuad(OutputStream, dim(q, -0.1));

	//Left
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(left, top);
	q.v2 = float2(ileft, itop);
	q.v3 = float2(ileft, ibottom);
	q.v4 = float2(left, bottom);
	renderQuad(OutputStream, dim(q, -0.0));

	//Top
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = coll;
	q.c4 = coll;
	q.v1 = float2(left, top);
	q.v2 = float2(right, top);
	q.v3 = float2(iright, itop);
	q.v4 = float2(ileft, itop);
	renderQuad(OutputStream, dim(q, -0.1));

	//Right
	q.c1 = coll;
	q.c2 = colr;
	q.c3 = colr;
	q.c4 = coll;
	q.v1 = float2(right, top);
	q.v2 = float2(right, bottom);
	q.v3 = float2(iright, ibottom);
	q.v4 = float2(iright, itop);
	renderQuad(OutputStream, dim(q, -0.3));

	//Bottom
	q.c1 = colr;
	q.c2 = colr;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(ileft, ibottom);
	q.v2 = float2(iright, ibottom);
	q.v3 = float2(right, bottom);
	q.v4 = float2(left, bottom);
	renderQuad(OutputStream, dim(q, -0.3));
}

[maxvertexcount(6 * 9)]
void GS_Black(point KEY input[1], inout TriangleStream<PS_IN> OutputStream)
{
	KEY k = input[0];
	if (!(k.meta & 1)) return;
	PS_IN v = (PS_IN)0;
	QUAD q = (QUAD)0;

	float dist = k.distance * Height * 0.05;

	float bez = 0.05;

	float left = (k.left - Left) / (Right - Left);
	float right = (k.right - Left) / (Right - Left);
	float top = Height + Height * 0.08 - dist * 0 + bez * Height;
	float bottom = Height * 0.4 - dist + bez * Height;

	float ileft = left + bez * Height * Aspect;
	float iright = right - bez * Height * Aspect;
	float itop = top - bez * Height;
	float ibottom = bottom + bez * Height;

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
	q.v1 = float2(ileft, itop);
	q.v2 = float2(iright, itop);
	q.v3 = float2(iright, ibottom);
	q.v4 = float2(ileft, ibottom);
	renderQuad(OutputStream, dim(q, 0.1));

	//Left
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(left, itop);
	q.v2 = float2(ileft, itop);
	q.v3 = float2(ileft, ibottom);
	q.v4 = float2(left, ibottom);
	renderQuad(OutputStream, dim(q, 0.2));

	//Top
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = coll;
	q.c4 = coll;
	q.v1 = float2(ileft, top);
	q.v2 = float2(iright, top);
	q.v3 = float2(iright, itop);
	q.v4 = float2(ileft, itop);
	renderQuad(OutputStream, dim(q, 0.2));

	//Right
	q.c1 = coll;
	q.c2 = colr;
	q.c3 = colr;
	q.c4 = coll;
	q.v1 = float2(right, itop);
	q.v2 = float2(right, ibottom);
	q.v3 = float2(iright, ibottom);
	q.v4 = float2(iright, itop);
	renderQuad(OutputStream, dim(q, 0.05));

	//Bottom
	q.c1 = colr;
	q.c2 = colr;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(ileft, ibottom);
	q.v2 = float2(iright, ibottom);
	q.v3 = float2(iright, bottom);
	q.v4 = float2(ileft, bottom);
	renderQuad(OutputStream, dim(q, 0.05));

	//Top Left
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = coll;
	q.c4 = coll;
	q.v1 = float2(ileft, itop);
	q.v2 = float2(ileft, itop);
	q.v3 = float2(left, itop);
	q.v4 = float2(ileft, top);
	renderQuad(OutputStream, dim(q, 0.3));

	//Top Right
	q.c1 = coll;
	q.c2 = coll;
	q.c3 = coll;
	q.c4 = coll;
	q.v1 = float2(iright, itop);
	q.v2 = float2(iright, itop);
	q.v3 = float2(iright, top);
	q.v4 = float2(right, itop);
	renderQuad(OutputStream, dim(q, 0.1));

	//Bottom Left
	q.c1 = colr;
	q.c2 = colr;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(ileft, ibottom);
	q.v2 = float2(ileft, ibottom);
	q.v3 = float2(right, ibottom);
	q.v4 = float2(iright, bottom);
	renderQuad(OutputStream, dim(q, 0.0));

	//Bottom Right
	q.c1 = colr;
	q.c2 = colr;
	q.c3 = colr;
	q.c4 = colr;
	q.v1 = float2(ileft, ibottom);
	q.v2 = float2(ileft, ibottom);
	q.v3 = float2(ileft, bottom);
	q.v4 = float2(left, ibottom);
	renderQuad(OutputStream, dim(q, 0.1));
}

float4 PS(PS_IN input) : SV_Target
{
	return input.col;
}