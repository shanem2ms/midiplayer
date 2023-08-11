uniform mat4 uMVP;
uniform mat4 uWorldInvTranspose;
uniform mat4 uWorld;
attribute vec3 aPosition;
attribute vec3 aTexCoord0;
attribute vec3 aNormal;
varying vec3 vTexCoord;
varying vec3 vWsPos;
varying vec3 vNormal;
void main() {
    gl_Position = uMVP * vec4(aPosition, 1.0);
    vTexCoord = aTexCoord0;
    vec4 norm = uWorldInvTranspose * vec4(aNormal, 0);
    vWsPos = (uWorld * vec4(aPosition, 1.0)).xyz;
    vNormal = normalize(norm.xyz);
}
