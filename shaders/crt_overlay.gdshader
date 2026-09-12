// Screen-space CRT pass: barrel curve, scanlines, chromatic split, vignette.
// Sits on a full-rect ColorRect above everything; no SubViewport needed, so
// source art keeps its native resolution.
shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear;

uniform float curve : hint_range(0.0, 0.3) = 0.045;
uniform float scanline_strength : hint_range(0.0, 1.0) = 0.16;
uniform float scanline_density : hint_range(100.0, 1400.0) = 640.0;
uniform float aberration : hint_range(0.0, 6.0) = 1.1;
uniform float vignette_strength : hint_range(0.0, 1.5) = 0.42;
uniform float brightness : hint_range(0.5, 2.0) = 1.08;
uniform vec4 bezel_colour : source_color = vec4(0.0, 0.0, 0.0, 1.0);

vec2 curve_uv(vec2 uv, float amount) {
	uv = uv * 2.0 - 1.0;
	vec2 offset = abs(uv.yx) / vec2(6.0, 4.0);
	uv += uv * offset * offset * amount * 8.0;
	return uv * 0.5 + 0.5;
}

void fragment() {
	vec2 uv = curve_uv(SCREEN_UV, curve);

	if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
		COLOR = bezel_colour;
	} else {
		vec2 split = vec2(aberration, 0.0) * SCREEN_PIXEL_SIZE;
		vec3 col = vec3(
			texture(screen_tex, uv + split).r,
			texture(screen_tex, uv).g,
			texture(screen_tex, uv - split).b);

		float scan = sin(uv.y * scanline_density * PI) * 0.5 + 0.5;
		col *= 1.0 - scanline_strength * scan;

		vec2 v = uv * (1.0 - uv.yx);
		col *= clamp(pow(v.x * v.y * 18.0, vignette_strength), 0.0, 1.0);

		COLOR = vec4(col * brightness, 1.0);
	}
}
