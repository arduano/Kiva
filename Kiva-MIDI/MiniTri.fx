// -----------------------------------------------------------------------------
// Original code from SlimDX project.
// Greetings to SlimDX Group. Original code published with the following license:
float Spin : VertexSpin;

struct NOTE {
	float start : START;
	float end : END;
	float4 colorl : COLORL;
	float4 colorr : COLORR;
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

VS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = input.pos;
	output.col = input.col;

	output.pos.x = cos(Spin) * input.pos.x - sin(Spin) * input.pos.y;
	output.pos.y = sin(Spin) * input.pos.x + cos(Spin) * input.pos.y;

	return output;
}

NOTE VS_Note(NOTE input)
{
	return input;
}

[maxvertexcount(3)]
void GS(triangle VS_IN input[3], inout TriangleStream<PS_IN> OutputStream)
{
	for (int i = 0; i < 3; i++)
	{
		OutputStream.Append(input[i]);
	}
}

[maxvertexcount(6)]
void GS_Note(point NOTE input[1], inout TriangleStream<PS_IN> OutputStream)
{
	NOTE n = input[0];
	PS_IN v = (PS_IN)0;

	float4 cl = n.colorl;
	float4 cr = n.colorr;

	v.col = cl;
	v.pos = float4(-0.5f, n.start, 0, 1);
	OutputStream.Append(v);
	v.pos = float4(-0.5f, n.end, 0, 1);
	OutputStream.Append(v);
	v.col = cr;
	v.pos = float4(0.5f, n.end, 0, 1);
	OutputStream.Append(v);

	OutputStream.RestartStrip();

	v.col = cr;
	v.pos = float4(0.5f, n.end, 0, 1);
	OutputStream.Append(v);
	v.pos = float4(0.5f, n.start, 0, 1);
	OutputStream.Append(v);
	v.col = cl;
	v.pos = float4(-0.5f, n.start, 0, 1);
	OutputStream.Append(v);
}

float4 PS( PS_IN input ) : SV_Target
{
	return input.col;
}