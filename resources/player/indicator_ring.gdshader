shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, blend_add, fog_disabled;

uniform vec4 ring_color : source_color = vec4(0.1, 0.4, 1.0, 0.95);
uniform float inner_radius : hint_range(0.0, 0.95) = 0.74;
uniform float outer_softness : hint_range(0.01, 0.5) = 0.1;
uniform float inner_softness : hint_range(0.01, 0.5) = 0.07;
uniform float emission_boost : hint_range(0.0, 8.0) = 2.5;

void fragment() {
	vec2 centered = UV * 2.0 - 1.0;
	float dist = length(centered);
	float outer = 1.0 - smoothstep(1.0 - outer_softness, 1.0, dist);
	float inner = smoothstep(inner_radius - inner_softness, inner_radius, dist);
	float ring = outer * inner;
	if (ring <= 0.001) {
		discard;
	}

	ALBEDO = ring_color.rgb;
	EMISSION = ring_color.rgb * emission_boost;
	ALPHA = ring_color.a * ring;
}
