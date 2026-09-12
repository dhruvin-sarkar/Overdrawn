// Swirling paint background, adapted from Balatro's original LOVE2D shader.
// Driven by built-in TIME instead of script-fed uniforms.
shader_type canvas_item;

uniform vec4 colour_1 : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 colour_2 : source_color = vec4(0.0, 0.4, 0.9, 1.0);
uniform vec4 colour_3 : source_color = vec4(1.0, 0.0, 0.2, 1.0);
uniform float contrast : hint_range(0.5, 4.0) = 1.8;
uniform float spin_amount : hint_range(0.0, 1.5) = 1.05;
uniform float spin_speed : hint_range(0.0, 4.0) = 0.7;
uniform float paint_speed : hint_range(0.0, 4.0) = 1.0;
uniform float pixel_filter : hint_range(100.0, 2000.0) = 740.0;
uniform vec2 swirl_centre = vec2(0.0, 0.0);

const float SPIN_EASE = 0.5;

void fragment() {
	vec2 screen_size = 1.0 / SCREEN_PIXEL_SIZE;
	float pixel_size = length(screen_size) / pixel_filter;

	vec2 uv = (floor(FRAGCOORD.xy * (1.0 / pixel_size)) * pixel_size - 0.5 * screen_size)
		/ length(screen_size) - swirl_centre;
	float uv_len = length(uv);

	float speed = (TIME * spin_speed * SPIN_EASE * 0.2) + 302.2;
	float angle = atan(uv.y, uv.x) + speed
		- SPIN_EASE * 20.0 * (spin_amount * uv_len + (1.0 - spin_amount));
	vec2 mid = (screen_size / length(screen_size)) / 2.0;
	uv = vec2(uv_len * cos(angle) + mid.x, uv_len * sin(angle) + mid.y) - mid;

	uv *= 30.0;
	float spd = TIME * paint_speed * 2.0;
	vec2 uv2 = vec2(uv.x + uv.y);

	for (int i = 0; i < 5; i++) {
		uv2 += sin(max(uv.x, uv.y)) + uv;
		uv += 0.5 * vec2(
			cos(5.1123314 + 0.353 * uv2.y + spd * 0.131121),
			sin(uv2.x - 0.113 * spd));
		uv -= cos(uv.x + uv.y) - sin(uv.x * 0.711 - uv.y);
	}

	float contrast_mod = 0.25 * contrast + 0.5 * spin_amount + 1.2;
	float paint_res = min(2.0, max(0.0, length(uv) * 0.035 * contrast_mod));
	float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
	float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
	float c3p = 1.0 - min(1.0, c1p + c2p);

	COLOR = (0.3 / contrast) * colour_1 + (1.0 - 0.3 / contrast) * (
		colour_1 * c1p + colour_2 * c2p + vec4(c3p * colour_3.rgb, c3p * colour_1.a));
}
