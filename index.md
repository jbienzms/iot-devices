---
title: Devices for Windows IoT Core
layout: page
---

## Top Resources ##

- [List of Currently Supported Devices](devices.md)
- [Getting Started Tutorials](https://www.hackster.io/projects/tags/windows-iot-devices)
- [How-To: Add Support for a New Device](customdevice.md)
- [MSDN-Style API Documentation]({{ site.baseurl }}/doc)


## About ##
Windows IoT Core is easy to use, but we wanted to make it even easier. This project builds on and extends the following key differentiators of Windows IoT Core:

- Runs Windows 10 apps written in C#, VB, C++, JavaScript and more
- Provides a Rich UI and Input Stack
- Offers a familiar Model  Event-Driven for App Developers

Currently this project offers the following benefits above and beyond what ships with Windows IoT Core:

- Event-Driven pattern for many Digital and Analog devices
- Specialized wrappers for devices like joystick, rotary encoder and graphics display
- Extensibility allows developers to easily add support for devices not currently supported by the framework
- WinCore implementation of multiple ADC chips
- Easily use multiple ADC chips in a single application
- Automatic XAML projection to onboard LCD displays
- PWM hardware and software implementations
- Well-known interfaces for common device types helps application developers avoid getting tied down to a particular part number or chipset
- Thread scheduling system that is compatible with the UWP Task model and runs efficiently on resource constrained hardware
-  Well-defined pattern for scheduling and unscheduling updates based on event subscriptions (the same pattern used by types in the [Windows.Devices.Sensors](https://msdn.microsoft.com/en-us/library/windows/apps/windows.devices.sensors.aspx) namespace) 

Other features planned:

- ISwitchable (e.g. Relay) support
- DAC support
- OneWire support
- More specialized wrappers