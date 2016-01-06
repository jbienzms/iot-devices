---
title: Supported Devices
layout: post
---
 
# Supported Devices #
The **Microsoft.IoT.Devices** library supports hundreds of devices. Some devices are supported directly using specialized classes built specifically for a device or chipset. Others are supported through generic classes that can be used to interface with a wide range of devices. 

## Specialized ##
This section lists the supported specialized devices and the classes in the framework which support them.


### ADC ###
| Part # | Class | Manufacturer | Description | Notes |
|:-------|:-------------|:------------|:------|:------|
| [ADC0832](http://www.ti.com/product/adc0832-n) | [ADC0832]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Adc_ADC0832.htm) | TI | 8-bit A/D Converter | Single and Differential |
| [MCP3008](http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP3008) | [MCP3008]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Adc_MCP3008.htm) | Microchip | 10-bit A/D Converter | Single and Differential |
| [MCP3208](http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP3208) | [MCP3208]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Adc_MCP3208.htm) | Microchip | 12-bit A/D Converter | Single and Differential |


### Display ###
**IMPORTANT**: The displays below can be coupled with [GraphicsDisplayPanel]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_DeviceCore_Controls_GraphicsDisplayPanel.htm) for seamless integration with XAML UI styles and controls.

| Part # | Class | Manufacturer | Description | Notes |
|:-------|:------|:-------------|:------------|:------|
| [SSD1306](http://www.adafruit.com/datasheets/SSD1306.pdf) | [SSD1306]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Display_SSD1306.htm) | Adafruit | SPI Display | **Not fully implemented** - work in progress |
| [ST7735](http://www.sitronix.com.tw/sitronix/product.nsf/Doc/ST7735?OpenDocument) | [ST7735]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Display_ST7735.htm) | Sitronix | Multi-Format Display Controller | Works with [Adafruit 1.8" color display](http://www.adafruit.com/products/358) |


### Input ###
| Part # | Class | Manufacturer | Description | Notes |
|:-------|:------|:-------------|:------------|:------|
| [SS944](http://www.sainsmart.com/sainsmart-joystick-module-free-10-cables-for-arduino.html) | [SS944]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Input_SS944.htm) | SainSmart | Dual axis Thumbstick with optional center Push Button. | Minimum one axis required |


### PWM ###
| Part # | Class | Manufacturer | Description | Notes |
|:-------|:------|:-------------|:------------|:------|
| [PCA9685](http://www.adafruit.com/products/815) | [PCA9685]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Pwm_PCA9685.htm) | Adafruit | 16-Channel 12-bit PWM / Servo Driver | This is an I2C device |




## Generic ##
This section covers support for a wide range of devices that do not require specialized classes.


### Input ###
| Class | Works With |
|:------|:-----------|
| [PushButton]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Input_PushButton.htm) | Momentary buttons that use a single GPIO pin. For example, the [Sunfounder Button Module](http://www.sunfounder.com/index.php?c=showcs&id=133&model=Button Module). This class exposes properties and events similar to a XAML Button control.|
| [RotaryEncoder]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Input_RotaryEncoder.htm) | Rotary knobs that use one GPIO for Clock and another for Direction; optionally including a Push Button. For example the [Sunfounder Rotary Encoder](http://www.sunfounder.com/index.php?c=showcs&id=140&model=Rotary Encoder Module). |
| [Switch]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Input_Switch.htm) | Any switch or sensor that use a single GPIO pin to indicate "on" or "off". For example, the Sunfounder [Switch Module](http://www.sunfounder.com/index.php?c=showcs&id=154&model=Switch Module), [Tilt Switch Module](http://www.sunfounder.com/index.php?c=showcs&id=126&model=Tilt Switch Module) or even [Obstacle Avoidance Module](http://www.sunfounder.com/index.php?c=showcs&id=143&model=Obstacle Avoidance Sensor Module). |

**Note**: The [SS944]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Input_SS944.htm) class may be used for any dual axis thumbstick or joystick. We may rename this class to 'Thumbstick' in the future.


### Lights ###
| Class | Works With |
|:------|:-----------|
| [RgbLed]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Lights_RgbLed.htm) | Any multi-color light that be controlled by ADC. For example, the Sunfounder [RGB LED Module](http://www.sunfounder.com/index.php?c=showcs&id=136&model=RGB LED Module) or even the [Dual-Color LED Module](http://www.sunfounder.com/index.php?c=showcs&id=138&model=Dual-color LED Module). A minimum of one color channel must be used. |


### PWM ###
| Class | Works With |
|:------|:-----------|
| [SoftPwm]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Pwm_SoftPwm.htm) | Uses CPU timing to generate PWM signals on regular GPIO pins. The number of pins reported as available are the same as reported by GpioController.PinCount. The max frequency is currently limited to 1Khz. |


### Sensors ###
| Class | Works With |
|:------|:-----------|
| [AnalogSensor]({{ site.baseurl }}/doc/html/T_Microsoft_IoT_Devices_Sensors_AnalogSensor.htm) | Any device that provides a value via an ADC pin. For example the Sunfounder [MQ-2 Gas Sensor Module](http://www.sunfounder.com/index.php?c=showcs&id=118&model=MQ-2 Gas Sensor Module) and [Photoresistor Sensor Module](http://www.sunfounder.com/index.php?c=showcs&id=123&model=Photoresistor Sensor Module). |



## Something Missing? ##
Are we missing something you need? Learn how to [build your own device library]({{ site.baseurl }}/customlib.md) on top of our Device Core. Or, head on over to our [source code](http://aka.ms/iotdevices) to see how we built the classes above. If you do add support for a missing sensor or device, please consider submitting a pull request. That way we can add your hard work to the NuGet library so that everyone can benefit. And we'll give you all the credit, of course! 