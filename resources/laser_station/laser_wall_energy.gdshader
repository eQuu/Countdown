shader_type spatial;
render_mode unshaded, cull_disabled, blend_add, depth_draw_opaque, fog_disabled;

uniform vec4 ColorParameter : source_color = vec4(0.1, 0.4, 1.0, 1.0);
uniform float intesityColor : hint_range(0.0, 40.0) = 12.0;
uniform float speed = -0.55;
uniform vec2 scale = vec2(2.0, 1.4);
uniform float fill_alpha : hint_range(0.0, 1.0) = 0.55;
uniform float lit_factor : hint_range(0.0, 1.0) = 1.0;
uniform float edge_width : hint_range(0.02, 0.45) = 0.14;
uniform float edge_boost : hint_range(0.0, 8.0) = 2.2;
uniform float corner_boost : hint_range(0.0, 12.0) = 3.2;
uniform float tip_boost : hint_range(0.0, 12.0) = 3.8;
uniform float wobble : hint_range(0.0, 1.0) = 0.2;
uniform float speedWobble : hint_range(0.0, 5.0) = 1.0;
uniform sampler2D textureObj : source_color, repeat_enable, filter_linear_mipmap;

instance uniform vec3 mesh_size = vec3(1.0, 1.0, 1.0);

varying vec3 model_vertex;

void vertex() {
	model_vertex = VERTEX;
}

void fragment() {
	vec2 uv = UV * scale;
	float wobbleOffset = sin(UV.y * 12.0 + TIME * speedWobble) * wobble * 0.08;
	uv.x += wobbleOffset;
	uv.y += TIME * speed;

	float energy = texture(textureObj, uv).r;
	energy = pow(clamp(energy, 0.0, 1.0), 1.15);

	float distEdgeX = min(UV.x, 1.0 - UV.x);
	float distEdgeY = min(UV.y, 1.0 - UV.y);
	float rimX = 1.0 - smoothstep(0.0, edge_width, distEdgeX);
	float rimY = 1.0 - smoothstep(0.0, edge_width, distEdgeY);
	float rim = max(rimX, rimY);
	float corner = rimX * rimY;

	vec3 half_size = max(mesh_size * 0.5, vec3(0.001));
	vec3 end_dist = half_size - abs(model_vertex);
	float tip_band = max(edge_width * half_size.x, 0.04);
	float height_band = max(edge_width * half_size.y, 0.03);
	float length_ends = 1.0 - smoothstep(0.0, tip_band, end_dist.x);
	float height_ends = 1.0 - smoothstep(0.0, height_band, end_dist.y);
	float tip_neon = length_ends * tip_boost;
	float tip_corners = length_ends * height_ends * corner_boost;

	float neon = rim * edge_boost + corner * corner_boost + tip_neon + tip_corners;
	float core = energy * (0.5 + (1.0 - length_ends) * 0.5);
	float mask = clamp(core + neon * (0.4 + energy * 0.6), 0.0, 5.0);

	float edge_mix = clamp(rim * 0.35 + length_ends * 0.55 + corner * 0.2, 0.0, 1.0);
	vec3 tint = ColorParameter.rgb;
	vec3 edgeTint = mix(tint, min(tint * 1.85 + vec3(0.2), vec3(1.0)), 0.65);
	vec3 neonTint = mix(tint, edgeTint, edge_mix);
	vec3 glow = neonTint * mask * intesityColor;

	ALBEDO = neonTint;
	EMISSION = glow * lit_factor;
	ALPHA = clamp(mask * fill_alpha, 0.0, 1.0) * lit_factor;
}
