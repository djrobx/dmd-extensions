﻿using System.Threading;
using LibDmd.Common;
using LibDmd.Frame;
using LibDmd.Input;

namespace LibDmd.Output.Pin2Dmd
{
	public class Pin2Dmd : Pin2DmdBase, IGray2Destination, IGray4Destination,
		IColoredGray2Destination, IColoredGray4Destination,
		IRawOutput, IFixedSizeDestination
	{
		private byte[] _frameBufferGray4;

		public override string Name { get; } = "PIN2DMD";

		/// <summary>
		/// How long to wait after sending data, in milliseconds
		/// </summary>
		public int Delay { get; set; } = 25;

		private UsbDevice _pin2DmdDevice;
		private byte[] _frameBufferRgb24;
		private byte[] _frameBufferGray4;
		private readonly byte[] _colorPalette;
		private readonly byte[] _colorPalettev3;
		private int _currentPreloadedPalette;
		private bool _paletteIsPreloaded;
		private bool _isXL;

		private static Pin2Dmd _instance;
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private bool _disablePreload = true;

		private Pin2Dmd()
		{
			// color palette
			_colorPalette = new byte[2052];
			_colorPalette[0] = 0x81;
			_colorPalette[1] = 0xC3;
			_colorPalette[2] = 0xE7;
			_colorPalette[3] = 0xFF;
			_colorPalette[4] = 0x04;
			_paletteIsPreloaded = false;

			// New firmware color palette
			_colorPalettev3 = new byte[64];
			_colorPalettev3[0] = 0x01;
			_colorPalettev3[1] = 0xc3;
			_colorPalettev3[2] = 0xe7;
			_colorPalettev3[3] = 0xfe;
			_colorPalettev3[4] = 0xed;
			_colorPalettev3[5] = 0x10; 
		}
		
		public Dimensions FixedSize { get; } = new Dimensions(128, 32);

		private static Pin2Dmd _instance;

		/// <summary>
		/// Returns the current instance of the PIN2DMD API. In any case,
		/// the instance get (re-)initialized.
		/// </summary>
		/// <returns></returns>
		public static Pin2Dmd GetInstance(int outputDelay)
		{
			if (_instance == null) {
				_instance = new Pin2Dmd { Delay = outputDelay };
			}
			_instance.Init();
			return _instance;
		}

		protected override void SetupFrameBuffers()
		{
			// 4 bits per pixel plus 4 init bytes
			var size = (FixedSize.Surface * 4 / 8) + 4;
			_frameBufferGray4 = new byte[size];
			_frameBufferGray4[0] = 0x81; // frame sync bytes
			_frameBufferGray4[1] = 0xC3;
			_frameBufferGray4[2] = 0xE7;
			_frameBufferGray4[3] = 0x00;
		}

		protected override bool HasValidName(string name)
		{
			return name.Contains("PIN2DMD") && !name.Contains("PIN2DMD XL");
		}

		public void RenderGray2(DmdFrame frame)
		{
			// 2-bit frames are rendered as 4-bit
			RenderGray4(frame.ConvertGrayToGray(0x0, 0x1, 0x4, 0xf));
		}

		public void RenderGray4(DmdFrame frame)
		{
			// convert to bit planes
			var planes = FrameUtil.Split(FixedSize, 4, frame.Data);

			_frameBufferGray4[3] = 0x0C;

			// copy to buffer
			var changed = FrameUtil.Copy(planes, _frameBufferGray4, 4);

			// send frame buffer to device
			if (changed) {
				RenderRaw(_frameBufferGray4);
			}
		}

		public void RenderColoredGray4(ColoredFrame frame)
		{
			SetPalette(frame.Palette, frame.PaletteIndex);

			_frameBufferGray4[3] = 0x0C;

			// copy to buffer
			var changed = FrameUtil.Copy(frame.Planes, _frameBufferGray4, 4);

			// send frame buffer to device
			if (changed) {
				RenderRaw(_frameBufferGray4);
			}
		}

		public void RenderColoredGray2(ColoredFrame frame)
		{
			SetPalette(frame.Palette, frame.PaletteIndex);

			_frameBufferGray4[3] = 0x06;

			var joinedFrame = FrameUtil.Join(FixedSize, frame.Planes);

			// send frame buffer to device
			var coloredGray2Data = FrameUtil.ConvertGrayToGray(joinedFrame, new byte[] {0x0, 0x1, 0x4, 0xf});
			RenderGray4(new DmdFrame(frame.Dimensions, coloredGray2Data));
		}

		public void ClearDisplay()
		{
			var buffer = new byte[2052];
			buffer[0] = 0x81;
			buffer[1] = 0xC3;
			buffer[2] = 0xE7;
			buffer[3] = 0x00;
			RenderRaw(buffer);
			Thread.Sleep(Delay);
		}
	}
}
