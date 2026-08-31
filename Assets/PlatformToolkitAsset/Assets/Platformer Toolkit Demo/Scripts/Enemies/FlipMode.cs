// FlipMode.cs
namespace GMTK.PlatformerToolkit {

    public enum FlipMode {
        None,       // Always shoots in the set direction
        FlipX,      // Flips between left and right based on player position
        FlipY,      // Flips between up and down based on player position
        FlipBoth    // Flips on both axes
    }
}
