// Screen-space CRT pass for a full-rect ColorRect on a top CanvasLayer:
// tube curvature with dark rounded corners, luminance-preserving scanlines and
// phosphor grille, chromatic split, bloom, signal noise and vignette. The CRT
// setting drives crt_strength (0 leaves the screen untouched) and crt_bloom.
// Curvature follows Balatro's formula; scanlines lighten on bright pixels the
// way a real beam widens.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear_mipmap;

global uniform float crt_strength;
global uniform float crt_bloom;

group_uniforms tube;
uniform vec2 curvature = vec2(0.03, 0.045);
uniform float corner_radius : hint_range(0.0, 0.5) = 0.08;
uniform float edge_feather : hint_range(0.001, 0.05) = 0.006;
uniform float vignette : hint_range(0.0, 1.0) = 0.3;

group_uniforms scan;
uniform float scanline_count = 270.0;
uniform float scanline_depth : hint_range(0.0, 1.0) = 0.5;
uniform float grille_depth : hint_range(0.0, 1.0) = 0.15;

group_uniforms signal;
uniform float aberration_px : hint_range(0.0, 4.0) = 1.0;
uniform float contrast : hint_range(1.0, 1.5) = 1.12;
uniform float noise_amount : hint_range(0.0, 0.2) = 0.035;
uniform float jitter_px : hint_range(0.0, 2.0) = 0.3;

group_uniforms bloom;
uniform float bloom_threshold : hint_range(0.0, 1.0) = 0.55;
uniform float bloom_intensity : hint_range(0.0, 2.0) = 0.45;
uniform float bloom_lod : hint_range(0.0, 6.0) = 3.0;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	float strength = crt_strength;
	float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;

	vec2 c = SCREEN_UV * 2.0 - 1.0;
	c += c * (c.yx * c.yx) * curvature * strength;

	// Rounded-rectangle tube mask, measured on the curved coordinates.
	vec2 half_size = vec2(aspect, 1.0);
	float r = corner_radius * strength;
	float d = length(max(abs(c) * half_size - (half_size - r), 0.0)) - r;
	float tube_mask = 1.0 - smoothstep(-edge_feather * 2.0 * strength, 0.0, d);

	vec2 uv = c * 0.5 + 0.5;

	float line = floor(uv.y * scanline_count);
	uv.x += (hash(vec2(line, floor(TIME * 24.0))) - 0.5) * jitter_px * strength * SCREEN_PIXEL_SIZE.x;

	vec2 split = vec2(aberration_px * strength * SCREEN_PIXEL_SIZE.x, 0.0);
	vec3 col = vec3(
		texture(screen_tex, uv + split).r,
		texture(screen_tex, uv).g,
		texture(screen_tex, uv - split).b);

	float luma = dot(col, vec3(0.299, 0.587, 0.114));
	float scan = 0.5 + 0.5 * cos(uv.y * scanline_count * TAU);
	float scan_amt = scanline_depth * strength * (1.0 - 0.6 * luma);
	col *= mix(1.0, scan, scan_amt) / (1.0 - 0.5 * scan_amt);

	float g = mod(floor(FRAGCOORD.x), 3.0);
	vec3 triad = vec3(float(g == 0.0), float(g == 1.0), float(g == 2.0));
	float g_amt = grille_depth * strength;
	col *= (1.0 - g_amt * (1.0 - triad)) / (1.0 - g_amt * 2.0 / 3.0);

	if (crt_bloom > 0.5) {
		vec3 glow = textureLod(screen_tex, uv, bloom_lod).rgb * 0.6
				+ textureLod(screen_tex, uv, bloom_lod + 1.5).rgb * 0.4;
		col += max(glow - bloom_threshold, 0.0) / (1.0 - bloom_threshold) * bloom_intensity * strength;
	}

	col = (col - 0.5) * mix(1.0, contrast, strength) + 0.5;
	col += (hash(floor(FRAGCOORD.xy * 0.5) + fract(TIME) * vec2(113.0, 71.0)) - 0.5) * noise_amount * strength;

	vec2 v = uv * (1.0 - uv);
	col *= mix(1.0, clamp(pow(v.x * v.y * 16.0, 0.35), 0.0, 1.0), vignette * strength);

	COLOR = vec4(col * tube_mask, 1.0);
}
