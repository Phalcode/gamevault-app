using System;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]
[assembly: AssemblyVersion("1.17.2.0")]
[assembly: AssemblyCopyright("© Phalcode™. All Rights Reserved.")]
#if DEBUG
[assembly: XmlnsDefinition("debug-mode", "Namespace")]
#endif
