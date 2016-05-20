﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media.Imaging;
using NLog;

namespace LibDmd.Output.FileOutput
{
	/// <summary>
	/// Output target for writing frames to png images.
	/// </summary>
	/// <remarks>
	/// Images are hashed and there should no duplicate images written.
	/// </remarks>
	public class FileOutput : IFrameDestination
	{
		public string OutputFolder { get; set; }

		public string Name { get; } = "File Writer";
		public bool IsRgb { get; } = true;
		public bool IsAvailable { get; } = true;

		private int _counter;
		private readonly HashSet<string> _hashes = new HashSet<string>();
		private readonly SHA1CryptoServiceProvider _hashProvider = new SHA1CryptoServiceProvider();

		public FileOutput(string outputFolder)
		{
			OutputFolder = outputFolder;
			if (!Directory.Exists(outputFolder)) {
				throw new InvalidFolderException($"Path \"{outputFolder}\" is not a folder.");
			}
		}

		public void Init()
		{
		}

		/// <summary>
		/// Renders an image to the display.
		/// </summary>
		/// <param name="bmp">Any bitmap</param>
		public void Render(BitmapSource bmp)
		{
			// hash bitmap to see if we already saved it
			var bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
			var bytes = new byte[bytesPerPixel * bmp.PixelWidth * bmp.PixelHeight];
			var rect = new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight);
			bmp.CopyPixels(rect, bytes, bmp.PixelWidth * bytesPerPixel, 0);
			var hash = Convert.ToBase64String(_hashProvider.ComputeHash(bytes));

			if (_hashes.Contains(hash)) {
				return;
			}
			_hashes.Add(hash);

			// write to disk
			var filePath = OutputFolder + @"\frame_" + _counter.ToString("D5") + ".png";
			using (var fileStream = new FileStream(filePath, FileMode.Create)) {
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bmp));
				encoder.Save(fileStream);
			}
			_counter++;
		}

		public void Dispose()
		{
			_hashProvider.Dispose();
		}
	}

	public class InvalidFolderException : Exception
	{
		public InvalidFolderException(string message) : base(message)
		{
		}
	}

}