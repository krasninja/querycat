﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace QueryCat.Backend.ThriftPlugins.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Errors {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Errors() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("QueryCat.Backend.ThriftPlugins.Resources.Errors", typeof(Errors).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot create object. Type &apos;{0}&apos;..
        /// </summary>
        internal static string CannotCreateObject {
            get {
                return ResourceManager.GetString("CannotCreateObject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot load NuGet package plugin &apos;{0}&apos; without proxy. To install, run &quot;plugin install-proxy&quot; command..
        /// </summary>
        internal static string CannotFindPluginsProxy {
            get {
                return ResourceManager.GetString("CannotFindPluginsProxy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot get address of the plugin main function..
        /// </summary>
        internal static string CannotGetLibraryAddress {
            get {
                return ResourceManager.GetString("CannotGetLibraryAddress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot get plugin context for file &apos;{0}&apos;..
        /// </summary>
        internal static string CannotGetPluginContext {
            get {
                return ResourceManager.GetString("CannotGetPluginContext", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Handler error..
        /// </summary>
        internal static string HandlerInternalError {
            get {
                return ResourceManager.GetString("HandlerInternalError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid token..
        /// </summary>
        internal static string InvalidToken {
            get {
                return ResourceManager.GetString("InvalidToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No connection..
        /// </summary>
        internal static string NoConnection {
            get {
                return ResourceManager.GetString("NoConnection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No object..
        /// </summary>
        internal static string NoObject {
            get {
                return ResourceManager.GetString("NoObject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Token &apos;{0}&apos; is not registered, did you call &apos;RegisterAuthToken&apos;?.
        /// </summary>
        internal static string TokenNotRegistered {
            get {
                return ResourceManager.GetString("TokenNotRegistered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Token &apos;{0}&apos; registration timeout..
        /// </summary>
        internal static string TokenRegistrationTimeout {
            get {
                return ResourceManager.GetString("TokenRegistrationTimeout", resourceCulture);
            }
        }
    }
}
