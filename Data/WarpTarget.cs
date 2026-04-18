using Microsoft.Xna.Framework;

namespace BuildingsExpanded.Data;

public class WarpTarget
{
	public string Id { get; set; } = "";
	public string? DisplayName { get; set; }
	public string? WarpLocation { get; set; }
	public string? RequiredObelisk { get; set; }
	public string? RequiredMod { get; set; }
	public string? Condition { get; set; }
	public Point WarpPosition { get; set; }
	public string? WarpHandler { get; set; }
	public string? Texture { get; set; }
	public Rectangle TextureSource { get; set; }
}
