
namespace GrayWolf.Gaze.Editor
{
    internal static class DrawerExtensions
    {
        internal static DrawerBase Root(this DrawerBase drawer)
        {
            if (drawer == null) 
                return null;
            
            if (drawer.Parent == null)
                return drawer;

            do
            {
                drawer = drawer.Parent;
            }
            while (drawer.Parent != null);

            return drawer;
        }
    }
}