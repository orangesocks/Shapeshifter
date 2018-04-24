﻿using WindowsClipboard = System.Windows.Clipboard;

namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows.Media.Imaging;

	using Data.Interfaces;

	using Infrastructure.Handles.Factories.Interfaces;
	using Infrastructure.Handles.Interfaces;

	using Interfaces;

	using Messages.Interceptors.Interfaces;
	using Native.Interfaces;
	using Serilog;
	using Shapeshifter.WindowsDesktop.Data.Wrappers;
	using Shapeshifter.WindowsDesktop.Data.Wrappers.Interfaces;

	class ClipboardInjectionService: IClipboardInjectionService
    {
        readonly IClipboardCopyInterceptor clipboardCopyInterceptor;
        readonly IClipboardHandleFactory clipboardHandleFactory;
        readonly IMemoryHandleFactory memoryHandleFactory;
        readonly ILogger logger;
        readonly IGeneralNativeApi generalNativeApi;
		readonly IEnumerable<IMemoryWrapper> memoryWrappers;

		public ClipboardInjectionService(
            IClipboardCopyInterceptor clipboardCopyInterceptor,
            IClipboardHandleFactory clipboardHandleFactory,
            IMemoryHandleFactory memoryHandleFactory,
            ILogger logger,
            IGeneralNativeApi generalNativeApi,
			IEnumerable<IMemoryWrapper> memoryWrappers)
        {
            this.clipboardCopyInterceptor = clipboardCopyInterceptor;
            this.clipboardHandleFactory = clipboardHandleFactory;
            this.memoryHandleFactory = memoryHandleFactory;
            this.logger = logger;
            this.generalNativeApi = generalNativeApi;
			this.memoryWrappers = memoryWrappers;
        }

        public async Task InjectDataAsync(IClipboardDataPackage package)
        {
            clipboardCopyInterceptor.SkipNext();

            using (var session = clipboardHandleFactory.StartNewSession())
            {
                session.EmptyClipboard();
                InjectPackageContents(session, package);
            }

            logger.Information("Clipboard package has been injected to the clipboard.", 1);
        }

        void InjectPackageContents(
            IClipboardHandle session,
            IClipboardDataPackage package)
        {
            foreach (var clipboardData in package.Contents)
            {
                InjectClipboardData(session, clipboardData);
            }
        }

        void InjectClipboardData(
            IClipboardHandle session,
            IClipboardData clipboardData)
        {
			var wrappers = memoryWrappers.Where(x => x.CanWrap(clipboardData));
			foreach(var wrapper in wrappers) {
				var success = session.SetClipboardData(
					clipboardData.RawFormat, 
					wrapper.GetDataPointer(
						clipboardData));
				if(success == IntPtr.Zero)
					throw new Exception("Could not set clipboard data.");
			}
        }

        public async Task InjectImageAsync(BitmapSource image)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetImage(image);
        }

        public async Task InjectTextAsync(string text)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetText(text);
        }

        public async Task InjectFilesAsync(params string[] files)
        {
            clipboardCopyInterceptor.SkipNext();

            var collection = new StringCollection();
            collection.AddRange(files);

            WindowsClipboard.SetFileDropList(collection);
        }

        public void ClearClipboard()
        {
            WindowsClipboard.Clear();
        }
    }
}