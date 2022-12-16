uniform vec4 meshColor;
uniform float ambient;
uniform vec3 lightPos;
uniform float opacity;
varying vec3 vNormal;
varying vec3 vWsPos;
varying vec3 vTexCoord;
void main()
{
	vec3 lightVec = normalize(vWsPos - lightPos);
	float lit = abs(dot(lightVec, vNormal));
	gl_FragColor = vec4(meshColor.xyz * (lit * (1.0 - ambient) + ambient), 1.0) * opacity;
}