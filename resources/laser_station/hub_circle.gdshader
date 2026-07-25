shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, blend_mix, fog_disabled;

uniform vec4 circle_color : source_color = vec4(1.0, 1.0, 1.0, 0.1);
uniform float soft_edge : hint_range(0.05, 1.0) = 0.55;
uniform float alpha_mul : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	vec2 centered = UV * 2.0 - 1.0;
	float dist = length(centered);
	if (dist > 1.0) {
		discard;
	}

	float edge_start = max(0.0, 1.0 - soft_edge);
	float falloff = 1.0 - smoothstep(edge_start, 1.0, dist);
	falloff *= falloff;

	ALBEDO = circle_color.rgb;
	ALPHA = circle_color.a * alpha_mul * falloff;
}
