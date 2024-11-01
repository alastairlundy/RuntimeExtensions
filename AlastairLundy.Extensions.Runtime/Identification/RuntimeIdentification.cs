/*
        MIT License
       
       Copyright (c) 2020-2024 Alastair Lundy
       
       Permission is hereby granted, free of charge, to any person obtaining a copy
       of this software and associated documentation files (the "Software"), to deal
       in the Software without restriction, including without limitation the rights
       to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
       copies of the Software, and to permit persons to whom the Software is
       furnished to do so, subject to the following conditions:
       
       The above copyright notice and this permission notice shall be included in all
       copies or substantial portions of the Software.
       
       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
       FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
       SOFTWARE.
   */

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
#nullable enable

// ReSharper disable once RedundantUsingDirective
using AlastairLundy.Extensions.System.Strings.Versioning;
#endif

using System;
using System.Collections.Generic;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using System.IO;
using System.Runtime.InteropServices;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

using System.Threading.Tasks;

using AlastairLundy.Extensions.Runtime.Identification.Exceptions;
using AlastairLundy.Extensions.Runtime.Internal.Localizations;

using AlastairLundy.Extensions.System;

#if NETSTANDARD2_0 || NETSTANDARD2_1
using OperatingSystem = AlastairLundy.Extensions.Runtime.OperatingSystemExtensions;
#endif

// ReSharper disable InconsistentNaming

namespace AlastairLundy.Extensions.Runtime.Identification
{
    /// <summary>
    /// A class to manage RuntimeId detection and programmatic generation.
    /// </summary>
    public class RuntimeIdentification
    {
        
        /// <summary>
        /// Returns the CPU architecture as a string in the format that a RuntimeID uses.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        protected string GetArchitectureString()
        {
#if NET5_0_OR_GREATER
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
        #if NET6_0_OR_GREATER
                Architecture.S390x => "s390x",
                Architecture.Wasm => throw new PlatformNotSupportedException(),
        #endif
                _ => null
            } ?? throw new InvalidOperationException();
#else
            switch (RuntimeInformation.OSArchitecture)
            {
                case Architecture.Arm:
                    return "arm";
                case Architecture.Arm64:
                    return "arm64";
                case Architecture.X64:
                    return "x64";
                case Architecture.X86:
                    return "x86";
                default:
                    throw new PlatformNotSupportedException();
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("linux")]
#endif
        private string GetOsReleasePropertyValue(string propertyName)
        {
            if (OperatingSystem.IsLinux())
            {
                string output = string.Empty;
                
                string[] osReleaseInfo = File.ReadAllLines("/etc/os-release");
                
                foreach (string s in osReleaseInfo)
                {
                    if (s.ToUpper().StartsWith(propertyName))
                    {
                        output = s.Replace(propertyName, string.Empty);
                    }
                }

                return output;
            }
            else
            {
                throw new PlatformNotSupportedException(Resources.Exceptions_PlatformNotSupported_LinuxOnly);
            }
        }
        
        /// <summary>
        /// Returns the OS name as a string in the format that a RuntimeID uses.
        /// </summary>
        /// <param name="identifierType"></param>
        /// <returns></returns>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("ios")]
        [SupportedOSPlatform("tvos")]
        [SupportedOSPlatform("watchos")]
        [SupportedOSPlatform("android")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
#endif
        protected async Task<string> GetOsNameString(RuntimeIdentifierType identifierType)
        {
#if NET5_0_OR_GREATER
            string? osName = null;
#else
            string osName = string.Empty;
#endif
            
            if (identifierType == RuntimeIdentifierType.AnyGeneric)
            {
                osName = "any";
            }
            else
            {
                if (OperatingSystem.IsWindows())
                {
                    osName = "win";
                }
                if (OperatingSystem.IsMacOS())
                {
                    osName = "osx";
                }
                if (OperatingSystem.IsFreeBSD())
                {
                    osName = "freebsd";
                }
#if NET6_0_OR_GREATER
                if (OperatingSystem.IsAndroid())
                {
                    osName = "android";
                }
                if (OperatingSystem.IsIOS())
                {
                    osName = "ios";
                }
                if (OperatingSystem.IsTvOS())
                {
                    osName = "tvos";
                }

                if (OperatingSystem.IsWatchOS())
                {
                    osName = "watchos";
                }
#endif
                if (OperatingSystem.IsLinux())
                {
                    if (identifierType == RuntimeIdentifierType.Generic)
                    {
                        osName = "linux";
                    }
                    else if (identifierType == RuntimeIdentifierType.Specific)
                    {
                        osName = await Task.Run(() =>
                            GetOsReleasePropertyValue("IDENTIFIER_LIKE="));
                    }
                    else if (identifierType == RuntimeIdentifierType.DistroSpecific || identifierType == RuntimeIdentifierType.VersionLessDistroSpecific)
                    {
                        osName = await Task.Run(()=> 
                            GetOsReleasePropertyValue("IDENTIFIER="));
                    }
                    else
                    {
                        osName = "linux";
                    }
                }
            }

            if (osName == null || string.IsNullOrEmpty(osName))
            {
                throw new PlatformNotSupportedException();
            }

            return osName;
        }
        
        /// <summary>
        /// Returns the OS version as a string in the format that a RuntimeID uses.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        internal string GetOsVersionString()
        {
#if NET5_0_OR_GREATER
            string? osVersion = null;
#else
            string osVersion = string.Empty;
#endif
            if (OperatingSystem.IsWindows())
            {
                bool isWindows10 = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) &&
                                   OperatingSystemExtensions.Version.IsOlderThan(new Version(10, 0, 20349, 0));
                
                bool isWindows11 = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
                
                if (isWindows10)
                {
                    osVersion = "10";
                }
                else if (isWindows11)
                {
                    osVersion = "11";
                }
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                else if (!isWindows10 && !isWindows11)
                {
#if NET5_0_OR_GREATER
                    osVersion = OperatingSystemExtensions.Version.Build switch
                    {
                        < 9200 => throw new PlatformNotSupportedException(),
                        9200 => "8",
                        9600 => "81",
                        _ => throw new PlatformNotSupportedException(Resources.Exceptions_PlatformNotSupported_WindowsOnly)
                    };
#else
                    switch (OperatingSystem.Version.Build)
                    {
                        case 9200:
                            osVersion = "8";
                            break;
                        case 9600:
                            osVersion = "81";
                            break;
                        default:
                            throw new PlatformNotSupportedException(Resources
                                .Exceptions_PlatformNotSupported_WindowsOnly);
                    }
#endif
                }
            }
            if (OperatingSystem.IsLinux())
            {
                osVersion = OperatingSystemExtensions.Version.ToString();
            }
#if NETCOREAPP3_1_OR_GREATER            
            if (OperatingSystem.IsFreeBSD())
            {
                osVersion = OperatingSystemExtensions.Version.ToString();
                
                switch (osVersion.CountDotsInString())
                {
                    case 3:
                        osVersion = osVersion.Remove(osVersion.Length - 4, 4);
                        break;
                    case 2:
                        osVersion = osVersion.Remove(osVersion.Length - 2, 2);
                        break;
                    case 1:
                        break;
                    case 0:
                        osVersion = $"{osVersion}.0";
                        break;
                    }
            }
#endif
            if (OperatingSystem.IsMacOS())
            {
                bool isAtLeastHighSierra = OperatingSystem.IsMacOSVersionAtLeast(10, 13);

                Version version = OperatingSystemExtensions.Version;

                if (isAtLeastHighSierra)
                {
                    
                    if (OperatingSystem.IsMacOSVersionAtLeast(11))
                    {
                        osVersion = $"{version.Major}";
                    }
                    else
                    {
                        osVersion = $"{version.Major}.{version.Major}";
                    }
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }
            }

            if (osVersion == null)
            {
                throw new PlatformNotSupportedException();
            }

            return osVersion;
        }

        /// <summary>
        /// Programmatically generates a .NET Runtime Identifier.
        /// Note: Microsoft advises against programmatically creating Runtime IDs but this may be necessary in some instances.
        /// For More Information Visit: https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
        /// </summary>
        /// <param name="identifierType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("ios")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("tvos")]
        [SupportedOSPlatform("watchos")]
        [UnsupportedOSPlatform("browser")]
#endif
        public string GenerateRuntimeIdentifier(RuntimeIdentifierType identifierType)
        {
            if (identifierType == RuntimeIdentifierType.AnyGeneric)
            {
                return GenerateRuntimeIdentifier(identifierType, false, false);
            }

            if (identifierType == RuntimeIdentifierType.Generic ||
                identifierType == RuntimeIdentifierType.Specific && (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()) ||
                identifierType == RuntimeIdentifierType.VersionLessDistroSpecific)
            {
                return GenerateRuntimeIdentifier(identifierType, true, false);
            }

            if (identifierType == RuntimeIdentifierType.Specific && (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
                || identifierType == RuntimeIdentifierType.DistroSpecific)
            {
                return GenerateRuntimeIdentifier(identifierType, true, true);
            }

            throw new ArgumentException();
        }
        
        /// <summary>
        /// Programmatically generates a .NET Runtime Identifier based on the system calling the method.
        /// Note: Microsoft advises against programmatically creating Runtime IDs but this may be necessary in some instances.
        /// For More Information Visit: https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
        /// </summary>
        /// <returns></returns>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("ios")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("tvos")]
        [SupportedOSPlatform("watchos")]
        [UnsupportedOSPlatform("browser")]
#endif
        public string GenerateRuntimeIdentifier(RuntimeIdentifierType identifierType, bool includeOperatingSystemName, bool includeOperatingSystemVersion)
        {
            string osName = GetOsNameString(identifierType).Result;
            string cpuArch = GetArchitectureString();
            
            if (identifierType == RuntimeIdentifierType.AnyGeneric ||
                identifierType == RuntimeIdentifierType.Generic && includeOperatingSystemName == false)
            {
                return $"any-{GetArchitectureString()}";
            }

            if (identifierType == RuntimeIdentifierType.Generic && includeOperatingSystemName)
            {
                return $"{osName}-{cpuArch}";
            }

            if (identifierType == RuntimeIdentifierType.Specific ||
                OperatingSystem.IsLinux() && identifierType == RuntimeIdentifierType.DistroSpecific ||
                OperatingSystem.IsLinux() && identifierType == RuntimeIdentifierType.VersionLessDistroSpecific)
            {
                string osVersion = GetOsVersionString();

                if (OperatingSystem.IsWindows())
                {
                    if (includeOperatingSystemVersion)
                    {
                        return $"{osName}{osVersion}-{cpuArch}";
                    }

                    return $"{osName}-{cpuArch}";
                }

                if (OperatingSystem.IsMacOS())
                {
                    if (includeOperatingSystemVersion)
                    {
                        return $"{osName}.{osVersion}-{cpuArch}";
                    }

                    return $"{osName}-{cpuArch}";
                }

                if (((OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()) && 
                        (identifierType == RuntimeIdentifierType.DistroSpecific) || identifierType == RuntimeIdentifierType.VersionLessDistroSpecific))
                {
                    if (includeOperatingSystemVersion)
                    {
                        return $"{osName}.{osVersion}-{cpuArch}";
                    }

                    return $"{osName}-{cpuArch}";
                }
                if (((OperatingSystem.IsLinux() && identifierType == RuntimeIdentifierType.Specific) && includeOperatingSystemVersion == false) ||
                    includeOperatingSystemVersion == false)
                {
                    return $"{osName}-{cpuArch}";
                }
            }
            else if((!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD()) && (identifierType == RuntimeIdentifierType.DistroSpecific || identifierType == RuntimeIdentifierType.VersionLessDistroSpecific))
            {
                Console.WriteLine(Resources.RuntimeInformation_NonLinuxSpecific_Warning);
                return GenerateRuntimeIdentifier(RuntimeIdentifierType.Specific);
            }

            throw new RuntimeIdentifierGenerationException();
        }

        /// <summary>
        /// Detects the RuntimeID if running on .NET 5 or later and generates the generic RuntimeID if running on .NET Standard 2.0 or later.
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        public string GetRuntimeIdentifier()
        {
#if NET5_0_OR_GREATER
            return RuntimeInformation.RuntimeIdentifier;
#else
            if (OperatingSystem.IsLinux())
            {
                return GenerateRuntimeIdentifier(RuntimeIdentifierType.DistroSpecific);
            }

            return GenerateRuntimeIdentifier(RuntimeIdentifierType.Specific);
#endif
        }

        /// <summary>
        /// Detects possible Runtime Identifiers that could be applicable to the system calling the method.
        /// </summary>
        /// <returns></returns>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("ios")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("tvos")]
        [SupportedOSPlatform("watchos")]
        [UnsupportedOSPlatform("browser")]
#endif
        public Dictionary<RuntimeIdentifierType, string> GetPossibleRuntimeIdentifierCandidates()
        {
            Dictionary<RuntimeIdentifierType, string> possibilities = new Dictionary<RuntimeIdentifierType, string>
            {
                { RuntimeIdentifierType.AnyGeneric, GenerateRuntimeIdentifier(RuntimeIdentifierType.AnyGeneric) },
                { RuntimeIdentifierType.Generic, GenerateRuntimeIdentifier(RuntimeIdentifierType.Generic) },
                { RuntimeIdentifierType.Specific, GenerateRuntimeIdentifier(RuntimeIdentifierType.Specific) }
            };

            if (OperatingSystem.IsLinux())
            {
                possibilities.Add(RuntimeIdentifierType.VersionLessDistroSpecific,GenerateRuntimeIdentifier(RuntimeIdentifierType.DistroSpecific, true, false));
                possibilities.Add(RuntimeIdentifierType.DistroSpecific,GenerateRuntimeIdentifier(RuntimeIdentifierType.DistroSpecific));
            }

            return possibilities;
        }
    }
}