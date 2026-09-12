// Burn-in materialise for the title art.
shader_type canvas_item;

#include "res://shaders/burn.gdshaderinc"

uniform sampler2D noise_texture : source_color, repeat_enable;
uniform float progress : hint_range(0.0, 1.0) = 1.0;
uniform float burn_size : hint_range(0.0, 1.0) = 0.12;
uniform vec4 ash_colour : source_color = vec4(0.04, 0.05, 0.09, 1.0);
uniform vec4 burn_colour : source_color = vec4(0.98, 0.97, 0.95, 1.0);
uniform vec4 hot_colour : source_color = vec4(0.55, 0.75, 1.0, 1.0);

void fragment() {
	vec4 src = texture(TEXTURE, UV);
	float n = texture(noise_texture, UV).r;
	COLOR = apply_burn(src, n, progress, burn_size, ash_colour, burn_colour, hot_colour);
}
