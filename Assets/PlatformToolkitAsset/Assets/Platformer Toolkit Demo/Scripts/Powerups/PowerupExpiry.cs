// PowerupExpiry.cs
namespace GMTK.PlatformerToolkit {

    public enum PowerupExpiry {
        Timer,      // expires after a set duration
        Ammo,       // expires after a set number of uses or distance
        None        // never expires on its own
    }
}
