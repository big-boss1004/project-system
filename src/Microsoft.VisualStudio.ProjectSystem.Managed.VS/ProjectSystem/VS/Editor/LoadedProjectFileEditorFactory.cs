﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using System;
using IOLEProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Guid(EditorFactoryGuid)]
    internal class LoadedProjectFileEditorFactory : IVsEditorFactory
    {
        public const string EditorFactoryGuid = "da07c581-c7b4-482a-86fe-39aacfe5ca5c";
        private static readonly Guid XmlEditorFactoryGuid = new Guid("{fa3cd31e-987b-443a-9b81-186104e8dac1}");
        private readonly IServiceProvider _serviceProvider;
        private IVsEditorFactory _xmlEditorFactory;

        public LoadedProjectFileEditorFactory(IServiceProvider serviceProvider)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            _serviceProvider = serviceProvider;
        }

        public Int32 Close()
        {
            if (_xmlEditorFactory != null)
            {
                return _xmlEditorFactory.Close();
            }
            return VSConstants.S_OK;
        }

        public Int32 CreateEditorInstance(UInt32 grfCreateDoc, String pszMkDocument, String pszPhysicalView, IVsHierarchy pvHier, UInt32 itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out String pbstrEditorCaption, out Guid pguidCmdUI, out Int32 pgrfCDW)
        {
            Requires.NotNull(_xmlEditorFactory, nameof(_xmlEditorFactory));
            Int32 result = _xmlEditorFactory.CreateEditorInstance(grfCreateDoc, pszMkDocument, pszPhysicalView, pvHier, itemid, punkDocDataExisting, out ppunkDocView, out ppunkDocData, out pbstrEditorCaption, out pguidCmdUI, out pgrfCDW);
            if (result == VSConstants.S_OK)
            {
                var punkView = Marshal.GetObjectForIUnknown(ppunkDocView);
                var viewWindow = punkView as WindowPane;
                if (viewWindow != null)
                {
                    var unknownData = Marshal.GetObjectForIUnknown(punkDocDataExisting);

                    // Reset the contents of the docdata buffer. This is necessary every time we open a new editor to make sure the data in the buffer is up to date.
                    ((IResettableBuffer)unknownData).Reset();

                    var project = (IVsProject)unknownData;
                    var wrapper = new XmlEditorWrapper(viewWindow, _serviceProvider, project);

                    ppunkDocView = Marshal.GetIUnknownForObject(wrapper);
                }
            }
            return result;
        }

        public Int32 MapLogicalView(ref Guid rguidLogicalView, out String pbstrPhysicalView)
        {
            var shellOpenDocument = _serviceProvider.GetService<IVsUIShellOpenDocument, SVsUIShellOpenDocument>();
            pbstrPhysicalView = null;
            if (shellOpenDocument == null)
            {
                return VSConstants.E_UNEXPECTED;
            }

            String unusedPhysicalView;
            Verify.HResult(shellOpenDocument.GetStandardEditorFactory(0, XmlEditorFactoryGuid, null, rguidLogicalView, out unusedPhysicalView, out _xmlEditorFactory));
            return _xmlEditorFactory.MapLogicalView(rguidLogicalView, out pbstrPhysicalView);
        }

        public Int32 SetSite(IOLEProvider site)
        {
            if (_xmlEditorFactory != null)
            {
                return _xmlEditorFactory.SetSite(site);
            }
            return VSConstants.S_OK;
        }
    }
}
