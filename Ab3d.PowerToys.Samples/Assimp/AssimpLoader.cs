﻿using System;
using System.CodeDom;
using System.Windows;
using Ab3d.Assimp;
using Assimp;

namespace Ab3d.Assimp
{
    public static class AssimpLoader
    {
        /// <summary>
        /// Loads native assimp library from the AppDomain.CurrentDomain.BaseDirectory.
        /// </summary>
        public static void LoadAssimpNativeLibrary()
        {
            string assimpLibraryFolder = AppDomain.CurrentDomain.BaseDirectory;
            LoadAssimpNativeLibrary(assimpLibraryFolder, assimpLibraryFolder);
        }

        /// <summary>
        /// Loads native assimp library from the specified folder.
        /// </summary>
        public static void LoadAssimpNativeLibrary(string assimpLibraryFolder)
        {
            LoadAssimpNativeLibrary(assimpLibraryFolder, assimpLibraryFolder);
        }

        /// <summary>
        /// Loads native assimp library from the specified folders.
        /// </summary>
        public static void LoadAssimpNativeLibrary(string assimp32BitLibraryFolder, string assimp64BitLibraryFolder)
        {
            // IMPORTANT:

            // To use assimp importer in your project, you need to prepare the native and the managed part of the library.
            //
            // 1) Prepare native assimp libraries
            //
            //    The core part of the assimp importer is its native library that is compiled into two dlls: Assimp32.dll and Assimp64.dll.
            //    One is for x86 application, the other for x64 applications.
            //    
            //    The easiest way to to make those two libraries available to your application is to make sure that they are added to the compiler output folder. 
            //    This can be done with adding the dlls to the root of your project and in the file properties set the "Copy to Output Directory" to "Copy is newer".
            //    
            //    You can also provide the dlls in some other folder. In this case you need to call AssimpWpfImporter.LoadAssimpNativeLibrary method and
            //    in the parameters provide path to x86 and x64 version of the library.
            //    
            //    If your project is not compiled for AnyCPU, then you can distribute only the version for your target platform.
            //
            //
            // 2) Ensure that Visual C++Redistributable for Visual Studio 2019 in available on the system
            // 
            //    The native Assimp library requires that the Visual C++ Redistributable for Visual Studio 2019 is available on the system.
            //    
            //    Visual C++ Redistributable for Visual Studio 2019 is installed on all Windows 10 systems and should be installed on
            //    all Windows Vista and newer systems that have automatic windows update enabled.
            //    More information about that can be read in the following article: https://docs.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=vs-2019/
            //    
            //    If your application is deployed to a system prior to Windows 10 and without installed windows updates, then you have to provide the required dlls by yourself.
            //
            //    You have two options:
            //    a) The recommended way is to Install Visual Studio VCRedist (redistributable package files) to the target system.
            //       Installer can be downloaded from the https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads
            //       (click on vc_redist.x86.exe or vc_redist.x64.exe),
            //    
            //    b) It is also possible to copy all required dlls with your application(see "Local deployment" part from the first link above).
            //
            //
            // 3) Add reference to managed libraries
            //
            //    After you have the native part set up, then you only need to add reference to:
            //    - Assimp.Net library (AssimpNet.dll file) that provides a managed wrapper for the native addimp library,
            //    - Ab3d.PowerToys.Assimp library (Ab3d.PowerToys.Assimp.dll file) that provides logic to convert assimp objects into WPF 3D objects.


            // In this sample both Assimp32.dll and Assimp64.dll are copied to output directory
            // and can be automatically found when needed.
            //
            // In such case it is not needed to call AssimpWpfImporter.LoadAssimpNativeLibrary as it is done below.
            // Here this is done for cases when there is a problem with loading assimp library because
            // Visual C++ Redistributable for Visual Studio 2015 is not installed on the system - in this case a message box is shown to the user.


            // To provide Assimp32 in its own folder, use the following path:
            //string assimp32Folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assimp32");
            //string assimp64Folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assimp64");

            // Here both Assimp32.dll and Assimp64.dll are available in output directory

            try
            {
                AssimpWpfImporter.LoadAssimpNativeLibrary(assimp32BitLibraryFolder, assimp64BitLibraryFolder); // This method can be called multiple times without problems
            }
            catch (AssimpException ex)
            {
                MessageBox.Show(
@"Error loading native assimp library!

The most common cause of this error is that the 
Visual C++ Redistributable for Visual Studio 2019
is not installed on the system. 

Please install it manually or contact support of the application.

Error message:
" + ex.Message);

                throw;
            }
        }
    }
}