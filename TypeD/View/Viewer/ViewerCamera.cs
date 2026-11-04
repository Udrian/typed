using System;

namespace TypeD.View.Viewer
{
    /// <summary>
    /// The editor camera of a viewer. It maps viewer (screen) coordinates to world coordinates using a position,
    /// the world point shown at the top-left corner of the viewer, and a zoom, so screen = (world - position) * zoom.
    /// </summary>
    public class ViewerCamera
    {
        /// <summary>The smallest allowed zoom.</summary>
        public const double MinZoom = 0.05;
        /// <summary>The largest allowed zoom.</summary>
        public const double MaxZoom = 32;
        /// <summary>The default keyboard navigation speed, in viewer units per second.</summary>
        public const double DefaultSpeed = 600;
        /// <summary>The smallest allowed keyboard navigation speed.</summary>
        public const double MinSpeed = 1;
        /// <summary>The largest allowed keyboard navigation speed.</summary>
        public const double MaxSpeed = 100000;

        private double _speed = DefaultSpeed;

        /// <summary>Gets the world X coordinate shown at the left edge of the viewer.</summary>
        public double X { get; private set; }
        /// <summary>Gets the world Y coordinate shown at the top edge of the viewer.</summary>
        public double Y { get; private set; }
        /// <summary>Gets the zoom, where 1 shows one world unit per viewer unit.</summary>
        public double Zoom { get; private set; } = 1;
        /// <summary>Gets how far the camera is zoomed out, the inverse of <see cref="Zoom"/>: 1 is no zoom, 2 shows twice as much of the world.</summary>
        public double ZoomOut => 1 / Zoom;

        /// <summary>Gets or sets the keyboard navigation speed, in viewer units per second.</summary>
        public double Speed
        {
            get => _speed;
            set
            {
                if (double.IsNaN(value))
                    return;
                _speed = Math.Clamp(value, MinSpeed, MaxSpeed);
            }
        }

        /// <summary>Gets the width of the viewer the camera renders to.</summary>
        public double ViewWidth { get; private set; }
        /// <summary>Gets the height of the viewer the camera renders to.</summary>
        public double ViewHeight { get; private set; }

        /// <summary>Raised whenever the position or zoom changes.</summary>
        public event Action Changed;

        /// <summary>Sets the size of the viewer the camera renders to, used to zoom about the center of the view.</summary>
        public void SetViewSize(double width, double height)
        {
            ViewWidth = Math.Max(0, width);
            ViewHeight = Math.Max(0, height);
        }

        /// <summary>Sets how far the camera is zoomed out (see <see cref="ZoomOut"/>), keeping the center of the view in place.</summary>
        public void SetZoomOut(double zoomOut)
        {
            if (double.IsNaN(zoomOut) || zoomOut <= 0)
                return;

            SetZoomAt(ViewWidth / 2, ViewHeight / 2, 1 / zoomOut);
        }

        /// <summary>Converts a viewer position to a world position.</summary>
        public (double X, double Y) ScreenToWorld(double screenX, double screenY)
        {
            return (X + screenX / Zoom, Y + screenY / Zoom);
        }

        /// <summary>Converts a world position to a viewer position.</summary>
        public (double X, double Y) WorldToScreen(double worldX, double worldY)
        {
            return ((worldX - X) * Zoom, (worldY - Y) * Zoom);
        }

        /// <summary>Moves the camera by a distance in viewer units, e.g. for keyboard navigation.</summary>
        public void Move(double screenDeltaX, double screenDeltaY)
        {
            if (screenDeltaX == 0 && screenDeltaY == 0)
                return;

            X += screenDeltaX / Zoom;
            Y += screenDeltaY / Zoom;
            OnChanged();
        }

        /// <summary>Drags the world by a pointer movement in viewer units, so the point under the pointer follows it.</summary>
        public void Pan(double screenDeltaX, double screenDeltaY)
        {
            Move(-screenDeltaX, -screenDeltaY);
        }

        /// <summary>Multiplies the zoom by a factor while keeping the world point under a viewer position in place.</summary>
        public void ZoomAt(double screenX, double screenY, double factor)
        {
            SetZoomAt(screenX, screenY, Zoom * factor);
        }

        /// <summary>Sets the zoom while keeping the world point under a viewer position in place.</summary>
        public void SetZoomAt(double screenX, double screenY, double zoom)
        {
            if (double.IsNaN(zoom) || zoom <= 0)
                return;

            var (worldX, worldY) = ScreenToWorld(screenX, screenY);
            zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
            if (zoom == Zoom)
                return;

            Zoom = zoom;
            X = worldX - screenX / Zoom;
            Y = worldY - screenY / Zoom;
            OnChanged();
        }

        /// <summary>Centers a world position in a viewer of the specified size, keeping the zoom.</summary>
        public void CenterOn(double worldX, double worldY, double viewWidth, double viewHeight)
        {
            X = worldX - viewWidth / 2 / Zoom;
            Y = worldY - viewHeight / 2 / Zoom;
            OnChanged();
        }

        /// <summary>
        /// Centers a world area in a viewer of the specified size and zooms out if needed so the whole area is visible,
        /// leaving a margin (as a fraction of the viewer) around it. The zoom is never increased beyond 1.
        /// </summary>
        public void Frame(double worldX, double worldY, double width, double height, double viewWidth, double viewHeight, double margin = 0.1)
        {
            if (viewWidth <= 0 || viewHeight <= 0)
                return;

            var available = 1 - margin * 2;
            var zoom = 1.0;
            if (width > 0)
                zoom = Math.Min(zoom, viewWidth * available / width);
            if (height > 0)
                zoom = Math.Min(zoom, viewHeight * available / height);
            Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
            CenterOn(worldX + width / 2, worldY + height / 2, viewWidth, viewHeight);
        }

        /// <summary>Moves the camera back to the world origin with a zoom of 1.</summary>
        public void Reset()
        {
            Set(0, 0, 1);
        }

        /// <summary>Sets the position (the world point at the top-left of the viewer) and the zoom.</summary>
        public void Set(double x, double y, double zoom)
        {
            if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(zoom) || double.IsInfinity(x) || double.IsInfinity(y) || zoom <= 0)
                return;

            X = x;
            Y = y;
            Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
            OnChanged();
        }

        private void OnChanged()
        {
            Changed?.Invoke();
        }
    }
}
