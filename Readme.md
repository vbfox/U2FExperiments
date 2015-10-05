FIDO U2F Experiments
====================

My experiments implementing the [FIDO](https://fidoalliance.org) second
factor specifications in C#.

Multiple libraries exists for the client-to-server communication parts but
there is no good library to communicate with keys connected locally via USB.

Things to try
-------------

While the code implements USB communications directly via Win32 APIs for now,
I might also experiment with :

* [HID API][HidApi] ([GitHub][HidApiGitHub]) to get some cross-platform
  compatibility.
* [Windows.Devices.HumanInterfaceDevice][Windows10Hid] to get Windows 10
  support. (Also to steal their nice API)

Warning
-------

**This repository is my playground, nothing is stable and it isn't intended
to be. I'm just playing with low level APIs.** 

[HidApi]: http://www.signal11.us/oss/hidapi/
[HidApiGitHub]: https://github.com/signal11/hidapi
[Windows10Hid]: https://msdn.microsoft.com/en-us/library/windows/hardware/windows.devices.humaninterfacedevice.aspx