namespace LaptopGuard.Sensors;

internal static class UsbDeviceFilter
{
    internal static bool ShouldTrigger(string? deviceId, string? deviceClass, string? service)
    {
        // Bluetooth endpoints (BTHENUM/BTHLEDEVICE), software audio endpoints (SWD),
        // and HID child nodes can disappear without a physical USB unplug.
        if (deviceId?.StartsWith(@"USB\", StringComparison.OrdinalIgnoreCase) != true)
        {
            return false;
        }

        // Bluetooth radios themselves can be USB devices. Ignore those as well.
        if (string.Equals(deviceClass, "Bluetooth", StringComparison.OrdinalIgnoreCase) ||
            service?.StartsWith("BTH", StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        // Preserve the existing exclusion for USB root hubs and hub infrastructure.
        return !deviceId.StartsWith(@"USB\ROOT_HUB", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(service, "USBHUB", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(service, "USBHUB3", StringComparison.OrdinalIgnoreCase);
    }
}
