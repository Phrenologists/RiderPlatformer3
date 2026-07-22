// IMapManager.cs
namespace GMTK.PlatformerToolkit {

    public interface IMapManager {
        void OnPlayerArrivedAtNode(MapNode node);
        void OnPlayerLeftNode(MapNode node);
    }
}
