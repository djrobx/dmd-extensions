using System;
using System.Threading;
using LibDmd.Frame;

namespace LibDmd.Output.Pin2Dmd
{
	public class Pin2Dmd : Pin2DmdBase, IGray2Destination, IGray4Destination,
		IColoredGray2Destination, IColoredGray4Destination,
		IRawOutput, IFixedSizeDestination
	{
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

		protected override bool HasValidName(string name)
		{
			return name.Contains("PIN2DMD") && !name.Contains("PIN2DMD XL");
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
