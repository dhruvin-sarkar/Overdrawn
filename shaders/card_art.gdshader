// Draws pixel card art. Sharp-bilinear filtering keeps texels crisp at any
// scale while softening their edges by one screen pixel when smoothing is on,
// and exactly matches nearest filtering when it is off. High contrast swaps
// ink colours per texel before filtering, so recoloured edges stay clean.
// Needs the node's texture filter set to Linear.
shader_type canvas_item;

global uniform float pixel_smoothing;

// The card's rectangle within the texture, in UVs (differs for atlas regions).
uniform vec4 atlas_rect = vec4(0.0, 0.0, 1.0, 1.0);
uniform int ink_count = 0;
uniform vec3 ink_0;
uniform vec3 contrast_0;
uniform vec3 ink_1;
uniform vec3 contrast_1;
// Recoloured area as a fraction of the card, from the top-left and mirrored
// from the bottom-right. (1, 1) is the whole card.
uniform vec2 region = vec2(1.0);
uniform vec2 edge_inset = vec2(0.0);

const float INK_TOLERANCE = 0.02;

varying vec4 modulate;

void vertex() {
	modulate = COLOR;
}

vec4 swap_ink(vec4 texel, vec2 card_pos) {
	bool inside = all(greaterThan(card_pos, edge_inset)) && all(lessThan(card_pos, 1.0 - edge_inset));
	bool in_region = all(lessThan(card_pos, region)) || all(greaterThan(card_pos, 1.0 - region));
	if (ink_count == 0 || !inside || !in_region) {
		return texel;
	}
	if (distance(texel.rgb, ink_0) < INK_TOLERANCE) {
		return vec4(contrast_0, texel.a);
	}
	if (ink_count > 1 && distance(texel.rgb, ink_1) < INK_TOLERANCE) {
		return vec4(contrast_1, texel.a);
	}
	return texel;
}

vec4 fetch_premultiplied(sampler2D tex, ivec2 texel, ivec2 lo, ivec2 hi, vec2 size) {
	ivec2 t = clamp(texel, lo, hi);
	vec2 card_pos = ((vec2(t) + 0.5) / size - atlas_rect.xy) / atlas_rect.zw;
	vec4 c = swap_ink(texelFetch(tex, t, 0), card_pos);
	return vec4(c.rgb * c.a, c.a);
}

void fragment() {
	vec2 size = vec2(textureSize(TEXTURE, 0));
	ivec2 lo = ivec2(round(atlas_rect.xy * size));
	ivec2 hi = ivec2(round((atlas_rect.xy + atlas_rect.zw) * size)) - 1;

	vec2 px = UV * size;
	vec2 box = clamp(fwidth(px) * pixel_smoothing, 1e-5, 1.0);
	vec2 tx = px - 0.5 * box;
	vec2 f = smoothstep(vec2(1.0) - box, vec2(1.0), fract(tx));
	ivec2 base = ivec2(floor(tx));

	vec4 top = mix(
		fetch_premultiplied(TEXTURE, base, lo, hi, size),
		fetch_premultiplied(TEXTURE, base + ivec2(1, 0), lo, hi, size), f.x);
	vec4 bottom = mix(
		fetch_premultiplied(TEXTURE, base + ivec2(0, 1), lo, hi, size),
		fetch_premultiplied(TEXTURE, base + ivec2(1, 1), lo, hi, size), f.x);
	vec4 c = mix(top, bottom, f.y);

	COLOR = vec4(c.a > 0.0 ? c.rgb / c.a : vec3(0.0), c.a) * modulate;
}
