﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows.Controls;

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implementation of ILaunchSettingsUIProvider for the Executable launch type.
    /// </summary>
    [Export(typeof(ILaunchSettingsUIProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [Order(0)]              // Lowest priority to allow this to be overridden
    internal class ProjectLaunchSettingsUIProvider : ILaunchSettingsUIProvider
    {
        /// <summary>
        /// Required to control the MEF scope
        /// </summary>
        [ImportingConstructor]
        public ProjectLaunchSettingsUIProvider(UnconfiguredProject uncProject)
        {

        }

        /// <summary>
        /// The name of the command that is written to the launchSettings.json file
        /// </summary>
        public string CommandName
        {
            get
            {
                return LaunchSettingsProvider.RunProjectCommandName;
            }
        }

        /// <summary>
        /// The name to display in the dropdown for this command
        /// </summary>
        public string FriendlyName
        {
            get
            {
                return PropertyPageResources.ProfileKindProjectName;
            }
        }

        /// <summary>
        /// Disable the executable and launch url controls
        /// </summary>
        public bool ShouldEnableProperty(string propertyName)
        {
            return string.Equals(propertyName, UIProfilePropertyName.Executable, System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(propertyName, UIProfilePropertyName.LaunchUrl, System.StringComparison.OrdinalIgnoreCase) ? false : true;
        }

        /// <summary>
        /// No custom UI
        /// </summary>
        public UserControl CustomUI { get { return null; } }

        /// <summary>
        /// Called when the selected profile changes to a profile which matches this command. curSettings will contain 
        /// the current values from the page, and activeProfile will point to the active one.
        /// </summary>
        public void ProfileSelected(IWritableLaunchSettings curSettings)
        {
            return;
        }
    }
}
