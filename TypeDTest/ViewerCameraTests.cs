using TypeD.View.Viewer;

namespace TypeDTest;

public class ViewerCameraTests
{
    [Fact]
    public void ScreenToWorld_AndWorldToScreen_AreInverse()
    {
        var camera = new ViewerCamera();
        camera.Set(100, -50, 2);

        var (worldX, worldY) = camera.ScreenToWorld(40, 60);
        Assert.Equal((120.0, -20.0), (worldX, worldY));
        Assert.Equal((40.0, 60.0), camera.WorldToScreen(worldX, worldY));
    }

    [Fact]
    public void Pan_KeepsTheWorldPointUnderThePointer()
    {
        var camera = new ViewerCamera();
        camera.Set(0, 0, 4);
        var grabbed = camera.ScreenToWorld(10, 10);

        camera.Pan(30, -20);

        Assert.Equal(grabbed, camera.ScreenToWorld(40, -10));
    }

    [Fact]
    public void Move_MovesByViewerUnitsAtAnyZoom()
    {
        var camera = new ViewerCamera();
        camera.Set(0, 0, 0.5);

        camera.Move(10, -10);

        Assert.Equal((20.0, -20.0), (camera.X, camera.Y));
    }

    [Fact]
    public void ZoomAt_KeepsTheWorldPointUnderThePointer()
    {
        var camera = new ViewerCamera();
        camera.Set(5, 7, 1);
        var anchored = camera.ScreenToWorld(200, 150);

        camera.ZoomAt(200, 150, 3);

        Assert.Equal(3, camera.Zoom);
        var (x, y) = camera.ScreenToWorld(200, 150);
        Assert.Equal(anchored.X, x, 9);
        Assert.Equal(anchored.Y, y, 9);
    }

    [Fact]
    public void ZoomAt_IsClampedAndIgnoresInvalidFactors()
    {
        var camera = new ViewerCamera();

        camera.ZoomAt(0, 0, 1000);
        Assert.Equal(ViewerCamera.MaxZoom, camera.Zoom);
        camera.ZoomAt(0, 0, 0.000001);
        Assert.Equal(ViewerCamera.MinZoom, camera.Zoom);
        camera.ZoomAt(0, 0, 0);
        camera.ZoomAt(0, 0, double.NaN);
        Assert.Equal(ViewerCamera.MinZoom, camera.Zoom);
    }

    [Fact]
    public void Frame_CentersAreaThatFitsWithoutZoomingIn()
    {
        var camera = new ViewerCamera();
        camera.Set(500, 500, 1);

        camera.Frame(10, 20, 100, 50, 800, 600);

        Assert.Equal(1, camera.Zoom);
        Assert.Equal((400.0, 300.0), camera.WorldToScreen(60, 45));
    }

    [Fact]
    public void Frame_ZoomsOutSoALargeAreaFitsWithMargin()
    {
        var camera = new ViewerCamera();

        camera.Frame(0, 0, 1600, 300, 800, 600, margin: 0.1);

        Assert.Equal(0.4, camera.Zoom, 9);
        var (left, top) = camera.WorldToScreen(0, 0);
        var (right, bottom) = camera.WorldToScreen(1600, 300);
        Assert.Equal(80, left, 9);
        Assert.Equal(720, right, 9);
        Assert.Equal(300, (top + bottom) / 2, 9);
    }

    [Fact]
    public void Changes_RaiseChanged_AndResetReturnsToOrigin()
    {
        var camera = new ViewerCamera();
        var changes = 0;
        camera.Changed += () => changes++;

        camera.Pan(5, 5);
        camera.ZoomAt(0, 0, 2);
        camera.Move(0, 0);
        camera.Reset();

        Assert.Equal(3, changes);
        Assert.Equal((0.0, 0.0, 1.0), (camera.X, camera.Y, camera.Zoom));
    }

    [Fact]
    public void SetZoomOut_IsTheInverseOfZoom_AndKeepsTheViewCenter()
    {
        var camera = new ViewerCamera();
        camera.SetViewSize(800, 600);
        var center = camera.ScreenToWorld(400, 300);

        camera.SetZoomOut(2);

        Assert.Equal(0.5, camera.Zoom, 6);
        Assert.Equal(2, camera.ZoomOut, 6);
        var newCenter = camera.ScreenToWorld(400, 300);
        Assert.Equal(center.X, newCenter.X, 6);
        Assert.Equal(center.Y, newCenter.Y, 6);
    }

    [Fact]
    public void SetZoomOut_IsClampedAndIgnoresInvalidValues()
    {
        var camera = new ViewerCamera();

        camera.SetZoomOut(0);
        camera.SetZoomOut(-1);
        camera.SetZoomOut(double.NaN);
        Assert.Equal(1, camera.Zoom);

        camera.SetZoomOut(1000);
        Assert.Equal(ViewerCamera.MinZoom, camera.Zoom, 6);
    }

    [Fact]
    public void Set_IgnoresInvalidValues()
    {
        var camera = new ViewerCamera();
        camera.Set(10, 20, 2);
        var changes = 0;
        camera.Changed += () => changes++;

        camera.Set(double.NaN, 0, 1);
        camera.Set(0, double.PositiveInfinity, 1);
        camera.Set(0, 0, 0);

        Assert.Equal(0, changes);
        Assert.Equal((10.0, 20.0, 2.0), (camera.X, camera.Y, camera.Zoom));
    }

    [Fact]
    public void Speed_DefaultsAndIsClamped()
    {
        var camera = new ViewerCamera();
        Assert.Equal(ViewerCamera.DefaultSpeed, camera.Speed);

        camera.Speed = 0;
        Assert.Equal(ViewerCamera.MinSpeed, camera.Speed);
        camera.Speed = double.MaxValue;
        Assert.Equal(ViewerCamera.MaxSpeed, camera.Speed);
        camera.Speed = double.NaN;
        Assert.Equal(ViewerCamera.MaxSpeed, camera.Speed);
    }
}
