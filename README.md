# Ab3d.PowerToys.Wpf.Samples

![Ab3d.PowerToys image](https://www.ab4d.com/images/Banner/Banner_PowerToys_intro.png)

[Ab3d.PowerToys](https://www.ab4d.com/PowerToys.aspx) is an **ultimate WPF and WinForms 3D toolkit** library that greatly simplifies developing desktop applications with scientific, technical, CAD or other 3D graphics.

Ab3d.PowerToys is using WPF 3D rendering engine (DirectX 9). Check [Ab3d.DXEngine](https://www.ab4d.com/DXEngine.aspx) for super fast DirectX 11 rendering engine that can render the existing WPF 3D scene much faster and with better visual quality.

The samples in this repository demonstrate many features of the library and show that with .Net it is easily possible to create applications with complex 3D graphics.

The Ab3d.PowerToys is a commercial library. You can start a 60-day trial when it is first used.

## Repository solutions

The Ab3d.DXEngine.Wpf.Samples repository contains two Visual Studio solutions:
* Ab3d.PowerToys MAIN WPF Samples.sln (**.NET Framework 4.5**)
* Ab3d.PowerToys MAIN WPF netcoreapp3 Samples.sln (**.NET Core 3.0**)

## Dependencies

The sample project is using Ab3d.PowerToys library from NuGet - https://www.nuget.org/packages/Ab3d.PowerToys.

The Ab3d.PowerToys library includes an obj file importer that can import 3D objects defined in obj files. To import 3D objects from other file formats, the samples project uses a third-party Assimp library. Assimp library is an open source and native importer that can import 3D models from almost any 3D file format (see more: https://github.com/assimp/assimp).

The compiled versions of assimp library for 32-bit and for 64-bit process are included in the lib folder. In the lib folder there is  also Assimp.Net.dll - a manager wrapper for native assimp importer. There is also Ab3d.PowerToys.Assimp.dll - a library that can convert imported 3D objects into WPF 3D objects.

## Support

* Online reference help: https://www.ab4d.com/help/PowerToys/html/R_Project_Ab3d_PowerToys.htm
* Forum: https://forum.ab4d.com/forumdisplay.php?fid=9
* Related blog posts: http://blog.ab4d.com/category/Ab3dPowerToys.aspx
* Feedback: https://www.ab4d.com/Feedback.aspx

## See also

* [AB4D Homepage](https://www.ab4d.com/)
* [Ab3d.DXEngine](https://www.ab4d.com/DXEngine.aspx) library
* [Ab3d.DXEngine Samples on GitHub](https://github.com/ab4d/Ab3d.DXEngine.Wpf.Samples)
* [AB4D products price list](https://www.ab4d.com/Purchase.aspx#PowerToys)
