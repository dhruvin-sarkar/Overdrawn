// Shared burn-in dissolve. progress 0 = fully dissolved, 1 = fully present.
// Driving progress upward makes the subject materialise through a burning edge.

vec4 apply_burn(vec4 src, float noise, float progress, float burn_size,
		vec4 ash_colour, vec4 burn_colour, vec4 hot_colour) {
	float edge = burn_size * step(0.001, progress) * step(progress, 0.999);
	float threshold = smoothstep(noise - edge, noise, progress);
	float border = smoothstep(noise, noise + edge, progress);

	vec4 result = src;
	result.a *= threshold;

	vec3 hot = mix(hot_colour.rgb, burn_colour.rgb, 1.0 - pow(1.0 - border, 5.0));
	vec3 ash = mix(ash_colour.rgb, hot, 1.0 - pow(1.0 - border, 1000.0));
	result.rgb = mix(ash, src.rgb, border);
	return result;
}
