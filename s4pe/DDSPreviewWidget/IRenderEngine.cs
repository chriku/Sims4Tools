using System;
using System.IO;

namespace S3PIDemoFE.DDSWidget
{
    public interface IRenderEngine
    {
        void OnDeviceCreated(object sender, EventArgs e);
        void OnDeviceDestroyed(object sender, EventArgs e);
        void OnDeviceLost(object sender, EventArgs e);
        void OnDeviceReset(object sender, EventArgs e);
        void OnMainLoop(object sender, EventArgs e);
        Stream DDS { set; }
    }
}
