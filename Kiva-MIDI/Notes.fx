
float NoteLeft;
float NoteRight;
float NoteBorder;
float ScreenAspect;
float KeyboardHeight;
int ScreenWidth;
int ScreenHeight;

struct NOTE {
	float start : START;
	float end : END;
    dword colorl : COLORL;
    dword colorr : COLORR;
};

struct VS_IN
{
	float4 pos : POSITION;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

NOTE VS_Note(NOTE input)
{
	input.start = input.start * (1 - KeyboardHeight) + KeyboardHeight;
	input.end = input.end * (1 - KeyboardHeight) + KeyboardHeight;
	return input;
}

[maxvertexcount(12)]
void GS_Note(point NOTE input[1], inout TriangleStream<PS_IN> OutputStream)
{
	NOTE n = input[0];
	PS_IN v = (PS_IN)0;

    float4 colorlConv = float4((float)(n.colorl >> 24 & 0xff) / 255.0, (float)(n.colorl >> 16 & 0xff) / 255.0, (float)(n.colorl >> 8 & 0xff) / 255.0, (float)(n.colorl & 0xff) / 255.0);
    float4 colorrConv = float4((float)(n.colorr >> 24 & 0xff) / 255.0, (float)(n.colorr >> 16 & 0xff) / 255.0, (float)(n.colorr >> 8 & 0xff) / 255.0, (float)(n.colorr & 0xff) / 255.0);

	colorlConv.w *= colorlConv.w;
	colorlConv.w *= colorlConv.w;

	float4 cl = colorlConv;
    float4 cr = colorrConv;

	float noteBorder = 0.00091;
	float noteBorderh = round(noteBorder * ScreenWidth) / ScreenWidth;
	float noteBorderv = round(noteBorder * ScreenHeight) / ScreenHeight / ScreenAspect;

	cl.xyz *= 0.2f;
	cr.xyz *= 0.2f;
	cl.xyz -= 0.05f;
	cr.xyz -= 0.05f;
	cl.xyz = clamp(cl.xyz, 0, 1);
	cr.xyz = clamp(cr.xyz, 0, 1);

	v.col = cl;
	v.pos = float4(NoteLeft, n.start, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	v.pos = float4(NoteLeft, n.end, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	v.col = cr;
	v.pos = float4(NoteRight, n.end, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	OutputStream.RestartStrip();

	v.col = cr;
	v.pos = float4(NoteRight, n.end, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	v.pos = float4(NoteRight, n.start, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	v.col = cl;
	v.pos = float4(NoteLeft, n.start, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	OutputStream.RestartStrip();

	cl = colorlConv;
	cr = colorrConv;
	cl.xyz += 0.1f;
	cr.xyz -= 0.3f;
	cl.xyz = clamp(cl.xyz, 0, 1);
	cr.xyz = clamp(cr.xyz, 0, 1);


	float borderTop = n.end - noteBorderv;
	float borderBottom = n.start + noteBorderv;
	if (borderTop < borderBottom) {
		return;
	}
	float borderLeft = NoteLeft + noteBorderh;
	float borderRight = NoteRight - noteBorderh;

	v.col = cl;
	v.pos = float4(borderLeft, borderBottom, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	v.pos = float4(borderLeft, borderTop, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	v.col = cr;
	v.pos = float4(borderRight, borderTop, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	OutputStream.RestartStrip();

	v.col = cr;
	v.pos = float4(borderRight, borderTop, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	v.pos = float4(borderRight, borderBottom, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	v.col = cl;
	v.pos = float4(borderLeft, borderBottom, 0, 1);
	v.pos.xy = v.pos.xy * 2 - 1;
	OutputStream.Append(v);
	OutputStream.RestartStrip();
}

float4 PS( PS_IN input ) : SV_Target
{
	return input.col;
}