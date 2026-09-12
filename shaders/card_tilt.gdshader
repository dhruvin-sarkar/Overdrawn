// Fakes a 3D camera perspective on a CanvasItem. Original by Hei (MIT).
// x_rot / y_rot are driven per-card from script; material is local to scene.
// The burn-in dissolve is folded in here because a node carries one material.
shader_type canvas_item;

#include "res://shaders/burn.gdshaderinc"

uniform float fov : hint_range(1.0, 179.0) = 60.0;
uniform bool cull_back = true;
uniform float inset : hint_range(0.0, 1.0) = 0.0;

uniform float y_rot : hint_range(-180.0, 180.0) = 0.0;
uniform float x_rot : hint_range(-180.0, 180.0) = 0.0;

uniform sampler2D noise_texture : source_color, repeat_enable;
uniform float progress : hint_range(0.0, 1.0) = 1.0;
uniform float burn_size : hint_range(0.0, 1.0) = 0.12;
uniform vec4 ash_colour : source_color = vec4(0.04, 0.05, 0.09, 1.0);
uniform vec4 burn_colour : source_color = vec4(0.98, 0.97, 0.95, 1.0);
uniform vec4 hot_colour : source_color = vec4(0.55, 0.75, 1.0, 1.0);

varying flat vec2 o;
varying vec3 p;

void vertex() {
	float sin_b = sin(y_rot / 180.0 * PI);
	float cos_b = cos(y_rot / 180.0 * PI);
	float sin_c = sin(x_rot / 180.0 * PI);
	float cos_c = cos(x_rot / 180.0 * PI);

	mat3 inv_rot_mat;
	inv_rot_mat[0] = vec3(cos_b, 0.0, -sin_b);
	inv_rot_mat[1] = vec3(sin_b * sin_c, cos_c, cos_b * sin_c);
	inv_rot_mat[2] = vec3(sin_b * cos_c, -sin_c, cos_b * cos_c);

	float t = tan(fov / 360.0 * PI);
	p = inv_rot_mat * vec3((UV - 0.5), 0.5 / t);
	float v = (0.5 / t) + 0.5;
	p.xy *= v * inv_rot_mat[2].z;
	o = v * inv_rot_mat[2].xy;

	VERTEX += (UV - 0.5) / TEXTURE_PIXEL_SIZE * t * (1.0 - inset);
}

void fragment() {
	if (cull_back && p.z <= 0.0) {
		discard;
	}

	vec2 uv = (p.xy / p.z).xy - o;
	vec2 card_uv = uv + 0.5;

	vec4 src = texture(TEXTURE, card_uv);
	src.a *= step(max(abs(uv.x), abs(uv.y)), 0.5);

	float n = texture(noise_texture, card_uv).r;
	COLOR = apply_burn(src, n, progress, burn_size, ash_colour, burn_colour, hot_colour);
}
