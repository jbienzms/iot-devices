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

### Issuing Commands / Sending Data to the Device ###
The Universal Windows Platform (UWP) was designed from the ground up to support asynchronous code, and asynchronous code helps developers create responsive user interfaces. Though it's possible to build "headless" applications for Windows IoT Core, many IoT applications will have user interfaces and all UWP libraries should leverage asynchronous code to ensure they're compatibility in a UI environment. Even if a UI is never used, asynchronous code still helps take advantage of multi-core processors when available.

Though the decision of when to use synchronous code vs asynchronous code is ultimately up to the developer, the MSDN article [Keeping apps fast and fluid](http://blogs.msdn.com/b/windowsappdev/archive/2012/03/20/keeping-apps-fast-and-fluid-with-asynchrony-in-the-windows-runtime.aspx) recommends any operation which *could* take longer than 50 milliseconds should be asynchronous.

Full guidance can be found in the MSDN article [asynchronous programming](https://msdn.microsoft.com/en-us/library/windows/apps/mt187335.aspx) but here are some common ways you'll define a method that executes a command or sends data to your device:

    // Tell the device to do something that will take less than 50ms to complete
    public void DoSomething() { ... }
    
    // Tell the device to do something that takes < 50 ms and requires parameters
    public void DoSomething(parameter1, parameter2) { ... }
    
    // Tell the device to do something that may take longer than 50 ms (Class Library)
    public Task DoSomethingAsync(...) { ... }
    
    // Tell the device to do something that may take longer than 50 ms (WinRT Component)
    public IAsyncOperation DoSomethingAsync(...) { ... }
    
    // Do something that may take longer than 50 ms and the device returns a value (Class Library)
    public Task<TResultDoSomethingAsync(...) { ... }
    
    // Do something that may take longer than 50 ms and the device returns a value (WinRT Component)
    public IAsyncOperation<TResultDoSomethingAsync(...) { ... }
    

In short, if the operation always takes less than 50 ms to complete you can just return the result (or void). If there's any chance the request may take longer than 50 ms you should return a Task (if building a Class Library) or IAsyncOperation (if building a WinRT Component).

**Note:** Task has an extension method called AsAsyncOperation which allows WinRT Component developers to use Task under the covers but return IAsyncOperation as required.

**Important:** Whenever a method is made asynchronous its name should end with "Async". Not only is this a standard naming convention but it also helps consumers of your device know that they need to wait for the operation to complete using the **await** keyword.

**Important:** Do not return bool (True or False) to indicate if an operation completed successfully. In the Universal Windows Platform success is inferred by the method completing or returning data. If a method is not successful it should throw an exception which the caller can catch and determine how to handle the failure.

### Receiving Data / Notifications from the Device ###
As discussed above, it's easy for a device to return data in response to a request. But what if the device is always generating data? Or what if the device needs to proactively notify application code without being queried? These scenarios are common for many types of devices, especially sensors like Accelerometers, Gyroscopes and GPS.

In the Universal Windows Platform, devices can raise **events** whenever they need to notify the application or deliver data. Sometimes events happen at discrete moments in time, like the [Click]({{ site.baseurl }}/doc/html/E_Microsoft_IoT_Devices_Input_PushButton_Click.htm) event of the PushButton. Other times events happen in a continuous stream, like the ReadingChanged event of Accelerometer. When a continuous stream is desired, the device should also expose a [ReportInterval]({{ site.baseurl }}/doc/html/P_Microsoft_IoT_Devices_Sensors_AnalogSensor_ReportInterval.htm) property that lets the consuming application decide how often it wants to receive updates. Using shorter values for **ReportInterval** results in more frequent updates but at the cost of battery life and possibly at the cost of UI responsiveness on CPU constrained devices.