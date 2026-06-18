using Avalonia.Media.Imaging;

namespace TypeD.Models.Data
{
    public class Content
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
        public Bitmap Thumbnail { get; protected set; }

        public virtual void Initialize() { }
    }
}
