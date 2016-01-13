---
title: Supporting a Custom Device
layout: post
---

The IoT Devices framework was designed from the ground up to be extensible. This page describes how to add support for a custom device.

## Step 1: Make sure you actually need something custom ##
**Microsoft.IoT.Devices** already supports hundreds of devices "out of the box". Some devices are supported using specialized classes dedicated to a device or chipset, but many are supported using generic classes that can be used with many devices. Before creating a custom device, check the [Supported Devices]({{ site.baseurl }}/devices.md) list to see if the device you want to use is already supported. Especially pay close attention to the **Generic** section to understand the types of devices supported by those classes.

## Step 2: Understand the Framework ##
If you do need something custom, the next step is to understand the underlying components that make up the devices framework. **Microsoft.IoT.Devices** is actually a hierarchy of 3 libraries:

![]({{ site.baseurl }}/images/structure.png)

### Microsoft.IoT.DeviceCore ###
**DeviceCore** defines the classes and well-known interfaces that can be implemented by other device libraries. This is a [Windows Runtime Component](https://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh441572.aspx) which means it can be used in any programming language supported by the Windows Runtime (C++, C#, VB and JavaScript). Examples of the types included in this library are [ISwitch]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_DeviceCore_Input_ISwitch.htm), [IPushButton]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_DeviceCore_Input_IPushButton.htm) and [IRotaryEncoder]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_DeviceCore_Input_IRotaryEncoder.htm).

### Microsoft.IoT.DeviceHelpers ###
**DeviceHelpers** provides additional helper classes that couldn't be included in the **DeviceCore** library. Because **DeviceCore** is a Windows Runtime Component, [certain restrictions](https://msdn.microsoft.com/en-us/library/windows/apps/xaml/br230301.aspx) are placed on the types that can be exported from it. Since **DeviceHelpers** is a Class Library and not a Runtime Component those restrictions don't apply. This does mean that **DeviceHelpers** can only be used to build custom libraries using managed languages (e.g. C# and VB) but the classes provided by **DeviceHelpers** are not critical; they are mainly time savers. **DeviceHelpers** includes classes like [ObservableEvent]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_DeviceHelpers_ObservableEvent_2.htm), [PushButtonHelper]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_DeviceHelpers_Input_PushButtonHelper.htm) and a number of custom exception classes.  

### Microsoft.IoT.Devices ###
**Devices** is the library that most IoT developers actually use. It's built on top of **DeviceCore** and **DeviceHelpers** and it includes concrete *implementations* of the interfaces defined in **DeviceCore**. For example it includes classes like [Switch]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Input_Switch.htm), [PushButton]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Input_PushButton.htm) and [RotaryEncoder]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Input_RotaryEncoder.htm) which implement the interfaces in DeviceCore by interfacing with hardware connected to GPIO. The **Devices** library is a [Windows Runtime Component](https://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh441572.aspx) even though it leverages **DeviceHelpers** internally. This is possible because it doesn't *publicly* expose any types from the **DeviceHelpers** library.

## Step 3: (Optional) Create Your Library ##
Adding support for a custom device is really about creating a new class for that device. This class can be created in your main IoT project or in a separate class library. Putting the class in your project will get you up and running the quickest, but putting in a class library will allow you to share that effort across multiple projects.  

If you decide to create a library, you have two options: Class Library (Universal Windows) or Windows Runtime Component (Universal Windows).

![]({{ site.baseurl }}/images/newlibproj.png)

If you know that your custom library will only be used in managed projects (C# and VB) it's easier to just create it as a Class Library. However if there's a chance your library might need to be used in unmanaged projects (JavaScript and C++) it's best to start from the beginning as a Windows Runtime Component. A project can always be changed from a Class Library to a Runtime Component or vice versa later under project properties, but be aware that you will likely have some extra work to do if you don't account for the Runtime Component [restrictions](https://msdn.microsoft.com/en-us/library/windows/apps/xaml/br230301.aspx) from the beginning.

## Step 4: Add References ##
No matter what language your class is written in, you'll need to reference the IoT Extension SDK. This is part of the Windows Runtime itself and to reference it, right-click on the References folder inside our project and choose **Add Reference…**. In the window that pops up expand the **Windows Universal** branch on the left and select **Extensions**. Then, check the box next to **Windows IoT Extensions for the UWP** on the right and click OK to close the dialog.

![]({{ site.baseurl }}/images/iotextension.png)

Also no matter what language you're using, you'll need to add a reference to the **Microsoft.IoT.DeviceCore** library. To do this, right-click on the **References** folder inside the project and choose **Manage NuGet Packages**. In the window that pops up type **Microsoft.IoT.DeviceCore** in the search box.

![]({{ site.baseurl }}/images/devicecorepack.png)

Select **Microsoft.IoT.DeviceCore** on the left-hand side and click the **Install** button on the right.

Finally, if you're using a managed language to build your custom library also add a reference to the **Microsoft.IoT.DeviceHelpers** library.

![]({{ site.baseurl }}/images/devicehelperspack.png)

## Step 5: Create Device Class ##

### Class Name ###
Now it's time to create the class for your device.

If your device has a specific part number or depends on a specific chipset, consider giving your class the same name as the part or chipset. We consider these classes to be "specialized". For example, see [ADC0832.cs]({{ site.repourl }}/blob/master/Lib/Microsoft.IoT.Devices/Adc/ADC0832.cs).

If the class you are creating can be used with multiple part numbers or chipsets, consider giving your class a name that describes the type of device it works with. We consider these classes to be "generic". For example, see [RotaryEncoder.cs]({{ site.repourl }}/blob/master/Lib/Microsoft.IoT.Devices/Input/RotaryEncoder/RotaryEncoder.cs).

### Events and Scheduling ###
The Universal Windows Platform was designed from the ground up to support multi-core, asynchronous programming and responsive user interfaces. Though it's possible to build "headless" applications for Windows IoT Core, many IoT applications will have user interfaces and all UWP applications need to support asynchronous programming. With this in mind, many devices - especially sensors - will deliver their data using events. 