﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.VisualStudio {
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
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.VisualStudio.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Class.
        /// </summary>
        internal static string ClassTemplateName {
            get {
                return ResourceManager.GetString("ClassTemplateName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The project &apos;{0}&apos; has been modified outside the environment, and there are unsaved changes to the project.
        ///
        ///Press Save As to save the unsaved changes and load the updated project from disk.
        ///Press Discard to discard the unsaved changes and load the updated project from disk.
        ///Press Overwrite to overwrite the external changes with your changes.
        ///Press Ignore to ignore the external changes. Your changes may be lost if you close and reopen the project.
        ///    .
        /// </summary>
        internal static string ConflictingModificationsPrompt {
            get {
                return ResourceManager.GetString("ConflictingModificationsPrompt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Conflicting Project Modification Detected.
        /// </summary>
        internal static string ConflictingProjectModificationTitle {
            get {
                return ResourceManager.GetString("ConflictingProjectModificationTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to _Discard.
        /// </summary>
        internal static string Discard {
            get {
                return ResourceManager.GetString("Discard", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected error occurred attempting to watch project file &apos;{0}&apos;.
        /// </summary>
        internal static string FailedToWatchProject {
            get {
                return ResourceManager.GetString("FailedToWatchProject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to _Ignore.
        /// </summary>
        internal static string Ignore {
            get {
                return ResourceManager.GetString("Ignore", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Ignore A_ll.
        /// </summary>
        internal static string IgnoreAll {
            get {
                return ResourceManager.GetString("IgnoreAll", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to _Overwrite.
        /// </summary>
        internal static string Overwrite {
            get {
                return ResourceManager.GetString("Overwrite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Project Modification Detected.
        /// </summary>
        internal static string ProjectModificationDlgTitle {
            get {
                return ResourceManager.GetString("ProjectModificationDlgTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The project &apos;{0}&apos; has been modified outside the environment.
        ///
        ///Press Reload to load the updated project from disk.
        ///Press Ignore to ignore the external changes. The changes will be used the next time you open the project.
        ///    .
        /// </summary>
        internal static string ProjectModificationsPrompt {
            get {
                return ResourceManager.GetString("ProjectModificationsPrompt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to _Reload.
        /// </summary>
        internal static string Reload {
            get {
                return ResourceManager.GetString("Reload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reload A_ll.
        /// </summary>
        internal static string ReloadAll {
            get {
                return ResourceManager.GetString("ReloadAll", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Renaming the code element {0} failed..
        /// </summary>
        internal static string RenameSymbolFailed {
            get {
                return ResourceManager.GetString("RenameSymbolFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are renaming a file. Would you also like to perform a rename in this project of all references to the code element {0}?.
        /// </summary>
        internal static string RenameSymbolPrompt {
            get {
                return ResourceManager.GetString("RenameSymbolPrompt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to _Save As.
        /// </summary>
        internal static string SaveAs {
            get {
                return ResourceManager.GetString("SaveAs", resourceCulture);
            }
        }
    }
}
